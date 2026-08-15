using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class PhuAI : MonoBehaviour
{
    private static PhuAI talkTextOwner;
    private static PhuAI plateTextOwner;

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Transform player;
    [Tooltip("PlayerHand hiện nằm trên object tay/UI riêng, không nằm trực tiếp trên Player.")]
    public PlayerHand playerHand;
    [SerializeField] private Transform targetPoint;

    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float arriveDistance = 0.3f;

    [Header("Return And Despawn")]
    [Tooltip("Điểm NPC quay về sau khi nhận món. Để trống để dùng chính vị trí NPC xuất hiện lúc Start.")]
    public Transform returnPoint;

    private NavMeshAgent agent;
    private bool hasArrived = false;
    private Vector3 spawnPosition;
    private Vector3 returnPosition;
    private Vector3 runtimeTargetPosition;
    private Vector3 runtimeReturnPosition;
    private bool hasRuntimeTargetPosition;
    private bool hasRuntimeReturnPosition;
    private PickupItem servedPlate;
    private bool servedPlateDestroyed;

    // =========================================================
    // TALK
    // =========================================================

    [Header("Talk")]
    [SerializeField] private GameObject talkHitbox;
    [SerializeField] private float talkDistance = 3f;

    // TEXT RIÊNG CHO [NÓI CHUYỆN]
    [SerializeField] private TMP_Text talkInteractionText;

    // =========================================================
    // DIALOGUE UI
    // =========================================================

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    // =========================================================
    // BEFORE COOKING
    // =========================================================

    [Header("Dialogue - Before Cooking")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] beforeCookingDialogue =
    {
        "Xin chào!",
        "Cho tôi một đĩa trứng chiên nhé."
    };

    // =========================================================
    // COOKING TASK TEXT
    // =========================================================

    [Header("Cooking Task")]
    [SerializeField]
    private string cookingText =
        "[Nấu một đĩa trứng chiên]";

    // TEXT RIÊNG NẰM TRÊN ĐẦU / GỐC
    // Text này sẽ luôn hiện từ lúc nhận nhiệm vụ
    // cho tới khi đặt dĩa thành công.
    [SerializeField] private TMP_Text cookingTaskText;

    // =========================================================
    // PLATE DROP
    // =========================================================

    [Header("Plate Drop")]
    [SerializeField] private GameObject plateDropHitbox;
    [SerializeField] private Transform platePlacePoint;

    // TEXT RIÊNG CHO [ĐẶT DĨA]
    [SerializeField] private TMP_Text plateInteractionText;

    [SerializeField]
    private string plateInteractionMessage =
        "[Đặt đĩa trứng]";

    // =========================================================
    // AFTER COOKING
    // =========================================================

    [Header("Dialogue - After Cooking")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] afterCookingDialogue =
    {
        "Món ăn xong rồi à?",
        "Cảm ơn đầu bếp!"
    };

    // =========================================================
    // STATE
    // =========================================================

    private enum State
    {
        Moving,
        WaitingForTalk,
        BeforeCookingDialogue,
        CookingTask,
        WaitingForPlate,
        AfterCookingDialogue,
        WaitingForPlateDeletion,
        Returning,
        Finished
    }

    private State currentState;

    private int dialogueIndex = 0;

    private Camera playerCamera;

    private void OnEnable()
    {
        DeleteEggPlate.PlateDestroyed += OnPlateDestroyed;
    }

    // =========================================================
    // RUNTIME CONFIGURATION
    // =========================================================

    public void ConfigureRuntime(
        Transform runtimePlayer,
        PlayerHand runtimePlayerHand,
        Transform runtimeTargetPoint,
        Transform runtimeReturnPoint,
        Vector3 sampledTargetPosition,
        Vector3 sampledReturnPosition,
        TMP_Text runtimeTalkText,
        GameObject runtimeDialoguePanel,
        TMP_Text runtimeDialogueText,
        TMP_Text runtimeCookingTaskText,
        Transform runtimePlatePlacePoint,
        TMP_Text runtimePlateInteractionText)
    {
        player = runtimePlayer;
        playerHand = runtimePlayerHand;
        targetPoint = runtimeTargetPoint;
        returnPoint = runtimeReturnPoint;
        runtimeTargetPosition = sampledTargetPosition;
        runtimeReturnPosition = sampledReturnPosition;
        hasRuntimeTargetPosition = true;
        hasRuntimeReturnPosition = true;
        talkInteractionText = runtimeTalkText;
        dialoguePanel = runtimeDialoguePanel;
        dialogueText = runtimeDialogueText;
        cookingTaskText = runtimeCookingTaskText;
        platePlacePoint = runtimePlatePlacePoint;
        plateInteractionText = runtimePlateInteractionText;

        AssignPrefabLocalReferences();

        Debug.Log(
            "PhuAI: Runtime references assigned for " +
            gameObject.name +
            ".",
            this
        );
    }

    private void AssignPrefabLocalReferences()
    {
        // Prefab không thể giữ reference tới scene, nhưng có thể dùng
        // collider trên root cho cả hai loại tương tác.
        if (talkHitbox == null)
        {
            talkHitbox = gameObject;
        }

        if (plateDropHitbox == null)
        {
            plateDropHitbox = gameObject;
        }
    }

    private void SetHitboxActive(
        GameObject hitbox,
        bool active)
    {
        if (hitbox == null ||
            hitbox == gameObject)
        {
            // Không tắt root NPC. State machine sẽ quyết định
            // loại tương tác nào đang được phép xử lý.
            return;
        }

        hitbox.SetActive(active);
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        spawnPosition = transform.position;
        AssignPrefabLocalReferences();

        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError(
                "PhuAI: Phú chưa có NavMeshAgent!"
            );

            return;
        }

        // Tốc độ chỉnh trong Inspector
        agent.speed = moveSpeed;

        agent.stoppingDistance = arriveDistance;

        playerCamera = Camera.main;

        if (playerHand == null)
        {
            playerHand = FindFirstObjectByType<PlayerHand>();
        }

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindWithTag("GameController");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else if (playerCamera != null)
            {
                player = playerCamera.transform;
            }
        }

        if (playerHand == null)
        {
            Debug.LogError(
                "PhuAI: Chưa gán PlayerHand và không tìm thấy PlayerHand trong scene!"
            );
        }

        // -----------------------------------------
        // TẮT TOÀN BỘ TEXT BAN ĐẦU
        // -----------------------------------------

        HideTalkText();
        HideCookingTaskText();
        HidePlateText();

        // Dialogue UI
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Hitbox nói chuyện
        SetHitboxActive(
            talkHitbox,
            false
        );

        // Hitbox đặt dĩa
        SetHitboxActive(
            plateDropHitbox,
            false
        );

        // -----------------------------------------
        // BẮT ĐẦU ĐI TỚI ĐIỂM
        // -----------------------------------------

        if (hasRuntimeTargetPosition ||
            targetPoint != null)
        {
            currentState = State.Moving;

            agent.isStopped = false;

            Vector3 destinationPosition =
                hasRuntimeTargetPosition
                    ? runtimeTargetPosition
                    : targetPoint.position;

            if (!agent.SetDestination(
                    destinationPosition))
            {
                Debug.LogError(
                    "PhuAI: Không thể tạo đường đến destination trên NavMesh."
                );

                return;
            }

            Debug.Log(
                "PhuAI: Bắt đầu đi tới NavMesh destination " +
                destinationPosition +
                "."
            );
        }
        else
        {
            Debug.LogError(
                "PhuAI: Chưa gán Target Point!"
            );

            currentState =
                State.WaitingForTalk;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (player == null &&
            currentState != State.Returning &&
            currentState != State.Finished)
        {
            return;
        }

        switch (currentState)
        {
            case State.Moving:

                UpdateMoving();

                break;

            case State.WaitingForTalk:

                UpdateWaitingForTalk();

                break;

            case State.BeforeCookingDialogue:

                UpdateDialogueClick();

                break;

            case State.CookingTask:

                UpdateCookingTask();

                break;

            case State.WaitingForPlate:

                UpdatePlateDrop();

                break;

            case State.AfterCookingDialogue:

                UpdateDialogueClick();

                break;

            case State.Returning:

                UpdateReturning();

                break;

            case State.Finished:

                break;
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void UpdateMoving()
    {
        if (agent == null ||
            !agent.isOnNavMesh ||
            agent.pathPending)
        {
            return;
        }

        // Luôn giữ tốc độ theo Inspector
        agent.speed = moveSpeed;

        Vector3 destinationPosition =
            hasRuntimeTargetPosition
                ? runtimeTargetPosition
                : targetPoint != null
                    ? targetPoint.position
                    : transform.position;

        float directDistance =
            Vector3.Distance(
                transform.position,
                destinationPosition
            );

        float stopDistance =
            Mathf.Max(
                arriveDistance,
                agent.stoppingDistance
            ) + 0.1f;

        bool reachedByPath =
            agent.hasPath &&
            agent.remainingDistance <= stopDistance;

        bool reachedByPosition =
            !agent.hasPath &&
            directDistance <= stopDistance;

        if (reachedByPath ||
            reachedByPosition)
        {
            ArriveAtTarget();
        }
    }

    // =========================================================
    // ARRIVE
    // =========================================================

    private void ArriveAtTarget()
    {
        if (hasArrived)
            return;

        hasArrived = true;

        agent.isStopped = true;

        currentState =
            State.WaitingForTalk;

        Debug.Log(
            "PhuAI: Đã tới destination và chuyển sang WaitingForTalk."
        );

        // Bật hitbox nói chuyện
        SetHitboxActive(
            talkHitbox,
            true
        );
    }

    // =========================================================
    // WAITING FOR TALK
    // =========================================================

    private void UpdateWaitingForTalk()
    {
        GameObject interactionTarget =
            talkHitbox != null
                ? talkHitbox
                : gameObject;

        // Chỉ hiện [Nói chuyện] khi tâm đang trỏ vào NPC
        bool hoveringTalk =
            IsMouseOverObject(interactionTarget);

        float distance =
            Vector3.Distance(
                player.transform.position,
                transform.position
            );

        if (hoveringTalk && distance <= talkDistance)
        {
            ShowTalkText();

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartBeforeCookingDialogue();
            }
        }
        else
        {
            HideTalkText();
        }
    }

    // =========================================================
    // START BEFORE COOKING DIALOGUE
    // =========================================================

    private void StartBeforeCookingDialogue()
    {
        currentState =
            State.BeforeCookingDialogue;

        dialogueIndex = 0;

        HideTalkText();

        SetHitboxActive(
            talkHitbox,
            false
        );

        ShowCurrentDialogue();
    }

    // =========================================================
    // SHOW DIALOGUE
    // =========================================================

    private void ShowCurrentDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (dialogueText == null)
            return;

        // -----------------------------------------
        // BEFORE COOKING
        // -----------------------------------------

        if (currentState ==
            State.BeforeCookingDialogue)
        {
            if (beforeCookingDialogue == null ||
                beforeCookingDialogue.Length == 0)
            {
                StartCookingTask();

                return;
            }

            dialogueText.text =
                beforeCookingDialogue[dialogueIndex];
        }

        // -----------------------------------------
        // AFTER COOKING
        // -----------------------------------------

        else if (currentState ==
                 State.AfterCookingDialogue)
        {
            if (afterCookingDialogue == null ||
                afterCookingDialogue.Length == 0)
            {
                FinishConversation();

                return;
            }

            dialogueText.text =
                afterCookingDialogue[dialogueIndex];
        }
    }

    // =========================================================
    // CLICK DIALOGUE
    // =========================================================

    private void UpdateDialogueClick()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        dialogueIndex++;

        // -----------------------------------------
        // BEFORE COOKING
        // -----------------------------------------

        if (currentState ==
            State.BeforeCookingDialogue)
        {
            if (dialogueIndex >=
                beforeCookingDialogue.Length)
            {
                StartCookingTask();
            }
            else
            {
                ShowCurrentDialogue();
            }
        }

        // -----------------------------------------
        // AFTER COOKING
        // -----------------------------------------

        else if (currentState ==
                 State.AfterCookingDialogue)
        {
            if (dialogueIndex >=
                afterCookingDialogue.Length)
            {
                FinishConversation();
            }
            else
            {
                ShowCurrentDialogue();
            }
        }
    }

    // =========================================================
    // START COOKING TASK
    // =========================================================

    private void StartCookingTask()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        HideTalkText();
        HidePlateText();

        currentState =
            State.CookingTask;

        // =====================================================
        // QUAN TRỌNG:
        // TEXT NHIỆM VỤ HIỆN NGAY TỪ ĐÂY
        // VÀ GIỮ NGUYÊN CHO TỚI KHI ĐẶT DĨA.
        // =====================================================

        ShowCookingTaskText();

        Debug.Log(
            "Nhiệm vụ bắt đầu: " +
            cookingText
        );
    }

    // =========================================================
    // COOKING TASK
    // =========================================================

    private void UpdateCookingTask()
    {
        // Text nhiệm vụ LUÔN được giữ
        ShowCookingTaskText();

        // Ở trạng thái này không cần click vào Phú.
        // Người chơi đi làm trứng ở bếp.
        //
        // Sau khi có dĩa trứng thì đi tới
        // Plate Drop Hitbox.
        //
        // Ta bật hitbox đặt dĩa.

        SetHitboxActive(
            plateDropHitbox,
            true
        );

        currentState =
            State.WaitingForPlate;
    }

    // =========================================================
    // WAITING FOR PLATE
    // =========================================================

    private void UpdatePlateDrop()
    {
        // -----------------------------------------
        // TEXT NHIỆM VỤ VẪN HIỆN
        // -----------------------------------------

        ShowCookingTaskText();

        // -----------------------------------------
        // TEXT ĐẶT DĨA CHỈ HIỆN KHI NHÌN VÀO HITBOX
        // -----------------------------------------

        GameObject interactionTarget =
            plateDropHitbox != null
                ? plateDropHitbox
                : gameObject;

        bool hoveringPlate =
            IsMouseOverObject(
                interactionTarget
            );

        float distance =
            Vector3.Distance(
                player.position,
                interactionTarget != null
                    ? interactionTarget.transform.position
                    : transform.position
            );

        if (hoveringPlate &&
            distance <= talkDistance)
        {
            ShowPlateText();

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPlacePlate();
            }
        }
        else
        {
            HidePlateText();
        }
    }

    // =========================================================
    // TRY PLACE PLATE
    // =========================================================

    private void TryPlacePlate()
    {
        if (playerHand == null)
        {
            Debug.LogError(
                "PhuAI: Không tìm thấy PlayerHand để đặt đĩa!"
            );

            return;
        }

        // -----------------------------------------
        // PHẢI ĐANG CẦM DĨA
        // -----------------------------------------

        if (!playerHand.IsHoldingItemType(
                PickupItem.ItemType.Plate))
        {
            Debug.Log(
                "Không thể đặt: Người chơi chưa cầm dĩa."
            );

            return;
        }

        PickupItem plate =
            playerHand.GetHeldItem();

        if (plate == null)
            return;

        // -----------------------------------------
        // DĨA PHẢI CÓ TRỨNG
        // -----------------------------------------

        if (!PlateHasVisibleEgg(
                plate.transform))
        {
            Debug.Log(
                "Không thể đặt: Dĩa chưa có trứng."
            );

            return;
        }

        // -----------------------------------------
        // ĐẶT DĨA
        // -----------------------------------------

        PlacePlate(plate);
    }

    // =========================================================
    // CHECK EGG ON PLATE
    // =========================================================

    private bool PlateHasVisibleEgg(
        Transform plate)
    {
        if (plate == null)
            return false;

        PlateEgg plateEgg =
            plate.GetComponent<PlateEgg>();

        return plateEgg != null &&
               plateEgg.HasEgg();
    }

    // =========================================================
    // PLACE PLATE
    // =========================================================

    private void PlacePlate(
        PickupItem plate)
    {
        if (playerHand == null)
            return;

        PickupItem heldPlate =
            playerHand.TakeHeldItem();

        if (heldPlate == null ||
            heldPlate != plate)
        {
            Debug.LogError(
                "PhuAI: Đĩa cần đặt không trùng với vật phẩm đang cầm!"
            );

            return;
        }

        Transform plateTransform =
            heldPlate.transform;

        servedPlate = heldPlate;
        servedPlateDestroyed = false;

        // Tách khỏi tay
        plateTransform.SetParent(null);

        // Đặt vào vị trí
        if (platePlacePoint != null)
        {
            plateTransform.position =
                platePlacePoint.position;

            plateTransform.rotation =
                platePlacePoint.rotation;
        }
        else
        {
            plateTransform.position =
                transform.position;

            plateTransform.rotation =
                transform.rotation;
        }

        // Collider
        Collider col =
            plate.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }

        // Rigidbody
        Rigidbody rb =
            plate.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }

        // -----------------------------------------
        // TẮT HITBOX ĐẶT DĨA
        // -----------------------------------------

        SetHitboxActive(
            plateDropHitbox,
            false
        );

        // -----------------------------------------
        // TẮT TEXT ĐẶT DĨA
        // -----------------------------------------

        HidePlateText();

        // -----------------------------------------
        // TẮT TEXT NHIỆM VỤ
        // -----------------------------------------

        HideCookingTaskText();

        Debug.Log(
            "Đã đặt dĩa trứng chiên thành công."
        );

        // -----------------------------------------
        // HỘI THOẠI TIẾP
        // -----------------------------------------

        StartAfterCookingDialogue();
    }

    // =========================================================
    // AFTER COOKING
    // =========================================================

    private void StartAfterCookingDialogue()
    {
        currentState =
            State.AfterCookingDialogue;

        dialogueIndex = 0;

        ShowCurrentDialogue();
    }

    // =========================================================
    // FINISH
    // =========================================================

    private void FinishConversation()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        HideTalkText();
        HidePlateText();
        HideCookingTaskText();

        if (servedPlateDestroyed)
        {
            StartReturning();
            return;
        }

        currentState = State.WaitingForPlateDeletion;

        Debug.Log(
            "PhuAI: Hội thoại đã xong, NPC đang chờ xác nhận " +
            "đĩa trứng được destroy trước khi rời đi."
        );
    }

    private void OnPlateDestroyed(PickupItem destroyedPlate)
    {
        if (destroyedPlate == null || destroyedPlate != servedPlate)
            return;

        servedPlateDestroyed = true;

        Debug.Log(
            "PhuAI: Đã nhận xác nhận destroy đĩa trứng.",
            this
        );

        if (currentState == State.WaitingForPlateDeletion)
        {
            StartReturning();
        }
    }

    // =========================================================
    // RETURN TO SPAWN
    // =========================================================

    private void StartReturning()
    {
        currentState = State.Returning;

        returnPosition =
            hasRuntimeReturnPosition
                ? runtimeReturnPosition
                : returnPoint != null
                ? returnPoint.position
                : spawnPosition;

        if (agent == null ||
            !agent.isOnNavMesh)
        {
            Debug.LogWarning(
                "PhuAI: NPC không ở trên NavMesh nên được despawn để tránh kẹt luồng."
            );

            DespawnImmediately();
            return;
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = arriveDistance;
        agent.isStopped = false;

        if (!agent.SetDestination(returnPosition))
        {
            Debug.LogWarning(
                "PhuAI: Không thể tạo đường về điểm spawn nên được despawn để tránh kẹt luồng."
            );

            DespawnImmediately();
            return;
        }

        Debug.Log(
            "PhuAI: Đang quay về điểm spawn tại " +
            returnPosition +
            "."
        );
    }

    private void UpdateReturning()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            DespawnImmediately();
            return;
        }

        if (agent.pathPending)
            return;

        if (agent.pathStatus ==
            NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning(
                "PhuAI: Đường về điểm spawn không hợp lệ."
            );

            DespawnImmediately();
            return;
        }

        float stopDistance =
            Mathf.Max(
                arriveDistance,
                agent.stoppingDistance
            );

        if (agent.remainingDistance <=
            stopDistance)
        {
            DespawnImmediately();
        }
    }

    private void DespawnImmediately()
    {
        if (currentState == State.Finished)
            return;

        currentState = State.Finished;

        Debug.Log(
            "PhuAI: NPC đã về khu spawn và despawn ngay."
        );

        Destroy(gameObject);
    }

    // =========================================================
    // TALK TEXT
    // =========================================================

    private void ShowTalkText()
    {
        if (talkInteractionText == null)
            return;

        talkTextOwner = this;

        talkInteractionText.text =
            "[Nhấp chuột - Nói chuyện]";

        talkInteractionText.gameObject.SetActive(true);
    }

    private void HideTalkText()
    {
        if (talkInteractionText == null)
            return;

        if (talkTextOwner != null &&
            talkTextOwner != this)
        {
            return;
        }

        talkInteractionText.gameObject.SetActive(false);

        if (talkTextOwner == this)
        {
            talkTextOwner = null;
        }
    }

    // =========================================================
    // COOKING TASK TEXT
    // =========================================================

    private void ShowCookingTaskText()
    {
        if (cookingTaskText == null)
            return;

        cookingTaskText.text =
            cookingText;

        cookingTaskText.gameObject.SetActive(true);
    }

    private void HideCookingTaskText()
    {
        if (cookingTaskText == null)
            return;

        cookingTaskText.gameObject.SetActive(false);
    }

    // =========================================================
    // PLATE TEXT
    // =========================================================

    private void ShowPlateText()
    {
        if (plateInteractionText == null)
            return;

        plateTextOwner = this;

        plateInteractionText.text =
            plateInteractionMessage;

        plateInteractionText.gameObject.SetActive(true);
    }

    private void HidePlateText()
    {
        if (plateInteractionText == null)
            return;

        if (plateTextOwner != null &&
            plateTextOwner != this)
        {
            return;
        }

        plateInteractionText.gameObject.SetActive(false);

        if (plateTextOwner == this)
        {
            plateTextOwner = null;
        }
    }

    private void OnDisable()
    {
        DeleteEggPlate.PlateDestroyed -= OnPlateDestroyed;

        if (talkTextOwner == this)
        {
            if (talkInteractionText != null)
            {
                talkInteractionText.gameObject.SetActive(false);
            }

            talkTextOwner = null;
        }

        if (plateTextOwner == this)
        {
            if (plateInteractionText != null)
            {
                plateInteractionText.gameObject.SetActive(false);
            }

            plateTextOwner = null;
        }
    }

    // =========================================================
    // MOUSE RAYCAST
    // =========================================================

    private bool IsMouseOverObject(
        GameObject target)
    {
        if (target == null)
            return false;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
            return false;

        if (Mouse.current == null)
            return false;

        Vector2 screenPoint =
            Cursor.lockState == CursorLockMode.Locked
                ? new Vector2(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f
                )
                : Mouse.current.position.ReadValue();

        Ray ray =
            playerCamera.ScreenPointToRay(
                screenPoint
            );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                100f))
        {
            // Chính object
            if (hit.collider.gameObject ==
                target)
            {
                return true;
            }

            // Collider con
            if (hit.collider.transform.IsChildOf(
                    target.transform))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // PUBLIC
    // =========================================================

    public bool HasArrived()
    {
        return hasArrived;
    }

    public bool IsFinished()
    {
        return currentState ==
               State.Finished;
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }
}
