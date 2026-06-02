using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sera iskeletini ( or. "SERA" FBX modeli) olcup etrafina yari saydam cam kabuk uretir.
/// - Kabuk yalnizca CELIK ISKELET (kare profiller) olcusune gore daralir; fanlar kabugu sismez.
/// - "dikme" (yan dikey direkler) varsa kose ayak izi + sacak yuksekligi BUNLARDAN alinir.
/// - "cati" parcalari varsa cati sirti yuksekligi BUNLARDAN alinir.
/// - Fan / panjur / egzoz / havalandirma parcalarinin onundeki cam panelleri BOS birakilir.
///
/// Kullanim:
/// 1. Sahnede bos bir GameObject olustur, bu scripti ekle.
/// 2. target alanina SERA objesini surukle (bos birakirsan isimle bulur).
/// 3. Play'e bas ya da component'e sag tik -> "Rebuild Cladding".
/// </summary>
[ExecuteAlways]
public class GreenhouseCladding : MonoBehaviour
{
    public enum RoofStyle { Gable, Flat }
    public enum RidgeAxis { Auto, AlongX, AlongZ }

    [Header("Hedef Iskelet")]
    [Tooltip("Kaplanacak sera iskeleti. Bos ise targetName ile sahnede aranir.")]
    public Transform target;
    public string targetName = "SERA";

    [Header("Bounding Box Filtreleri")]
    [Tooltip("Kabuk olcusu yalnizca bu kelimeleri iceren parcalara gore hesaplanir (celik iskelet).")]
    public string[] structureKeywords = { "tube", "square", "dikme", "angle iron" };
    [Tooltip("Bu kelimeleri iceren parcalarin onu BOS birakilir. Sadece exhaust fan parcalari baz alinir.")]
    public string[] openingKeywords = { "exhaust fan" };

    [Header("Referans Parcalar (birebir oturma)")]
    [Tooltip("Referans parcalarini kullan. Yan dikme + cati parcalarini olcup tam hizalar.")]
    public bool useReferenceParts = true;
    [Tooltip("Yan dikey direkler. Tepeleri sacak (eave) yuksekligini, konumlari kose ayak izini verir.")]
    public string[] postKeywords = { "dikme" };
    [Tooltip("Cati / kose parcalari. En yuksek noktasi cati sirti (ridge) yuksekligini verir.")]
    public string[] roofKeywords = { "çatı", "cati" };

    [Header("Cati")]
    public RoofStyle roofStyle = RoofStyle.Gable;
    [Tooltip("Cati sirti (ridge) hangi eksende? Auto yanlis secerse elle ayarla.")]
    public RidgeAxis ridgeAxis = RidgeAxis.Auto;
    [Tooltip("Referans parca yoksa: duvar (sacak) yuksekligi orani.")]
    [Range(0.2f, 1f)] public float wallHeightRatio = 0.6f;
    [Tooltip("Cati SIRTINI (tepe) celik profilin ustune bu kadar metre kaldirir. Tepe oturusu icin sabit birak.")]
    public float ridgeLift = 0.08f;
    [Tooltip("SADECE sacak (yan duvar) tarafini kaldirir/indirir; tepe sabit kalir (tepeden menteselidir). Negatif olabilir.")]
    public float roofLift = 0.08f;
    [Tooltip("Cati sacagi duvarin uzerine bu kadar metre biner; profil et kalinligi bosluklarini kapatir (claddingler opussun).")]
    public float seamOverlap = 0.03f;

    [Header("Kabuk Olcusu")]
    [Tooltip("Cam, iskeletin disina bu kadar metre tasar.")]
    public float padding = 0.02f;
    [Tooltip("Cam panel hucre boyutu (m). Fan deliklerinin hassasiyetini belirler.")]
    public float panelSize = 0.2f;
    [Tooltip("Fan acikligi cevresine birakilacak ek bosluk (m). 0 = kaplama fanin tam bitisinden baslar (collider gibi).")]
    public float openingMargin = 0f;
    [Tooltip("Taban kismi acik kalsin (zemin paneli uretme).")]
    public bool openBottom = true;

    [Header("Duvar Cam Gorunumu")]
    public Color glassColor = new Color(0.62f, 0.85f, 0.90f, 0.16f);
    [Range(0f, 1f)] public float smoothness = 0.92f;

