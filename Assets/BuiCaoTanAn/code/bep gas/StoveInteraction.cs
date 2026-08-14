using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class StoveInteraction : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private TextMeshProUGUI interactionText;


    // =========================================================
    // HITBOX
    // =========================================================

    [Header("Hitbox")]
    [Tooltip("Hitbox dùng để bật bếp và reset khi nấu fail.")]
    [SerializeField] private GameObject stoveHitbox;

    [Tooltip("Hitbox riêng của vùng chảo.")]
    [SerializeField] private GameObject panHitbox;


    // =========================================================
    // FIRE
    // =========================================================

    [Header("Fire")]
    [SerializeField] private GameObject fireObject;


    // =========================================================
    // OIL
    // =========================================================

    [Header("Oil")]
    [SerializeField] private GameObject oilObject;


    // =========================================================
    // COOKED EGG
    // =========================================================

    [Header("Cooked Egg On Pan")]
    [Tooltip("Trứng chín nằm trên chảo.")]
    [SerializeField] private GameObject cookedEggObject;


    // =========================================================
    // MINI GAME
    // =========================================================

    [Header("Mini Game")]
    [SerializeField] private EggCookingMiniGame eggCookingMiniGame;


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;


    // =========================================================
    // STATE
    // =========================================================

    private bool isLookingAtStove;
    private bool isLookingAtPan;

    private bool stoveOn;
    private bool oilAdded;

    private bool eggCooking;
    private bool eggCompleted;
    private bool cookingFailed;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ResetAllObjects();

        HideText();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        CheckLooking();

        if (!isLookingAtStove && !isLookingAtPan)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Interact();
        }
    }


    // =========================================================
    // CHECK LOOKING
    // =========================================================

    private void CheckLooking()
    {
        isLookingAtStove = false;
        isLookingAtPan = false;

        if (playerCamera == null)
        {
            HideText();
            return;
        }

        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                interactionDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide
            );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;


            // =================================================
            // PAN
            // =================================================

            if (panHitbox != null &&
                IsHit(hit.collider, panHitbox))
            {
                isLookingAtPan = true;

                UpdatePanText();

                return;
            }


            // =================================================
            // STOVE
            // =================================================

            if (stoveHitbox != null &&
                IsHit(hit.collider, stoveHitbox))
            {
                isLookingAtStove = true;

                UpdateStoveText();

                return;
            }
        }

        HideText();
    }


    // =========================================================
    // HIT CHECK
    // =========================================================

    private bool IsHit(
        Collider collider,
        GameObject target)
    {
        if (collider == null || target == null)
            return false;

        if (collider.gameObject == target)
            return true;

        if (collider.transform.IsChildOf(
            target.transform))
            return true;

        return false;
    }


    // =========================================================
    // INTERACT
    // =========================================================

    private void Interact()
    {
        if (isLookingAtPan)
        {
            InteractWithPan();
            return;
        }

        if (isLookingAtStove)
        {
            InteractWithStove();
            return;
        }
    }


    // =========================================================
    // STOVE TEXT
    // =========================================================

    private void UpdateStoveText()
    {
        if (interactionText == null)
            return;


        // FAIL
        if (cookingFailed)
        {
            interactionText.text =
                "[Click - Reset Stove]";

            interactionText.gameObject.SetActive(true);

            return;
        }


        // BẾP TẮT
        if (!stoveOn)
        {
            interactionText.text =
                "[Click - Turn On Stove]";

            interactionText.gameObject.SetActive(true);

            return;
        }

        HideText();
    }


    // =========================================================
    // PAN TEXT
    // =========================================================

    private void UpdatePanText()
    {
        if (interactionText == null)
            return;


        // MINI GAME
        if (eggCooking)
        {
            HideText();
            return;
        }


        // =====================================================
        // TRỨNG ĐÃ NẤU XONG
        // =====================================================

        if (eggCompleted)
        {
            if (playerHand != null &&
                playerHand.IsHoldingItemType(
                    PickupItem.ItemType.Plate))
            {
                // Kiểm tra cái đĩa đang cầm
                PickupItem heldItem =
                    playerHand.GetHeldItem();

                if (heldItem != null)
                {
                    PlateEgg plate =
                        heldItem.GetComponent<PlateEgg>();

                    if (plate != null &&
                        !plate.HasEgg())
                    {
                        interactionText.text =
                            "[Click - Take Egg]";

                        interactionText.gameObject.SetActive(true);

                        return;
                    }
                }
            }

            HideText();

            return;
        }


        // =====================================================
        // BẾP CHƯA BẬT
        // =====================================================

        if (!stoveOn)
        {
            HideText();
            return;
        }


        // =====================================================
        // CHƯA CÓ DẦU
        // =====================================================

        if (!oilAdded)
        {
            if (playerHand != null &&
                playerHand.IsHoldingItemType(
                    PickupItem.ItemType.OilBottle))
            {
                interactionText.text =
                    "[Click - Pour Oil]";

                interactionText.gameObject.SetActive(true);

                return;
            }

            HideText();

            return;
        }


        // =====================================================
        // CẦM TRỨNG
        // =====================================================

        if (playerHand != null &&
            playerHand.IsHoldingItemType(
                PickupItem.ItemType.Egg))
        {
            interactionText.text =
                "[Click - Crack Egg]";

            interactionText.gameObject.SetActive(true);

            return;
        }

        HideText();
    }


    // =========================================================
    // STOVE INTERACTION
    // =========================================================

    private void InteractWithStove()
    {
        // FAIL → RESET
        if (cookingFailed)
        {
            ResetStove();

            return;
        }


        // BẬT BẾP
        if (!stoveOn)
        {
            TurnOnStove();
        }
    }


    // =========================================================
    // PAN INTERACTION
    // =========================================================

    private void InteractWithPan()
    {
        // Mini game đang chạy
        if (eggCooking)
            return;


        // =====================================================
        // TRỨNG ĐÃ NẤU XONG
        // =====================================================

        if (eggCompleted)
        {
            TakeEggWithCurrentPlate();

            return;
        }


        // =====================================================
        // BẾP CHƯA BẬT
        // =====================================================

        if (!stoveOn)
            return;


        // =====================================================
        // ĐỔ DẦU
        // =====================================================

        if (!oilAdded)
        {
            if (playerHand != null &&
                playerHand.IsHoldingItemType(
                    PickupItem.ItemType.OilBottle))
            {
                AddOil();
            }

            return;
        }


        // =====================================================
        // CẦM TRỨNG
        // =====================================================

        if (playerHand != null &&
            playerHand.IsHoldingItemType(
                PickupItem.ItemType.Egg))
        {
            StartEggMiniGame();
        }
    }


    // =========================================================
    // TURN ON STOVE
    // =========================================================

    private void TurnOnStove()
    {
        stoveOn = true;
        cookingFailed = false;

        if (fireObject != null)
        {
            fireObject.SetActive(true);
        }

        UpdateStoveText();

        Debug.Log("🔥 Bếp đã bật!");
    }


    // =========================================================
    // ADD OIL
    // =========================================================

    private void AddOil()
    {
        oilAdded = true;

        if (oilObject != null)
        {
            oilObject.SetActive(true);
        }

        UpdatePanText();

        Debug.Log("🛢 Đã đổ dầu!");
    }


    // =========================================================
    // START EGG MINI GAME
    // =========================================================

    private void StartEggMiniGame()
    {
        if (eggCookingMiniGame == null)
        {
            Debug.LogError(
                "❌ Chưa gán Egg Cooking Mini Game!"
            );

            return;
        }


        // Lấy quả trứng khỏi tay và hủy sau khi đập vào chảo.
        if (playerHand != null)
        {
            PickupItem heldEgg = playerHand.TakeHeldItem();

            if (heldEgg != null)
            {
                Destroy(heldEgg.gameObject);
            }
        }


        eggCooking = true;
        eggCompleted = false;
        cookingFailed = false;


        eggCookingMiniGame.StartMiniGame();

        HideText();

        Debug.Log(
            "🥚 Trứng đã đưa vào mini game!"
        );
    }


    // =========================================================
    // MINI GAME SUCCESS
    // =========================================================

    public void EggSuccess()
    {
        eggCooking = false;
        eggCompleted = true;
        cookingFailed = false;


        // Trứng chín hiện trên chảo
        if (cookedEggObject != null)
        {
            cookedEggObject.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "❌ Cooked Egg Object chưa được gán!"
            );
        }


        HideText();

        Debug.Log(
            "🟢 Nấu thành công! " +
            "Trứng đang nằm trên chảo."
        );
    }


    // =========================================================
    // MINI GAME FAIL
    // =========================================================

    public void EggFail()
    {
        eggCooking = false;
        eggCompleted = false;
        cookingFailed = true;


        if (cookedEggObject != null)
        {
            cookedEggObject.SetActive(false);
        }


        HideText();

        Debug.Log(
            "🔴 Nấu thất bại! " +
            "Click StoveHitbox để reset."
        );
    }


    // =========================================================
    // TAKE EGG WITH CURRENT PLATE
    // =========================================================

    private void TakeEggWithCurrentPlate()
    {
        if (playerHand == null)
            return;


        // =====================================================
        // KIỂM TRA ĐANG CẦM ĐĨA
        // =====================================================

        if (!playerHand.IsHoldingItemType(
            PickupItem.ItemType.Plate))
        {
            return;
        }


        // =====================================================
        // LẤY ĐÚNG OBJECT ĐĨA ĐANG CẦM
        // =====================================================

        PickupItem heldItem =
            playerHand.GetHeldItem();

        if (heldItem == null)
        {
            Debug.LogError(
                "❌ Không tìm thấy object đang cầm!"
            );

            return;
        }


        // =====================================================
        // TÌM SCRIPT PLATE EGG
        // =====================================================

        PlateEgg plate =
            heldItem.GetComponent<PlateEgg>();

        if (plate == null)
        {
            Debug.LogError(
                "❌ Cái đĩa " +
                heldItem.gameObject.name +
                " chưa có PlateEgg.cs!"
            );

            return;
        }


        // =====================================================
        // ĐĨA ĐÃ CÓ TRỨNG
        // =====================================================

        if (plate.HasEgg())
        {
            Debug.Log(
                "❌ Đĩa " +
                heldItem.gameObject.name +
                " đã có trứng rồi!"
            );

            HideText();

            return;
        }


        // =====================================================
        // ĐẶT TRỨNG LÊN ĐĨA
        // =====================================================

        bool success =
            plate.AddEgg();

        if (!success)
        {
            return;
        }


        // =====================================================
        // TRỨNG TRÊN CHẢO BIẾN MẤT
        // =====================================================

        if (cookedEggObject != null)
        {
            cookedEggObject.SetActive(false);
        }


        // =====================================================
        // TẮT LỬA
        // =====================================================

        if (fireObject != null)
        {
            fireObject.SetActive(false);
        }


        // =====================================================
        // TẮT DẦU
        // =====================================================

        if (oilObject != null)
        {
            oilObject.SetActive(false);
        }


        // =====================================================
        // RESET BẾP
        // =====================================================

        stoveOn = false;
        oilAdded = false;
        eggCooking = false;
        eggCompleted = false;
        cookingFailed = false;


        HideText();


        Debug.Log(
            "🍳 Đã lấy trứng bằng " +
            heldItem.gameObject.name
        );

        Debug.Log(
            "🔥 Lửa OFF"
        );

        Debug.Log(
            "🛢 Dầu OFF"
        );

        Debug.Log(
            "🔄 Bếp reset!"
        );
    }


    // =========================================================
    // RESET STOVE
    // =========================================================

    public void ResetStove()
    {
        stoveOn = false;
        oilAdded = false;
        eggCooking = false;
        eggCompleted = false;
        cookingFailed = false;


        ResetAllObjects();


        // Tắt object fail của mini game
        if (eggCookingMiniGame != null)
        {
            eggCookingMiniGame.HideFailObject();
        }


        HideText();


        Debug.Log(
            "🔄 Bếp đã reset!"
        );
    }


    // =========================================================
    // RESET OBJECTS
    // =========================================================

    private void ResetAllObjects()
    {
        if (fireObject != null)
        {
            fireObject.SetActive(false);
        }


        if (oilObject != null)
        {
            oilObject.SetActive(false);
        }


        if (cookedEggObject != null)
        {
            cookedEggObject.SetActive(false);
        }
    }


    // =========================================================
    // OLD RESET NAME
    // =========================================================

    public void TurnOffStove()
    {
        ResetStove();
    }


    // =========================================================
    // GETTERS
    // =========================================================

    public bool IsStoveOn()
    {
        return stoveOn;
    }


    public bool HasOil()
    {
        return oilAdded;
    }


    public bool HasCookedEgg()
    {
        return eggCompleted;
    }


    public bool IsCooking()
    {
        return eggCooking;
    }


    // =========================================================
    // HIDE TEXT
    // =========================================================

    private void HideText()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
            return;

        Gizmos.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward *
            interactionDistance
        );
    }
}
