using UnityEngine;
using TMPro;
using System.Collections;

public class GameController : MonoBehaviour
{
    public int totalScore;
    public int highScore;
    public int lifeCount = 3;
    public int nextLifeAt = 100;

    [Header("UI (TextMeshPro)")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text livesText;

    [Header("Respawn & Invencibilidade")]
    [SerializeField] private Transform respawnPoint;     // arraste um Empty no início da fase
    [SerializeField] private float invulnSeconds = 1.0f; // tempo invencível ao respawn
    [SerializeField] private string enemyLayerName = "Enemy"; // layer dos inimigos

    public static GameController instance;

    void Awake() { instance = this; }

    void Start() { UpdateHUD(); }

    public void AddScore(int amount)
    {
        totalScore += amount;
        if (totalScore > highScore) highScore = totalScore;

        while (totalScore >= nextLifeAt)
        {
            GainLife();
            nextLifeAt += 100;
        }
        UpdateHUD();
    }

    public void LoseLife()
    {
        lifeCount--;
        if (lifeCount <= 0)
        {
            // GAME OVER simples: reinicia a cena
            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(s.buildIndex);
            return;
        }

        RespawnPlayer();
        UpdateHUD();
    }

    public void GainLife()
    {
        lifeCount++;
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (scoreText)     scoreText.text     = totalScore.ToString("D4");
        if (highScoreText) highScoreText.text = highScore.ToString("D4");
        if (livesText)     livesText.text     = lifeCount.ToString("D2");
    }

    private void RespawnPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        // reposiciona no início
        if (respawnPoint != null) player.transform.position = respawnPoint.position;

        // zera velocidade
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        // invencibilidade curtinha
        if (invulnSeconds > 0f && !string.IsNullOrEmpty(enemyLayerName))
            StartCoroutine(TempInvulnerability(player, invulnSeconds, enemyLayerName));
    }

    private IEnumerator TempInvulnerability(GameObject player, float seconds, string enemyLayer)
    {
        int playerLayer = player.layer;
        int enemyLayerIndex = LayerMask.NameToLayer(enemyLayer);
        if (enemyLayerIndex == -1) yield break;

        // ignora colisão Player x Enemy
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayerIndex, true);

        // feedback visual: pisca sprite
        var sr = player.GetComponentInChildren<SpriteRenderer>();
        float t = 0f;
        while (t < seconds)
        {
            if (sr) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }
        if (sr) sr.enabled = true;

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayerIndex, false);
    }
}
