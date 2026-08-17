using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ClickReadImage : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;

    [Header("UI")]
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private GameObject imageCanvas;

    private bool isLookingAtThis;
    private bool imageIsOpen;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }

        if (imageCanvas != null)
            imageCanvas.SetActive(false);
    }

    private void Update()
    {
        CheckLook();

        // =========================================
        // CLICK
        // =========================================

        if (Input.GetMouseButtonDown(0))
        {
            // Đang mở ảnh → click lần 2 để tắt
            if (imageIsOpen)
            {
                CloseImage();
                return;
            }

            // Chưa mở → phải đang nhìn vào Hitbox
            if (isLookingAtThis)
            {
                OpenImage();
            }
        }
    }

    // =========================================
    // CHECK TÂM NHÌN VÀO HITBOX
    // =========================================

    private void CheckLook()
    {
        isLookingAtThis = false;

        if (playerCamera == null)
        {
            HideInteractionText();
            return;
        }

        // Nếu ảnh đang mở thì không cần hiện
        // [Click để đọc]
        if (imageIsOpen)
        {
            HideInteractionText();
            return;
        }

        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == gameObject)
            {
                isLookingAtThis = true;
                break;
            }

            if (
                hit.collider.transform.IsChildOf(transform)
            )
            {
                isLookingAtThis = true;
                break;
            }
        }

        if (isLookingAtThis)
        {
            ShowInteractionText();
        }
        else
        {
            HideInteractionText();
        }
    }

    // =========================================
    // HIỆN [CLICK ĐỂ ĐỌC]
    // =========================================

    private void ShowInteractionText()
    {
        if (interactionText == null)
            return;

        interactionText.text = "[Click để đọc]";
        interactionText.gameObject.SetActive(true);
    }

    // =========================================
    // ẨN TEXT
    // =========================================

    private void HideInteractionText()
    {
        if (interactionText == null)
            return;

        interactionText.gameObject.SetActive(false);
    }

    // =========================================
    // MỞ ẢNH
    // =========================================

    private void OpenImage()
    {
        if (imageCanvas == null)
        {
            Debug.LogWarning(
                "ClickReadImage: Chưa gán Image Canvas!",
                this
            );

            return;
        }

        imageIsOpen = true;

        imageCanvas.SetActive(true);

        HideInteractionText();

        Debug.Log(
            "ClickReadImage: Đã mở ảnh.",
            this
        );
    }

    // =========================================
    // TẮT ẢNH
    // =========================================

    private void CloseImage()
    {
        if (imageCanvas == null)
            return;

        imageIsOpen = false;

        imageCanvas.SetActive(false);

        Debug.Log(
            "ClickReadImage: Đã tắt ảnh.",
            this
        );
    }
}

/// <summary>
/// Keeps a hover interaction label above the mouse cursor/crosshair.
/// Added at runtime so the same behaviour works with shared UI labels
/// in every scene.
/// </summary>
public sealed class CursorFollowInteractionText : MonoBehaviour
{
    private const float CursorGap = 16f;
    private const float ScreenPadding = 12f;

    private RectTransform rectTransform;
    private TMP_Text label;
    private Canvas rootCanvas;

    public static void Ensure(TMP_Text interactionText)
    {
        if (interactionText == null)
            return;

        if (interactionText.GetComponent<CursorFollowInteractionText>() == null)
        {
            interactionText.gameObject.AddComponent<CursorFollowInteractionText>();
        }
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        label = GetComponent<TMP_Text>();

        Canvas canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;
    }

    private void LateUpdate()
    {
        if (rectTransform == null || label == null || rootCanvas == null)
            return;

        Vector2 cursorPosition = GetCursorScreenPosition();
        Vector2 size = rectTransform.rect.size;

        size.x = Mathf.Max(size.x, label.preferredWidth);
        size.y = Mathf.Max(size.y, label.preferredHeight);

        Vector2 screenScale = new Vector2(
            Mathf.Abs(rectTransform.lossyScale.x),
            Mathf.Abs(rectTransform.lossyScale.y)
        );

        float width = size.x * screenScale.x;
        float height = size.y * screenScale.y;

        // Put the label's bottom edge above the cursor instead of placing
        // its centre directly on top of the cursor.
        Vector2 desiredPosition = new Vector2(
            cursorPosition.x + (rectTransform.pivot.x - 0.5f) * width,
            cursorPosition.y + CursorGap + height * rectTransform.pivot.y
        );

        desiredPosition.x = Mathf.Clamp(
            desiredPosition.x,
            ScreenPadding + width * rectTransform.pivot.x,
            Screen.width - ScreenPadding - width * (1f - rectTransform.pivot.x)
        );

        desiredPosition.y = Mathf.Clamp(
            desiredPosition.y,
            ScreenPadding + height * rectTransform.pivot.y,
            Screen.height - ScreenPadding - height * (1f - rectTransform.pivot.y)
        );

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform.parent as RectTransform,
                desiredPosition,
                uiCamera,
                out Vector3 worldPosition))
        {
            rectTransform.position = worldPosition;
        }
    }

    private static Vector2 GetCursorScreenPosition()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Input.mousePosition;
    }
}
