using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Customer Prefabs")]
    [SerializeField]
    private GameObject[] customerPrefabs;

    [Header("Spawn Settings")]
    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private float spawnInterval = 15f;

    [SerializeField]
    private bool spawnImmediately = true;

    [Tooltip(
        "Bật để giới hạn tổng số customer được tạo trong một lần chạy."
    )]
    [SerializeField]
    private bool useSpawnLimit = true;

    [Tooltip(
        "Tổng số customer tối đa có thể được tạo. " +
        "Chỉ được sử dụng khi Use Spawn Limit được bật."
    )]
    [Min(1)]
    [SerializeField]
    private int maximumSpawnCount = 10;

    [Header("Navigation")]
    [SerializeField]
    private Transform stopPoint;

    [Tooltip(
        "NavMesh Area mà customer được phép sử dụng. " +
        "Spawn Point và Stop Point phải nằm trên area này."
    )]
    [SerializeField]
    private string allowedAreaName = "Pathway";

    [Tooltip(
        "Bán kính tìm điểm gần nhất trên Pathway NavMesh."
    )]
    [SerializeField]
    private float navMeshSampleRadius = 2f;

    [Header("NPC Runtime References")]
    [Tooltip("Player trong scene. Nếu để trống, PhuAI sẽ tự tìm object có tag GameController.")]
    public Transform player;

    [Tooltip("PlayerHand trong scene. Chỉ cần assign một lần tại Spawner.")]
    public PlayerHand playerHand;

    [Tooltip("UI hướng dẫn nói chuyện dùng chung cho NPC.")]
    public TMP_Text talkInteractionText;

    [Tooltip("Panel hội thoại dùng chung cho NPC.")]
    public GameObject dialoguePanel;

    [Tooltip("Text nội dung hội thoại dùng chung cho NPC.")]
    public TMP_Text dialogueText;

    [Tooltip("Text nhiệm vụ nấu ăn dùng chung cho NPC.")]
    public TMP_Text cookingTaskText;

    [Tooltip("Điểm đặt đĩa sau khi giao món.")]
    public Transform platePlacePoint;

    [Tooltip("UI hướng dẫn đặt đĩa dùng chung cho NPC.")]
    public TMP_Text plateInteractionText;

    private Coroutine spawnCoroutine;
    private int spawnedCount;

    private void OnEnable()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        if (spawnImmediately && CanSpawn())
        {
            SpawnCustomer();
        }

        while (CanSpawn())
        {
            yield return new WaitForSeconds(
                Mathf.Max(0.1f, spawnInterval)
            );

            if (!CanSpawn())
            {
                break;
            }

            SpawnCustomer();
        }

        spawnCoroutine = null;
    }

    private bool CanSpawn()
    {
        return !useSpawnLimit ||
               spawnedCount < maximumSpawnCount;
    }

    private void SpawnCustomer()
    {
        if (customerPrefabs == null ||
            customerPrefabs.Length == 0)
        {
            Debug.LogWarning(
                "CustomerSpawner: No customer prefab assigned."
            );

            return;
        }

        if (spawnPoint == null || stopPoint == null)
        {
            Debug.LogWarning(
                "CustomerSpawner: Spawn Point or Stop Point is missing."
            );

            return;
        }

        if (!TryGetAllowedAreaMask(out int allowedAreaMask))
        {
            return;
        }

        if (!TryGetNavMeshPosition(
                spawnPoint.position,
                allowedAreaMask,
                out Vector3 spawnPosition))
        {
            Debug.LogWarning(
                "CustomerSpawner: Spawn Point is not near a NavMesh."
            );

            return;
        }

        if (!TryGetNavMeshPosition(
                stopPoint.position,
                allowedAreaMask,
                out Vector3 destination))
        {
            Debug.LogWarning(
                "CustomerSpawner: Stop Point is not near a NavMesh."
            );

            return;
        }

        GameObject selectedPrefab =
            customerPrefabs[
                Random.Range(0, customerPrefabs.Length)
            ];

        if (selectedPrefab == null)
        {
            Debug.LogWarning(
                "CustomerSpawner: Selected prefab is null."
            );

            return;
        }

        GameObject customer = Instantiate(
            selectedPrefab,
            spawnPosition,
            spawnPoint.rotation
        );

        NavMeshAgent agent =
            customer.GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogWarning(
                $"{customer.name} does not have a NavMeshAgent."
            );

            Destroy(customer);
            return;
        }

        spawnedCount++;

        // Customer chỉ có thể lập đường đi trên area Pathway.
        agent.areaMask = allowedAreaMask;

        PhuAI npc =
            customer.GetComponent<PhuAI>();

        if (npc != null)
        {
            npc.ConfigureRuntime(
                player,
                playerHand,
                stopPoint,
                spawnPoint,
                destination,
                spawnPosition,
                talkInteractionText,
                dialoguePanel,
                dialogueText,
                cookingTaskText,
                platePlacePoint,
                plateInteractionText
            );

            // PhuAI tự điều khiển hành trình, state tương tác,
            // quay về Spawn Point và despawn.
            return;
        }

        StartCoroutine(
            SendCustomerToDestination(
                agent,
                spawnPosition,
                destination
            )
        );
    }

    private IEnumerator SendCustomerToDestination(
        NavMeshAgent agent,
        Vector3 spawnPosition,
        Vector3 destination)
    {
        // Chờ object được Unity khởi tạo hoàn chỉnh.
        yield return null;

        if (agent == null)
            yield break;

        if (!agent.isOnNavMesh)
        {
            if (!agent.Warp(spawnPosition))
            {
                Debug.LogWarning(
                    $"{agent.name} could not be placed on the NavMesh."
                );

                Destroy(agent.gameObject);
                yield break;
            }
        }

        agent.isStopped = false;

        if (!agent.SetDestination(destination))
        {
            Debug.LogWarning(
                $"{agent.name} could not reach the Stop Point."
            );
        }
    }

    private bool TryGetNavMeshPosition(
        Vector3 position,
        int areaMask,
        out Vector3 navMeshPosition)
    {
        if (NavMesh.SamplePosition(
                position,
                out NavMeshHit hit,
                navMeshSampleRadius,
                areaMask))
        {
            navMeshPosition = hit.position;
            return true;
        }

        navMeshPosition = position;
        return false;
    }

    private bool TryGetAllowedAreaMask(out int areaMask)
    {
        int areaIndex =
            NavMesh.GetAreaFromName(allowedAreaName);

        if (areaIndex < 0)
        {
            Debug.LogError(
                $"CustomerSpawner: NavMesh Area " +
                $"'{allowedAreaName}' does not exist.",
                this
            );

            areaMask = 0;
            return false;
        }

        areaMask = 1 << areaIndex;
        return true;
    }
}
