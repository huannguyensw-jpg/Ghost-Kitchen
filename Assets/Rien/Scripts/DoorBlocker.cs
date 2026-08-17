using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DoorBlocker : MonoBehaviour
{
    [SerializeField] private string playerTag = "GameController";
    [SerializeField, Min(0.1f)] private float rescanInterval = 1f;

    private Collider blocker;
    private float nextScanTime;

    private void Awake()
    {
        blocker = GetComponent<Collider>();
        blocker.isTrigger = false;

        UpdateIgnoredCollisions();
    }

    private void Update()
    {
        if (Time.time < nextScanTime)
            return;

        nextScanTime = Time.time + rescanInterval;
        UpdateIgnoredCollisions();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;

        if (!IsPlayer(other.transform))
        {
            Physics.IgnoreCollision(blocker, other, true);
        }
    }

    private void UpdateIgnoredCollisions()
    {
        Collider[] allColliders = FindObjectsByType<Collider>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (Collider other in allColliders)
        {
            if (other == null || other == blocker)
                continue;

            if (other.transform.IsChildOf(transform))
                continue;

            // Chỉ Player va chạm với cửa chặn.
            // NPC và các object khác được đi xuyên qua.
            Physics.IgnoreCollision(
                blocker,
                other,
                !IsPlayer(other.transform)
            );
        }
    }

    private bool IsPlayer(Transform target)
    {
        while (target != null)
        {
            if (target.CompareTag(playerTag))
                return true;

            target = target.parent;
        }

        return false;
    }
}
