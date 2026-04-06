using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ActuatorCommandPayload
{
    public bool fan_active;
    public bool heater_active;
    public bool irrigation_active;
    public bool mister_active;
    public bool grow_light_active;
    public bool fan_mnt;
    public bool heater_mnt;
    public bool irrigation_mnt;
    public bool mister_mnt;
    public bool grow_light_mnt;
}

public class ActuatorCommandSync : MonoBehaviour
{
    [Header("API")]
    public string apiBaseUrl = "http://127.0.0.1:8000";
    public float pollIntervalSeconds = 1f;

    [Header("Mode")]
    public bool enableRemoteOverride = false;

    [Header("References")]
    public GreenhouseManager greenhouseManager;

    private void Start()
    {
        if (greenhouseManager == null)
            greenhouseManager = GreenhouseManager.Instance;

        StartCoroutine(PollLoop());
    }

    private IEnumerator PollLoop()
    {
        var wait = new WaitForSeconds(pollIntervalSeconds);
        while (true)
        {
            yield return PullCommands();
            yield return wait;
        }
    }

    private IEnumerator PullCommands()
    {
        if (!enableRemoteOverride || greenhouseManager == null)
            yield break;

        using (UnityWebRequest req = UnityWebRequest.Get(apiBaseUrl + "/actuators"))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            string json = req.downloadHandler.text;
            if (string.IsNullOrEmpty(json))
                yield break;

            ActuatorCommandPayload commands = JsonUtility.FromJson<ActuatorCommandPayload>(json);
            if (commands == null)
                yield break;

            greenhouseManager.fanActive = commands.fan_mnt ? false : commands.fan_active;
            greenhouseManager.heaterActive = commands.heater_mnt ? false : commands.heater_active;
            greenhouseManager.irrigationActive = commands.irrigation_mnt ? false : commands.irrigation_active;
            greenhouseManager.misterActive = commands.mister_mnt ? false : commands.mister_active;
            greenhouseManager.growLightActive = commands.grow_light_mnt ? false : commands.grow_light_active;
        }
    }
}
