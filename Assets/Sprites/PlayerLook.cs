using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;

    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 2.5f;

    [Header("Vertical Look")]
    [SerializeField] private float minLookAngle = -89f;
    [SerializeField] private float maxLookAngle = 89f;

    private float cameraPitch = 0f;

    private void Start()
    {
        // Khóa chuột vào giữa màn hình
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Player quay trái/phải
        transform.Rotate(Vector3.up * mouseX);

        // Camera quay lên/xuống
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLookAngle, maxLookAngle);

        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }
}