using System;
using UnityEngine;

/// <summary>
/// Onceden tanimli kamera pozisyonlari arasinda gecis yapar.
/// Start'ta defaultIndex preset'i uygulanir (en son gonderilen = Camera 3).
/// </summary>
public class CameraPresetController : MonoBehaviour
{
    [Serializable]
    public class CameraPreset
    {
        public string label = "Camera";
        public Vector3 position;
        public Vector3 eulerAngles;
    }

    [Header("Hedef Kamera (bos ise Main Camera)")]
    public Camera targetCamera;

    [Header("Kamera Pozisyonlari")]
    public CameraPreset[] presets =
    {
        new CameraPreset
        {
            label = "Camera 1",
            position = new Vector3(2.479f, 2.14f, 6.7f),
            eulerAngles = new Vector3(27.479f, -18.184f, 2.432f),
        },
        new CameraPreset
        {
            label = "Camera 2",
            position = new Vector3(-0.13f, 2.14f, 10.78f),
            eulerAngles = new Vector3(18.598f, -209.08f, 0f),
        },
        new CameraPreset
        {
            label = "Camera 3",
            position = new Vector3(4.84f, 3.4f, 2.55f),
            eulerAngles = new Vector3(23.441f, -31.392f, 0f),
        },
    };

    [Tooltip("Start'ta uygulanacak preset index (en son gonderilen Camera 3 = index 2).")]
    public int defaultIndex = 2;

    public int SelectedIndex { get; private set; } = -1;
    public int PresetCount => presets != null ? presets.Length : 0;

    void Awake()
    {
        ResolveCamera();
    }

    void Start()
    {
        Apply(defaultIndex);
    }

    void ResolveCamera()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();
    }

    public string GetLabel(int index)
    {
        if (presets == null || index < 0 || index >= presets.Length) return "Camera";
        return string.IsNullOrEmpty(presets[index].label) ? $"Camera {index + 1}" : presets[index].label;
    }

    public void Apply(int index)
    {
        if (presets == null || index < 0 || index >= presets.Length) return;
        ResolveCamera();
        if (targetCamera == null)
        {
            Debug.LogWarning("[CameraPresetController] Hedef kamera bulunamadi.");
            return;
        }

        var p = presets[index];
        var tr = targetCamera.transform;
        tr.position = p.position;
        tr.eulerAngles = p.eulerAngles;
        SelectedIndex = index;
    }
}
