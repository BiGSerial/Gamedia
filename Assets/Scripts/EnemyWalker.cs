using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyWalker : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float speed = 2.0f;
    [Tooltip("1 = direita, -1 = esquerda")]
    [SerializeField] private int direction = -1;

    [Header("Detecção de borda/parede")]
    [SerializeField] private Transform groundCheck;        // empty na ponta do pé, um pouco à frente
    [SerializeField] private Transform wallCheck;          // empty à frente, altura do peito
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private float wallCheckDistance = 0.12f;
    [SerializeField] private LayerMask groundLayer;        // layer do chão/plataforma (Piso)

    [Header("Stomp (pisão do player)")]
    [SerializeField] private float stompYOffset = 0.1f;    // quanto acima do inimigo o player precisa estar
    [SerializeField] private float stompBounce = 8f;       // quique no player após matar
    [SerializeField] private int stompScore = 100;         // pontos ao matar

    [Header("Morte com pulo + queda")]
    [SerializeField] private bool destroyOnDeath = true;   // destruir objeto após cair
    [SerializeField] private float deathJumpY = 5.5f;      // impulso vertical ao morrer
    [SerializeField] private float deathKnockbackX = 1.5f; // empurrão horizontal ao morrer (±)
    [SerializeField] private float deathGravity = 4.5f;    // gravidade enquanto cai morto
    [SerializeField] private float deathTorque = 25f;      // rotação (0 = sem girar)
    [SerializeField] private float offscreenMargin = 1.0f; // margem abaixo da câmera para destruir
    [SerializeField] private float deathTimeout = 4.0f;    // segurança para destruir

    [Header("Dano no Player (lateral/baixo)")]
    [SerializeField] private float hitCooldown = 0.35f;    // janela antiduplo-hit
    [SerializeField] private float hitKnockbackX = 6f;     // empurrão no player ao tomar dano

    [Header("Animação (opcional)")]
    [SerializeField] private Animator animator;            // arraste o Animator aqui, se houver
    [SerializeField] private string walkBool = "walk";
    [SerializeField] private string deadTrigger = "dead";

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private bool isDead = false;

    // offsets locais originais dos sensores (pra espelhar no Flip)
    private Vector3 groundChkLocal0, wallChkLocal0;
    private float originalGravity;
    private float lastHitTime = -999f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // direção válida
        direction = direction < 0 ? -1 : 1;

        // se não foi configurado no Inspector, usa layer "Piso"
        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Piso");

        if (groundCheck) groundChkLocal0 = groundCheck.localPosition;
        if (wallCheck)   wallChkLocal0   = wallCheck.localPosition;

        UpdateSensorOffsets(); // garante posição correta ao iniciar

        originalGravity = rb.gravityScale;

        // Recomendações no Rigidbody2D (confira no Inspector):
        // rb.freezeRotation = true;
        // rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        // rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        if (isDead) return;

        Patrol();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // Move continuamente no eixo X
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }

    private void LateUpdate()
    {
        if (isDead) return;

        // força o flip depois do Animator (evita animação “desvirar”)
        if (sr) sr.flipX = (direction < 0);
    }

    private void Patrol()
    {
        // chão à frente
        bool hasGroundAhead = false;
        if (groundCheck)
        {
            RaycastHit2D groundHit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
            hasGroundAhead = groundHit.collider != null;
        }

        // parede à frente
        bool hasWallAhead = false;
        if (wallCheck)
        {
            Vector2 lookDir = new Vector2(direction, 0f);
            RaycastHit2D wallHit = Physics2D.Raycast(wallCheck.position, lookDir, wallCheckDistance, groundLayer);

            if (wallHit.collider != null)
            {
                const float minHitDist = 0.02f; // ignora hits colados/quinas
                if (wallHit.distance > minHitDist)
                {
                    float facing = Mathf.Sign(direction);
                    // normal apontando contra a direção indica parede
                    hasWallAhead = wallHit.normal.x * facing < -0.2f;
                }
            }
        }

        if (!hasGroundAhead || hasWallAhead)
            Flip();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool(walkBool, Mathf.Abs(rb.linearVelocity.x) > 0.05f);
    }

    private void Flip()
    {
        direction *= -1;

        // mantém sensores sempre à frente
        UpdateSensorOffsets();
        // o sprite vira em LateUpdate para não brigar com o Animator
    }

    private void UpdateSensorOffsets()
    {
        if (groundCheck)
            groundCheck.localPosition = new Vector3(Mathf.Abs(groundChkLocal0.x) * direction, groundChkLocal0.y, groundChkLocal0.z);

        if (wallCheck)
            wallCheck.localPosition   = new Vector3(Mathf.Abs(wallChkLocal0.x)   * direction, wallChkLocal0.y,   wallChkLocal0.z);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.collider.CompareTag("Player"))
        {
            Rigidbody2D prb = collision.collider.attachedRigidbody;

            // Critérios de "stomp": player acima + descendo
            bool playerAbove = collision.transform.position.y > (transform.position.y + stompYOffset);
            bool playerDescending = (prb != null && prb.linearVelocity.y <= 0f);

            if (playerAbove && playerDescending)
            {
                // ====== PISÃO: mata o inimigo, quica e pontua
                Die();

                if (prb != null)
                    prb.linearVelocity = new Vector2(prb.linearVelocity.x, stompBounce);

                if (GameController.instance != null)
                    GameController.instance.AddScore(10);
            }
            else
            {
                // ====== DANO NO PLAYER (lateral/por baixo)
                if (Time.time - lastHitTime >= hitCooldown)
                {
                    lastHitTime = Time.time;

                    // Knockback no player pra feedback
                    if (prb != null)
                    {
                        float dir = Mathf.Sign(prb.position.x - rb.position.x); // empurra pra fora do inimigo
                        prb.linearVelocity = new Vector2(dir * hitKnockbackX, prb.linearVelocity.y);
                    }

                    // Perde vida + respawn no início (feito pelo GameController)
                    if (GameController.instance != null)
                        GameController.instance.LoseLife();
                }
            }
        }
    }

    private void Die()
    {
        isDead = true;

        // 1) desativa colisão para atravessar o piso
        if (col) col.enabled = false;

        // 2) solta a física e configura queda “dramática”
        rb.isKinematic = false;
        rb.gravityScale = deathGravity;

        // Zera vel e aplica pulo + knockback
        float dirKnock = Random.value < 0.5f ? -1f : 1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(dirKnock * deathKnockbackX, deathJumpY), ForceMode2D.Impulse);

        // rotação leve (se quiser)
        if (Mathf.Abs(deathTorque) > 0.01f)
            rb.AddTorque(deathTorque * -dirKnock, ForceMode2D.Impulse);

        // 3) animação de morte (se houver)
        if (animator != null && !string.IsNullOrEmpty(deadTrigger))
            animator.SetTrigger(deadTrigger);

        // 4) destruir quando sair da tela (ou por timeout de segurança)
        if (destroyOnDeath)
            InvokeRepeating(nameof(CheckOffscreenAndDestroy), 0.15f, 0.15f);

        // fallback de segurança para não ficar eterno
        Destroy(gameObject, deathTimeout);
    }

    private void CheckOffscreenAndDestroy()
    {
        var cam = Camera.main;
        if (!cam) { Destroy(gameObject); return; }

        // ponto mais baixo da câmera ortográfica
        float camBottom = cam.transform.position.y - cam.orthographicSize;
        if (transform.position.y < camBottom - offscreenMargin)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 dir = new Vector3(Mathf.Sign(direction), 0f, 0f);
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * wallCheckDistance);
        }
    }
}
