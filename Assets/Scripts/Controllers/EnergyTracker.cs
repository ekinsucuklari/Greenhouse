using UnityEngine;

public class EnergyTracker : MonoBehaviour
{
    public ActuatorBase[] actuators;

    public float totalEnergyWh;
    public float currentPowerW;

    public void UpdateTracking(float dt)
    {
        currentPowerW = 0f;

        foreach (var act in actuators)
        {
            act.UpdateEnergy(dt);

            if (act.isActive)
                currentPowerW += act.powerWatts;
        }

        totalEnergyWh += currentPowerW * dt / 3600f;
    }

    public float GetCostTL(float pricePerKwh = 4.2f)
    {
        return (totalEnergyWh / 1000f) * pricePerKwh;
    }
}
