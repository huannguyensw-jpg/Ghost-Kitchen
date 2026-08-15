using System.Collections.Generic;
using UnityEngine;

public class DeleteEggPlate : MonoBehaviour
{
    [Header("Delete Settings")]
    [SerializeField] private float deleteTime = 3f;

    [Header("Player")]
    [SerializeField] private PlayerHand playerHand;

    // Những dĩa đang ở trong vùng DeleteEgg
    private Dictionary<PickupItem, float> platesInZone =
        new Dictionary<PickupItem, float>();

    private void Update()
    {
        if (platesInZone.Count == 0)
            return;

        // Lưu danh sách để tránh lỗi khi Destroy trong lúc foreach
        List<PickupItem> plates =
            new List<PickupItem>(platesInZone.Keys);

        foreach (PickupItem plate in plates)
        {
            if (plate == null)
            {
                platesInZone.Remove(plate);
                continue;
            }

            // Dĩa không còn trứng
            if (!HasVisibleEgg(plate))
            {
                platesInZone.Remove(plate);
                continue;
            }

            // Nếu đang cầm dĩa thì KHÔNG đếm
            if (IsHoldingPlate(plate))
            {
                platesInZone[plate] = 0f;
                continue;
            }

            // Tăng timer riêng cho dĩa này
            platesInZone[plate] += Time.deltaTime;

            // Đủ 3 giây
            if (platesInZone[plate] >= deleteTime)
            {
                DeletePlate(plate);
            }
        }
    }

    // =========================================================
    // DĨA ĐI VÀO VÙNG
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        PickupItem plate = FindPlate(other);

        if (plate == null)
            return;

        // Chỉ nhận dĩa có trứng
        if (!HasVisibleEgg(plate))
            return;

        // Nếu đã có trong danh sách thì không thêm lại
        if (!platesInZone.ContainsKey(plate))
        {
            platesInZone.Add(plate, 0f);

            Debug.Log(
                "🍳 Dĩa trứng vào DeleteEgg: " +
                plate.name
            );
        }
    }

    // =========================================================
    // DĨA RA KHỎI VÙNG
    // =========================================================

    private void OnTriggerExit(Collider other)
    {
        PickupItem plate = FindPlate(other);

        if (plate == null)
            return;

        if (platesInZone.ContainsKey(plate))
        {
            platesInZone.Remove(plate);

            Debug.Log(
                "Dĩa ra khỏi DeleteEgg: " +
                plate.name
            );
        }
    }

    // =========================================================
    // TÌM DĨA
    // =========================================================

    private PickupItem FindPlate(Collider other)
    {
        if (other == null)
            return null;

        // Collider nằm trên chính object dĩa
        PickupItem plate =
            other.GetComponent<PickupItem>();

        if (plate != null &&
            plate.GetItemType() ==
            PickupItem.ItemType.Plate)
        {
            return plate;
        }

        // Collider nằm trên object con của dĩa
        plate =
            other.GetComponentInParent<PickupItem>();

        if (plate != null &&
            plate.GetItemType() ==
            PickupItem.ItemType.Plate)
        {
            return plate;
        }

        return null;
    }

    // =========================================================
    // KIỂM TRA DĨA CÓ TRỨNG
    // =========================================================

    private bool HasVisibleEgg(PickupItem plate)
    {
        if (plate == null)
            return false;

        Transform[] children =
            plate.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == plate.transform)
                continue;

            string objectName =
                child.name.ToLower();

            // Tìm object có tên chứa Egg
            if (objectName.Contains("egg"))
            {
                if (child.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // =========================================================
    // KIỂM TRA ĐANG CẦM
    // =========================================================

    private bool IsHoldingPlate(PickupItem plate)
    {
        if (playerHand == null)
            return false;

        return playerHand.IsHoldingItem(plate);
    }

    // =========================================================
    // XÓA DĨA
    // =========================================================

    private void DeletePlate(PickupItem plate)
    {
        if (plate == null)
            return;

        // Kiểm tra lại lần cuối
        if (!HasVisibleEgg(plate))
        {
            platesInZone.Remove(plate);
            return;
        }

        // Tuyệt đối không xóa dĩa đang cầm
        if (IsHoldingPlate(plate))
        {
            platesInZone[plate] = 0f;
            return;
        }

        Debug.Log(
            "🗑️ Xóa dĩa trứng: " +
            plate.name
        );

        platesInZone.Remove(plate);

        Destroy(plate.gameObject);
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        Collider col =
            GetComponent<Collider>();

        if (col == null)
            return;

        Gizmos.color = Color.red;

        if (col is BoxCollider box)
        {
            Gizmos.matrix =
                transform.localToWorldMatrix;

            Gizmos.DrawWireCube(
                box.center,
                box.size
            );
        }
    }
}