using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PickupItem : MonoBehaviour
{
    public enum ItemType
    {
        None,
        OilBottle,
        Egg,
        Meat,
        Vegetable,
        Pan,
        Plate,
        Other
    }

    [Header("Item Type")]
    [SerializeField] private ItemType itemType = ItemType.Other;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private TextMeshProUGUI pickupText;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupDistance = 3f;

    private bool isLookingAtItem = false;

    private static PickupItem currentItem;

    private void Update()
    {
        CheckPlayerLookingAtItem();

        if (isLookingAtItem &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (playerHand != null)
            {
                playerHand.PickupItem(this);
            }
        }
    }

    private void CheckPlayerLookingAtItem()
    {
        isLookingAtItem = false;

        if (playerCamera == null || playerHand == null)
            return;

        if (playerHand.IsHoldingItem(this))
        {
            if (currentItem == this)
            {
                HidePickupText();
                currentItem = null;
            }

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
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                isLookingAtItem = true;

                if (currentItem != this)
                {
                    if (currentItem != null)
                    {
                        currentItem.HidePickupText();
                    }

                    currentItem = this;

                    ShowPickupText();
                }

                return;
            }
        }

        if (currentItem == this)
        {
            HidePickupText();
            currentItem = null;
        }
    }

    private void ShowPickupText()
    {
        if (pickupText != null)
        {
            pickupText.text = "[E - Nhặt]";
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
        if (currentItem == this)
        {
            HidePickupText();
            currentItem = null;
        }
    }

    public void SetPickedUp()
    {
        if (currentItem == this)
        {
            HidePickupText();
            currentItem = null;
        }

        isLookingAtItem = false;
    }

    // =========================
    // ITEM TYPE
    // =========================

    public ItemType GetItemType()
    {
        return itemType;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
            return;

        Gizmos.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward * pickupDistance
        );
    }
}
