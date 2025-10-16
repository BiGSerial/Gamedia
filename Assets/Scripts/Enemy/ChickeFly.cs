using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ChickeFly : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField] private bool isDead = false;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathJumpY = 5.5f;
    [SerializeField] private float deathKnockbackX = 1.5f;
    [SerializeField] private float deathGravity = 4.5f;
    [SerializeField] private float deathTorque = 25f;
    [SerializeField] private float offscreenMargin = 1.0f;
    [SerializeField] private float deathTimeout = 4.0f;
    [SerializeField] private Animator animator;           // opcional
    [SerializeField] private string deadTrigger = "dead"; // opcional
    public AudioSource deathSound;                        // opcional

    [Header("Stomp (pulo na cabeça)")]
    [SerializeField] private float stompBounce = 8f;
    [SerializeField] private int stompScore = 10;

    [Header("Trigger Settings")]
    [SerializeField] private BoxCollider2D headTrigger;
    [SerializeField] private float triggerHeight = 0.5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float changeDirectionTime = 1f;
    [SerializeField] private float verticalDistance = 3f; // Distância total de movimento vertical
    
    private float directionTimer;
    private int currentDirection = 1; // 1 para cima, -1 para baixo
    private float startY;
    private float minHeight;
    private float maxHeight;

    private Rigidbody2D rb;
    private Collider2D[] allColliders;
    private float originalGravity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = false; // permitimos girar na morte
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        originalGravity = rb.gravityScale;

        // While alive, ignore gravity and move manually (flying behavior)
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        allColliders = GetComponentsInChildren<Collider2D>(true);
    }
    
    void Start()
    {
        CreateHeadTrigger();
        directionTimer = changeDirectionTime;
        
        // Define os limites baseados na posição inicial e na distância configurada
        startY = transform.position.y;
        minHeight = startY - (verticalDistance / 2f);
        maxHeight = startY + (verticalDistance / 2f);
    }

    void Update()
    {
        if (!isDead)
        {
            MoveVertically();
        }
    }
    
    void MoveVertically()
    {
        // Move o personagem verticalmente
        transform.Translate(Vector3.up * currentDirection * moveSpeed * Time.deltaTime);
        
        // Diminui o timer
        directionTimer -= Time.deltaTime;
        
        // Muda de direção aleatoriamente ou quando atinge os limites
        if (directionTimer <= 0 || transform.position.y >= maxHeight || transform.position.y <= minHeight)
        {
            currentDirection = Random.Range(0, 2) == 0 ? 1 : -1;
            directionTimer = Random.Range(changeDirectionTime * 0.5f, changeDirectionTime * 1.5f);
        }
    }
    
    void CreateHeadTrigger()
    {
        if (headTrigger != null) return;
        // Cria um trigger na cabeça do inimigo
        GameObject triggerObj = new GameObject("HeadTrigger");
        triggerObj.transform.SetParent(transform);
        triggerObj.transform.localPosition = new Vector3(0, triggerHeight, 0);
        
        headTrigger = triggerObj.AddComponent<BoxCollider2D>();
        headTrigger.isTrigger = true;
        headTrigger.size = new Vector2(0.8f, 0.3f);
        
        triggerObj.tag = "EnemyHead";
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isDead)
        {
            // Verifica se o player está caindo (velocidade Y negativa)
            Rigidbody2D playerRb = collision.attachedRigidbody ? collision.attachedRigidbody : collision.GetComponent<Rigidbody2D>();
            if (playerRb != null && playerRb.linearVelocity.y <= 0f)
            {
                // Sequência tipo EnemyWalker: morre, player quica e pontua
                Die();
                if (deathSound) deathSound.Play();

                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounce);
                if (GameController.instance != null)
                    GameController.instance.AddScore(stompScore);
            }
        }
    }

    // Fallback: also allow stomp detection via solid collision
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.collider.CompareTag("Player"))
        {
            Rigidbody2D prb = collision.collider.attachedRigidbody;
            bool playerAbove = collision.transform.position.y > (transform.position.y + 0.05f);
            bool playerDescending = (prb != null && prb.linearVelocity.y <= 0f);

            if (playerAbove && playerDescending)
            {
                Die();
                if (deathSound) deathSound.Play();

                if (prb != null)
                    prb.linearVelocity = new Vector2(prb.linearVelocity.x, stompBounce);

                if (GameController.instance != null)
                    GameController.instance.AddScore(stompScore);
            }
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1) desativa colisão para atravessar tudo
        if (allColliders != null)
        {
            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i]) allColliders[i].enabled = false;
            }
        }

        // 2) solta a física e configura queda dramática
        rb.constraints = RigidbodyConstraints2D.None;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = deathGravity;

        // Zera vel e aplica pulo + knockback lateral aleatório
        float dirKnock = Random.value < 0.5f ? -1f : 1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(dirKnock * deathKnockbackX, deathJumpY), ForceMode2D.Impulse);
        if (Mathf.Abs(deathTorque) > 0.01f)
            rb.AddTorque(deathTorque * -dirKnock, ForceMode2D.Impulse);

        // 3) animação (se houver)
        if (animator != null && !string.IsNullOrEmpty(deadTrigger))
            animator.SetTrigger(deadTrigger);

        // 4) destruir quando sair da tela (ou por timeout de segurança)
        if (destroyOnDeath)
            InvokeRepeating(nameof(CheckOffscreenAndDestroy), 0.15f, 0.15f);

        Destroy(gameObject, deathTimeout);
    }

    private void CheckOffscreenAndDestroy()
    {
        var cam = Camera.main;
        if (!cam) { Destroy(gameObject); return; }

        float camBottom = cam.transform.position.y - cam.orthographicSize;
        if (transform.position.y < camBottom - offscreenMargin)
            Destroy(gameObject);
    }
}
