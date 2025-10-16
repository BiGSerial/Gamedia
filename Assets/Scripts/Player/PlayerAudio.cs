using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;          // arraste seu Rigidbody2D aqui
    [SerializeField] private LayerMask groundLayer;   // layer do chão
    [SerializeField] private Transform groundCheck;   // um Empty no pé do player
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip[] footstepClips; // aleatoriza passos

    [Header("Footstep Settings")]
    [SerializeField] private float baseStepInterval = 0.4f; // tempo entre passos em velocidade "padrão"
    [SerializeField] private float minSpeedForSteps = 0.1f; // evita passo parado
    [SerializeField] private Vector2 footstepPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Volumes")]
    [Range(0f, 1f)] public float jumpVolume = 0.9f;
    [Range(0f, 1f)] public float stepVolume = 0.6f;

    private AudioSource _source;
    private float _stepTimer;
    private bool _wasGroundedLastFrame;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        bool grounded = IsGrounded();
        float speed = Mathf.Abs(rb.linearVelocity.x);

        // Dispara passos no chão e em movimento
        if (grounded && speed > minSpeedForSteps)
        {
            // Quanto mais rápido, menor o intervalo (passos mais frequentes)
            float speedFactor = Mathf.Clamp(speed, 0.5f, 8f);
            float interval = baseStepInterval / Mathf.Lerp(0.7f, 1.6f, (speedFactor - 0.5f) / 7.5f);

            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                PlayFootstep();
                _stepTimer = interval;
            }
        }
        else
        {
            // reseta timer para não “acumular”
            _stepTimer = 0f;
        }

        // (Opcional) som de aterrissagem
        if (!_wasGroundedLastFrame && grounded)
        {
            // você pode usar um clip específico de landing aqui se quiser
            // _source.PlayOneShot(landingClip, stepVolume);
        }

        _wasGroundedLastFrame = grounded;
    }

    public void PlayJump() // chame isso no momento do pulo no seu controlador
    {
        if (jumpClip == null) return;

        // pequena variação de pitch deixa mais natural
        float oldPitch = _source.pitch;
        _source.pitch = Random.Range(0.98f, 1.03f);
        _source.PlayOneShot(jumpClip, jumpVolume);
        _source.pitch = oldPitch;
    }

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        var clip = footstepClips[Random.Range(0, footstepClips.Length)];

        float oldPitch = _source.pitch;
        _source.pitch = Random.Range(footstepPitchRange.x, footstepPitchRange.y);
        _source.PlayOneShot(clip, stepVolume);
        _source.pitch = oldPitch;
    }

    private bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    // gizmo útil no editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
