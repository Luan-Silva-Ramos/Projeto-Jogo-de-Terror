using UnityEngine;
using UnityEngine.InputSystem;

// Controlador simples para testar a escala e a circulacao do blockout.
[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonTestController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(0f)] private float walkSpeed = 3f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private float verticalSpeed;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null) cameraTransform = childCamera.transform;
        }
        if (cameraTransform == null)
        {
            Debug.LogError("Adicione a Main Camera como filha do Player_Test e atribua Camera Transform.", this);
            enabled = false;
            return;
        }
        pitch = Mathf.DeltaAngle(0f, cameraTransform.localEulerAngles.x);
    }

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);

        // Clicar na Game retoma o controle, sem aplicar o delta desse clique.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Application.isFocused && mouse != null && mouse.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);
            ApplyMovement(Vector2.zero);
            return;
        }

        if (mouse != null)
        {
            // Delta do mouse ja representa o deslocamento neste frame.
            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector2 input = Vector2.zero;
        if (keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        }
        ApplyMovement(Vector2.ClampMagnitude(input, 1f));
    }

    private void ApplyMovement(Vector2 input)
    {
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed = Mathf.Max(verticalSpeed + gravity * Time.deltaTime, -50f);
        Vector3 movement = (transform.right * input.x + transform.forward * input.y) * walkSpeed;
        movement.y = verticalSpeed;
        CollisionFlags collisions = controller.Move(movement * Time.deltaTime);
        if ((collisions & CollisionFlags.Below) != 0 && verticalSpeed < 0f) verticalSpeed = -2f;
        if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) SetCursorLocked(false);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