    [Header("Cati Cam Gorunumu (ayri malzeme)")]
    [Tooltip("Cati icin ayri malzeme kullan. Kapali ise duvar camiyla ayni gorunur.")]
    public bool separateRoofMaterial = true;
    public Color roofColor = new Color(0.70f, 0.86f, 0.92f, 0.25f);
    [Range(0f, 1f)] public float roofSmoothness = 0.90f;

    [Header("Genel")]
    public bool doubleSided = true;
    public bool castShadows = false;

    [Header("Otomatik")]
    public bool buildOnStart = true;

    const string CladdingChildName = "GreenhouseGlass";

    private struct ShellDims
    {
        public float minX, maxX, minZ, maxZ, minY, eaveY, ridgeY;
    }

    private struct WallPlane
    {
        public Vector3 anchor;
        public Vector3 uDir;
        public Vector3 vDir;
        public float uMin, uMax, vMin, vMax;
        public List<Rect> openings;
    }

    void Start()
    {
        if (buildOnStart && Application.isPlaying)
            Rebuild();
    }

    [ContextMenu("Rebuild Cladding")]
    public void Rebuild()
    {
        var t = ResolveTarget();
        if (t == null)
        {
            Debug.LogWarning($"[GreenhouseCladding] Hedef bulunamadi. target ata ya da '{targetName}' adli obje ekle.");
            return;
        }

        var renderers = t.GetComponentsInChildren<Renderer>(true);
        if (!TryGetStructureBounds(renderers, out Bounds structure))
        {
            Debug.LogWarning("[GreenhouseCladding] Iskelet bounding box hesaplanamadi.");
            return;
        }

        var dims = ComputeDims(renderers, structure);
        var openings = CollectOpeningBounds(renderers);

        ClearExisting();

        BuildShellMesh(dims, openings, out Mesh wallsMesh, out Mesh roofMesh);

        var wallMat = CreateGlassMaterial(glassColor, smoothness);
        var roofMat = separateRoofMaterial
            ? CreateGlassMaterial(roofColor, roofSmoothness)
            : wallMat;

        CreatePiece(CladdingChildName + "_Walls", wallsMesh, wallMat);
        CreatePiece(CladdingChildName + "_Roof", roofMesh, roofMat);

        Debug.Log($"[GreenhouseCladding] Kabuk olusturuldu. " +
                  $"Sacak: {dims.eaveY:F2}, Sirt: {dims.ridgeY:F2}, aciklik: {openings.Count}");
    }

    void CreatePiece(string name, Mesh mesh, Material mat)
    {
        if (mesh == null) return;

        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
    }

