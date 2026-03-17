using UnityEngine;

[System.Serializable]
public class PlantState
{
    public float growthStage = 0f;        // 0.0 - 1.0
    public float health = 1f;             // 0.0 - 1.0
    public float accumulatedGDD = 0f;     // toplam GDD
}