using UnityEngine;

public sealed class NPCFacePlayerRuntimeInstaller : MonoBehaviour
{
    private const float ScanInterval = 0.25f;
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<NPCFacePlayerRuntimeInstaller>() != null)
            return;

        GameObject installerObject = new GameObject(
            "NPC Face Player Runtime Installer"
        );

        DontDestroyOnLoad(installerObject);
        installerObject.AddComponent<NPCFacePlayerRuntimeInstaller>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
            return;

        nextScanTime = Time.unscaledTime + ScanInterval;
        AddToDayNPCs();
        AddToNightNPCs();
    }

    private static void AddToDayNPCs()
    {
        PhuAI[] npcs = FindObjectsByType<PhuAI>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < npcs.Length; i++)
        {
            EnsureComponent(npcs[i].gameObject);
        }
    }

    private static void AddToNightNPCs()
    {
        PhuAIMA[] npcs = FindObjectsByType<PhuAIMA>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < npcs.Length; i++)
        {
            EnsureComponent(npcs[i].gameObject);
        }
    }

    private static void EnsureComponent(GameObject npc)
    {
        if (npc == null || npc.GetComponent<NPCAlwaysFacePlayer>() != null)
            return;

        npc.AddComponent<NPCAlwaysFacePlayer>();
    }
}
