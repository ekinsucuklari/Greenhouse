using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeControlUI : MonoBehaviour
{
    [Header("Data Source")]
    public SimulationClock clock;

    [Header("UI References (optional)")]
    public TMP_Text speedLabel;
    public TMP_Text pauseLabel;
    public TMP_Text timeLabel;
    public Button btn1x;
    public Button btn60x;
    public Button btn3600x;
    public Button btnPause;

    void Start()
    {
        if (clock == null) clock = SimulationClock.Instance;
        if (clock == null) clock = FindFirstObjectByType<SimulationClock>();

        if (btn1x != null)     btn1x.onClick.AddListener(SetSpeed1x);
        if (btn60x != null)    btn60x.onClick.AddListener(SetSpeed60x);
        if (btn3600x != null)  btn3600x.onClick.AddListener(SetSpeed3600x);
        if (btnPause != null)  btnPause.onClick.AddListener(TogglePause);
    }

    void Update()
    {
        if (clock == null) return;

        if (speedLabel != null)
            speedLabel.text = $"Speed: {clock.timeScale:F0}x";

        if (pauseLabel != null)
            pauseLabel.text = clock.isPaused ? "PAUSED" : "RUNNING";

        if (timeLabel != null)
        {
            float h = clock.HourOfDay;
            int hours = Mathf.FloorToInt(h);
            int mins  = Mathf.FloorToInt((h - hours) * 60f);
            timeLabel.text = $"Day {clock.DayCount}  {hours:00}:{mins:00}";
        }
    }

    public void SetSpeed1x()    { if (clock != null) clock.timeScale = 1f; }
    public void SetSpeed60x()   { if (clock != null) clock.timeScale = 60f; }
    public void SetSpeed3600x() { if (clock != null) clock.timeScale = 3600f; }
    public void TogglePause()   { if (clock != null) clock.isPaused = !clock.isPaused; }
}
