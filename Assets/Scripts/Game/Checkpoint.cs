using UnityEngine;

// Add this to a GameObject with a 2D Collider set as Trigger.
// When the Player passes, it marks this checkpoint as active (isChecked)
// and updates the GameController respawn point.
public class Checkpoint : MonoBehaviour
{
    [Header("State")]
    [Tooltip("Se verdadeiro, este checkpoint já foi ativado.")]
    public bool isChecked = false;

    [Header("Respawn Point")]
    [Tooltip("Ponto exato para respawn. Se vazio, usa o próprio transform.")]
    public Transform respawnPoint;

    [Header("Animator (opcional)")]
    public Animator animator;
    [Tooltip("Nome do bool no Animator que indica checkpoint ativo.")]
    public string animBoolChecked = "isChecked";
    [Tooltip("Nome do trigger no Animator ao passar no checkpoint.")]
    public string animTriggerPass = "passCheck";

    [Header("Feedback (opcional)")]
    public AudioSource activateSound;

    private static Checkpoint current;

    void Awake()
    {
        if (!respawnPoint) respawnPoint = transform;
        if (!animator) animator = GetComponentInChildren<Animator>();
        ApplyAnimatorState();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isChecked) return;
        if (!other.CompareTag("Player")) return;

        Activate();
    }

    public void Activate()
    {
        // Unset previous checkpoint visual state
        if (current && current != this)
        {
            current.isChecked = false;
            current.ApplyAnimatorState();
        }

        current = this;
        isChecked = true;
        ApplyAnimatorState();

        if (activateSound) activateSound.Play();

        // Set respawn point in GameController
        if (GameController.instance != null)
        {
            GameController.instance.SetRespawn(respawnPoint);
        }
    }

    private void ApplyAnimatorState()
    {
        if (!animator) return;
        if (!string.IsNullOrEmpty(animBoolChecked))
            animator.SetBool(animBoolChecked, isChecked);
        if (isChecked && !string.IsNullOrEmpty(animTriggerPass))
            animator.SetTrigger(animTriggerPass);
    }

    void OnDrawGizmosSelected()
    {
        Transform p = respawnPoint ? respawnPoint : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(p.position, 0.25f);
        Gizmos.DrawLine(transform.position, p.position);
    }
}

