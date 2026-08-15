using UnityEngine;
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
            interactionText.gameObject.SetActive(false);

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

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance))
        {
            // Hit chính object
            if (hit.collider.gameObject == gameObject)
            {
                isLookingAtThis = true;
            }
            // Hoặc hit Collider của object con
            else if (
                hit.collider.transform.IsChildOf(transform)
            )
            {
                isLookingAtThis = true;
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