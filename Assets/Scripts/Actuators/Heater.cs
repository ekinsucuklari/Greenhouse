using UnityEngine;

public class Heater : ActuatorBase
{
    void Awake()
    {
        actuatorName = "Heater";
        powerWatts = 3000f;
    }
}
