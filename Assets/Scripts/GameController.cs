using UnityEngine;
using TMPro;

public class GameController : MonoBehaviour
{
    public int totalScore;
    public int highScore;
    public int lifeCount = 3;
    public int nextLifeAt = 100;

    [Header("UI (TextMeshPro)")]
    [SerializeField] private TMP_Text scoreText;      // arraste do Canvas
    [SerializeField] private TMP_Text highScoreText;  // arraste do Canvas
    [SerializeField] private TMP_Text livesText;      // arraste do Canvas

    public static GameController instance;
    void Start()
    {
        instance = this;
        UpdateHUD();
    }

    public void AddScore(int amount)
    {
        this.totalScore += amount;

        if (this.totalScore > this.highScore)
        {
            this.highScore = this.totalScore;
        }


     

        while (this.totalScore >= this.nextLifeAt)
        {
            this.GainLife();
            this.nextLifeAt += 100;
        }

       
        UpdateHUD();
    }

    public void LoseLife()
    {
        this.lifeCount--;

        if (this.lifeCount <= 0)
        {
            // Game Over logic here
            Debug.Log("Game Over!");
        }

        UpdateHUD();
    }

    public void GainLife()
    {
        this.lifeCount++;

        UpdateHUD();

    }
    
    private void UpdateHUD()
    {
        if (scoreText)     scoreText.text     = totalScore.ToString("D4");
        if (highScoreText) highScoreText.text = highScore.ToString("D4");
        if (livesText)     livesText.text     = lifeCount.ToString("D2");
    }
        
}