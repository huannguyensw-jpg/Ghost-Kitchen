using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PickupItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private TextMeshProUGUI pickupText;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupDistance = 3f;

    private bool isLookingAtItem = false;

    private void Update()
    {
        CheckPlayerLookingAtItem();

        if (isLookingAtItem &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            playerHand.PickupItem(this);
        }
    }

    private void CheckPlayerLookingAtItem()
    {
        isLookingAtItem = false;

        // Không cho hiện chữ nếu đang cầm vật phẩm này
        if (playerHand.IsHoldingItem(this))
        {
            HidePickupText();
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            pickupDistance
        ))
        {
            // Kiểm tra ray có trúng chính vật phẩm này không
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                isLookingAtItem = true;
                ShowPickupText();
                return;
            }
        }

        HidePickupText();
    }

    private void ShowPickupText()
    {
        if (pickupText != null)
        {
            pickupText.text = "[E (Nhặt)]";
            pickupText.gameObject.SetActive(true);
        }
    }

    private void HidePickupText()
    {
        if (pickupText != null)
        {
            pickupText.gameObject.SetActive(false);
        }
    }

    public void HideText()
    {
        HidePickupText();
    }

    public void SetPickedUp()
    {
        HidePickupText();
        isLookingAtItem = false;
    }
}