using UnityEngine;

public class Fan : ActuatorBase
{
    void Awake()
    {
        actuatorName = "Fan";
        powerWatts = 350f;
    }
}
