using UnityEngine;

[CreateAssetMenu(fileName = "NewCrop", menuName = "Greenhouse/CropProfile")]
public class CropProfile : ScriptableObject
{
    public string cropName;

    [Header("Sicaklik (C)")]
    public float tempMin = 18f;
    public float tempMax = 28f;
    public float tempOptimal = 24f;

    [Header("Nem (%)")]
    public float humidityMin = 50f;
    public float humidityMax = 80f;

    [Header("Toprak")]
    public float soilMoistureMin = 40f;
    public float soilMoistureMax = 70f;
    public float targetEC = 2.5f;
    public float targetPH = 6.5f;

    [Header("Isik")]
    public float minLightLux = 20000f;
    public float dailyLightHours = 14f;

    [Header("Buyume")]
    public float baseGrowthTemp = 10f;
    public float requiredGDD = 1500f;
}
