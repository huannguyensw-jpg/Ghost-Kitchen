using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class EggCookingMiniGame : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Mini Game UI")]
    [SerializeField] private GameObject miniGamePanel;

    [Header("Track")]
    [SerializeField] private RectTransform track;

    [Header("Moving Bar")]
    [SerializeField] private RectTransform movingBar;

    [Header("Success Zone")]
    [Tooltip("Vùng thành công trên Track. Có thể chỉnh vị trí và kích thước RectTransform trong Inspector.")]
    [FormerlySerializedAs("centerZone")]
    public RectTransform successZone;


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 500f;


    // =========================================================
    // FAIL OBJECT
    // =========================================================

    [Header("Fail Object")]
    [SerializeField] private GameObject failObject;


    // =========================================================
    // STOVE
    // =========================================================

    [Header("Stove")]
    [SerializeField] private StoveInteraction stoveInteraction;


    // =========================================================
    // PLAYER MOVEMENT
    // =========================================================

    [Header("Player Movement")]
    [Tooltip("Kéo script di chuyển Player vào đây.")]
    [SerializeField] private MonoBehaviour playerMovement;


    // =========================================================
    // STATE
    // =========================================================

    private bool isPlaying;
    private bool movingRight;

    // Không nhận lại cú click vừa dùng để đặt trứng vào chảo.
    private int startFrame;

    private float minX;
    private float maxX;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false);
        }

        if (failObject != null)
        {
            failObject.SetActive(false);
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!isPlaying)
            return;


        MoveBar();


        if (Time.frameCount == startFrame &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log(
                "[EggMiniGame] Ignored the crack-egg click on start frame " +
                startFrame + "."
            );
        }


        if (Time.frameCount > startFrame &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log(
                "[EggMiniGame] Stop click received at frame " +
                Time.frameCount +
                ", bar local X = " +
                movingBar.localPosition.x
            );

            StopBar();
        }
    }


    // =========================================================
    // START MINI GAME
    // =========================================================

    public void StartMiniGame()
    {
        if (miniGamePanel == null)
        {
            Debug.LogError(
                "❌ Mini Game Panel chưa được gán!"
            );

            return;
        }

        if (track == null)
        {
            Debug.LogError(
                "❌ Track chưa được gán!"
            );

            return;
        }

        if (movingBar == null)
        {
            Debug.LogError(
                "❌ Moving Bar chưa được gán!"
            );

            return;
        }

        if (successZone == null)
        {
            Debug.LogError(
                "❌ Success Zone chưa được gán!"
            );

            return;
        }


        // Hiện mini game
        miniGamePanel.SetActive(true);


        // Tắt fail object cũ
        if (failObject != null)
        {
            failObject.SetActive(false);
        }


        // Khóa player
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }


        // Tính biên
        CalculateMovementLimits();


        // Đưa thanh về trái
        Vector3 pos =
            movingBar.localPosition;

        pos.x = minX;

        movingBar.localPosition =
            pos;


        movingRight = true;
        startFrame = Time.frameCount;
        isPlaying = true;


        Debug.Log(
            "[EggMiniGame] Started at frame " +
            startFrame +
            ". Waiting for the next click."
        );

        Debug.Log(
            "[EggMiniGame] Movement range: min X = " +
            minX +
            ", max X = " +
            maxX
        );
    }


    // =========================================================
    // CALCULATE LIMIT
    // =========================================================

    private void CalculateMovementLimits()
    {
        RectTransform parentRect =
            movingBar.parent as RectTransform;

        if (parentRect == null)
        {
            Debug.LogError(
                "❌ MovingBar phải nằm trong RectTransform!"
            );

            return;
        }


        // Track corners
        Vector3[] trackCorners =
            new Vector3[4];

        track.GetWorldCorners(
            trackCorners
        );


        Vector3 leftWorld =
            trackCorners[0];

        Vector3 rightWorld =
            trackCorners[3];


        Vector3 leftLocal =
            parentRect.InverseTransformPoint(
                leftWorld
            );

        Vector3 rightLocal =
            parentRect.InverseTransformPoint(
                rightWorld
            );


        float trackLeft =
            leftLocal.x;

        float trackRight =
            rightLocal.x;


        // Bar corners
        Vector3[] barCorners =
            new Vector3[4];

        movingBar.GetWorldCorners(
            barCorners
        );


        Vector3 barLeftLocal =
            parentRect.InverseTransformPoint(
                barCorners[0]
            );

        Vector3 barRightLocal =
            parentRect.InverseTransformPoint(
                barCorners[3]
            );


        float barWidth =
            Mathf.Abs(
                barRightLocal.x -
                barLeftLocal.x
            );


        float halfBarWidth =
            barWidth * 0.5f;


        minX =
            trackLeft +
            halfBarWidth;

        maxX =
            trackRight -
            halfBarWidth;


        if (minX > maxX)
        {
            float temp =
                minX;

            minX =
                maxX;

            maxX =
                temp;
        }
    }


    // =========================================================
    // MOVE BAR
    // =========================================================

    private void MoveBar()
    {
        if (movingBar == null)
            return;

        if (maxX <= minX)
            return;


        Vector3 pos =
            movingBar.localPosition;


        float direction =
            movingRight
                ? 1f
                : -1f;


        pos.x +=
            direction *
            moveSpeed *
            Time.deltaTime;


        if (pos.x >= maxX)
        {
            pos.x = maxX;
            movingRight = false;
        }


        if (pos.x <= minX)
        {
            pos.x = minX;
            movingRight = true;
        }


        movingBar.localPosition =
            pos;
    }


    // =========================================================
    // STOP BAR
    // =========================================================

    private void StopBar()
    {
        if (!isPlaying)
            return;


        isPlaying = false;


        bool success =
            IsBarInsideSuccessZone();


        Debug.Log(
            "[EggMiniGame] Result evaluated: " +
            (success ? "SUCCESS" : "FAIL")
        );


        if (success)
        {
            Success();
        }
        else
        {
            Fail();
        }
    }


    // =========================================================
    // CHECK CENTER
    // =========================================================

    private bool IsBarInsideSuccessZone()
    {
        Vector3[] barCorners =
            new Vector3[4];

        Vector3[] zoneCorners =
            new Vector3[4];


        movingBar.GetWorldCorners(
            barCorners
        );

        successZone.GetWorldCorners(
            zoneCorners
        );


        float barLeft = Mathf.Min(
            barCorners[0].x,
            barCorners[3].x
        );

        float barRight = Mathf.Max(
            barCorners[0].x,
            barCorners[3].x
        );

        float barCenter =
            (barLeft + barRight) * 0.5f;


        float zoneLeft = Mathf.Min(
            zoneCorners[0].x,
            zoneCorners[3].x
        );

        float zoneRight = Mathf.Max(
            zoneCorners[0].x,
            zoneCorners[3].x
        );


        // Chỉ cần tâm của Moving Bar nằm trong Success Zone.
        // Cách này ổn định khi hai RectTransform có kích thước khác nhau.
        bool success =
            barCenter >= zoneLeft &&
            barCenter <= zoneRight;


        Debug.Log(
            "[EggMiniGame] Bar world range: " +
            barLeft +
            " -> " +
            barRight +
            ", center = " +
            barCenter
        );

        Debug.Log(
            "[EggMiniGame] Success zone world range: " +
            zoneLeft +
            " -> " +
            zoneRight
        );

        Debug.Log(
            "[EggMiniGame] Bar center inside success zone = " +
            success
        );


        return success;
    }


    // =========================================================
    // SUCCESS
    // =========================================================

    private void Success()
    {
        Debug.Log(
            "[EggMiniGame] SUCCESS - showing the cooked egg on the pan."
        );


        EnablePlayer();


        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false);
        }


        // Đảm bảo hiệu ứng thất bại cũ không còn hiện khi đã thành công.
        if (failObject != null)
        {
            failObject.SetActive(false);
        }


        // Báo cho StoveInteraction
        // Trứng sẽ hiện trên CHẢO
        if (stoveInteraction != null)
        {
            stoveInteraction.EggSuccess();
        }
    }


    // =========================================================
    // FAIL
    // =========================================================

    private void Fail()
    {
        Debug.Log(
            "[EggMiniGame] FAIL - showing the fail object."
        );


        EnablePlayer();


        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false);
        }


        if (failObject != null)
        {
            failObject.SetActive(true);
        }


        // StoveInteraction giữ trạng thái FAIL
        // Chờ click StoveHitbox để reset
        if (stoveInteraction != null)
        {
            stoveInteraction.EggFail();
        }
    }


    // =========================================================
    // PLAYER
    // =========================================================

    private void EnablePlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }


    // =========================================================
    // PUBLIC
    // =========================================================

    public bool IsPlaying()
    {
        return isPlaying;
    }


    // =========================================================
    // CANCEL
    // =========================================================

    public void CancelMiniGame()
    {
        isPlaying = false;

        EnablePlayer();


        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false);
        }
    }


    // =========================================================
    // FAIL OBJECT
    // =========================================================

    public void HideFailObject()
    {
        if (failObject != null)
        {
            failObject.SetActive(false);
        }
    }
}
