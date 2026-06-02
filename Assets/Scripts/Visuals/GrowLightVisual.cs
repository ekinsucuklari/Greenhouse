using UnityEngine;

/// <summary>
/// GreenhouseManager.growLightActive true oldugunda sahnedeki "bulb" objesinden
/// ortama ayarli (asiri olmayan) bir isik sacar. Isik yumusak sekilde acilip kapanir
/// ve istege bagli olarak ampul materyalinde emissive parlama olusturur.
///
/// Kullanim:
/// 1. Sahnede bir GameObject'e bu scripti ekle.
/// 2. bulb bos ise bulbName ile sahnede aranir.
/// 3. Play'e bas; grow light aktiflesince isik devreye girer.
/// </summary>
public class GrowLightVisual : MonoBehaviour
{
    [Header("Veri Kaynagi")]
    public GreenhouseManager gm;

    [Header("Isik Kaynagi")]
    [Tooltip("Bos ise bulbName ile sahnede aranir.")]
    public Transform bulb;
    public string bulbName = "bulb";

    [Header("Isik Ayarlari")]
    [Tooltip("Grow light acikken hedef isik siddeti (asiri olmasin diye dusuk tut).")]
    public float intensity = 1.6f;
    [Tooltip("Isigin etki yaricapi (metre).")]
    public float range = 9f;
    [Tooltip("Grow light tipik mor-pembe ton; istersen beyaza cek.")]
    public Color lightColor = new Color(0.78f, 0.65f, 1f);
    [Tooltip("Yumusak acilma/kapanma hizi (saniyede).")]
    public float fadeSpeed = 4f;
    [Tooltip("Golge dususun (Soft daha pahali ama daha guzel).")]
    public LightShadows shadows = LightShadows.None;

    [Header("Ampul Parlamasi (emissive)")]
    public bool glowBulb = true;
    public float glowIntensity = 2.5f;

    [Header("Parlama Kuresi (ampul alt kotuna)")]
    [Tooltip("Ampulun altina isik kaynagi gibi parlayan ayri bir kure olustur.")]
    public bool createGlowSphere = true;
    [Tooltip("Kure capi (metre).")]
    public float glowSphereSize = 0.18f;
    [Tooltip("Ampul alt kotundan dikey kaydirma (- asagi, + yukari).")]
    public float glowSphereYOffset = 0f;
    [Tooltip("Parlayan kurenin rengi.")]
    public Color glowSphereColor = new Color(1f, 0.93f, 0.78f);
    [Tooltip("Kure emissive parlaklik carpani.")]
    public float glowSphereEmission = 4f;

    private Light _light;
    private float _current;
    private Renderer _bulbRenderer;
    private Material _bulbMat;
    private Color _bulbBaseEmission;
    private bool _hasEmission;
    private Renderer _sphereRenderer;
    private Material _sphereMat;

    const string SphereName = "GrowLightGlow";

    void Start()
    {
        if (gm == null) gm = GreenhouseManager.Instance;
        if (gm == null) gm = FindFirstObjectByType<GreenhouseManager>();

        if (bulb == null) FindBulb();
        if (bulb == null)
        {
            Debug.LogWarning($"[GrowLightVisual] '{bulbName}' bulunamadi. bulb alanini ata.");
            enabled = false;
            return;
        }

        SetupLight();
        SetupGlow();
        SetupGlowSphere();
        ApplyImmediate(0f);
    }

    void Update()
    {
        bool on = gm != null && gm.growLightActive;
        float target = on ? 1f : 0f;
        _current = Mathf.MoveTowards(_current, target, fadeSpeed * Time.deltaTime);
        Apply(_current);
    }

    void Apply(float k)
    {
        if (_light != null)
        {
            _light.enabled = k > 0.001f;
            _light.intensity = intensity * k;
        }
        if (_hasEmission && _bulbMat != null)
        {
            Color em = _bulbBaseEmission + lightColor * (glowIntensity * k);
            _bulbMat.SetColor("_EmissionColor", em);
        }
        if (_sphereRenderer != null)
        {
            // Kapaliyken gizle, acikken parlat.
            _sphereRenderer.enabled = k > 0.001f;
            if (_sphereMat != null)
            {
                Color em = glowSphereColor * (glowSphereEmission * Mathf.Max(0.0001f, k));
                _sphereMat.SetColor("_EmissionColor", em);
                _sphereMat.SetColor("_BaseColor", glowSphereColor);
                _sphereMat.SetColor("_Color", glowSphereColor);
            }
        }
    }

    void ApplyImmediate(float k)
    {
        _current = k;
        Apply(k);
    }

    void SetupLight()
    {
        _light = bulb.GetComponent<Light>();
        if (_light == null) _light = bulb.gameObject.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.color = lightColor;
        _light.range = range;
        _light.shadows = shadows;
        _light.intensity = 0f;
        _light.enabled = false;
    }

    void SetupGlow()
    {
        if (!glowBulb) return;
        _bulbRenderer = bulb.GetComponentInChildren<Renderer>();
        if (_bulbRenderer == null) return;

        _bulbMat = _bulbRenderer.material; // instance — paylasilan materyali bozmamak icin
        if (_bulbMat == null) return;

        _bulbMat.EnableKeyword("_EMISSION");
        _hasEmission = _bulbMat.HasProperty("_EmissionColor");
        if (_hasEmission)
            _bulbBaseEmission = _bulbMat.GetColor("_EmissionColor");
    }

    void SetupGlowSphere()
    {
        if (!createGlowSphere) return;

        // Ampulun alt kotunu bul (renderer bounds), kurenin merkezini oraya koy.
        Vector3 pos = bulb.position;
        var rend = _bulbRenderer != null ? _bulbRenderer : bulb.GetComponentInChildren<Renderer>();
        if (rend != null)
            pos = new Vector3(rend.bounds.center.x, rend.bounds.min.y + glowSphereYOffset, rend.bounds.center.z);
        else
            pos += Vector3.up * glowSphereYOffset;

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = SphereName;
        var col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        sphere.transform.SetParent(bulb, true);
        sphere.transform.position = pos;
        sphere.transform.localRotation = Quaternion.identity;
        // Ampul scale'inden bagimsiz gercek dunya capi
        sphere.transform.localScale = Vector3.one;
        float worldDiameter = glowSphereSize;
        Vector3 ls = bulb.lossyScale;
        sphere.transform.localScale = new Vector3(
            worldDiameter / Mathf.Max(0.0001f, ls.x),
            worldDiameter / Mathf.Max(0.0001f, ls.y),
            worldDiameter / Mathf.Max(0.0001f, ls.z));

        _sphereRenderer = sphere.GetComponent<Renderer>();
        _sphereMat = CreateGlowMaterial(glowSphereColor);
        _sphereRenderer.sharedMaterial = _sphereMat;
        _sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _sphereRenderer.enabled = false;
    }

    Material CreateGlowMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "GrowLightGlowMat (Runtime)" };
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Smoothness", 0.5f);
            mat.SetFloat("_Metallic", 0f);
        }
        return mat;
    }

    void FindBulb()
    {
        // Once tam ada gore, sonra icerik eslesmesine gore ara.
        var exact = GameObject.Find(bulbName);
        if (exact != null) { bulb = exact.transform; return; }

        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t != null && t.name.ToLowerInvariant().Contains(bulbName.ToLowerInvariant()))
            {
                bulb = t;
                return;
            }
        }
    }
}
