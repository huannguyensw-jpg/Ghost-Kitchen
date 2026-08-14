using UnityEngine;

public class PlateEgg : MonoBehaviour
{
    [Header("Egg On This Plate")]
    [SerializeField] private GameObject eggObject;

    private bool hasEgg = false;

    private void Awake()
    {
        // Mỗi đĩa bắt đầu không có trứng
        hasEgg = false;

        if (eggObject != null)
        {
            eggObject.SetActive(false);
        }
    }

    // =====================================================
    // CHECK
    // =====================================================

    public bool HasEgg()
    {
        return hasEgg;
    }

    // =====================================================
    // ADD EGG
    // =====================================================

    public bool AddEgg()
    {
        // Đĩa này đã có trứng
        if (hasEgg)
        {
            Debug.Log(
                "❌ Đĩa " +
                gameObject.name +
                " đã có trứng rồi!"
            );

            return false;
        }

        if (eggObject == null)
        {
            Debug.LogError(
                "❌ PlateEgg: Chưa gán Egg Object cho " +
                gameObject.name
            );

            return false;
        }

        hasEgg = true;

        eggObject.SetActive(true);

        Debug.Log(
            "🍳 Đã đặt trứng lên " +
            gameObject.name
        );

        return true;
    }

    // =====================================================
    // REMOVE EGG
    // =====================================================

    public void RemoveEgg()
    {
        hasEgg = false;

        if (eggObject != null)
        {
            eggObject.SetActive(false);
        }

        Debug.Log(
            "🍳 Đã lấy trứng khỏi " +
            gameObject.name
        );
    }

    // =====================================================
    // RESET
    // =====================================================

    public void ResetPlate()
    {
        hasEgg = false;

        if (eggObject != null)
        {
            eggObject.SetActive(false);
        }
    }
}