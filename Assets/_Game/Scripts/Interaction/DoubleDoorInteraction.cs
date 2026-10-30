using UnityEngine;
using UnityEngine.InputSystem;

public class DoubleDoorInteraction : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform leftHinge;
    [SerializeField] private Transform rightHinge;

    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private float openingSpeed = 90f;

    private Quaternion leftClosed;
    private Quaternion rightClosed;
    private Quaternion leftOpen;
    private Quaternion rightOpen;
    private bool isOpen;

    private void Start()
    {
        if (player == null || interactionPoint == null ||
            leftHinge == null || rightHinge == null)
        {
            Debug.LogError(
                "Preencha as quatro referências da porta no Inspector.", this);
            enabled = false;
            return;
        }

        leftClosed = leftHinge.localRotation;
        rightClosed = rightHinge.localRotation;

        leftOpen = leftClosed * Quaternion.Euler(0f, -90f, 0f);
        rightOpen = rightClosed * Quaternion.Euler(0f, 90f, 0f);
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        bool isNear = Vector3.Distance(
            player.position, interactionPoint.position
        ) <= interactionDistance;

        if (isNear && Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            isOpen = !isOpen;
        }

        float step = openingSpeed * Time.deltaTime;

        leftHinge.localRotation = Quaternion.RotateTowards(
            leftHinge.localRotation,
            isOpen ? leftOpen : leftClosed,
            step
        );

        rightHinge.localRotation = Quaternion.RotateTowards(
            rightHinge.localRotation,
            isOpen ? rightOpen : rightClosed,
            step
        );
    }
}