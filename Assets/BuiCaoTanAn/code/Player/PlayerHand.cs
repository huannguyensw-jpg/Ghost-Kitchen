using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHand : MonoBehaviour
{
    [Header("Hand Point")]
    [SerializeField] private Transform handPoint;

    [Header("Held Item Position")]
    [SerializeField] private Vector3 heldLocalPosition;
    [SerializeField] private Vector3 heldLocalRotation;

    [Header("Drop")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float dropDistance = 1f;
    [SerializeField] private float dropForce = 2f;

    private PickupItem heldItem;

    private Vector3 originalScale;

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            DropItem();
        }
    }

    // =========================
    // PICKUP
    // =========================

    public void PickupItem(PickupItem item)
    {
        if (item == null)
            return;

        if (heldItem != null)
            return;

        if (handPoint == null)
        {
            Debug.LogError(
                "PlayerHand: Chưa gán Hand Point!"
            );

            return;
        }

        heldItem = item;

        item.SetPickedUp();

        // Lưu scale
        originalScale = item.transform.lossyScale;

        // Rigidbody
        Rigidbody rb =
            item.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Collider
        Collider col =
            item.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        // Đưa vào tay
        item.transform.SetParent(
            handPoint,
            false
        );

        item.transform.localPosition =
            heldLocalPosition;

        item.transform.localRotation =
            Quaternion.Euler(
                heldLocalRotation
            );

        SetWorldScale(
            item.transform,
            originalScale
        );
    }

    // =========================
    // DROP
    // =========================

    private void DropItem()
    {
        if (heldItem == null)
            return;

        if (playerCamera == null)
        {
            Debug.LogError(
                "PlayerHand: Chưa gán Player Camera!"
            );

            return;
        }

        PickupItem item = heldItem;

        heldItem = null;

        Vector3 dropPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward *
            dropDistance;

        Quaternion dropRotation =
            item.transform.rotation;

        item.transform.SetParent(null);

        item.transform.position =
            dropPosition;

        item.transform.rotation =
            dropRotation;

        SetWorldScale(
            item.transform,
            originalScale
        );

        // Collider
        Collider col =
            item.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }

        // Rigidbody
        Rigidbody rb =
            item.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.AddForce(
                playerCamera.transform.forward *
                dropForce,
                ForceMode.Impulse
            );
        }
    }

    // =========================
    // REMOVE HELD ITEM
    // =========================

    public void RemoveHeldItem()
    {
        if (heldItem == null)
            return;

        PickupItem item = heldItem;

        heldItem = null;

        item.transform.SetParent(null);

        Collider col =
            item.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }

        Rigidbody rb =
            item.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public PickupItem TakeHeldItem()
    {
        PickupItem item = heldItem;

        heldItem = null;

        return item;
    }

    // =========================
    // CHECK HELD ITEM
    // =========================

    public bool IsHoldingItem(PickupItem item)
    {
        return heldItem == item;
    }

    public bool IsHoldingAnything()
    {
        return heldItem != null;
    }

    public PickupItem GetHeldItem()
    {
        return heldItem;
    }

    public bool IsHoldingItemType(
        PickupItem.ItemType type)
    {
        if (heldItem == null)
            return false;

        return heldItem.GetItemType() == type;
    }

    // =========================
    // SCALE
    // =========================

    private void SetWorldScale(
        Transform target,
        Vector3 worldScale)
    {
        Vector3 parentScale =
            target.parent != null
                ? target.parent.lossyScale
                : Vector3.one;

        target.localScale = new Vector3(
            worldScale.x / parentScale.x,
            worldScale.y / parentScale.y,
            worldScale.z / parentScale.z
        );
    }
    public void ClearHeldItem()
    {
        heldItem = null;
    }


}