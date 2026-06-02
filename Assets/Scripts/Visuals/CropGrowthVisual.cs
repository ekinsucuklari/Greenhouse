using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ekim noktalarina (or. LPattern1 objeleri) crop temsil eden bir gorsel uretir ve
/// GreenhouseManager.plantState.growthStage degerine gore buyutur.
/// Bitki sagligina (plantState.health) gore yaprak rengi yesilden solguna doner.
///
/// Kullanim:
/// 1. Sahnede bir GameObject'e bu scripti ekle.
/// 2. plantingSpots listesine LPattern1 objelerini surukle (bos ise spotKeywords ile bulunur).
/// 3. Play'e bas; bitkiler buyume oranina gore olceklenir.
/// </summary>
public class CropGrowthVisual : MonoBehaviour
{
    [Header("Veri Kaynagi")]
    public GreenhouseManager gm;

    [Header("Ekim Noktalari")]
    [Tooltip("Crop yerlestirilecek noktalar. Bos ise spotKeywords ile sahnede aranir.")]
    public List<Transform> plantingSpots = new();
    public string[] spotKeywords = { "lpattern" };
    [Tooltip("Bitkiyi noktanin renderer ust yuzeyine yerlestir (planter ustu).")]
    public bool placeOnTopOfSpot = true;
    public float baseYOffset = 0f;

    [Header("Tray Bolumleme (uzunlamasina)")]
    [Tooltip("Her tray uzun ekseni boyunca kac esit bolume ayrilsin (her bolume 1 bitki).")]
    public int cropsPerTray = 9;
    [Tooltip("Tray uclarindan birakilacak bosluk orani (0 = uca kadar).")]
    [Range(0f, 0.45f)] public float lengthInset = 0.06f;
    [Tooltip("Bitkiyi tray genisliginin ortasindan kaydirma orani (-0.5..0.5, 0 = tam orta).")]
    [Range(-0.5f, 0.5f)] public float widthOffset = 0f;

    [Header("Crop Gorseli (opsiyonel prefab)")]
    [Tooltip("Atanirsa her noktaya bu prefab konur; bos ise basit bitki (govde + yaprak) uretilir.")]
    public GameObject cropPrefab;

    [Header("Buyume")]
    [Tooltip("growthStage = 0 iken genislik olcegi (fide).")]
    public float minScale = 0.05f;
    [Tooltip("growthStage = 1 iken genislik (footprint) olcegi. Cok buyumesin diye dusuk tutulur.")]
    public float maxScale = 0.3f;
    [Tooltip("Boy (Y) icin ekstra carpan. >1 => crop yukariya dogru gozle gorulur sekilde uzar.")]
    public float heightGrowth = 2.2f;
    [Tooltip("Manager yoksa kullanilacak sabit buyume orani (test icin).")]
    [Range(0f, 1f)] public float fallbackStage = 0.25f;

    [Header("Saglik Rengi")]
    public Color healthyColor = new Color(0.24f, 0.68f, 0.20f);
    public Color stressedColor = new Color(0.62f, 0.55f, 0.16f);
    public Color stemColor = new Color(0.36f, 0.25f, 0.14f);

    [Header("Otomatik")]
    public bool buildOnStart = true;

    const string GenName = "CropVisual";

    private readonly List<Transform> _plants = new();
    private Material _foliageMat;

    void Start()
    {
        if (gm == null) gm = GreenhouseManager.Instance;
        if (gm == null) gm = FindFirstObjectByType<GreenhouseManager>();
        if (buildOnStart) Rebuild();
    }

    [ContextMenu("Rebuild Crops")]
    public void Rebuild()
    {
        ClearExisting();

        if (plantingSpots == null || plantingSpots.Count == 0)
            AutoFindSpots();

        if (plantingSpots.Count == 0)
        {
            Debug.LogWarning("[CropGrowthVisual] Ekim noktasi bulunamadi. plantingSpots ata ya da spotKeywords kontrol et.");
            return;
        }

        _foliageMat = CreateLitMaterial(healthyColor);
        var stemMat = CreateLitMaterial(stemColor);

        foreach (var spot in plantingSpots)
        {
            if (spot == null) continue;
            CreatePlantsForSpot(spot, stemMat);
        }

        UpdateGrowth();
    }