    /// <summary>Cladding (duvar + cati) parcalarinin su an gorunur olup olmadigi.</summary>
    public bool IsVisible
    {
        get
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                if (c != null && c.name.StartsWith(CladdingChildName) && c.gameObject.activeSelf)
                    return true;
            }
            return false;
        }
    }

    /// <summary>Hem cati hem yan duvar cladding'ini ac/kapat.</summary>
    public void SetVisible(bool visible)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var c = transform.GetChild(i);
            if (c != null && c.name.StartsWith(CladdingChildName))
                c.gameObject.SetActive(visible);
        }
    }

    public void ToggleVisible() => SetVisible(!IsVisible);

    [ContextMenu("Clear Cladding")]
    public void ClearExisting()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null && child.name.StartsWith(CladdingChildName))
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    Transform ResolveTarget()
    {
        if (target != null) return target;
        if (string.IsNullOrEmpty(targetName)) return null;
        var found = GameObject.Find(targetName);
        return found != null ? found.transform : null;
    }

    bool MatchesAny(string name, string[] keywords)
    {
        if (keywords == null) return false;
        string lower = name.ToLowerInvariant();
        foreach (var k in keywords)
        {
            if (string.IsNullOrEmpty(k)) continue;
            if (lower.Contains(k.ToLowerInvariant())) return true;
        }
        return false;
    }

    bool TryGetStructureBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool has = false;

        foreach (var r in renderers)
        {
            if (r == null || r.gameObject.name == CladdingChildName) continue;
            if (!MatchesAny(r.gameObject.name, structureKeywords)) continue;
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }

        if (has) return true;

        // Fan/aciklik disindaki her seyi kullan
        foreach (var r in renderers)
        {
            if (r == null || r.gameObject.name == CladdingChildName) continue;
            if (MatchesAny(r.gameObject.name, openingKeywords)) continue;
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return has;
    }

    bool TryGetKeywordBounds(Renderer[] renderers, string[] keywords, out Bounds bounds)
    {
        bounds = default;
        bool has = false;
        foreach (var r in renderers)
        {
            if (r == null || r.gameObject.name == CladdingChildName) continue;
            if (!MatchesAny(r.gameObject.name, keywords)) continue;
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return has;
    }

    ShellDims ComputeDims(Renderer[] renderers, Bounds structure)
    {
        var d = new ShellDims
        {
            minX = structure.min.x,
            maxX = structure.max.x,
            minZ = structure.min.z,
            maxZ = structure.max.z,
            minY = structure.min.y,
            ridgeY = structure.max.y,
        };

        bool gable = roofStyle == RoofStyle.Gable;
        d.eaveY = gable ? Mathf.Lerp(d.minY, d.ridgeY, wallHeightRatio) : d.ridgeY;

        if (useReferenceParts)
        {
            // Yan dikmeler: ayak izi (kose) + sacak yuksekligi
            if (TryGetKeywordBounds(renderers, postKeywords, out Bounds posts))
            {
                d.minX = posts.min.x;
                d.maxX = posts.max.x;
                d.minZ = posts.min.z;
                d.maxZ = posts.max.z;
                d.minY = posts.min.y;
                d.eaveY = posts.max.y;
            }

            // Cati parcalari: sirt yuksekligi
            if (TryGetKeywordBounds(renderers, roofKeywords, out Bounds roof))
            {
                d.ridgeY = roof.max.y;
            }
        }

        // Tutarlilik
        if (!gable) d.eaveY = d.ridgeY;
        if (d.ridgeY < d.eaveY) d.ridgeY = d.eaveY;

        return d;
    }

    List<Bounds> CollectOpeningBounds(Renderer[] renderers)
    {
        var list = new List<Bounds>();
        foreach (var r in renderers)
        {
            if (r == null || r.gameObject.name == CladdingChildName) continue;
            if (MatchesAny(r.gameObject.name, openingKeywords))
                list.Add(r.bounds);
        }
        return list;
    }

    void BuildShellMesh(ShellDims d, List<Bounds> openings, out Mesh wallsMesh, out Mesh roofMesh)
    {
        float minX = d.minX - padding;
        float maxX = d.maxX + padding;
        float minZ = d.minZ - padding;
        float maxZ = d.maxZ + padding;
        float minY = d.minY;

        // Sirt (tepe) ridgeLift ile sabit yukseltilir; sacak roofLift ile ayri kontrol edilir
        // (tepeden menteseli: roofLift sadece yan/sacak tarafini kaldirir/indirir).
        float ridgeTop = d.ridgeY + ridgeLift;
        float eaveLevel = d.eaveY + roofLift;

        bool gable = roofStyle == RoofStyle.Gable && ridgeTop > eaveLevel + 0.001f;
        bool ridgeAlongX = ridgeAxis == RidgeAxis.AlongX ? true
            : ridgeAxis == RidgeAxis.AlongZ ? false
            : (maxX - minX) >= (maxZ - minZ);

        float rectTop = eaveLevel;
        float roofBase = gable ? Mathf.Max(minY, eaveLevel - Mathf.Max(0f, seamOverlap)) : eaveLevel;

        // Duvar geometrisi
        var wVerts = new List<Vector3>();
        var wTris = new List<int>();
        // Cati geometrisi (ayri malzeme)
        var rVerts = new List<Vector3>();
        var rTris = new List<int>();

        var walls = new List<WallPlane>
        {
            MakeWall(new Vector3(0, 0, maxZ), Vector3.right, Vector3.up, minX, maxX, minY, rectTop),
            MakeWall(new Vector3(0, 0, minZ), Vector3.right, Vector3.up, minX, maxX, minY, rectTop),
            MakeWall(new Vector3(maxX, 0, 0), Vector3.forward, Vector3.up, minZ, maxZ, minY, rectTop),
            MakeWall(new Vector3(minX, 0, 0), Vector3.forward, Vector3.up, minZ, maxZ, minY, rectTop),
        };

        AssignOpeningsToWalls(walls, openings, minX, maxX, minZ, maxZ);

        foreach (var w in walls)
            TileWall(wVerts, wTris, w);

        if (!gable)
        {
            AddQuad(rVerts, rTris,
                new Vector3(minX, ridgeTop, maxZ), new Vector3(maxX, ridgeTop, maxZ),
                new Vector3(maxX, ridgeTop, minZ), new Vector3(minX, ridgeTop, minZ));
        }
        else
        {
            float midX = (minX + maxX) * 0.5f;
            float midZ = (minZ + maxZ) * 0.5f;
            float halfCross = 0.5f * (ridgeAlongX ? (maxZ - minZ) : (maxX - minX));

            // Cati yukseklik fonksiyonu: sirtta ridgeTop, sacakta roofBase
            float H(float x, float z)
            {
                if (halfCross <= 1e-4f) return roofBase;
                float cross = ridgeAlongX ? z : x;
                float center = ridgeAlongX ? midZ : midX;
                float t = Mathf.Clamp01(1f - Mathf.Abs(cross - center) / halfCross);
                return Mathf.Lerp(roofBase, ridgeTop, t);
            }

            // Egimli cati yuzeyleri -> CATI mesh'i
            if (ridgeAlongX)
            {
                var r1 = new Vector3(minX, ridgeTop, midZ);
                var r2 = new Vector3(maxX, ridgeTop, midZ);
                AddQuad(rVerts, rTris, new Vector3(minX, roofBase, maxZ), new Vector3(maxX, roofBase, maxZ), r2, r1);
                AddQuad(rVerts, rTris, new Vector3(maxX, roofBase, minZ), new Vector3(minX, roofBase, minZ), r1, r2);
            }
            else
            {
                var r1 = new Vector3(midX, ridgeTop, minZ);
                var r2 = new Vector3(midX, ridgeTop, maxZ);
                AddQuad(rVerts, rTris, new Vector3(maxX, roofBase, minZ), new Vector3(maxX, roofBase, maxZ), r2, r1);
                AddQuad(rVerts, rTris, new Vector3(minX, roofBase, maxZ), new Vector3(minX, roofBase, minZ), r1, r2);
            }

            // Duvar ustlerini cati siluetine kadar doldur (alinlik ucgenleri) -> DUVAR mesh'i
            AddGableFill(wVerts, wTris, new Vector3(0, 0, maxZ), Vector3.right, minX, maxX, roofBase, u => H(u, maxZ));
            AddGableFill(wVerts, wTris, new Vector3(0, 0, minZ), Vector3.right, minX, maxX, roofBase, u => H(u, minZ));
            AddGableFill(wVerts, wTris, new Vector3(maxX, 0, 0), Vector3.forward, minZ, maxZ, roofBase, u => H(maxX, u));
            AddGableFill(wVerts, wTris, new Vector3(minX, 0, 0), Vector3.forward, minZ, maxZ, roofBase, u => H(minX, u));
        }

        if (!openBottom)
        {
            AddQuad(wVerts, wTris,
                new Vector3(minX, minY, minZ), new Vector3(maxX, minY, minZ),
                new Vector3(maxX, minY, maxZ), new Vector3(minX, minY, maxZ));
        }

        wallsMesh = MakeMesh("GreenhouseWallsMesh", wVerts, wTris);
        roofMesh = MakeMesh("GreenhouseRoofMesh", rVerts, rTris);
    }

    static Mesh MakeMesh(string name, List<Vector3> verts, List<int> tris)
    {
        if (verts.Count == 0 || tris.Count == 0) return null;
        var mesh = new Mesh { name = name };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static WallPlane MakeWall(Vector3 anchor, Vector3 uDir, Vector3 vDir,
        float uMin, float uMax, float vMin, float vMax)
    {
        return new WallPlane
        {
            anchor = anchor, uDir = uDir, vDir = vDir,
            uMin = uMin, uMax = uMax, vMin = vMin, vMax = vMax,
            openings = new List<Rect>()
        };
    }

    void AssignOpeningsToWalls(List<WallPlane> walls, List<Bounds> openings,
        float minX, float maxX, float minZ, float maxZ)
    {
        foreach (var ob in openings)
        {
            Vector3 c = ob.center;
            float dZmax = Mathf.Abs(c.z - maxZ);
            float dZmin = Mathf.Abs(c.z - minZ);
            float dXmax = Mathf.Abs(c.x - maxX);
            float dXmin = Mathf.Abs(c.x - minX);

            float min = Mathf.Min(Mathf.Min(dZmax, dZmin), Mathf.Min(dXmax, dXmin));
            float m = openingMargin;

            if (min == dZmax || min == dZmin)
            {
                var rect = new Rect(ob.min.x - m, ob.min.y - m,
                    ob.size.x + 2 * m, ob.size.y + 2 * m);
                int idx = (min == dZmax) ? 0 : 1;
                walls[idx].openings.Add(rect);
            }
            else
            {
                var rect = new Rect(ob.min.z - m, ob.min.y - m,
                    ob.size.z + 2 * m, ob.size.y + 2 * m);
                int idx = (min == dXmax) ? 2 : 3;
                walls[idx].openings.Add(rect);
            }
        }
    }

    void TileWall(List<Vector3> verts, List<int> tris, WallPlane w)
    {
        float uLen = w.uMax - w.uMin;
        float vLen = w.vMax - w.vMin;
        if (uLen <= 0f || vLen <= 0f) return;

        int uCount = Mathf.Max(1, Mathf.CeilToInt(uLen / Mathf.Max(0.05f, panelSize)));
        int vCount = Mathf.Max(1, Mathf.CeilToInt(vLen / Mathf.Max(0.05f, panelSize)));
        float du = uLen / uCount;
        float dv = vLen / vCount;

        for (int i = 0; i < uCount; i++)
        {
            float u0 = w.uMin + i * du;
            float u1 = u0 + du;
            for (int j = 0; j < vCount; j++)
            {
                float v0 = w.vMin + j * dv;
                float v1 = v0 + dv;

                var cell = new Rect(u0, v0, du, dv);
                bool blocked = false;
                foreach (var op in w.openings)
                {
                    if (op.Overlaps(cell, true)) { blocked = true; break; }
                }
                if (blocked) continue;

                Vector3 a = w.anchor + w.uDir * u0 + w.vDir * v0;
                Vector3 b = w.anchor + w.uDir * u1 + w.vDir * v0;
                Vector3 c = w.anchor + w.uDir * u1 + w.vDir * v1;
                Vector3 d = w.anchor + w.uDir * u0 + w.vDir * v1;
                AddQuad(verts, tris, a, b, c, d);
            }
        }
    }

    // Bir duvarin ustunu roofBase'den, verilen cati yukseklik fonksiyonuna kadar doldurur.
    // Yan duvarlarda yukseklik = roofBase oldugundan hicbir sey eklenmez; alin duvarlarinda ucgen olusur.
    void AddGableFill(List<Vector3> verts, List<int> tris, Vector3 anchor, Vector3 uDir,
        float uMin, float uMax, float roofBase, System.Func<float, float> heightAt)
    {
        float uLen = uMax - uMin;
        if (uLen <= 0f) return;

        int n = Mathf.Max(1, Mathf.CeilToInt(uLen / Mathf.Max(0.05f, panelSize)));
        float du = uLen / n;

        for (int i = 0; i < n; i++)
        {
            float u0 = uMin + i * du;
            float u1 = u0 + du;
            float h0 = heightAt(u0);
            float h1 = heightAt(u1);

            if (h0 <= roofBase + 1e-4f && h1 <= roofBase + 1e-4f)
                continue;

            Vector3 a = anchor + uDir * u0 + Vector3.up * roofBase;
            Vector3 b = anchor + uDir * u1 + Vector3.up * roofBase;
            Vector3 c = anchor + uDir * u1 + Vector3.up * h1;
            Vector3 d = anchor + uDir * u0 + Vector3.up * h0;
            AddQuad(verts, tris, a, b, c, d);
        }
    }

    static void AddQuad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
        t.Add(i); t.Add(i + 2); t.Add(i + 3);
    }

    static void AddTri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
    }

    Material CreateGlassMaterial(Color color, float matSmoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = "GreenhouseGlass (Runtime)" };

        if (shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", matSmoothness);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            if (doubleSided) mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetShaderPassEnabled("ShadowCaster", castShadows);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_Color", color);
            mat.SetFloat("_Glossiness", matSmoothness);
        }

        return mat;
    }
}
