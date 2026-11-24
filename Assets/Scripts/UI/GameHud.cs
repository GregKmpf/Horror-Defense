using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The bar across the top of the screen while playing: the barn's health, the wave and what comes next, the time
// left, the money, and buttons for the game speed. It only reads GameManager, WaveManager, Objetivo and Economy,
// and its buttons call WaveManager.StartNextWaveNow and GameSpeed.
// Texts are only rewritten when what they show changes.
public class GameHud : MonoBehaviour
{
    [Header("Base")]
    [Tooltip("Stretches from the left with the base's health")]
    public RectTransform healthFill;
    public TMP_Text healthLabel;

    [Tooltip("Flashes when the base takes damage")]
    public Graphic healthFlash;

    [Header("Waves")]
    public TMP_Text waveLabel;
    public TMP_Text waveStatus;
    public Button nextWaveButton;

    [Header("Time and money")]
    public TMP_Text timeLabel;

    [Tooltip("The time turns this colour when less than Low Time is left")]
    public Color lowTimeColor = new Color(0.85f, 0.2f, 0.15f);
    public float lowTime = 30f;
    public TMP_Text moneyLabel;

    [Header("Speed")]
    public Button normalSpeedButton;
    public Button fastSpeedButton;
    public float fastSpeed = 2f;
    public Color speedOn = new Color(0.45f, 0.1f, 0.08f);
    public Color speedOff = new Color(0.09f, 0.071f, 0.059f);

    WaveManager waves;
    Color timeColor;
    int shownHealth = -1;
    int shownTime = -1;
    int shownWave = -1;
    int shownStatus = int.MinValue;
    float flash;

    void Start()
    {
        GameManager game = GameManager.Instance;
        waves = game != null ? game.waves : FindAnyObjectByType<WaveManager>();
        timeColor = timeLabel.color;

        nextWaveButton.onClick.AddListener(() => waves?.StartNextWaveNow());
        normalSpeedButton.onClick.AddListener(() => GameSpeed.Set(1f));
        fastSpeedButton.onClick.AddListener(() => GameSpeed.Set(fastSpeed));
        GameSpeed.Changed += ShowSpeed;
        ShowSpeed(GameSpeed.Current);

        if (Economy.Instance != null)
        {
            Economy.Instance.Changed += ShowMoney;
            ShowMoney(Economy.Instance.Money);
        }
        else
        {
            moneyLabel.text = "";
        }

        if (Objetivo.Instancia != null)
        {
            Objetivo.Instancia.aoReceberDano.AddListener(Flash);
        }
        if (healthFlash != null)
        {
            healthFlash.canvasRenderer.SetAlpha(0f);
        }
    }

    void OnDestroy()
    {
        GameSpeed.Changed -= ShowSpeed;
        if (Economy.Instance != null)
        {
            Economy.Instance.Changed -= ShowMoney;
        }
        if (Objetivo.Instancia != null)
        {
            Objetivo.Instancia.aoReceberDano.RemoveListener(Flash);
        }
    }

    void Update()
    {
        ShowHealth();
        ShowTime();
        ShowWaves();

        if (healthFlash != null && flash > 0f)
        {
            flash = Mathf.Max(flash - Time.unscaledDeltaTime * 2f, 0f);
            healthFlash.canvasRenderer.SetAlpha(flash);
        }
    }

    void ShowHealth()
    {
        Objetivo goal = Objetivo.Instancia;
        if (goal == null || goal.vidaAtual == shownHealth)
        {
            return;
        }

        shownHealth = goal.vidaAtual;
        float fraction = goal.vidaMaxima > 0 ? Mathf.Clamp01(goal.vidaAtual / (float)goal.vidaMaxima) : 0f;
        healthFill.anchorMax = new Vector2(fraction, healthFill.anchorMax.y);
        healthLabel.text = $"{goal.vidaAtual}/{goal.vidaMaxima}";
    }

    void ShowTime()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(game.TimeRemaining);
        if (seconds == shownTime)
        {
            return;
        }
        shownTime = seconds;
        timeLabel.text = UIFormat.Time(seconds);
        timeLabel.color = seconds <= lowTime ? lowTimeColor : timeColor;
    }

    void ShowWaves()
    {
        if (waves == null)
        {
            return;
        }

        bool waiting = waves.IsWaitingForNextWave;
        bool finished = waves.WaveCount > 0 && waves.CurrentWave == waves.WaveCount && !waves.IsRunning;

        // Encode what the status line shows in one number, to only rewrite it when that changes:
        // positive while counting down (seconds left), negative during a wave (enemies left), 0 when done
        int status = finished ? 0
            : waiting ? Mathf.CeilToInt(waves.TimeUntilNextWave)
            : -(waves.EnemiesAlive + waves.EnemiesToSpawn) - 1;

        if (waves.CurrentWave != shownWave)
        {
            shownWave = waves.CurrentWave;
            shownStatus = int.MinValue;
            waveLabel.text = waves.CurrentWave == 0 ? "PREPARE-SE" : $"ONDA {waves.CurrentWave}/{waves.WaveCount}";
        }

        if (status == shownStatus)
        {
            return;
        }
        shownStatus = status;
        nextWaveButton.gameObject.SetActive(waiting);

        if (finished)
        {
            waveStatus.text = "Todas as ondas contidas";
        }
        else if (waiting)
        {
            waveStatus.text = $"Onda {waves.CurrentWave + 1} chega em {status} s";
        }
        else
        {
            int left = -status - 1;
            waveStatus.text = left == 1 ? "1 inimigo restante" : $"{left} inimigos restantes";
        }
    }

    void ShowMoney(int money)
    {
        moneyLabel.text = UIFormat.Money(money);
    }

    void ShowSpeed(float speed)
    {
        bool fast = speed > 1.01f;
        normalSpeedButton.image.color = fast ? speedOff : speedOn;
        fastSpeedButton.image.color = fast ? speedOn : speedOff;
    }

    void Flash()
    {
        flash = 1f;
    }
}
