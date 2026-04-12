using UnityEngine;

public class IrrigationPump : ActuatorBase
{
    void Awake()
    {
        actuatorName = "Sulama Pompasi";
        powerWatts = 200f;
    }
}