    [ContextMenu("Clear Crops")]
    public void ClearExisting()
    {
        var toRemove = new List<Transform>();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child != transform && child.name == GenName)
                toRemove.Add(child);
        }
        // Sahnedeki noktalarin altina parent edilmis olabilir; tum sahnede de ara
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t != null && t.name == GenName && !toRemove.Contains(t))
                toRemove.Add(t);
        }
        foreach (var t in toRemove)
        {
            if (t == null) continue;
            if (Application.isPlaying) Destroy(t.gameObject);
            else DestroyImmediate(t.gameObject);
        }
        _plants.Clear();
    }

    void AutoFindSpots()
    {
        plantingSpots = new List<Transform>();
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t == null) continue;
            string lower = t.name.ToLowerInvariant();
            foreach (var k in spotKeywords)
            {
                if (string.IsNullOrEmpty(k)) continue;
                if (lower.Contains(k.ToLowerInvariant()))
                {
                    plantingSpots.Add(t);
                    break;
                }
            }
        }
    }

    // Tray'i uzun ekseni boyunca cropsPerTray esit bolume ayirir, her bolumun ortasina 1 bitki koyar.
    void CreatePlantsForSpot(Transform spot, Material stemMat)
    {
        var rend = spot.GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            var single = CreatePlantAt(spot.position + Vector3.up * baseYOffset, stemMat);
            if (single != null) _plants.Add(single);
            return;
        }

        Bounds b = rend.bounds;
        float topY = (placeOnTopOfSpot ? b.max.y : b.center.y) + baseYOffset;

        // Uzun yatay eksen = bitkilerin dizilecegi yon; kisa eksen = genislik (ortaya hizalanir).
        bool lengthIsX = b.size.x >= b.size.z;
        float lengthSize = lengthIsX ? b.size.x : b.size.z;
        float lengthMin = lengthIsX ? b.min.x : b.min.z;
        float crossCenter = lengthIsX ? b.center.z : b.center.x;
        float crossSize = lengthIsX ? b.size.z : b.size.x;

        float inset = lengthSize * lengthInset;
        float a = lengthMin + inset;
        float bEnd = lengthMin + lengthSize - inset;
        float cross = crossCenter + crossSize * widthOffset;

        int count = Mathf.Max(1, cropsPerTray);
        for (int i = 0; i < count; i++)
        {
            float f = (i + 0.5f) / count; // bolumun tam ortasi
            float along = Mathf.Lerp(a, bEnd, f);
            float x = lengthIsX ? along : cross;
            float z = lengthIsX ? cross : along;

            var plant = CreatePlantAt(new Vector3(x, topY, z), stemMat);
            if (plant != null) _plants.Add(plant);
        }
    }

    Transform CreatePlantAt(Vector3 worldPos, Material stemMat)
    {
        var root = new GameObject(GenName);
        root.transform.SetParent(transform, true);
        root.transform.position = worldPos;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        if (cropPrefab != null)
        {
            var inst = Instantiate(cropPrefab, root.transform);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            return root.transform;
        }

        // Basit bitki: govde (silindir) + yaprak kumesi (kureler)
        var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        StripCollider(stem);
        stem.name = "Stem";
        stem.transform.SetParent(root.transform, false);
        stem.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f); // yukseklik ~1
        stem.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        stem.GetComponent<MeshRenderer>().sharedMaterial = stemMat;

        AddFoliage(root.transform, new Vector3(0f, 1.05f, 0f), 0.55f);
        AddFoliage(root.transform, new Vector3(0.18f, 0.85f, 0.05f), 0.38f);
        AddFoliage(root.transform, new Vector3(-0.15f, 0.9f, -0.12f), 0.40f);

        return root.transform;
    }

    void AddFoliage(Transform parent, Vector3 localPos, float size)
    {
        var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        StripCollider(foliage);
        foliage.name = "Foliage";
        foliage.transform.SetParent(parent, false);
        foliage.transform.localPosition = localPos;
        foliage.transform.localScale = new Vector3(size, size * 1.1f, size);
        foliage.GetComponent<MeshRenderer>().sharedMaterial = _foliageMat;
    }

    static void StripCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

    void Update()
    {
        UpdateGrowth();
    }

    void UpdateGrowth()
    {
        float stage = fallbackStage;
        float health = 1f;
        if (gm != null && gm.plantState != null)
        {
            stage = Mathf.Clamp01(gm.plantState.growthStage);
            health = Mathf.Clamp01(gm.plantState.health);
        }

        // Genislik (footprint) az buyur, boy (Y) heightGrowth carpani ile daha belirgin uzar.
        float width = Mathf.Lerp(minScale, maxScale, stage);
        float height = Mathf.Lerp(minScale, maxScale * Mathf.Max(1f, heightGrowth), stage);
        var scaleVec = new Vector3(width, height, width);
        for (int i = 0; i < _plants.Count; i++)
        {
            if (_plants[i] == null) continue;
            _plants[i].localScale = scaleVec;
        }

        if (_foliageMat != null)
        {
            Color c = Color.Lerp(stressedColor, healthyColor, health);
            _foliageMat.SetColor("_BaseColor", c);
            _foliageMat.SetColor("_Color", c);
        }
    }

    Material CreateLitMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "CropMat (Runtime)" };
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        if (shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Smoothness", 0.2f);
            mat.SetFloat("_Metallic", 0f);
        }
        return mat;
    }
}
