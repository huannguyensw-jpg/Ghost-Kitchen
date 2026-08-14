using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using TMPro;

public class PhuAI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform targetPoint;

    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float arriveDistance = 0.3f;

    private NavMeshAgent agent;
    private bool hasArrived = false;

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
        "Chào bạn!",
        "Bạn giúp tôi làm một dĩa trứng chiên được không?"
    };

    // =========================================================
    // COOKING TASK TEXT
    // =========================================================

    [Header("Cooking Task")]
    [SerializeField]
    private string cookingText =
        "[Làm dĩa trứng chiên]";

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
        "[Đặt dĩa trứng]";

    // =========================================================
    // AFTER COOKING
    // =========================================================

    [Header("Dialogue - After Cooking")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] afterCookingDialogue =
    {
        "Ồ, bạn làm xong rồi!",
        "Cảm ơn bạn nhé!"
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
        Finished
    }

    private State currentState;

    private int dialogueIndex = 0;

    private Camera playerCamera;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
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
        if (talkHitbox != null)
        {
            talkHitbox.SetActive(false);
        }

        // Hitbox đặt dĩa
        if (plateDropHitbox != null)
        {
            plateDropHitbox.SetActive(false);
        }

        // -----------------------------------------
        // BẮT ĐẦU ĐI TỚI ĐIỂM
        // -----------------------------------------

        if (targetPoint != null)
        {
            currentState = State.Moving;

            agent.isStopped = false;

            agent.SetDestination(
                targetPoint.position
            );

            Debug.Log(
                "Phú bắt đầu đi tới điểm cố định."
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
        if (player == null)
            return;

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

            case State.Finished:

                break;
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void UpdateMoving()
    {
        if (targetPoint == null)
            return;

        // Luôn giữ tốc độ theo Inspector
        agent.speed = moveSpeed;

        float distance =
            Vector3.Distance(
                transform.position,
                targetPoint.position
            );

        if (distance <= arriveDistance)
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
            "Phú đã tới điểm cố định."
        );

        // Bật hitbox nói chuyện
        if (talkHitbox != null)
        {
            talkHitbox.SetActive(true);
        }
    }

    // =========================================================
    // WAITING FOR TALK
    // =========================================================

    private void UpdateWaitingForTalk()
    {
        // Chỉ hiện [Nói chuyện] khi tâm đang trỏ vào NPC
        bool hoveringTalk =
            IsMouseOverObject(talkHitbox);

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

        if (talkHitbox != null)
        {
            talkHitbox.SetActive(false);
        }

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

        if (plateDropHitbox != null)
        {
            plateDropHitbox.SetActive(true);
        }

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

        bool hoveringPlate =
            IsMouseOverObject(
                plateDropHitbox
            );

        float distance =
            Vector3.Distance(
                player.position,
                plateDropHitbox != null
                    ? plateDropHitbox.transform.position
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
        PlayerHand playerHand =
            player.GetComponent<PlayerHand>();

        if (playerHand == null)
        {
            Debug.LogError(
                "PhuAI: Player không có PlayerHand!"
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
        Transform[] children =
            plate.GetComponentsInChildren<Transform>(
                true
            );

        foreach (Transform child in children)
        {
            if (child == plate)
                continue;

            string objectName =
                child.name.ToLower();

            if (objectName.Contains("egg"))
            {
                if (child.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // =========================================================
    // PLACE PLATE
    // =========================================================

    private void PlacePlate(
        PickupItem plate)
    {
        PlayerHand playerHand =
            player.GetComponent<PlayerHand>();

        if (playerHand == null)
            return;

        Transform plateTransform =
            plate.transform;

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

        // Xóa khỏi tay
        playerHand.ClearHeldItem();

        // -----------------------------------------
        // TẮT HITBOX ĐẶT DĨA
        // -----------------------------------------

        if (plateDropHitbox != null)
        {
            plateDropHitbox.SetActive(false);
        }

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

        currentState =
            State.Finished;

        Debug.Log(
            "Phú đã nói chuyện xong."
        );
    }

    // =========================================================
    // TALK TEXT
    // =========================================================

    private void ShowTalkText()
    {
        if (talkInteractionText == null)
            return;

        talkInteractionText.text =
            "[Nói chuyện]";

        talkInteractionText.gameObject.SetActive(true);
    }

    private void HideTalkText()
    {
        if (talkInteractionText == null)
            return;

        talkInteractionText.gameObject.SetActive(false);
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

        plateInteractionText.text =
            plateInteractionMessage;

        plateInteractionText.gameObject.SetActive(true);
    }

    private void HidePlateText()
    {
        if (plateInteractionText == null)
            return;

        plateInteractionText.gameObject.SetActive(false);
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

        Ray ray =
            playerCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
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