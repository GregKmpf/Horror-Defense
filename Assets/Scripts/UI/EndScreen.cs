using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shown when GameManager ends the game: victory or defeat, why, a few numbers from the game, and buttons to play
// again or go back to the menu. The game is frozen by then (Time.timeScale = 0), which the UI doesn't mind.
public class EndScreen : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text title;
    public TMP_Text reason;
    public TMP_Text stats;
    public Button restartButton;
    public Button menuButton;

    public Color victoryColor = new Color(0.86f, 0.8f, 0.66f);
    public Color defeatColor = new Color(0.75f, 0.12f, 0.09f);

    GameManager game;

    void Start()
    {
        panel.SetActive(false);
        game = GameManager.Instance;
        if (game == null)
        {
            return;
        }

        game.GameEnded += Show;
        restartButton.onClick.AddListener(game.Restart);
        menuButton.onClick.AddListener(game.GoToMenu);
        menuButton.gameObject.SetActive(!string.IsNullOrEmpty(game.menuScene));
    }

    void OnDestroy()
    {
        if (game != null)
        {
            game.GameEnded -= Show;
        }
    }

    void Show(GameState result, GameEndReason why)
    {
        bool won = result == GameState.Won;
        title.text = won ? "VITÓRIA" : "DERROTA";
        title.color = won ? victoryColor : defeatColor;

        switch (why)
        {
            case GameEndReason.AllWavesCleared:
                reason.text = "A fazenda resistiu a todas as ondas.";
                break;
            case GameEndReason.BaseDestroyed:
                reason.text = "As criaturas tomaram o celeiro.";
                break;
            case GameEndReason.TimeUp:
                reason.text = "O tempo acabou antes de a última onda cair.";
                break;
            default:
                reason.text = "";
                break;
        }

        int health = Objetivo.Instancia != null ? Objetivo.Instancia.vidaAtual : 0;
        int maxHealth = Objetivo.Instancia != null ? Objetivo.Instancia.vidaMaxima : 0;
        stats.text =
            $"Dificuldade  <b>{game.Difficulty.displayName}</b>\n" +
            $"Tempo  <b>{UIFormat.Time(game.TimeElapsed)}</b>\n" +
            $"Inimigos mortos  <b>{game.EnemiesKilled}</b>\n" +
            $"Chegaram ao celeiro  <b>{game.EnemiesReachedBase}</b>\n" +
            $"Vida do celeiro  <b>{health}/{maxHealth}</b>";

        panel.SetActive(true);
    }
}
