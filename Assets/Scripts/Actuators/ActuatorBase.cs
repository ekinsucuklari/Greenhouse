using UnityEngine;

public abstract class ActuatorBase : MonoBehaviour
{
    public string actuatorName;
    public float powerWatts;
    public bool isActive;
    public float totalEnergyWh;

    public void UpdateEnergy(float dt)
    {
        if (isActive)
        {
            totalEnergyWh += powerWatts * dt / 3600f;
        }
    }
}
