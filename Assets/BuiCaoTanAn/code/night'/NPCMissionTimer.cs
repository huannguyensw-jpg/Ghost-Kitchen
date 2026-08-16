using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class NPCMissionTimer : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static NPCMissionTimer Instance { get; private set; }


    // =========================================================
    // CUSTOMER
    // =========================================================

    [Header("Customer Prefabs")]

    [SerializeField]
    private GameObject[] customerPrefabs;

    [Header("Spawn Settings")]

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private Transform stopPoint;

    [SerializeField]
    private bool spawnFirstNPCImmediately = true;

    [Header("Navigation")]

    [SerializeField]
    private string allowedAreaName = "Pathway";

    [SerializeField]
    private float navMeshSampleRadius = 2f;

    [SerializeField]
    private float npcMoveSpeed = 3.5f;


    // =========================================================
    // TIMER
    // =========================================================

    [Header("Timer")]

    [SerializeField]
    private float timeLimit = 30f;

    [SerializeField]
    private TMP_Text timerText;

    [SerializeField]
    private bool showTimer = true;


    // =========================================================
    // NPC MISSIONS
    // =========================================================

    [Header("NPC Missions")]

    [SerializeField]
    private int totalNPCs = 3;

    private int completedNPCs = 0;

    private bool missionRunning = false;

    private bool gameFinished = false;

    private float currentTime;


    // =========================================================
    // SCREEN EFFECT
    // =========================================================

    [Header("Screen Effect")]

    [SerializeField]
    private Canvas screenCanvas;

    // ---------------------------------------------------------
    // CHỈ DÙNG CHO HIỆU ỨNG ĐỎ 10 GIÂY CUỐI
    // ---------------------------------------------------------

    [Tooltip("Image dùng cho hiệu ứng đỏ khi còn 10 giây.")]
    [SerializeField]
    private Image screenImage;

    // ---------------------------------------------------------
    // HÌNH THẮNG RIÊNG
    // ---------------------------------------------------------

    [Tooltip("Hình riêng hiển thị khi người chơi thắng.")]
    [SerializeField]
    private Image winScreenImage;

    // ---------------------------------------------------------
    // HÌNH THUA RIÊNG
    // ---------------------------------------------------------

    [Tooltip("Hình riêng hiển thị khi người chơi thua.")]
    [SerializeField]
    private Image loseScreenImage;

    [SerializeField]
    private float effectDuration = 1.5f;

    [SerializeField]
    private Color normalScreenColor = Color.black;

    [SerializeField]
    private Color dangerScreenColor = Color.red;

    [SerializeField]
    private Color deadScreenColor = Color.black;


    // =========================================================
    // RESULT TEXT
    // =========================================================

    [Header("Result Text")]

    [SerializeField]
    private TMP_Text aliveText;

    [SerializeField]
    private TMP_Text deadText;

    [SerializeField]
    private float resultTextDuration = 3f;


    // =========================================================
    // NPC RUNTIME REFERENCES
    // =========================================================

    [Header("NPC Runtime References")]

    [Tooltip("Player trong scene.")]
    public Transform player;

    [Tooltip("Player Hand.")]
    public PlayerHand playerHand;

    [Tooltip("Text hiện [Nói chuyện].")]
    public TMP_Text talkInteractionText;

    [Tooltip("Panel hội thoại.")]
    public GameObject dialoguePanel;

    [Tooltip("Text nội dung hội thoại.")]
    public TMP_Text dialogueText;

    [Tooltip("Text nhiệm vụ nấu ăn.")]
    public TMP_Text cookingTaskText;

    [Tooltip("Điểm đặt đĩa.")]
    public Transform platePlacePoint;

    [Tooltip("Text hiện khi có thể đặt đĩa.")]
    public TMP_Text plateInteractionText;


    // =========================================================
    // CURRENT NPC
    // =========================================================

    private GameObject currentNPC;

    private PhuAIMA currentPhuAI;

    private Coroutine timerCoroutine;

    private Coroutine fadeNPCcoroutine;

    private Coroutine spawnCoroutine;

    private bool isSpawningNPC = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    private void Start()
    {
        HideAliveText();
        HideDeadText();

        // -----------------------------------------------------
        // SCREEN EFFECT BAN ĐẦU
        // -----------------------------------------------------

        SetScreenAlpha(0f);

        if (timerText != null)
        {
            timerText.text = "";
        }

        // -----------------------------------------------------
        // TẮT HÌNH THẮNG
        // -----------------------------------------------------

        HideWinScreen();

        // -----------------------------------------------------
        // TẮT HÌNH THUA
        // -----------------------------------------------------

        HideLoseScreen();

        // -----------------------------------------------------
        // TẮT TALK TEXT
        // -----------------------------------------------------

        if (talkInteractionText != null)
        {
            talkInteractionText.gameObject.SetActive(false);
        }

        // -----------------------------------------------------
        // TẮT PLATE TEXT
        // -----------------------------------------------------

        if (plateInteractionText != null)
        {
            plateInteractionText.gameObject.SetActive(false);
        }

        // -----------------------------------------------------
        // TẮT DIALOGUE
        // -----------------------------------------------------

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // -----------------------------------------------------
        // SPAWN NPC ĐẦU TIÊN
        // -----------------------------------------------------

        if (spawnFirstNPCImmediately)
        {
            SpawnNextNPC();
        }
    }


    // =========================================================
    // SPAWN NPC
    // =========================================================

    public void SpawnNextNPC()
    {
        if (gameFinished)
            return;

        if (isSpawningNPC)
            return;

        if (completedNPCs >= totalNPCs)
        {
            FinishAllMissions();
            return;
        }

        if (customerPrefabs == null ||
            customerPrefabs.Length == 0)
        {
            Debug.LogError(
                "NPCMissionTimer: Chưa gán Customer Prefabs.",
                this
            );

            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError(
                "NPCMissionTimer: Chưa gán Spawn Point.",
                this
            );

            return;
        }

        if (stopPoint == null)
        {
            Debug.LogError(
                "NPCMissionTimer: Chưa gán Stop Point.",
                this
            );

            return;
        }

        spawnCoroutine = StartCoroutine(
            SpawnNPCRoutine()
        );
    }


    private IEnumerator SpawnNPCRoutine()
    {
        isSpawningNPC = true;

        yield return null;

        if (!TryGetAllowedAreaMask(out int areaMask))
        {
            isSpawningNPC = false;
            yield break;
        }

        if (!TryGetNavMeshPosition(
                spawnPoint.position,
                areaMask,
                out Vector3 spawnPosition))
        {
            Debug.LogError(
                "NPCMissionTimer: Spawn Point không nằm gần NavMesh."
            );

            isSpawningNPC = false;
            yield break;
        }

        if (!TryGetNavMeshPosition(
                stopPoint.position,
                areaMask,
                out Vector3 destination))
        {
            Debug.LogError(
                "NPCMissionTimer: Stop Point không nằm gần NavMesh."
            );

            isSpawningNPC = false;
            yield break;
        }

        GameObject prefab =
            customerPrefabs[
                Random.Range(
                    0,
                    customerPrefabs.Length
                )
            ];

        if (prefab == null)
        {
            Debug.LogError(
                "NPCMissionTimer: Customer Prefab bị null."
            );

            isSpawningNPC = false;
            yield break;
        }

        currentNPC = Instantiate(
            prefab,
            spawnPosition,
            spawnPoint.rotation
        );

        if (currentNPC == null)
        {
            isSpawningNPC = false;
            yield break;
        }

        NavMeshAgent agent =
            currentNPC.GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError(
                currentNPC.name +
                " không có NavMeshAgent."
            );

            Destroy(currentNPC);

            isSpawningNPC = false;
            yield break;
        }

        agent.areaMask = areaMask;
        agent.speed = npcMoveSpeed;
        agent.isStopped = false;

        if (!agent.isOnNavMesh)
        {
            agent.Warp(spawnPosition);
        }

        agent.SetDestination(destination);

        // =====================================================
        // LẤY PHUAIMA
        // =====================================================

        currentPhuAI =
            currentNPC.GetComponent<PhuAIMA>();

        if (currentPhuAI == null)
        {
            Debug.LogError(
                currentNPC.name +
                " không có component PhuAIMA! " +
                "ConfigureRuntime sẽ không được gọi.",
                currentNPC
            );
        }

        // =====================================================
        // GỬI REFERENCES CHO PHUAIMA
        // =====================================================

        if (currentPhuAI != null)
        {
            currentPhuAI.ConfigureRuntime(
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
        }

        isSpawningNPC = false;

        // =====================================================
        // CHỜ NPC ĐI ĐẾN ĐIỂM DỪNG
        // =====================================================

        yield return StartCoroutine(
            WaitUntilNPCReachStopPoint(
                agent,
                destination
            )
        );

        if (currentNPC == null)
            yield break;

        // Timer chưa chạy khi NPC chỉ vừa đến nơi.
        // PhuAIMA sẽ báo lại đúng lúc người chơi bắt đầu nói chuyện.
    }


    private IEnumerator WaitUntilNPCReachStopPoint(
        NavMeshAgent agent,
        Vector3 destination)
    {
        float waitTime = 0f;

        while (agent != null &&
               currentNPC != null)
        {
            if (agent.isStopped)
                break;

            float distance =
                Vector3.Distance(
                    agent.transform.position,
                    destination
                );

            if (distance <= 0.5f)
            {
                agent.isStopped = true;
                break;
            }

            waitTime += Time.deltaTime;

            if (waitTime > 60f)
            {
                agent.isStopped = true;
                break;
            }

            yield return null;
        }
    }


    // =========================================================
    // TIMER
    // =========================================================

    public void BeginCurrentMissionCountdown(
        PhuAIMA requestingNPC)
    {
        if (gameFinished || missionRunning)
            return;

        // Chỉ NPC đang được Mission Timer quản lý mới có quyền
        // bắt đầu đếm ngược.
        if (requestingNPC == null ||
            requestingNPC != currentPhuAI)
        {
            return;
        }

        StartMissionTimer();
    }

    private void StartMissionTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        currentTime = timeLimit;

        missionRunning = true;

        // Hiện đủ thời gian ngay tại frame bắt đầu hội thoại.
        UpdateTimerUI();

        // -----------------------------------------------------
        // RESET SCREEN EFFECT
        // -----------------------------------------------------

        if (screenImage != null)
        {
            Color resetColor = normalScreenColor;
            resetColor.a = 0f;

            screenImage.color = resetColor;
        }

        // -----------------------------------------------------
        // ĐẢM BẢO HÌNH THẮNG / THUA ĐỀU TẮT
        // -----------------------------------------------------

        HideWinScreen();
        HideLoseScreen();

        timerCoroutine =
            StartCoroutine(
                MissionTimerRoutine()
            );
    }


    private IEnumerator MissionTimerRoutine()
    {
        while (
            missionRunning &&
            !gameFinished &&
            currentTime > 0f)
        {
            currentTime -= Time.deltaTime;

            currentTime =
                Mathf.Max(
                    0f,
                    currentTime
                );

            UpdateTimerUI();

            UpdateDangerEffect();

            yield return null;
        }

        if (!missionRunning ||
            gameFinished)
        {
            yield break;
        }

        currentTime = 0f;

        UpdateTimerUI();

        missionRunning = false;

        // =====================================================
        // HẾT 30 GIÂY
        // =====================================================

        StartCoroutine(
            MissionFailedRoutine()
        );
    }


    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        if (!showTimer)
        {
            timerText.text = "";
            return;
        }

        int seconds =
            Mathf.CeilToInt(currentTime);

        timerText.text =
            seconds.ToString();
    }


    // =========================================================
    // 10 GIÂY CUỐI → ĐỎ DẦN
    // =========================================================

    private void UpdateDangerEffect()
    {
        if (screenImage == null)
            return;

        if (currentTime > 10f)
        {
            SetScreenAlpha(0f);
            return;
        }

        float dangerProgress =
            1f -
            Mathf.Clamp01(
                currentTime / 10f
            );

        Color color =
            Color.Lerp(
                normalScreenColor,
                dangerScreenColor,
                dangerProgress
            );

        color.a = dangerProgress;

        screenImage.color = color;
    }


    // =========================================================
    // FAIL
    // =========================================================

    private IEnumerator MissionFailedRoutine()
    {
        missionRunning = false;

        if (timerText != null)
        {
            timerText.text = "0";
        }

        // -----------------------------------------------------
        // TẮT HÌNH THẮNG
        // -----------------------------------------------------

        HideWinScreen();

        // -----------------------------------------------------
        // HIỆU ỨNG ĐỎ HOÀN TOÀN
        // -----------------------------------------------------

        if (screenImage != null)
        {
            yield return StartCoroutine(
                FadeScreenToColor(
                    dangerScreenColor,
                    0.5f
                )
            );
        }

        // -----------------------------------------------------
        // SAU ĐÓ TỐI ĐEN
        // -----------------------------------------------------

        if (screenImage != null)
        {
            yield return StartCoroutine(
                FadeScreenToColor(
                    deadScreenColor,
                    effectDuration
                )
            );
        }

        // -----------------------------------------------------
        // HIỆN HÌNH THUA RIÊNG
        // -----------------------------------------------------

        if (loseScreenImage != null)
        {
            loseScreenImage.gameObject.SetActive(true);

            Color color =
                loseScreenImage.color;

            color.a = 0f;

            loseScreenImage.color = color;

            yield return StartCoroutine(
                FadeImageAlpha(
                    loseScreenImage,
                    1f,
                    effectDuration
                )
            );
        }

        // -----------------------------------------------------
        // HIỆN TEXT THUA
        // -----------------------------------------------------

        ShowDeadText();

        Debug.Log(
            "NPCMissionTimer: THẤT BẠI - Hết 30 giây."
        );
    }


    // =========================================================
    // COMPLETE MISSION
    // =========================================================

    public void CompleteCurrentMission()
    {
        if (gameFinished)
            return;

        if (!missionRunning)
            return;

        missionRunning = false;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);

            timerCoroutine = null;
        }

        if (timerText != null)
        {
            timerText.text = "";
        }

        completedNPCs++;

        Debug.Log(
            "NPCMissionTimer: NPC hoàn thành. " +
            completedNPCs +
            "/" +
            totalNPCs
        );

        // -----------------------------------------------------
        // KHÔNG TỰ DESTROY NPC
        // PHUAIMA TỰ FADE + DESTROY
        // -----------------------------------------------------

        if (fadeNPCcoroutine != null)
        {
            StopCoroutine(fadeNPCcoroutine);

            fadeNPCcoroutine = null;
        }

        fadeNPCcoroutine =
            StartCoroutine(
                WaitForNPCDestroyedThenSpawnNext()
            );
    }


    // =========================================================
    // CHỜ PHUAIMA TỰ DESTROY
    // =========================================================

    private IEnumerator WaitForNPCDestroyedThenSpawnNext()
    {
        float safetyTimeout = 30f;

        float elapsed = 0f;

        while (currentNPC != null &&
               elapsed < safetyTimeout)
        {
            elapsed += Time.deltaTime;

            yield return null;
        }

        if (currentNPC != null)
        {
            Debug.LogWarning(
                "NPCMissionTimer: NPC không tự destroy sau " +
                safetyTimeout +
                "s, buộc phải Destroy để tránh kẹt game.",
                currentNPC
            );

            Destroy(currentNPC);
        }

        currentNPC = null;
        currentPhuAI = null;

        fadeNPCcoroutine = null;

        HandleAfterNPCCompleted();
    }


    // =========================================================
    // SAU KHI NPC HOÀN THÀNH
    // =========================================================

    private void HandleAfterNPCCompleted()
    {
        if (completedNPCs >= totalNPCs)
        {
            FinishAllMissions();
        }
        else
        {
            SpawnNextNPC();
        }
    }


    // =========================================================
    // HOÀN THÀNH TẤT CẢ
    // =========================================================

    private void FinishAllMissions()
    {
        if (gameFinished)
            return;

        gameFinished = true;

        missionRunning = false;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);

            timerCoroutine = null;
        }

        if (timerText != null)
        {
            timerText.text = "";
        }

        StartCoroutine(
            FinishGameRoutine()
        );
    }


    private IEnumerator FinishGameRoutine()
    {
        Debug.Log(
            "NPCMissionTimer: ĐÃ HOÀN THÀNH TẤT CẢ NPC."
        );

        // -----------------------------------------------------
        // TẮT HÌNH THUA
        // -----------------------------------------------------

        HideLoseScreen();

        // -----------------------------------------------------
        // RESET SCREEN EFFECT VỀ MÀU BÌNH THƯỜNG
        // -----------------------------------------------------

        if (screenImage != null)
        {
            Color currentColor =
                screenImage.color;

            Color resetColor =
                normalScreenColor;

            resetColor.a =
                currentColor.a;

            screenImage.color =
                resetColor;
        }

        // -----------------------------------------------------
        // FADE SCREEN EFFECT RA
        // -----------------------------------------------------

        if (screenImage != null)
        {
            yield return StartCoroutine(
                FadeScreenToAlpha(
                    0f,
                    effectDuration
                )
            );
        }

        // -----------------------------------------------------
        // HIỆN HÌNH THẮNG RIÊNG
        // -----------------------------------------------------

        if (winScreenImage != null)
        {
            winScreenImage.gameObject.SetActive(true);

            Color color =
                winScreenImage.color;

            color.a = 0f;

            winScreenImage.color = color;

            yield return StartCoroutine(
                FadeImageAlpha(
                    winScreenImage,
                    1f,
                    effectDuration
                )
            );
        }

        // -----------------------------------------------------
        // HIỆN TEXT THẮNG
        // -----------------------------------------------------

        ShowAliveText();

        yield return new WaitForSeconds(
            resultTextDuration
        );

        // -----------------------------------------------------
        // GIỮ HÌNH THẮNG
        // -----------------------------------------------------
        // Không tắt hình thắng ở đây.
        // Hình thắng sẽ tiếp tục được giữ trên màn hình.
        // -----------------------------------------------------
    }


    // =========================================================
    // ALIVE TEXT
    // =========================================================

    private void ShowAliveText()
    {
        HideDeadText();

        if (aliveText != null)
        {
            aliveText.gameObject.SetActive(true);

            aliveText.text =
                "[Bạn đã sống]";
        }
    }


    private void HideAliveText()
    {
        if (aliveText != null)
        {
            aliveText.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // DEAD TEXT
    // =========================================================

    private void ShowDeadText()
    {
        HideAliveText();

        if (deadText != null)
        {
            deadText.gameObject.SetActive(true);

            deadText.text =
                "[Bạn đã chết]";
        }
    }


    private void HideDeadText()
    {
        if (deadText != null)
        {
            deadText.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // WIN SCREEN
    // =========================================================

    private void ShowWinScreen()
    {
        if (winScreenImage == null)
            return;

        winScreenImage.gameObject.SetActive(true);

        Color color =
            winScreenImage.color;

        color.a = 1f;

        winScreenImage.color = color;
    }


    private void HideWinScreen()
    {
        if (winScreenImage == null)
            return;

        winScreenImage.gameObject.SetActive(false);

        Color color =
            winScreenImage.color;

        color.a = 0f;

        winScreenImage.color = color;
    }


    // =========================================================
    // LOSE SCREEN
    // =========================================================

    private void ShowLoseScreen()
    {
        if (loseScreenImage == null)
            return;

        loseScreenImage.gameObject.SetActive(true);

        Color color =
            loseScreenImage.color;

        color.a = 1f;

        loseScreenImage.color = color;
    }


    private void HideLoseScreen()
    {
        if (loseScreenImage == null)
            return;

        loseScreenImage.gameObject.SetActive(false);

        Color color =
            loseScreenImage.color;

        color.a = 0f;

        loseScreenImage.color = color;
    }


    // =========================================================
    // FADE SCREEN EFFECT
    // =========================================================

    private IEnumerator FadeScreenToAlpha(
        float targetAlpha,
        float duration)
    {
        if (screenImage == null)
            yield break;

        Color startColor =
            screenImage.color;

        Color targetColor =
            startColor;

        targetColor.a =
            targetAlpha;

        float elapsed = 0f;

        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            Color color =
                Color.Lerp(
                    startColor,
                    targetColor,
                    t
                );

            screenImage.color =
                color;

            yield return null;
        }

        screenImage.color =
            targetColor;
    }


    // =========================================================
    // FADE SCREEN TO COLOR
    // =========================================================

    private IEnumerator FadeScreenToColor(
        Color targetColor,
        float duration)
    {
        if (screenImage == null)
            yield break;

        Color startColor =
            screenImage.color;

        float elapsed = 0f;

        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            screenImage.color =
                Color.Lerp(
                    startColor,
                    targetColor,
                    t
                );

            yield return null;
        }

        screenImage.color =
            targetColor;
    }


    // =========================================================
    // FADE IMAGE RIÊNG
    // =========================================================

    private IEnumerator FadeImageAlpha(
        Image image,
        float targetAlpha,
        float duration)
    {
        if (image == null)
            yield break;

        Color startColor =
            image.color;

        Color targetColor =
            startColor;

        targetColor.a =
            targetAlpha;

        float elapsed = 0f;

        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            image.color =
                Color.Lerp(
                    startColor,
                    targetColor,
                    t
                );

            yield return null;
        }

        image.color =
            targetColor;
    }


    // =========================================================
    // SET SCREEN ALPHA
    // =========================================================

    private void SetScreenAlpha(
        float alpha)
    {
        if (screenImage == null)
            return;

        Color color =
            screenImage.color;

        color.a =
            alpha;

        screenImage.color =
            color;
    }


    // =========================================================
    // NAVMESH
    // =========================================================

    private bool TryGetAllowedAreaMask(
        out int areaMask)
    {
        int areaIndex =
            NavMesh.GetAreaFromName(
                allowedAreaName
            );

        if (areaIndex < 0)
        {
            Debug.LogError(
                "NPCMissionTimer: Không tìm thấy NavMesh Area '" +
                allowedAreaName +
                "'."
            );

            areaMask = 0;

            return false;
        }

        areaMask =
            1 << areaIndex;

        return true;
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
            navMeshPosition =
                hit.position;

            return true;
        }

        navMeshPosition =
            position;

        return false;
    }


    // =========================================================
    // PUBLIC GETTERS
    // =========================================================

    public GameObject GetCurrentNPC()
    {
        return currentNPC;
    }


    public PhuAIMA GetCurrentPhuAI()
    {
        return currentPhuAI;
    }


    public float GetCurrentTime()
    {
        return currentTime;
    }


    public int GetCompletedNPCCount()
    {
        return completedNPCs;
    }


    public bool IsMissionRunning()
    {
        return missionRunning;
    }


    public bool IsGameFinished()
    {
        return gameFinished;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetMission()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);

            timerCoroutine = null;
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);

            spawnCoroutine = null;
        }

        if (fadeNPCcoroutine != null)
        {
            StopCoroutine(fadeNPCcoroutine);

            fadeNPCcoroutine = null;
        }

        if (currentNPC != null)
        {
            Destroy(currentNPC);
        }

        currentNPC = null;
        currentPhuAI = null;

        completedNPCs = 0;

        currentTime = 0f;

        missionRunning = false;

        gameFinished = false;

        isSpawningNPC = false;

        // -----------------------------------------------------
        // RESET TEXT
        // -----------------------------------------------------

        HideAliveText();
        HideDeadText();

        // -----------------------------------------------------
        // RESET TIMER
        // -----------------------------------------------------

        if (timerText != null)
        {
            timerText.text = "";
        }

        // -----------------------------------------------------
        // RESET DIALOGUE
        // -----------------------------------------------------

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // -----------------------------------------------------
        // RESET TALK
        // -----------------------------------------------------

        if (talkInteractionText != null)
        {
            talkInteractionText.gameObject.SetActive(false);
        }

        // -----------------------------------------------------
        // RESET PLATE
        // -----------------------------------------------------

        if (plateInteractionText != null)
        {
            plateInteractionText.gameObject.SetActive(false);
        }

        // -----------------------------------------------------
        // RESET SCREEN EFFECT
        // -----------------------------------------------------

        SetScreenAlpha(0f);

        // -----------------------------------------------------
        // RESET HÌNH THẮNG
        // -----------------------------------------------------

        HideWinScreen();

        // -----------------------------------------------------
        // RESET HÌNH THUA
        // -----------------------------------------------------

        HideLoseScreen();

        // -----------------------------------------------------
        // SPAWN LẠI
        // -----------------------------------------------------

        SpawnNextNPC();
    }
}
