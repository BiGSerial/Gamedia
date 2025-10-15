using UnityEngine;


public class Player : MonoBehaviour
{
    public float speed, jumpForce;
    [Header("Sprint")]
    public float sprintMultiplier = 2f;
    [Range(1f, 2.5f)] public float runAnimSpeed = 1.4f; // multiplicador de animaÃ§Ã£o ao correr

    public AudioSource jumpSound;
    public AudioSource footstepSound;

   

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

        bool sprintKey = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetButton("Fire3");
        bool isMoving = Mathf.Abs(movement.x) > 0.0001f;
        // Só inicia corrida no chão; no ar mantém apenas se já vinha correndo e a tecla continua pressionada
        if (isGrounded)
        {
            sprintLatched = isMoving && sprintKey;
        }
        else
        {
            sprintLatched = sprintLatched && sprintKey;
        }
        float currentSpeed = speed * (sprintLatched ? sprintMultiplier : 1f);

        Vector3 newPosition = transform.position + movement * currentSpeed * Time.deltaTime;

        // Limit the minimum x position
        if (newPosition.x < -7.194367f)
            newPosition.x = -7.194367f;

        transform.position = newPosition;

        bool walking = isMoving;
        animator.SetBool("walking", walking);
        // velocidade da animaÃ§Ã£o ao correr
        if (walking && isGrounded)
            animator.speed = (sprintLatched ? runAnimSpeed : 1f);
        else
            animator.speed = 1f;

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
    private bool sprintLatched = false;

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
                jumpSound.Play();
            }
            else if (canDoubleJump)
            {
                animator.SetBool("dblJump", true);
                animator.SetBool("jump", false);
                rb.AddForce(new Vector2(0f, jumpForce / 1.5f), ForceMode2D.Impulse);
                canDoubleJump = false;
                jumpSound.Play();
            }
        }
    }
}
 
