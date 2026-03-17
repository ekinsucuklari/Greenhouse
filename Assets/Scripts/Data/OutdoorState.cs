using UnityEngine;

[System.Serializable]
public class OutdoorState
{
    public float outsideTemp = 20f;       // °C
    public float solarRadiation = 0f;     // W/m²
    public float outsideHumidity = 55f;   // %
    public float windSpeed = 0f;          // m/s
    public bool isCloudy = false;
}