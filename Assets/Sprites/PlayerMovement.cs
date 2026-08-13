using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

    [Header("Acceleration")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 30f;

    private CharacterController controller;
    private Vector3 currentVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        if (Keyboard.current == null)
            return;

        float x = 0f;
        float z = 0f;

        // A / D
        if (Keyboard.current.aKey.isPressed)
            x -= 1f;

        if (Keyboard.current.dKey.isPressed)
            x += 1f;

        // W / S
        if (Keyboard.current.wKey.isPressed)
            z += 1f;

        if (Keyboard.current.sKey.isPressed)
            z -= 1f;

        Vector2 input = new Vector2(x, z);

        // Không cho đi chéo nhanh hơn
        input = Vector2.ClampMagnitude(input, 1f);

        // Di chuyển theo hướng Player đang nhìn
        Vector3 moveDirection =
            transform.right * input.x +
            transform.forward * input.y;

        // Shift để chạy
        bool isRunning =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 targetVelocity = moveDirection * targetSpeed;

        // Có bấm WASD → tăng tốc
        if (input.sqrMagnitude > 0.01f)
        {
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            // Thả WASD → giảm tốc
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                Vector3.zero,
                deceleration * Time.deltaTime
            );
        }

        controller.Move(currentVelocity * Time.deltaTime);
    }
}