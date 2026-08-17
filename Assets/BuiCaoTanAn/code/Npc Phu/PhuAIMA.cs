using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class PhuAIMA : MonoBehaviour
{
    // =========================================================
    // STATIC TEXT OWNER
    // =========================================================

    private static PhuAIMA talkTextOwner;
    private static PhuAIMA plateTextOwner;


    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField]
    private Transform player;

    [Tooltip("PlayerHand của Player.")]
    public PlayerHand playerHand;

    [SerializeField]
    private Transform targetPoint;


    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement Settings")]

    [SerializeField]
    private float moveSpeed = 3.5f;

    [SerializeField]
    private float arriveDistance = 0.3f;

    private NavMeshAgent agent;

    private bool hasArrived = false;

    private Vector3 runtimeTargetPosition;
    private bool hasRuntimeTargetPosition = false;


    // =========================================================
    // FADE AND DESPAWN
    // =========================================================

    [Header("Fade And Despawn")]

    [Tooltip("Thời gian NPC mờ dần.")]
    [Min(0.05f)]
    [SerializeField]
    private float fadeDuration = 1.5f;

    [Tooltip("Thời gian chờ trước khi bắt đầu mờ.")]
    [Min(0f)]
    [SerializeField]
    private float fadeDelay = 0f;

    [Tooltip("Sau khi mờ xong thì xóa NPC.")]
    [SerializeField]
    private bool destroyAfterFade = true;

    private float fadeStartTime;

    private Renderer[] npcRenderers;

    private Material[] npcMaterials;

    private Color[] originalColors;

    // ĐÃ SỬA:
    // Material KHÔNG chuyển Transparent lúc NPC spawn.
    // Chỉ chuyển Transparent khi StartFadeAndDespawn().


    // =========================================================
    // TALK
    // =========================================================

    [Header("Talk")]

    [SerializeField]
    private GameObject talkHitbox;

    [SerializeField]
    private float talkDistance = 3f;

    [Tooltip("Text hiện [Nhấp chuột - Nói chuyện].")]
    [SerializeField]
    private TMP_Text talkInteractionText;


    // =========================================================
    // DIALOGUE UI
    // =========================================================

    [Header("Dialogue UI")]

    [SerializeField]
    private GameObject dialoguePanel;

    [SerializeField]
    private TMP_Text dialogueText;


    // =========================================================
    // BEFORE COOKING
    // =========================================================

    [Header("Dialogue - Before Cooking")]

    [TextArea(2, 5)]
    [SerializeField]
    private string[] beforeCookingDialogue =
    {
        "Hello!",
        "Could you make me a plate of fried eggs?"
    };


    // =========================================================
    // COOKING TASK
    // =========================================================

    [Header("Cooking Task")]

    [SerializeField]
    private string cookingText =
        "[Cook a Plate of Fried Eggs]";

    [Tooltip("Text nhiệm vụ.")]
    [SerializeField]
    private TMP_Text cookingTaskText;


    // =========================================================
    // PLATE DROP
    // =========================================================

    [Header("Plate Drop")]

    [SerializeField]
    private GameObject plateDropHitbox;

    [SerializeField]
    private Transform platePlacePoint;

    [Tooltip("Text hiện khi nhìn vào chỗ đặt dĩa.")]
    [SerializeField]
    private TMP_Text plateInteractionText;

    [SerializeField]
    private string plateInteractionMessage =
        "[Place the Egg Plate]";


    // =========================================================
    // AFTER COOKING
    // =========================================================

    [Header("Dialogue - After Cooking")]

    [TextArea(2, 5)]
    [SerializeField]
    private string[] afterCookingDialogue =
    {
        "Oh, you finished it!",
        "Thank you!"
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
        Fading,
        Finished
    }

    private State currentState;

    private int dialogueIndex = 0;

    private Camera playerCamera;


    // =========================================================
    // RUNTIME CONFIGURATION
    // =========================================================

    public void ConfigureRuntime(
        Transform runtimePlayer,
        PlayerHand runtimePlayerHand,
        Transform runtimeTargetPoint,
        Vector3 sampledTargetPosition,
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

        runtimeTargetPosition = sampledTargetPosition;

        hasRuntimeTargetPosition = true;

        talkInteractionText = runtimeTalkText;

        dialoguePanel = runtimeDialoguePanel;

        dialogueText = runtimeDialogueText;

        cookingTaskText = runtimeCookingTaskText;

        platePlacePoint = runtimePlatePlacePoint;

        plateInteractionText = runtimePlateInteractionText;

        AssignPrefabLocalReferences();

        Debug.Log(
            "PhuAIMA: Runtime references đã được gán cho " +
            gameObject.name,
            this
        );
    }


    // =========================================================
    // ASSIGN LOCAL REFERENCES
    // =========================================================

    private void AssignPrefabLocalReferences()
    {
        if (talkHitbox == null)
        {
            talkHitbox = gameObject;
        }

        if (plateDropHitbox == null)
        {
            plateDropHitbox = gameObject;
        }
    }


    // =========================================================
    // HITBOX
    // =========================================================

    private void SetHitboxActive(
        GameObject hitbox,
        bool active)
    {
        if (hitbox == null)
            return;

        if (hitbox == gameObject)
            return;

        hitbox.SetActive(active);
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AssignPrefabLocalReferences();

        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError(
                "PhuAIMA: Không tìm thấy NavMeshAgent!",
                this
            );

            return;
        }

        playerCamera = Camera.main;


        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        if (playerHand == null)
        {
            playerHand =
                FindFirstObjectByType<PlayerHand>();
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


        // -----------------------------------------------------
        // FADE MATERIAL
        // -----------------------------------------------------
        //
        // QUAN TRỌNG:
        // Chỉ lấy material và màu gốc.
        // KHÔNG chuyển material sang Transparent ở đây.
        //

        PrepareFadeMaterials();


        // -----------------------------------------------------
        // TẮT TEXT BAN ĐẦU
        // -----------------------------------------------------

        HideTalkText();

        HideCookingTaskText();

        HidePlateText();


        // -----------------------------------------------------
        // TẮT DIALOGUE
        // -----------------------------------------------------

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }


        // -----------------------------------------------------
        // TẮT HITBOX
        // -----------------------------------------------------

        SetHitboxActive(
            talkHitbox,
            false
        );

        SetHitboxActive(
            plateDropHitbox,
            false
        );


        // -----------------------------------------------------
        // BẮT ĐẦU ĐI
        // -----------------------------------------------------

        StartMovingToTarget();
    }


    // =========================================================
    // START MOVING
    // =========================================================

    private void StartMovingToTarget()
    {
        if (!IsAgentReady())
        {
            Debug.LogWarning(
                "PhuAIMA: Agent chưa sẵn sàng lên NavMesh ở frame này.",
                this
            );

            return;
        }

        Vector3 destination;

        if (hasRuntimeTargetPosition)
        {
            destination =
                runtimeTargetPosition;
        }
        else if (targetPoint != null)
        {
            destination =
                targetPoint.position;
        }
        else
        {
            Debug.LogError(
                "PhuAIMA: Chưa có Target Point.",
                this
            );

            return;
        }

        currentState =
            State.Moving;

        agent.speed =
            moveSpeed;

        agent.stoppingDistance =
            arriveDistance;

        bool success =
            agent.SetDestination(
                destination
            );

        if (!success)
        {
            Debug.LogError(
                "PhuAIMA: Không thể SetDestination.",
                this
            );

            return;
        }

        Debug.Log(
            "PhuAIMA: NPC bắt đầu đi tới " +
            destination,
            this
        );
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
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

            case State.Fading:

                UpdateFade();

                break;

            case State.Finished:

                break;
        }
    }


    // =========================================================
    // SAFE NAVMESH CHECK
    // =========================================================

    private bool IsAgentReady()
    {
        if (agent == null)
            return false;

        if (!agent.isActiveAndEnabled)
            return false;

        if (!gameObject.activeInHierarchy)
            return false;

        if (!agent.isOnNavMesh)
            return false;

        return true;
    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    private void UpdateMoving()
    {
        if (!IsAgentReady())
        {
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        Vector3 destination;

        if (hasRuntimeTargetPosition)
        {
            destination =
                runtimeTargetPosition;
        }
        else if (targetPoint != null)
        {
            destination =
                targetPoint.position;
        }
        else
        {
            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                destination
            );

        float stopDistance =
            Mathf.Max(
                arriveDistance,
                agent.stoppingDistance
            ) + 0.1f;

        bool reached = false;

        if (agent.hasPath)
        {
            reached =
                agent.remainingDistance <=
                stopDistance;
        }
        else
        {
            reached =
                distance <=
                stopDistance;
        }

        if (reached)
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

        if (IsAgentReady())
        {
            agent.ResetPath();

            agent.velocity = Vector3.zero;
        }

        currentState =
            State.WaitingForTalk;

        SetHitboxActive(
            talkHitbox,
            true
        );

        Debug.Log(
            "PhuAIMA: NPC đã tới vị trí nhận khách.",
            this
        );
    }


    // =========================================================
    // WAITING FOR TALK
    // =========================================================

    private void UpdateWaitingForTalk()
    {
        if (player == null)
            return;

        GameObject target =
            talkHitbox != null
                ? talkHitbox
                : gameObject;

        bool hovering =
            IsMouseOverObject(target);

        float distance =
            Vector3.Distance(
                player.position,
                transform.position
            );

        if (hovering &&
            distance <= talkDistance)
        {
            ShowTalkText();

            if (Mouse.current != null &&
                Mouse.current.leftButton
                    .wasPressedThisFrame)
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
    // START BEFORE COOKING
    // =========================================================

    private void StartBeforeCookingDialogue()
    {
        currentState =
            State.BeforeCookingDialogue;

        if (NPCMissionTimer.Instance != null)
        {
            NPCMissionTimer.Instance.BeginCurrentMissionCountdown(this);
        }
        else
        {
            Debug.LogError(
                "PhuAIMA: Không tìm thấy NPCMissionTimer để bắt đầu đếm giờ.",
                this
            );
        }

        dialogueIndex = 0;

        HideTalkText();

        SetHitboxActive(
            talkHitbox,
            false
        );

        ShowCurrentDialogue();
    }


    // =========================================================
    // SHOW CURRENT DIALOGUE
    // =========================================================

    private void ShowCurrentDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (dialogueText == null)
        {
            return;
        }

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
                beforeCookingDialogue[
                    dialogueIndex
                ];
        }
        else if (
            currentState ==
            State.AfterCookingDialogue)
        {
            if (afterCookingDialogue == null ||
                afterCookingDialogue.Length == 0)
            {
                StartFadeAndDespawn();

                return;
            }

            dialogueText.text =
                afterCookingDialogue[
                    dialogueIndex
                ];
        }
    }


    // =========================================================
    // DIALOGUE CLICK
    // =========================================================

    private void UpdateDialogueClick()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton
            .wasPressedThisFrame)
        {
            return;
        }

        dialogueIndex++;


        // -----------------------------------------------------
        // BEFORE
        // -----------------------------------------------------

        if (currentState ==
            State.BeforeCookingDialogue)
        {
            if (beforeCookingDialogue == null ||
                dialogueIndex >=
                beforeCookingDialogue.Length)
            {
                StartCookingTask();
            }
            else
            {
                ShowCurrentDialogue();
            }
        }


        // -----------------------------------------------------
        // AFTER
        // -----------------------------------------------------

        else if (
            currentState ==
            State.AfterCookingDialogue)
        {
            if (afterCookingDialogue == null ||
                dialogueIndex >=
                afterCookingDialogue.Length)
            {
                StartFadeAndDespawn();
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

        ShowCookingTaskText();

        SetHitboxActive(
            plateDropHitbox,
            true
        );

        Debug.Log(
            "PhuAIMA: Bắt đầu nhiệm vụ: " +
            cookingText,
            this
        );
    }


    // =========================================================
    // COOKING TASK
    // =========================================================

    private void UpdateCookingTask()
    {
        ShowCookingTaskText();

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
        ShowCookingTaskText();

        GameObject target =
            plateDropHitbox != null
                ? plateDropHitbox
                : gameObject;

        bool hovering =
            IsMouseOverObject(target);

        float distance =
            Vector3.Distance(
                player != null
                    ? player.position
                    : transform.position,
                target.transform.position
            );

        if (hovering &&
            distance <= talkDistance)
        {
            ShowPlateText();

            if (Mouse.current != null &&
                Mouse.current.leftButton
                    .wasPressedThisFrame)
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
                "PhuAIMA: PlayerHand chưa được gán.",
                this
            );

            return;
        }

        if (!playerHand.IsHoldingItemType(
                PickupItem.ItemType.Plate))
        {
            Debug.Log(
                "PhuAIMA: Người chơi chưa cầm dĩa.",
                this
            );

            return;
        }

        PickupItem plate =
            playerHand.GetHeldItem();

        if (plate == null)
        {
            return;
        }

        if (!PlateHasVisibleEgg(
                plate.transform))
        {
            Debug.Log(
                "PhuAIMA: Dĩa chưa có trứng.",
                this
            );

            return;
        }

        PlacePlate(
            plate
        );
    }


    // =========================================================
    // CHECK EGG
    // =========================================================

    private bool PlateHasVisibleEgg(
        Transform plate)
    {
        if (plate == null)
            return false;

        PlateEgg plateEgg =
            plate.GetComponent<PlateEgg>();

        if (plateEgg == null)
        {
            return false;
        }

        return plateEgg.HasEgg();
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

        if (heldPlate == null)
        {
            return;
        }

        if (heldPlate != plate)
        {
            Debug.LogError(
                "PhuAIMA: Held plate không trùng plate.",
                this
            );

            return;
        }

        Transform plateTransform =
            heldPlate.transform;

        plateTransform.SetParent(null);

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

        Collider col =
            plate.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }

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

        SetHitboxActive(
            plateDropHitbox,
            false
        );

        HidePlateText();

        HideCookingTaskText();


        // -----------------------------------------------------
        // NPC ĐỨNG YÊN
        // -----------------------------------------------------

        if (IsAgentReady())
        {
            agent.ResetPath();

            agent.velocity =
                Vector3.zero;
        }

        Debug.Log(
            "PhuAIMA: Đã nhận dĩa trứng. " +
            "NPC đứng nguyên tại chỗ và chuẩn bị mờ.",
            this
        );

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
    // FADE START
    // =========================================================

    private void StartFadeAndDespawn()
    {
        if (currentState ==
            State.Fading ||
            currentState ==
            State.Finished)
        {
            return;
        }


        // -----------------------------------------------------
        // TẮT UI
        // -----------------------------------------------------

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        HideTalkText();

        HidePlateText();

        HideCookingTaskText();


        // -----------------------------------------------------
        // TẮT HITBOX
        // -----------------------------------------------------

        SetHitboxActive(
            talkHitbox,
            false
        );

        SetHitboxActive(
            plateDropHitbox,
            false
        );


        // -----------------------------------------------------
        // DỪNG NPC HOÀN TOÀN
        // -----------------------------------------------------

        if (IsAgentReady())
        {
            agent.ResetPath();

            agent.velocity =
                Vector3.zero;
        }


        // =====================================================
        // QUAN TRỌNG NHẤT
        // =====================================================
        //
        // CHỈ BÂY GIỜ mới chuyển material sang Transparent.
        //
        // Khi NPC spawn:
        // Opaque bình thường.
        //
        // Khi NPC bắt đầu fade:
        // Opaque -> Transparent.
        //

        PrepareMaterialsForFade();


        // Đảm bảo alpha bắt đầu từ 100%.
        SetNPCAlpha(1f);


        currentState =
            State.Fading;

        fadeStartTime =
            Time.time + fadeDelay;

        Debug.Log(
            "PhuAIMA: Bắt đầu fade NPC tại chỗ.",
            this
        );
    }


    // =========================================================
    // FADE UPDATE
    // =========================================================

    private void UpdateFade()
    {
        if (Time.time < fadeStartTime)
        {
            return;
        }

        float duration =
            Mathf.Max(
                0.05f,
                fadeDuration
            );

        float elapsed =
            Time.time -
            fadeStartTime;

        float t =
            Mathf.Clamp01(
                elapsed / duration
            );

        SetNPCAlpha(
            1f - t
        );

        if (t >= 1f)
        {
            currentState =
                State.Finished;

            if (NPCMissionTimer.Instance != null)
            {
                Debug.Log(
                    "[NIGHT RESULT UI] NPC ma fade xong, báo hoàn thành nhiệm vụ.",
                    this
                );

                NPCMissionTimer.Instance.CompleteCurrentMission();
            }
            else
            {
                Debug.LogError(
                    "[NIGHT RESULT UI] NPC ma fade xong nhưng không tìm thấy NPCMissionTimer.Instance.",
                    this
                );
            }

            if (destroyAfterFade)
            {
                Destroy(gameObject);
            }
        }
    }


    // =========================================================
    // PREPARE MATERIALS
    // =========================================================
    //
    // CHỈ LẤY MATERIAL VÀ MÀU GỐC.
    //
    // KHÔNG đổi shader / surface / blend ở đây.
    //
    // Vì vậy NPC mới spawn sẽ không bị trong suốt.
    //

    private void PrepareFadeMaterials()
    {
        npcRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        if (npcRenderers == null ||
            npcRenderers.Length == 0)
        {
            Debug.LogWarning(
                "PhuAIMA: NPC không có Renderer để fade.",
                this
            );

            return;
        }

        List<Material>
            materialList =
            new List<Material>();

        List<Color>
            colorList =
            new List<Color>();


        foreach (
            Renderer renderer
            in npcRenderers)
        {
            if (renderer == null)
                continue;


            Material[] materials =
                renderer.materials;


            foreach (
                Material material
                in materials)
            {
                if (material == null)
                    continue;


                materialList.Add(
                    material
                );


                Color color =
                    GetMaterialColor(
                        material
                    );


                colorList.Add(
                    color
                );

                // KHÔNG GỌI SetupMaterialForFade() Ở ĐÂY.
            }
        }


        npcMaterials =
            materialList.ToArray();

        originalColors =
            colorList.ToArray();
    }


    // =========================================================
    // PREPARE MATERIALS FOR FADE
    // =========================================================
    //
    // Hàm này chỉ được gọi lúc NPC thực sự bắt đầu fade.
    //

    private void PrepareMaterialsForFade()
    {
        if (npcMaterials == null ||
            npcMaterials.Length == 0)
        {
            return;
        }


        foreach (
            Material material
            in npcMaterials)
        {
            if (material == null)
                continue;

            SetupMaterialForFade(
                material
            );
        }
    }


    // =========================================================
    // GET MATERIAL COLOR
    // =========================================================

    private Color GetMaterialColor(
        Material material)
    {
        if (material == null)
        {
            return Color.white;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor(
                "_BaseColor"
            );
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor(
                "_Color"
            );
        }

        return Color.white;
    }


    // =========================================================
    // SET MATERIAL COLOR
    // =========================================================

    private void SetMaterialColor(
        Material material,
        Color color)
    {
        if (material == null)
            return;


        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                color
            );
        }


        if (material.HasProperty("_Color"))
        {
            material.SetColor(
                "_Color",
                color
            );
        }
    }


    // =========================================================
    // SETUP TRANSPARENT
    // =========================================================

    private void SetupMaterialForFade(
        Material material)
    {
        if (material == null)
            return;


        // =====================================================
        // BUILT-IN / STANDARD
        // =====================================================

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat(
                "_Mode",
                2f
            );

            material.SetInt(
                "_SrcBlend",
                (int)
                    UnityEngine.Rendering.BlendMode.SrcAlpha
            );

            material.SetInt(
                "_DstBlend",
                (int)
                    UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );

            material.SetInt(
                "_ZWrite",
                0
            );

            material.DisableKeyword(
                "_ALPHATEST_ON"
            );

            material.EnableKeyword(
                "_ALPHABLEND_ON"
            );

            material.DisableKeyword(
                "_ALPHAPREMULTIPLY_ON"
            );

            material.renderQueue =
                3000;
        }


        // =====================================================
        // URP
        // =====================================================

        else if (
            material.HasProperty("_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );

            material.SetFloat(
                "_Blend",
                0f
            );

            material.SetInt(
                "_SrcBlend",
                (int)
                    UnityEngine.Rendering.BlendMode.SrcAlpha
            );

            material.SetInt(
                "_DstBlend",
                (int)
                    UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );

            material.SetInt(
                "_ZWrite",
                0
            );


            // Đúng keyword Transparent của URP.
            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT"
            );


            material.renderQueue =
                3000;
        }
    }


    // =========================================================
    // SET NPC ALPHA
    // =========================================================

    private void SetNPCAlpha(
        float alpha)
    {
        if (npcMaterials == null ||
            originalColors == null)
        {
            return;
        }


        for (
            int i = 0;
            i < npcMaterials.Length;
            i++)
        {
            if (npcMaterials[i] == null)
                continue;


            Color baseColor =
                originalColors[i];


            Color newColor =
                new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    baseColor.a * alpha
                );


            SetMaterialColor(
                npcMaterials[i],
                newColor
            );
        }
    }


    // =========================================================
    // UI HELPERS - MOUSE
    // =========================================================

    private bool IsMouseOverObject(
        GameObject target)
    {
        if (target == null)
            return false;


        if (playerCamera == null)
        {
            playerCamera =
                Camera.main;

            if (playerCamera == null)
                return false;
        }


        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );


        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            talkDistance + 2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (RaycastHit hit in hits)
        {
            if (
                hit.collider.gameObject == target ||
                hit.collider.transform.IsChildOf(
                    target.transform
                ))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // TALK TEXT
    // =========================================================

    private void ShowTalkText()
    {
        if (talkInteractionText != null)
        {
            if (talkTextOwner == null ||
                talkTextOwner == this)
            {
                talkTextOwner = this;

                talkInteractionText.text =
                    "[Nhấp chuột - Nói chuyện]";

                talkInteractionText.gameObject.SetActive(
                    true
                );
            }
        }
    }


    private void HideTalkText()
    {
        if (talkInteractionText != null &&
            talkTextOwner == this)
        {
            talkInteractionText.gameObject.SetActive(
                false
            );

            talkTextOwner = null;
        }
    }


    // =========================================================
    // COOKING TEXT
    // =========================================================

    private void ShowCookingTaskText()
    {
        if (cookingTaskText != null)
        {
            cookingTaskText.text =
                cookingText;

            cookingTaskText.gameObject.SetActive(
                true
            );
        }
    }


    private void HideCookingTaskText()
    {
        if (cookingTaskText != null)
        {
            cookingTaskText.gameObject.SetActive(
                false
            );
        }
    }


    // =========================================================
    // PLATE TEXT
    // =========================================================

    private void ShowPlateText()
    {
        if (plateInteractionText != null)
        {
            if (plateTextOwner == null ||
                plateTextOwner == this)
            {
                plateTextOwner = this;

                plateInteractionText.text =
                    plateInteractionMessage;

                plateInteractionText.gameObject.SetActive(
                    true
                );
            }
        }
    }


    private void HidePlateText()
    {
        if (plateInteractionText != null &&
            plateTextOwner == this)
        {
            plateInteractionText.gameObject.SetActive(
                false
            );

            plateTextOwner = null;
        }
    }


    // =========================================================
    // ON DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (talkTextOwner == this)
        {
            talkTextOwner = null;
        }

        if (plateTextOwner == this)
        {
            plateTextOwner = null;
        }
    }
}
