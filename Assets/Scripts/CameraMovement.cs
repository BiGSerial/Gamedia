using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraMovement : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;               // arraste o Player (ou use a Tag "Player")

    [Header("Limites")]
    [SerializeField] private float leftLimit = -7.194367f;   // limite da borda esquerda (mundo)
    [SerializeField] private float bottomLimit = -2.48f;      // limite inferior para a base da câmera

    [Header("Look Up / Down")]
    [SerializeField] private float lookOffsetY = 2f;         // quanto a câmera desloca no Y ao olhar
    [SerializeField] private float lookLerp = 6f;            // suavidade do deslocamento

    [Header("Ground Check (para habilitar o look)")]
    // defina a Layer do chão
    [SerializeField] private LayerMask groundMask; 
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, -0.5f, 0f);

    private Camera cam;
    private float currentLookY; // deslocamento atual aplicado ao Y

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // Centraliza já no início
        LateUpdate();
    }

    void LateUpdate()
    {
        if (!player || cam == null) return;

        // --- (A) Calcula o deslocamento vertical para "olhar" ---
        float desiredLookY = 0f;
        float v = Input.GetAxisRaw("Vertical"); // ↑ (1), ↓ (-1)

        if (IsGrounded())
        {
            if (v > 0.1f) desiredLookY = +lookOffsetY;    // olhar para cima
            else if (v < -0.1f) desiredLookY = -lookOffsetY; // olhar para baixo
        }
        // Suaviza a transição do offset Y
        currentLookY = Mathf.Lerp(currentLookY, desiredLookY, Time.deltaTime * lookLerp);

        // --- (B) Posição alvo (segue player) ---
        Vector3 target = new Vector3(
            player.position.x,
            player.position.y + currentLookY,   // aplica o deslocamento de olhar
            transform.position.z
        );

        // --- (C) Trava a borda esquerda da câmera (câmera ortográfica 2D) ---
        float halfWidth = cam.orthographicSize * cam.aspect;   // metade da largura visível
        float minCamX = leftLimit + halfWidth;                  // menor X permitido para a CÂMERA
        target.x = Mathf.Max(target.x, minCamX);

        float minCamY = bottomLimit + cam.orthographicSize;     // garante base >= bottomLimit
        target.y = Mathf.Max(target.y, minCamY);

        transform.position = target;
    }

    // Checagem simples de chão por Raycast
    private bool IsGrounded()
    {
        if (!player) return false;

        Vector3 origin = player.position + new Vector3(groundCheckOffset.x, groundCheckOffset.y, 0);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundMask);
        return hit.collider != null;
    }

    // (Opcional) visualize o ray no Editor
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!player) return;
        Gizmos.color = Color.yellow;
        Vector3 origin = player.position + new Vector3(groundCheckOffset.x, groundCheckOffset.y, 0);
        Gizmos.DrawLine(origin, origin + new Vector3(0, -groundCheckDistance, 0));
    }
#endif
}
