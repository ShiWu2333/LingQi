using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private bool useGravity = false;

    [Header("Dodge")]
    [SerializeField] private float dodgeDistance = 4f;
    [SerializeField] private float dodgeDuration = 0.25f;
    [SerializeField] private float dodgeStaminaCost = 20f;

    [Header("Resources")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float maxMana = 60f;
    [SerializeField] private float staminaRegenPerSecond = 25f;
    [SerializeField] private float manaRegenPerSecond = 15f;

    public float CurrentStamina => currentStamina;
    public float CurrentMana => currentMana;
    public bool IsDodging => isDodging;

    private CharacterController characterController;
    private float verticalVelocity;
    private float currentStamina;
    private float currentMana;
    private bool isDodging;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentStamina = maxStamina;
        currentMana = maxMana;
    }

    private void Update()
    {
        HandleRegen();
        HandleMovement();
        HandleDodgeInput();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        Vector3 worldInput = transform.TransformDirection(input.normalized);

        if (isDodging)
        {
            return;
        }

        Vector3 velocity = worldInput * moveSpeed;
        if (useGravity)
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            velocity.y = verticalVelocity;
        }

        characterController.Move(velocity * Time.deltaTime);

        if (worldInput.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldInput, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (useGravity && characterController.isGrounded)
        {
            verticalVelocity = 0f;
        }
    }

    private void HandleDodgeInput()
    {
        if (isDodging)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && TryConsumeStamina(dodgeStaminaCost))
        {
            Vector3 dodgeDirection = transform.forward;
            StartCoroutine(DodgeRoutine(dodgeDirection));
        }
    }

    private IEnumerator DodgeRoutine(Vector3 direction)
    {
        isDodging = true;
        float elapsed = 0f;
        float speed = dodgeDistance / Mathf.Max(0.01f, dodgeDuration);

        while (elapsed < dodgeDuration)
        {
            characterController.Move(direction.normalized * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDodging = false;
    }

    private void HandleRegen()
    {
        if (!isDodging)
        {
            currentStamina = Mathf.MoveTowards(currentStamina, maxStamina, staminaRegenPerSecond * Time.deltaTime);
        }

        currentMana = Mathf.MoveTowards(currentMana, maxMana, manaRegenPerSecond * Time.deltaTime);
    }

    public bool TryConsumeStamina(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentStamina < amount)
        {
            return false;
        }

        currentStamina -= amount;
        return true;
    }

    public bool TryConsumeMana(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentMana < amount)
        {
            return false;
        }

        currentMana -= amount;
        return true;
    }
}
