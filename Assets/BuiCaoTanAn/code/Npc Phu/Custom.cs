using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Custom : MonoBehaviour
{
    [Header("Customer Prefabs")]
    [SerializeField]
    private GameObject[] customerPrefabs;

    [Header("Spawn Settings")]
    [SerializeField]
    private Transform spawnPoint;

    [Header("Navigation")]
    [SerializeField]
    private Transform stopPoint;

    [Tooltip("NavMesh Area mà NPC được phép đi.")]
    [SerializeField]
    private string allowedAreaName = "Pathway";

    [SerializeField]
    private float navMeshSampleRadius = 2f;

    [Header("NPC Runtime References")]

    [SerializeField]
    private Transform player;

    [SerializeField]
    private PlayerHand playerHand;

    [SerializeField]
    private TMP_Text talkInteractionText;

    [SerializeField]
    private GameObject dialoguePanel;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    private TMP_Text cookingTaskText;

    [SerializeField]
    private Transform platePlacePoint;

    [SerializeField]
    private TMP_Text plateInteractionText;

    // =========================================================
    // SCENE TRANSITION
    // =========================================================

    [Header("Scene Transition")]

    [Tooltip("Sau khi NPC hoàn thành, chờ bao nhiêu giây.")]
    [SerializeField]
    private float waitBeforeFade = 5f;

#if UNITY_EDITOR

    [Tooltip("Kéo Scene .unity muốn chuyển tới đây.")]
    [SerializeField]
    private SceneAsset nextScene;

#endif

    [Tooltip("Tên scene dùng trong bản build. Có thể dùng tên gọi thường như Maingame.")]
    [SerializeField]
    private string nextSceneName = "Maingame";

    [Tooltip("Canvas chứa Fade Image.")]
    [SerializeField]
    private Canvas fadeCanvas;

    [Tooltip("Image màu đen phủ toàn màn hình.")]
    [SerializeField]
    private Image fadeImage;

    [Tooltip("Thời gian màn hình tối dần.")]
    [SerializeField]
    private float fadeDuration = 2f;

    // =========================================================
    // RUNTIME
    // =========================================================

    private GameObject currentCustomer;

    private bool hasSpawnedCustomer = false;

    private bool sceneTransitionStarted = false;

    private Coroutine transitionCoroutine;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // FADE BAN ĐẦU
        // -----------------------------------------------------

        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(false);
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;

            color.a = 0f;

            fadeImage.color = color;

            fadeImage.gameObject.SetActive(false);
        }

        // -----------------------------------------------------
        // SPAWN 1 NPC
        // -----------------------------------------------------

        SpawnCustomer();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (sceneTransitionStarted)
            return;

        // Chưa spawn NPC
        if (!hasSpawnedCustomer)
            return;

        // =====================================================
        // PHÁT HIỆN NPC ĐÃ BỊ DESTROY (DỰ PHÒNG AN TOÀN)
        // =====================================================

        if (currentCustomer == null)
        {
            StartSceneTransition();
        }
    }

    // =========================================================
    // SPAWN CUSTOMER
    // =========================================================

    private void SpawnCustomer()
    {
        // Chỉ có 1 NPC
        if (currentCustomer != null)
        {
            return;
        }

        if (customerPrefabs == null ||
            customerPrefabs.Length == 0)
        {
            Debug.LogWarning(
                "CustomerSpawner: Chưa gán Customer Prefab."
            );

            return;
        }

        if (spawnPoint == null ||
            stopPoint == null)
        {
            Debug.LogWarning(
                "CustomerSpawner: Chưa gán Spawn Point hoặc Stop Point."
            );

            return;
        }

        // =====================================================
        // NAVMESH AREA
        // =====================================================

        if (!TryGetAllowedAreaMask(
                out int allowedAreaMask))
        {
            return;
        }

        // =====================================================
        // SPAWN POSITION
        // =====================================================

        if (!TryGetNavMeshPosition(
                spawnPoint.position,
                allowedAreaMask,
                out Vector3 spawnPosition))
        {
            Debug.LogWarning(
                "CustomerSpawner: Spawn Point không nằm gần NavMesh."
            );

            return;
        }

        // =====================================================
        // STOP POSITION
        // =====================================================

        if (!TryGetNavMeshPosition(
                stopPoint.position,
                allowedAreaMask,
                out Vector3 destination))
        {
            Debug.LogWarning(
                "CustomerSpawner: Stop Point không nằm gần NavMesh."
            );

            return;
        }

        // =====================================================
        // CHỌN PREFAB
        // =====================================================

        GameObject selectedPrefab =
            customerPrefabs[
                Random.Range(
                    0,
                    customerPrefabs.Length
                )
            ];

        if (selectedPrefab == null)
        {
            Debug.LogWarning(
                "CustomerSpawner: Customer Prefab bị null."
            );

            return;
        }

        // =====================================================
        // INSTANTIATE
        // =====================================================

        currentCustomer = Instantiate(
            selectedPrefab,
            spawnPosition,
            spawnPoint.rotation
        );

        hasSpawnedCustomer = true;

        Debug.Log(
            "👤 CustomerSpawner: Đã spawn NPC."
        );

        // =====================================================
        // NAVMESH AGENT
        // =====================================================

        NavMeshAgent agent =
            currentCustomer.GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogWarning(
                currentCustomer.name +
                " không có NavMeshAgent."
            );

            Destroy(currentCustomer);

            currentCustomer = null;

            return;
        }

        // Chỉ đi trên Pathway
        agent.areaMask =
            allowedAreaMask;

        // =====================================================
        // PHU AI
        // =====================================================

        PhuAI npc =
            currentCustomer.GetComponent<PhuAI>();

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

            Debug.Log(
                "👤 CustomerSpawner: Đã ConfigureRuntime cho PhuAI."
            );

            return;
        }

        // =====================================================
        // NẾU KHÔNG CÓ PHU AI
        // =====================================================

        StartCoroutine(
            SendCustomerToDestination(
                agent,
                spawnPosition,
                destination
            )
        );
    }

    // =========================================================
    // SEND CUSTOMER
    // =========================================================

    private IEnumerator SendCustomerToDestination(
        NavMeshAgent agent,
        Vector3 spawnPosition,
        Vector3 destination)
    {
        yield return null;

        if (agent == null)
            yield break;

        if (!agent.isOnNavMesh)
        {
            if (!agent.Warp(spawnPosition))
            {
                Debug.LogWarning(
                    agent.name +
                    " không thể đặt lên NavMesh."
                );

                Destroy(agent.gameObject);

                currentCustomer = null;

                yield break;
            }
        }

        agent.isStopped = false;

        if (!agent.SetDestination(destination))
        {
            Debug.LogWarning(
                agent.name +
                " không thể đi tới Stop Point."
            );
        }
    }

    // =========================================================
    // HÀM GỌI KHI HOÀN THÀNH (DÙNG CHO EVENT HOẶC BÊN NGOÀI GỌI VÀO)
    // =========================================================

    public void ForceStartTransition()
    {
        StartSceneTransition();
    }

    // =========================================================
    // START SCENE TRANSITION
    // =========================================================

    private void StartSceneTransition()
    {
        if (sceneTransitionStarted)
            return;

        sceneTransitionStarted = true;

        Debug.Log(
            "🎉 Yêu cầu hoàn thành! Bắt đầu đếm ngược chuyển cảnh..."
        );

        Debug.Log(
            "⏳ Chờ " +
            waitBeforeFade +
            " giây..."
        );

        transitionCoroutine =
            StartCoroutine(
                FinishCustomerAndChangeScene()
            );
    }

    // =========================================================
    // WAIT → FADE → SCENE
    // =========================================================

    private IEnumerator FinishCustomerAndChangeScene()
    {
        // -----------------------------------------------------
        // CHỜ SỐ GIÂY CÀI ĐẶT
        // -----------------------------------------------------

        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                waitBeforeFade
            )
        );

        Debug.Log(
            "🌑 Bắt đầu Fade..."
        );

        // -----------------------------------------------------
        // FADE
        // -----------------------------------------------------

        yield return StartCoroutine(
            FadeToBlack()
        );

        // -----------------------------------------------------
        // LOAD SCENE
        // -----------------------------------------------------

        LoadNextScene();
    }

    // =========================================================
    // FADE TO BLACK
    // =========================================================

    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
        {
            Debug.LogWarning(
                "⚠ CustomerSpawner: Chưa gán Fade Image."
            );

            yield break;
        }

        // Bật Canvas
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
        }

        // Bật Image
        fadeImage.gameObject.SetActive(true);

        // -----------------------------------------------------
        // BẮT ĐẦU TRONG SUỐT
        // -----------------------------------------------------

        Color color =
            fadeImage.color;

        color.a = 0f;

        fadeImage.color =
            color;

        // -----------------------------------------------------
        // FADE NGAY
        // -----------------------------------------------------

        if (fadeDuration <= 0f)
        {
            color.a = 1f;

            fadeImage.color =
                color;

            yield break;
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float alpha =
                Mathf.Clamp01(
                    timer / fadeDuration
                );

            color.a = alpha;

            fadeImage.color =
                color;

            yield return null;
        }

        // Đảm bảo đen hoàn toàn
        color.a = 1f;

        fadeImage.color =
            color;
    }

    // =========================================================
    // LOAD SCENE
    // =========================================================

    private void LoadNextScene()
    {
        Debug.Log(
            "===== LOAD SCENE ====="
        );

        string sceneName = nextSceneName;

#if UNITY_EDITOR
        if (nextScene != null)
        {
            string scenePath =
                AssetDatabase.GetAssetPath(nextScene);

            if (!string.IsNullOrEmpty(scenePath))
            {
                sceneName =
                    System.IO.Path.GetFileNameWithoutExtension(
                        scenePath
                    );
            }
        }
#endif

        sceneName = SceneController.ResolveSceneName(sceneName);

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "❌ CustomerSpawner: Chưa cấu hình scene đích."
            );

            return;
        }

        Debug.Log(
            "🎬 Scene đích: " +
            sceneName
        );

        // -----------------------------------------------------
        // KIỂM TRA BUILD SETTINGS
        // -----------------------------------------------------

        if (!Application.CanStreamedLevelBeLoaded(
                sceneName))
        {
            Debug.LogError(
                "❌ Scene '" +
                sceneName +
                "' chưa được thêm vào " +
                "Build Settings / Build Profiles!"
            );

            return;
        }

        // -----------------------------------------------------
        // LOAD
        // -----------------------------------------------------

        Debug.Log(
            "🚪 Đang chuyển sang Scene: " +
            sceneName
        );

        SceneManager.LoadScene(
            sceneName
        );
    }

    // =========================================================
    // NAVMESH POSITION
    // =========================================================

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
    // AREA MASK
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
                "CustomerSpawner: NavMesh Area '" +
                allowedAreaName +
                "' không tồn tại."
            );

            areaMask = 0;

            return false;
        }

        areaMask =
            1 << areaIndex;

        return true;
    }

    // =========================================================
    // PUBLIC
    // =========================================================

    public GameObject GetCurrentCustomer()
    {
        return currentCustomer;
    }

    public bool IsSceneTransitionStarted()
    {
        return sceneTransitionStarted;
    }
}
