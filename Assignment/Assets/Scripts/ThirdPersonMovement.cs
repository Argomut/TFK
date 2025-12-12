using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSmoothTime = 0.1f;

    [Header("Audio")]
    public AudioSource footstepSource;
    public float walkPitch = 1f;
    public float runPitch = 1.2f;

    [Header("Vault Audio")]
    public AudioSource vaultAudioSource;
    public AudioClip vaultSound;
    public float vaultVolume = 1f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Vault Settings")]
    public float detectDistance = 2.0f;
    public float chestHeight = 1.2f;
    public float topCheckHeight = 2f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float stamina;
    public float staminaDrainPerSecond = 15f;
    public float staminaRegenPerSecond = 10f;
    public float staminaRegenDelay = 1.0f;

    [Header("UI")]
    public Slider staminaBar;

    [Header("References")]
    public Animator animator;

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 velocity;
    private float rotationVelocity;

    bool isVaulting;
    Vector3 vaultStartPosition;

    private float staminaRegenTimer = 0f;
    private bool isExhausted = false;

    private Material staminaMat;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
        stamina = maxStamina;
        staminaMat = staminaBar.fillRect.GetComponent<Image>().material;
    }

    void Update()
    {
        if (isVaulting) return;

        HandleVaultDetection();
        HandleMovement();

        staminaBar.value = stamina;

        staminaBar.value = Mathf.Lerp(staminaBar.value, stamina, Time.deltaTime * 10f);

        float staminaPercent = stamina / maxStamina;
        staminaMat.SetFloat("_Stamina", staminaPercent);

    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        // ---------- RUNNING ----------
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
        bool isRunning = false;

        if (wantsToRun && stamina > 0f && !isExhausted)
        {
            isRunning = true;
            stamina -= staminaDrainPerSecond * Time.deltaTime;
            staminaRegenTimer = 0f;

            if (stamina <= 0f)
            {
                stamina = 0f;
                isExhausted = true; 
            }
        }
        else
        {
            isRunning = false;
        }

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // ------------------ STAMINA REGEN ------------------
        if (!isRunning && stamina < maxStamina)
        {
            staminaRegenTimer += Time.deltaTime;

            if (staminaRegenTimer >= staminaRegenDelay)
            {
                stamina += staminaRegenPerSecond * Time.deltaTime;
                stamina = Mathf.Clamp(stamina, 0f, maxStamina);

                if (stamina > maxStamina * 0.2f)
                    isExhausted = false; 
            }
        }

        // ---------- GROUND CHECK ----------
        bool isGrounded = IsGrounded();

        // ---------- FOOTSTEP SOUND ----------
        bool isMoving = inputDir.magnitude >= 0.1f;

        if (!isVaulting && isGrounded && isMoving && !animator.GetBool("IsJumping"))
        {
            if (!footstepSource.isPlaying)
                footstepSource.Play();

            footstepSource.pitch = isRunning ? runPitch : walkPitch;
        }
        else
        {
            if (footstepSource.isPlaying)
                footstepSource.Stop();
        }

        // Animator update
        animator.SetBool("IsRunning", isRunning);

        // ---------- ROTATION + MOVE ----------
        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir * currentSpeed * Time.deltaTime);
        }

        // ---------- JUMPING ----------
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            animator.SetBool("IsJumping", false);
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetBool("IsJumping", true);

            if (footstepSource.isPlaying)
                footstepSource.Stop();
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        animator.SetFloat("Speed", inputDir.magnitude);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position,
            Vector3.down,
            controller.height / 2 + groundCheckDistance,
            groundMask);
    }

    // ---------- VAULTING SYSTEM ----------
    void HandleVaultDetection()
    {
        if (!controller.isGrounded) return;
        if (Input.GetAxisRaw("Vertical") <= 0) return;

        Vector3 chestPos = transform.position + Vector3.up * chestHeight;
        Vector3 topPos = transform.position + Vector3.up * topCheckHeight;

        Debug.DrawRay(chestPos, transform.forward * detectDistance, Color.red);
        Debug.DrawRay(topPos, transform.forward * detectDistance, Color.green);

        if (Physics.Raycast(chestPos, transform.forward, out RaycastHit hit, detectDistance))
        {
            if (hit.collider.CompareTag("Fence"))
            {
                if (!Physics.Raycast(topPos, transform.forward, detectDistance))
                {
                    StartCoroutine(VaultOver(hit));
                }
            }
        }
    }

    void OnAnimatorMove()
    {
        if (isVaulting && animator.applyRootMotion)
        {
            transform.position = animator.rootPosition;
            transform.rotation = animator.rootRotation;
        }
    }

    IEnumerator VaultOver(RaycastHit hit)
    {
        isVaulting = true;

        vaultStartPosition = transform.position;

        velocity = Vector3.zero;
        animator.applyRootMotion = true;

        animator.SetTrigger("Vault");

        // Play vault sound here
        if (vaultAudioSource && vaultSound)
        {
            vaultAudioSource.PlayOneShot(vaultSound, vaultVolume);
        }

        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("Vault"));

        yield return new WaitForSeconds(0.1f);

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
        {
            yield return null;
        }

        if (animator.applyRootMotion)
        {
            transform.position = animator.rootPosition;
            transform.rotation = animator.rootRotation;
        }

        yield return new WaitForSeconds(0.1f);

        animator.applyRootMotion = false;
        isVaulting = false;

        Debug.Log($"Vault completed: {vaultStartPosition} -> {transform.position}");
    }
}
