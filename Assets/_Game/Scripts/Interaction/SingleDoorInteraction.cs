using UnityEngine;
using UnityEngine.InputSystem;

public class SingleDoorInteraction : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform hinge;
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private float openingSpeed = 90f;
    [SerializeField] private float openingAngle = -90f;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen;

    private void Start()
    {
        if (player == null || interactionPoint == null || hinge == null)
        {
            Debug.LogError("Preencha Player, Interaction Point e Hinge no Inspector.", this);
            enabled = false;
            return;
        }

        closedRotation = hinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openingAngle, 0f);
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        bool isNear = Vector3.Distance(player.position, interactionPoint.position)
            <= interactionDistance;

        if (isNear && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            isOpen = !isOpen;

        hinge.localRotation = Quaternion.RotateTowards(
            hinge.localRotation,
            isOpen ? openRotation : closedRotation,
            openingSpeed * Time.deltaTime);
    }
}
