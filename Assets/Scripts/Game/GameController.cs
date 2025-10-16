using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public int totalScore;
    public int highScore;
    public int lifeCount = 3;
    public int nextLifeAt = 100;
    public int appleCount;

    [Header("UI (TextMeshPro)")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text countAppleText; // Texto para exibir a contagem de maçãs (se houver)

    [Header("Respawn & Invencibilidade")]
    [SerializeField] private Transform respawnPoint;     // arraste um Empty no início da fase
    [SerializeField] private float invulnSeconds = 5.0f; // tempo invencível ao respawn
    [SerializeField] private string enemyLayerName = "Enemy"; // layer dos inimigos
    [Header("Queda para fora da tela")]
    [SerializeField] private float cameraBottomLimit = -2.48f; // mesmo valor configurado na câmera
    [SerializeField] private float fallDeathBuffer = 0.5f;     // margem além da base visível
    [Header("Limites de Movimento Horizontal")]
    [SerializeField] private bool useHorizontalLimits = false;
    [SerializeField] private float minX = -7.5f;
    [SerializeField] private float maxX = 7.5f;

    public static GameController instance;

    [Header("Morte do Player")]
    [SerializeField] private float deathJumpY = 20f;
    [SerializeField] private float deathKnockbackX = 2f;
    [SerializeField] private float deathGravity = 4.5f;
    [SerializeField] private float deathTimeout = 3.0f;
    [SerializeField] private bool disableCollidersOnDeath = true;

    public AudioSource deathSound;
    
    public AudioSource newLifeSound;

    private Transform playerTransform;
    private bool isDying = false;

    // Removido: referência inexistente a GameManager

    void Awake()
    {
        // Singleton persistente para não resetar atributos ao reposicionar/respawn
       if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);

        // Re-vincula referências quando uma cena é (re)carregada
        if (instance == this)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnValidate()
    {
        if (minX > maxX)
        {
            float temp = minX;
            minX = maxX;
            maxX = temp;
        }
    }

    void Start()
    {
        RebindHUD();
        RefindRespawnPoint();
        EnsurePlayerReference();
        UpdateHUD();
    }

    void Update()
    {
        EnsurePlayerReference();

        if (!isDying && playerTransform && playerTransform.position.y < cameraBottomLimit - fallDeathBuffer)
        {
            LoseLife();

           

            return;
        }
    }

    public void AddScore(int amount)
    {
        totalScore += amount;
        if (totalScore > highScore) highScore = totalScore;

        while (totalScore >= nextLifeAt)
        {
            GainLife();
            nextLifeAt += 100;
            if (newLifeSound) newLifeSound.Play();
        }
        UpdateHUD();
    }

    public void CountApple(int amount)
    {
        appleCount += amount;
      

        while (appleCount >= nextLifeAt)
        {
            GainLife();
            nextLifeAt += 100;
            if (newLifeSound) newLifeSound.Play();
        }
        UpdateHUD();
    }

    public void LoseLife()
    {
        if (isDying) return;

        lifeCount--;
        if (lifeCount <= 0)
        {
            // GAME OVER: Reseta o estado para um novo jogo e recarrega cena
            totalScore = 0;
            lifeCount = 3;
            nextLifeAt = 100;
            ReloadCurrentScene();
            return; // evita usar refs antigas no mesmo frame
        }
        if (deathSound) deathSound.Play();
         
        RespawnPlayer();
        UpdateHUD();
    }

    public void GainLife()
    {
        lifeCount++;
        UpdateHUD();
    }

    public bool AreHorizontalLimitsEnabled => useHorizontalLimits;
    public float CurrentMinX => minX;
    public float CurrentMaxX => maxX;

    public void SetHorizontalLimits(float min, float max, bool enabled = true)
    {
        useHorizontalLimits = enabled;

        if (!enabled)
            return;

        if (min > max)
        {
            float temp = min;
            min = max;
            max = temp;
        }

        minX = min;
        maxX = max;
    }

    private void UpdateHUD()
    {
        // Garante que, se a cena foi recarregada, as refs sejam refeitas
        if (!scoreText || !highScoreText || !livesText)
            RebindHUD();

        if (scoreText)     scoreText.text     = totalScore.ToString("D4");
        if (highScoreText) highScoreText.text = highScore.ToString("D4");
        if (livesText)     livesText.text     = lifeCount.ToString("D2");
        if (countAppleText) countAppleText.text = appleCount.ToString("D4");
    }

    // private void RebindHUD()
    // {
    //     // Se o objeto foi destruído entre cenas, as refs parecem null para Unity
    //     if (!scoreText)     scoreText     = FindTMPByNameOrTag("ScoreText", new[] { "score" });
    //     if (!highScoreText) highScoreText = FindTMPByNameOrTag("HighScoreText", new[] { "high", "record" });
    //     if (!livesText)     livesText     = FindTMPByNameOrTag("LivesText", new[] { "life", "vidas" });
    // }

    private void RebindHUD()
    {
        // Se o objeto foi destruído entre cenas, as refs ficam null
        if (!scoreText)     scoreText     = FindTMPByNameOrTag("ScoreText", new[] { "score" });
        if (!highScoreText) highScoreText = FindTMPByNameOrTag("HighScoreText", new[] { "high", "record" });
        if (!livesText)     livesText     = FindTMPByNameOrTag("LivesText", new[] { "life", "vidas" });
        if (!countAppleText) countAppleText = FindTMPByNameOrTag("CountAppleText", new[] { "apple", "maçã" });
    }

    private void RefindRespawnPoint()
    {
        if (respawnPoint && respawnPoint.gameObject.scene.IsValid())
            return;

        var respawnObj = GameObject.FindGameObjectWithTag("Respawn");
        if (respawnObj)
        {
            respawnPoint = respawnObj.transform;
            return;
        }

        var allPoints = GameObject.FindObjectsOfType<Transform>(true);
        foreach (var t in allPoints)
        {
            if (t.name.Equals("Respawn", System.StringComparison.OrdinalIgnoreCase) ||
                t.name.ToLowerInvariant().Contains("spawn"))
            {
                respawnPoint = t;
                return;
            }
        }
    }

    private TMP_Text FindTMPByNameOrTag(string preferredTagOrName, string[] nameHints)
    {
        // 1) Tenta por Tag (sem explodir caso a tag não exista)
        try
        {
            var byTag = GameObject.FindGameObjectWithTag(preferredTagOrName);
            if (byTag)
            {
                var t = byTag.GetComponent<TMP_Text>();
                if (t) return t;
            }
        }
        catch (UnityException)
        {
            // Tag não definida no Project Settings: ignora e segue para busca por nome
        }

        // 2) Tenta por nome exato (qualquer ativo/inativo)
        var all = GameObject.FindObjectsOfType<TMP_Text>(true);
        foreach (var t in all)
        {
            if (string.Equals(t.name, preferredTagOrName, System.StringComparison.OrdinalIgnoreCase))
                return t;
        }

        // 3) Tenta por heurística de nome (contém palavras-chave)
        foreach (var t in all)
        {
            string n = t.name.ToLowerInvariant();
            foreach (var hint in nameHints)
            {
                if (n.Contains(hint))
                    return t;
            }
        }
        return null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Após trocar de cena, reassocia Player e HUD e atualiza textos
        EnsurePlayerReference();
        RebindHUD();
        RefindRespawnPoint();
        UpdateHUD();
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void EnsurePlayerReference()
    {
        if (playerTransform && playerTransform.gameObject.activeInHierarchy)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
            playerTransform = playerObj.transform;
    }

    private void RespawnPlayer()
    {
        EnsurePlayerReference();
        if (!playerTransform) return;

        if (respawnPoint != null) 
            playerTransform.position = respawnPoint.position;

        var rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb) 
            rb.linearVelocity = Vector2.zero;

        if (invulnSeconds > 0f && !string.IsNullOrEmpty(enemyLayerName))
            StartCoroutine(TempInvulnerability(playerTransform.gameObject, invulnSeconds, enemyLayerName));
    }

    // Permite que checkpoints atualizem o ponto de respawn em runtime
    public void SetRespawn(Transform point)
    {
        if (point != null)
            respawnPoint = point;
    }

    public void LoseLifeFromHit(Vector2 hitFromPosition)
    {
        if (isDying) return;

        lifeCount--;
        if (lifeCount <= 0)
        {
            totalScore = 0;
            lifeCount = 3;
            nextLifeAt = 100;
            ReloadCurrentScene();
            return;
        }

        UpdateHUD();
        if (deathSound) deathSound.Play();
        StartCoroutine(DeathSequence(hitFromPosition));
    }

    private IEnumerator DeathSequence(Vector2 hitFrom)
    {
        EnsurePlayerReference();
        if (!playerTransform) yield break;

        isDying = true;

        GameObject player = playerTransform.gameObject;
        var rb = player.GetComponent<Rigidbody2D>();
        var playerScript = player.GetComponent<Player>();
        var anim = player.GetComponent<Animator>();
        var colliders = disableCollidersOnDeath ? player.GetComponentsInChildren<Collider2D>(true) : null;

        float originalGravity = rb ? rb.gravityScale : 0f;

        if (playerScript) playerScript.enabled = false;
        if (anim) { anim.SetBool("walking", false); anim.SetBool("jump", false); anim.SetBool("dblJump", false); }

        if (colliders != null)
        {
            foreach (var c in colliders) if (c) c.enabled = false;
        }

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = deathGravity;
            float dir = Mathf.Sign(playerTransform.position.x - hitFrom.x);
            rb.AddForce(new Vector2(dir * deathKnockbackX, deathJumpY), ForceMode2D.Impulse);
        }

        float t = 0f;
        while (t < deathTimeout)
        {
            if (playerTransform.position.y < cameraBottomLimit - fallDeathBuffer)
                break;
            t += Time.deltaTime;
            yield return null;
        }

        if (rb) rb.gravityScale = originalGravity;
        if (colliders != null)
        {
            foreach (var c in colliders) if (c) c.enabled = true;
        }
        if (playerScript) playerScript.enabled = true;

        RespawnPlayer();
        isDying = false;
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

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f; // garante que não ficou pausado
        var scene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
}


