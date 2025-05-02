using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetMovement(float speed)
    {
        _animator.SetFloat("Speed", speed);
    }

    public void SetJump(bool isJumping)
    {
        _animator.SetBool("Jump", isJumping);
    }

    public void SetGrounding(bool isGrounded)
    {
        _animator.SetBool("Grounded", isGrounded);
    }

    public void SetCrouch(bool isCrouching)
    {
        _animator.SetBool("Crouch", isCrouching);
    }
}