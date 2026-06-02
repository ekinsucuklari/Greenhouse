using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fan aktuatoru (GreenhouseManager.fanActive) acikken fan parcalarini kendi Z ekseni
/// etrafinda dondurur. Fan kapaninca yumusakca yavaslayip durur.
///
/// Kullanim:
/// 1. Sahnede bir GameObject'e bu scripti ekle.
/// 2. fans listesine fan gruplarini (rotatinfan1, 2, 5, 7) surukle.
///    Bos birakirsan nameKeywords ile sahnede otomatik aranir.
/// </summary>
public class FanRotator : MonoBehaviour
{
    [Header("Veri Kaynagi")]
    public GreenhouseManager gm;

    [Header("Fan Parcalari")]
    [Tooltip("Donecek fan gruplari. Bos ise nameKeywords ile sahnede otomatik bulunur.")]
    public List<Transform> fans = new();
    [Tooltip("fans bos ise bu kelimeleri iceren objeler otomatik bulunur.")]
    public string[] nameKeywords = { "rotatinfan", "rotatingfan", "rotatfan" };

    [Header("Donme Ayarlari")]
    [Tooltip("Tam hizdaki donme hizi (derece/saniye).")]
    public float rotationSpeed = 720f;
    [Tooltip("Hizlanma/yavaslama orani (derece/saniye^2 benzeri).")]
    public float spinUpRate = 540f;
    [Tooltip("Donme yonu (ters cevirmek icin -1).")]
    public float direction = 1f;
    [Tooltip("Sim duraklatildiginda fanlar da dursun.")]
    public bool stopWhenPaused = true;

    private float _currentSpeed;

    void Start()
    {
        if (gm == null) gm = GreenhouseManager.Instance;
        if (gm == null) gm = FindFirstObjectByType<GreenhouseManager>();

        if (fans == null || fans.Count == 0)
            AutoFindFans();
    }

    void AutoFindFans()
    {
        fans = new List<Transform>();
        var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (t == null) continue;
            string lower = t.name.ToLowerInvariant();
            foreach (var k in nameKeywords)
            {
                if (string.IsNullOrEmpty(k)) continue;
                if (lower.Contains(k.ToLowerInvariant()))
                {
                    fans.Add(t);
                    break;
                }
            }
        }

        if (fans.Count == 0)
            Debug.LogWarning("[FanRotator] Fan parcasi bulunamadi. fans listesine elle ata ya da nameKeywords'u kontrol et.");
    }

    void Update()
    {
        bool fanOn = gm != null && gm.fanActive;
        if (stopWhenPaused && gm != null && gm.simClock != null && gm.simClock.isPaused)
            fanOn = false;

        float target = fanOn ? rotationSpeed : 0f;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, target, spinUpRate * Time.deltaTime);

        if (Mathf.Approximately(_currentSpeed, 0f)) return;

        float deltaAngle = _currentSpeed * direction * Time.deltaTime;
        for (int i = 0; i < fans.Count; i++)
        {
            if (fans[i] == null) continue;
            fans[i].Rotate(0f, 0f, deltaAngle, Space.Self);
        }
    }
}
