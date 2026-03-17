using UnityEngine;

[System.Serializable]
public class SoilState
{
    public float moisture = 60f;      // % (0-100)
    public float ec = 2.0f;           // mS/cm
    public float ph = 6.5f;           // pH
}