using UnityEngine;

public class Apple : MonoBehaviour
{
    private SpriteRenderer sr;
    private CircleCollider2D cc;

    private int scoreValue = 5;
    private int scorePerApple = 1;
    public GameObject prefabCollected;

    public AudioSource collectSound;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        cc = GetComponent<CircleCollider2D>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Somar pontos no GameController
        if (GameController.instance != null)
        {

            GameController.instance.AddScore(scoreValue);
            GameController.instance.CountApple(scorePerApple);


        }

        collectSound.Play();

        // Desativar visual/colisor da maçã
        sr.enabled = false;
        cc.enabled = false;

        prefabCollected.SetActive(true);

        // Destruir depois de um pequeno delay (tempo pro efeito)
        Destroy(gameObject, 0.25f);
    }
}
