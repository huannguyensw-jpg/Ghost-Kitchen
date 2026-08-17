using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class NPCAlwaysFacePlayer : MonoBehaviour
{
    private enum ModelFrontAxis
    {
        PositiveZ,
        NegativeZ,
        PositiveX,
        NegativeX,
        Custom
    }

    [SerializeField]
    private Transform player;

    [Tooltip("Chỉ quay quanh trục Y để NPC không bị cúi hoặc ngửa.")]
    [SerializeField]
    private bool horizontalOnly = true;

    [Header("Hướng mặt trước của model")]
    [Tooltip("Chọn trục gần đúng đang hướng ra phía mặt của model.")]
    [SerializeField]
    private ModelFrontAxis modelFrontAxis = ModelFrontAxis.PositiveZ;

    [Tooltip("Chỉ dùng khi Model Front Axis là Custom. Ví dụ (0.1, 0, 1) nếu mặt lệch nhẹ khỏi +Z.")]
    [SerializeField]
    private Vector3 customLocalFront = Vector3.forward;

    [Tooltip("Tinh chỉnh thêm góc lệch nhỏ của mặt model theo độ.")]
    [Range(-180f, 180f)]
    [SerializeField]
    private float frontAngleOffset;

    private NavMeshAgent agent;
    private bool restoreAgentRotation;

    private void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            restoreAgentRotation = agent.updateRotation;
            agent.updateRotation = false;
        }

        FindPlayer();
    }

    private void OnDisable()
    {
        if (agent != null)
        {
            agent.updateRotation = restoreAgentRotation;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            FindPlayer();
        }

        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;

        if (horizontalOnly)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );

        Vector3 localFront = GetLocalFrontDirection();
        Quaternion modelFrontRotation = Quaternion.LookRotation(
            localFront,
            Vector3.up
        );

        transform.rotation = lookRotation *
            Quaternion.Inverse(modelFrontRotation);
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindWithTag("GameController");

        if (playerObject != null)
        {
            player = playerObject.transform;
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            player = mainCamera.transform;
        }
    }

    private Vector3 GetLocalFrontDirection()
    {
        Vector3 localFront;

        switch (modelFrontAxis)
        {
            case ModelFrontAxis.NegativeZ:
                localFront = Vector3.back;
                break;
            case ModelFrontAxis.PositiveX:
                localFront = Vector3.right;
                break;
            case ModelFrontAxis.NegativeX:
                localFront = Vector3.left;
                break;
            case ModelFrontAxis.Custom:
                localFront = customLocalFront;
                localFront.y = 0f;
                break;
            default:
                localFront = Vector3.forward;
                break;
        }

        if (localFront.sqrMagnitude <= 0.0001f)
        {
            localFront = Vector3.forward;
        }

        localFront.Normalize();

        return Quaternion.Euler(0f, frontAngleOffset, 0f) *
            localFront;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 front = transform.TransformDirection(
            GetLocalFrontDirection()
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(
            transform.position + Vector3.up,
            front * 1.5f
        );
        Gizmos.DrawSphere(
            transform.position + Vector3.up + front * 1.5f,
            0.08f
        );
    }
}
