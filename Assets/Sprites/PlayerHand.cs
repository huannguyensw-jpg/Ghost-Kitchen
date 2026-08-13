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

    // Lưu scale ban đầu của vật phẩm
    private Vector3 originalScale;

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            DropItem();
        }
    }

    public void PickupItem(PickupItem item)
    {
        // Đang cầm đồ thì không nhặt thêm
        if (heldItem != null)
            return;

        heldItem = item;

        item.SetPickedUp();

        // Lưu scale ban đầu
        originalScale = item.transform.lossyScale;

        // Rigidbody
        Rigidbody rb = item.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Collider
        Collider col = item.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        // Đưa vật phẩm vào HandPoint
        item.transform.SetParent(handPoint, false);

        // Vị trí trên tay
        item.transform.localPosition = heldLocalPosition;

        // Góc xoay trên tay
        item.transform.localRotation =
            Quaternion.Euler(heldLocalRotation);

        // Giữ scale của vật phẩm
        SetWorldScale(item.transform, originalScale);
    }

    private void DropItem()
    {
        if (heldItem == null)
            return;

        PickupItem item = heldItem;

        heldItem = null;

        // Lưu vị trí / rotation thế giới trước khi bỏ parent
        Vector3 dropPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward * dropDistance;

        Quaternion dropRotation =
            item.transform.rotation;

        // Bỏ khỏi tay
        item.transform.SetParent(null);

        // Đặt lại vị trí
        item.transform.position = dropPosition;

        // Giữ rotation
        item.transform.rotation = dropRotation;

        // Giữ scale ban đầu
        SetWorldScale(item.transform, originalScale);

        // Bật Collider
        Collider col = item.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = true;
        }

        // Bật Rigidbody
        Rigidbody rb = item.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.AddForce(
                playerCamera.transform.forward * dropForce,
                ForceMode.Impulse
            );
        }
    }

    private void SetWorldScale(Transform target, Vector3 worldScale)
    {
        Vector3 parentScale = target.parent != null
            ? target.parent.lossyScale
            : Vector3.one;

        target.localScale = new Vector3(
            worldScale.x / parentScale.x,
            worldScale.y / parentScale.y,
            worldScale.z / parentScale.z
        );
    }

    public bool IsHoldingItem(PickupItem item)
    {
        return heldItem == item;
    }
}