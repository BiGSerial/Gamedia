using UnityEngine;


public class Player : MonoBehaviour
{
    public float speed, jumpForce;

    private Rigidbody2D rb;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Move();
        Jump();
    }

    void Move()
    {
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0f, 0f);
        Vector3 newPosition = transform.position + movement * speed * Time.deltaTime;

        // Limit the minimum x position
        if (newPosition.x < -7.194367f)
            newPosition.x = -7.194367f;

        transform.position = newPosition;

        if (movement != Vector3.zero)
            animator.SetBool("walking", true);
        else
            animator.SetBool("walking", false);

        // Flip the sprite based on movement direction
        if (Input.GetAxis("Horizontal") < 0)
            GetComponent<SpriteRenderer>().flipX = true;  // Left direction - flipped
        else if (Input.GetAxis("Horizontal") > 0)
            GetComponent<SpriteRenderer>().flipX = false;  // Right direction - normal

        // Detect falling
        if (!isGrounded && rb.linearVelocity.y < -0.1f)
        {
            animator.SetBool("fall", true);
            animator.SetBool("dblJump", false);
            animator.SetBool("jump", false);
        }
      
    }

    private bool canDoubleJump = false;
    private bool isGrounded = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Piso"))
        {
            isGrounded = true;
            canDoubleJump = false;
            animator.SetBool("fall", false);
            
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Piso"))
        {
            animator.SetBool("jump", true);
            isGrounded = false;
        }
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
                isGrounded = false;
                canDoubleJump = true;
                animator.SetBool("jump", true);
            }
            else if (canDoubleJump)
            {
                animator.SetBool("dblJump", true);
                animator.SetBool("jump", false);
                rb.AddForce(new Vector2(0f, jumpForce / 1.5f), ForceMode2D.Impulse);
                canDoubleJump = false;
            }
        }
    }
}
 