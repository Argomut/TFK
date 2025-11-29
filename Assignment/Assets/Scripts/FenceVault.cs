using UnityEngine;
using System.Collections;

public class FenceVault : MonoBehaviour
{
    public Animator animator;
    public float detectDistance = 2.0f;
    public float chestHeight = 1.2f;
    public float topCheckHeight = 2f;

    CharacterController cc;
    ThirdPersonMovement movement;
    bool isVaulting;
    Vector3 vaultStartPosition;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        movement = GetComponent<ThirdPersonMovement>();
    }

    void Update()
    {
        if (isVaulting) return;
        if (!cc.isGrounded) return;
        if (Input.GetAxisRaw("Vertical") <= 0) return;

        Vector3 start = transform.position + Vector3.up * chestHeight;
        Vector3 topStart = transform.position + Vector3.up * topCheckHeight;

        Debug.DrawRay(start, transform.forward * detectDistance, Color.red);
        Debug.DrawRay(topStart, transform.forward * detectDistance, Color.green);

        if (Physics.Raycast(start, transform.forward, out RaycastHit hit, detectDistance))
        {
            if (hit.collider.CompareTag("Fence"))
            {
                if (!Physics.Raycast(topStart, transform.forward, detectDistance))
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
        movement.enabled = false;

        vaultStartPosition = transform.position;

        animator.applyRootMotion = true;
        animator.SetTrigger("Vault");

        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Vault"));

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
        movement.enabled = true;
        isVaulting = false;

        Debug.Log($"Vault completed: {vaultStartPosition} -> {transform.position}");
    }
}