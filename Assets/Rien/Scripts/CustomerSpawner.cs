using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Customer Prefabs")]
    [SerializeField]
    private GameObject[] customerPrefabs;

    [Header("Spawn Settings")]
    [SerializeField]
    private Transform spawnPoint;

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

    [Header("Scene Transition")]
    [Tooltip("Scene chuyển tới sau khi toàn bộ customer đã hoàn thành và despawn.")]
    [SerializeField]
    private string nextSceneName = "Night";

    [Min(0f)]
    [SerializeField]
    private float waitBeforeSceneChange = 0f;

    [Tooltip("Thời gian màn hình tối dần trước khi chuyển scene.")]
    [Min(0f)]
    [SerializeField]
    private float fadeDuration = 2f;

    [SerializeField]
    private Canvas fadeCanvas;

    [SerializeField]
    private Image fadeImage;

    private Coroutine spawnCoroutine;
    private Coroutine sceneTransitionCoroutine;
    private int spawnedCount;
    private GameObject currentCustomer;
    private bool sceneTransitionStarted;
    private readonly List<GameObject> shuffledCustomerPrefabs =
        new List<GameObject>();
    private GameObject lastSelectedPrefab;

    private void OnEnable()
    {
        InitializeFade();
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (sceneTransitionCoroutine != null)
        {
            StopCoroutine(sceneTransitionCoroutine);
            sceneTransitionCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (CanSpawn())
        {
            // Unity chỉ trả về null sau khi NPC cũ đã thực sự bị
            // Destroy. Không cho phép hai khách cùng tồn tại.
            if (currentCustomer != null)
            {
                yield return new WaitUntil(
                    () => currentCustomer == null
                );
            }

            if (!CanSpawn())
            {
                break;
            }

            if (!SpawnCustomer())
            {
                break;
            }
        }

        // Sau khi spawn NPC cuối, CanSpawn() trở thành false ngay.
        // Vẫn phải chờ NPC cuối despawn rồi mới chuyển scene.
        if (currentCustomer != null)
        {
            yield return new WaitUntil(
                () => currentCustomer == null
            );
        }

        spawnCoroutine = null;

        if (useSpawnLimit &&
            spawnedCount >= maximumSpawnCount &&
            currentCustomer == null)
        {
            StartSceneTransition();
        }
    }

    private void StartSceneTransition()
    {
        if (sceneTransitionStarted ||
            string.IsNullOrWhiteSpace(nextSceneName))
        {
            return;
        }

        sceneTransitionStarted = true;
        sceneTransitionCoroutine = StartCoroutine(
            FinishCustomersAndChangeScene()
        );
    }

    private IEnumerator FinishCustomersAndChangeScene()
    {
        Debug.Log(
            "[MAIN TO NIGHT FADE] Đã hoàn thành toàn bộ NPC, chuẩn bị fade.",
            this
        );

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, waitBeforeSceneChange)
        );

        yield return StartCoroutine(FadeToBlack());

        LoadNextScene();
    }

    private void InitializeFade()
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
            fadeImage.gameObject.SetActive(false);
        }

        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
        {
            Debug.LogWarning(
                "[MAIN TO NIGHT FADE] Chưa gán Fade Image.",
                this
            );
            yield break;
        }

        if (fadeCanvas == null)
        {
            fadeCanvas = fadeImage.GetComponentInParent<Canvas>(true);
        }

        PrepareFadeVisuals();

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        Canvas.ForceUpdateCanvases();

        // Chờ một frame trong suốt để Canvas vừa được bật chắc chắn
        // được camera dựng trước khi bắt đầu tăng alpha.
        yield return null;

        Debug.Log(
            $"[MAIN TO NIGHT FADE] Bắt đầu fade trong {fadeDuration:0.##} giây.",
            this
        );

        if (fadeDuration <= 0f)
        {
            color.a = 1f;
            fadeImage.color = color;
            yield return null;
            yield break;
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;

        // Giữ ít nhất một frame đen hoàn toàn trước khi load Night.
        yield return null;

        Debug.Log(
            "[MAIN TO NIGHT FADE] Fade hoàn tất, bắt đầu load scene Night.",
            this
        );
    }

    private void PrepareFadeVisuals()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.enabled = true;

            // Overlay không phụ thuộc Main Camera/Cinemachine nên luôn hiện
            // trong lúc đổi từ Main Game sang Night.
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.worldCamera = null;
            fadeCanvas.overrideSorting = true;
            fadeCanvas.sortingOrder = short.MaxValue;

            RectTransform canvasRect =
                fadeCanvas.transform as RectTransform;

            if (canvasRect != null)
            {
                canvasRect.localScale = Vector3.one;
            }
        }

        RectTransform imageRect = fadeImage.rectTransform;
        imageRect.SetAsLastSibling();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.localScale = Vector3.one;

        fadeImage.raycastTarget = true;
    }

    private void LoadNextScene()
    {
        string resolvedSceneName =
            SceneController.ResolveSceneName(nextSceneName);

        if (!Application.CanStreamedLevelBeLoaded(resolvedSceneName))
        {
            Debug.LogError(
                $"CustomerSpawner: Scene '{resolvedSceneName}' " +
                "chưa có trong Build Settings.",
                this
            );

            sceneTransitionStarted = false;
            sceneTransitionCoroutine = null;
            return;
        }

        Time.timeScale = 1f;
        Debug.Log(
            $"[MAIN TO NIGHT FADE] Load scene '{resolvedSceneName}'.",
            this
        );
        SceneManager.LoadScene(resolvedSceneName);
    }

    private bool CanSpawn()
    {
        return !useSpawnLimit ||
               spawnedCount < maximumSpawnCount;
    }

    private bool SpawnCustomer()
    {
        if (customerPrefabs == null ||
            customerPrefabs.Length == 0)
        {
            Debug.LogWarning(
                "CustomerSpawner: No customer prefab assigned."
            );

            return false;
        }

        if (spawnPoint == null || stopPoint == null)
        {
            Debug.LogWarning(
                "CustomerSpawner: Spawn Point or Stop Point is missing."
            );

            return false;
        }

        if (!TryGetAllowedAreaMask(out int allowedAreaMask))
        {
            return false;
        }

        if (!TryGetNavMeshPosition(
                spawnPoint.position,
                allowedAreaMask,
                out Vector3 spawnPosition))
        {
            Debug.LogWarning(
                "CustomerSpawner: Spawn Point is not near a NavMesh."
            );

            return false;
        }

        if (!TryGetNavMeshPosition(
                stopPoint.position,
                allowedAreaMask,
                out Vector3 destination))
        {
            Debug.LogWarning(
                "CustomerSpawner: Stop Point is not near a NavMesh."
            );

            return false;
        }

        GameObject selectedPrefab = GetNextCustomerPrefab();

        if (selectedPrefab == null)
        {
            Debug.LogWarning(
                "CustomerSpawner: No valid customer prefab assigned."
            );

            return false;
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
            return false;
        }

        currentCustomer = customer;
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
            return true;
        }

        StartCoroutine(
            SendCustomerToDestination(
                agent,
                spawnPosition,
                destination
            )
        );

        return true;
    }

    private GameObject GetNextCustomerPrefab()
    {
        if (shuffledCustomerPrefabs.Count == 0)
        {
            RefillShuffledCustomerPrefabs();
        }

        if (shuffledCustomerPrefabs.Count == 0)
        {
            return null;
        }

        int lastPosition = shuffledCustomerPrefabs.Count - 1;
        GameObject selectedPrefab = shuffledCustomerPrefabs[lastPosition];
        shuffledCustomerPrefabs.RemoveAt(lastPosition);
        lastSelectedPrefab = selectedPrefab;

        return selectedPrefab;
    }

    private void RefillShuffledCustomerPrefabs()
    {
        shuffledCustomerPrefabs.Clear();

        for (int i = 0; i < customerPrefabs.Length; i++)
        {
            GameObject prefab = customerPrefabs[i];
            if (prefab != null &&
                !shuffledCustomerPrefabs.Contains(prefab))
            {
                shuffledCustomerPrefabs.Add(prefab);
            }
        }

        for (int i = shuffledCustomerPrefabs.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            GameObject temporaryPrefab = shuffledCustomerPrefabs[i];
            shuffledCustomerPrefabs[i] =
                shuffledCustomerPrefabs[randomIndex];
            shuffledCustomerPrefabs[randomIndex] = temporaryPrefab;
        }

        // Danh sách được lấy từ cuối. Tránh NPC cuối vòng trước
        // xuất hiện lại ngay ở đầu vòng mới khi có từ 2 prefab trở lên.
        int nextPosition = shuffledCustomerPrefabs.Count - 1;
        if (nextPosition > 0 &&
            shuffledCustomerPrefabs[nextPosition] == lastSelectedPrefab)
        {
            GameObject temporaryPrefab =
                shuffledCustomerPrefabs[nextPosition];
            shuffledCustomerPrefabs[nextPosition] =
                shuffledCustomerPrefabs[0];
            shuffledCustomerPrefabs[0] = temporaryPrefab;
        }
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
