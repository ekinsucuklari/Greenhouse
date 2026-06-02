using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sol kenarda bir "Real Data" butonu olusturur. Butona basildiginda mevcut
/// dashboard paneli (varsayilan: DashboardRoot) soldan sahneye kayarak girer ve
/// buton geri ok'una donusur. Tekrar basildiginda panel sola dogru kayip gizlenir.
///
/// Kullanim:
/// 1. Sahnede bos bir GameObject'e bu scripti ekle.
/// 2. dashboardPanel bos ise panelObjectName ile sahnede aranir.
/// 3. Play'e bas; panel baslangicta gizli, buton "Real Data" yazar.
/// </summary>
public class RealDataPanel : MonoBehaviour
{
    [Header("Kaydirilacak Panel")]
    [Tooltip("Bos ise panelObjectName ile sahnede aranir.")]
    public RectTransform dashboardPanel;
    public string panelObjectName = "DashboardRoot";

    [Header("Davranis")]
    [Tooltip("Baslangicta panel gizli (sola kaydirilmis) olsun.")]
    public bool startHidden = true;
    [Tooltip("Kayma suresi (saniye).")]
    public float slideDuration = 0.45f;
    [Tooltip("Panelin tamamen ekran disina cikmasi icin ekstra mesafe (px).")]
    public float extraSlide = 140f;

    [Header("Buton")]
    public string openLabel = "Real Data \u00BB";   // Real Data »
    public string backLabel = "\u00AB Geri";          // « Geri
    public Vector2 buttonSize = new Vector2(150f, 48f);
    public float buttonMargin = 16f;
    [Tooltip("Butonun dikey baslangic konumu (anchor sol-orta, +yukari).")]
    public float buttonPosY = 486f;
    public int sortingOrder = 200;

    static readonly Color BtnNormal = new Color(0.16f, 0.34f, 0.58f, 1f);
    static readonly Color BtnHover  = new Color(0.24f, 0.46f, 0.74f, 1f);

    private Vector2 _shownPos;
    private bool _isShown;
    private Coroutine _anim;
    private TMP_Text _btnLabel;

    void Start()
    {
        if (dashboardPanel == null) FindPanel();
        if (dashboardPanel == null)
        {
            Debug.LogWarning($"[RealDataPanel] '{panelObjectName}' bulunamadi. dashboardPanel ata.");
            return;
        }

        _shownPos = dashboardPanel.anchoredPosition;
        BuildButton();
        StartCoroutine(InitState());
    }

    IEnumerator InitState()
    {
        // Layout'un rect genisligini hesaplamasi icin bir kare bekle.
        yield return new WaitForEndOfFrame();

        if (startHidden)
        {
            _isShown = false;
            dashboardPanel.anchoredPosition = HiddenPos();
        }
        else
        {
            _isShown = true;
            dashboardPanel.anchoredPosition = _shownPos;
        }
        UpdateButtonLabel();
    }

    float SlideDistance()
    {
        float w = dashboardPanel != null ? dashboardPanel.rect.width : 0f;
        if (w < 1f) w = 1920f;
        return w + extraSlide;
    }

    Vector2 HiddenPos() => _shownPos - new Vector2(SlideDistance(), 0f);

    public void Toggle()
    {
        if (dashboardPanel == null) return;
        _isShown = !_isShown;
        Vector2 target = _isShown ? _shownPos : HiddenPos();
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(SlideTo(target));
        UpdateButtonLabel();
    }

    IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = dashboardPanel.anchoredPosition;
        float t = 0f;
        float dur = Mathf.Max(0.01f, slideDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            dashboardPanel.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        dashboardPanel.anchoredPosition = target;
        _anim = null;
    }

    void UpdateButtonLabel()
    {
        if (_btnLabel != null)
            _btnLabel.text = _isShown ? backLabel : openLabel;
    }

    void FindPanel()
    {
        var go = GameObject.Find(panelObjectName);
        if (go != null) dashboardPanel = go.GetComponent<RectTransform>();
    }

    void BuildButton()
    {
        var canvasGO = new GameObject("RealDataCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var btnGO = new GameObject("RealDataButton", typeof(RectTransform));
        btnGO.transform.SetParent(canvasGO.transform, false);
        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(buttonMargin, buttonPosY);
        rt.sizeDelta = buttonSize;

        var img = btnGO.AddComponent<Image>();
        img.color = BtnNormal;
        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = BtnNormal;
        colors.highlightedColor = BtnHover;
        colors.pressedColor = new Color(BtnNormal.r * 0.6f, BtnNormal.g * 0.6f, BtnNormal.b * 0.6f, 1f);
        colors.selectedColor = BtnHover;
        btn.colors = colors;
        btn.onClick.AddListener(Toggle);

        var lblGO = new GameObject("Text", typeof(RectTransform));
        lblGO.transform.SetParent(btnGO.transform, false);
        _btnLabel = lblGO.AddComponent<TextMeshProUGUI>();
        _btnLabel.text = openLabel;
        _btnLabel.fontSize = 16f;
        _btnLabel.fontStyle = FontStyles.Bold;
        _btnLabel.alignment = TextAlignmentOptions.Center;
        _btnLabel.color = Color.white;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }
}
