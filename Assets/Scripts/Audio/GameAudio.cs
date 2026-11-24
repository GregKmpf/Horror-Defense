using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Sfx
{
    Minigun,
    Cannon,
    Heavy,
    Artillery,
    Rocket,
    Shock,
    Explosion,
    EnemyDeath,
    BaseHit,
    Build,
    Sell,
    Click,
    WaveStart,
    Victory,
    Defeat,
}

// Every sound in the game. It installs itself when the game starts and lives across scenes, so nothing has to be
// placed in a scene. The clips are made by SoundSynth.
// It listens to Enemy (deaths, enemies reaching the base), the WaveManager (wave start) and the GameManager (end
// of the game) on its own. Turrets, projectiles and the build menu call Play directly. Any click on a button
// plays a click.
// Two buttons above the speed buttons, Música and Sons, turn the music and the effects on and off. M also
// toggles the music. Both are saved in PlayerPrefs.
// The pause menu pauses the AudioListener: effects stop with the game, but the music and the interface sounds
// ignore it and keep playing.
public class GameAudio : MonoBehaviour
{
    const string MusicKey = "audio.music";
    const string SfxKey = "audio.sfx";
    const int VoiceCount = 24;
    const float MusicVolume = 0.35f;

    static readonly Color On = new Color(0.45f, 0.1f, 0.08f);
    static readonly Color Off = new Color(0.09f, 0.071f, 0.059f);

    static GameAudio instance;

    public static bool MusicOn { get; private set; } = true;
    public static bool SfxOn { get; private set; } = true;

    struct Sound
    {
        public AudioClip clip;
        public float volume;
        public float minGap;       // Seconds before the same sound can play again, so a crowd doesn't clip
        public float pitchJitter;  // Random pitch change each time, so repeats don't sound identical
        public bool ignorePause;   // Interface and end of game sounds play while the game is paused
    }

    Sound[] sounds;
    float[] lastPlayed;
    AudioSource[] voices;
    int nextVoice;
    AudioSource music;

    WaveManager waves;
    GameManager game;

    Image musicButton;
    Image sfxButton;
    Text musicLabel;
    Text sfxLabel;

    readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    // ---------- Installation ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (instance != null)
        {
            return;
        }
        GameObject go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        go.AddComponent<GameAudio>();
    }

    // ---------- Public API ----------

    public static void Play(Sfx sfx)
    {
        if (instance != null)
        {
            instance.PlaySound(sfx);
        }
    }

    public static void PlayTurretShot(TurretSound sound)
    {
        switch (sound)
        {
            case TurretSound.Minigun:
                Play(Sfx.Minigun);
                break;
            case TurretSound.Heavy:
                Play(Sfx.Heavy);
                break;
            case TurretSound.Artillery:
                Play(Sfx.Artillery);
                break;
            case TurretSound.Rocket:
                Play(Sfx.Rocket);
                break;
            case TurretSound.Shock:
                Play(Sfx.Shock);
                break;
            default:
                Play(Sfx.Cannon);
                break;
        }
    }

    public static void SetMusic(bool on)
    {
        MusicOn = on;
        PlayerPrefs.SetInt(MusicKey, on ? 1 : 0);
        PlayerPrefs.Save();
        if (instance != null)
        {
            instance.ApplyMusic();
            instance.RefreshButtons();
        }
    }

    public static void SetSfx(bool on)
    {
        SfxOn = on;
        PlayerPrefs.SetInt(SfxKey, on ? 1 : 0);
        PlayerPrefs.Save();
        if (instance != null)
        {
            if (!on)
            {
                instance.StopAllSfx();
            }
            instance.RefreshButtons();
        }
    }

    // ---------- Lifecycle ----------

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        MusicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        SfxOn = PlayerPrefs.GetInt(SfxKey, 1) == 1;

        CreateSounds();
        CreateVoices();
        CreateMusic();
        CreateButtons();
    }

    void OnEnable()
    {
        if (instance != this)
        {
            return;
        }
        Enemy.AnyDied += OnEnemyDied;
        Enemy.AnyReachedBase += OnEnemyReachedBase;
        SceneManager.sceneLoaded += OnSceneLoaded;
        HookScene();
    }

    void OnDisable()
    {
        if (instance != this)
        {
            return;
        }
        Enemy.AnyDied -= OnEnemyDied;
        Enemy.AnyReachedBase -= OnEnemyReachedBase;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnhookScene();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
        {
            SetMusic(!MusicOn);
        }

        PlayClickOnButtons();
    }

    // ---------- Setup ----------

    void CreateSounds()
    {
        int count = System.Enum.GetValues(typeof(Sfx)).Length;
        sounds = new Sound[count];
        lastPlayed = new float[count];
        for (int i = 0; i < count; i++)
        {
            lastPlayed[i] = -100f;
        }

        Add(Sfx.Minigun, SoundSynth.Minigun(), 0.22f, 0.06f, 0.08f);
        Add(Sfx.Cannon, SoundSynth.Cannon(), 0.5f, 0.08f, 0.06f);
        Add(Sfx.Heavy, SoundSynth.Heavy(), 0.4f, 0.08f, 0.06f);
        Add(Sfx.Artillery, SoundSynth.Artillery(), 0.65f, 0.15f, 0.05f);
        Add(Sfx.Rocket, SoundSynth.Rocket(), 0.4f, 0.1f, 0.08f);
        Add(Sfx.Shock, SoundSynth.Shock(), 0.35f, 0.1f, 0.1f);
        Add(Sfx.Explosion, SoundSynth.Explosion(), 0.45f, 0.12f, 0.1f);
        Add(Sfx.EnemyDeath, SoundSynth.EnemyDeath(), 0.45f, 0.1f, 0.15f);
        Add(Sfx.BaseHit, SoundSynth.BaseHit(), 0.8f, 0.15f, 0.05f);
        Add(Sfx.Build, SoundSynth.Build(), 0.6f, 0.05f, 0f, true);
        Add(Sfx.Sell, SoundSynth.Sell(), 0.5f, 0.05f, 0f, true);
        Add(Sfx.Click, SoundSynth.Click(), 0.4f, 0.03f, 0.03f, true);
        Add(Sfx.WaveStart, SoundSynth.WaveStart(), 0.7f, 1f, 0f);
        Add(Sfx.Victory, SoundSynth.Victory(), 0.7f, 1f, 0f, true);
        Add(Sfx.Defeat, SoundSynth.Defeat(), 0.75f, 1f, 0f, true);
    }

    void Add(Sfx sfx, AudioClip clip, float volume, float minGap, float pitchJitter, bool ignorePause = false)
    {
        sounds[(int)sfx] = new Sound
        {
            clip = clip,
            volume = volume,
            minGap = minGap,
            pitchJitter = pitchJitter,
            ignorePause = ignorePause,
        };
    }

    void CreateVoices()
    {
        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            voices[i] = voice;
        }
    }

    void CreateMusic()
    {
        music = gameObject.AddComponent<AudioSource>();
        music.clip = SoundSynth.Music();
        music.loop = true;
        music.playOnAwake = false;
        music.spatialBlend = 0f;
        music.volume = MusicVolume;
        music.ignoreListenerPause = true;
        music.priority = 0;
        ApplyMusic();
    }

    void ApplyMusic()
    {
        music.mute = !MusicOn;
        if (MusicOn && !music.isPlaying)
        {
            music.Play();
        }
    }

    // ---------- Playing ----------

    void PlaySound(Sfx sfx)
    {
        if (!SfxOn)
        {
            return;
        }

        Sound sound = sounds[(int)sfx];
        if (sound.clip == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now - lastPlayed[(int)sfx] < sound.minGap)
        {
            return;
        }
        lastPlayed[(int)sfx] = now;

        AudioSource voice = FreeVoice();
        voice.clip = sound.clip;
        voice.volume = sound.volume;
        voice.pitch = 1f + Random.Range(-sound.pitchJitter, sound.pitchJitter);
        voice.ignoreListenerPause = sound.ignorePause;
        voice.Play();
    }

    // A voice that isn't playing, or the one that started the longest ago when all of them are busy
    AudioSource FreeVoice()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            AudioSource voice = voices[(nextVoice + i) % voices.Length];
            if (!voice.isPlaying)
            {
                nextVoice = (nextVoice + i + 1) % voices.Length;
                return voice;
            }
        }

        AudioSource oldest = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        oldest.Stop();
        return oldest;
    }

    void StopAllSfx()
    {
        foreach (AudioSource voice in voices)
        {
            voice.Stop();
        }
    }

    // A click when the left button goes down on any button, toggle or other control that can be used
    void PlayClickOnButtons()
    {
        Mouse mouse = Mouse.current;
        EventSystem events = EventSystem.current;
        if (mouse == null || events == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        PointerEventData pointer = new PointerEventData(events) { position = mouse.position.ReadValue() };
        uiHits.Clear();
        events.RaycastAll(pointer, uiHits);
        if (uiHits.Count == 0)
        {
            return;
        }

        Selectable control = uiHits[0].gameObject.GetComponentInParent<Selectable>();
        if (control != null && control.IsInteractable())
        {
            Play(Sfx.Click);
        }
    }

    // ---------- Game events ----------

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookScene();
    }

    // The WaveManager and GameManager belong to the game scene, so they are found again after every load
    void HookScene()
    {
        UnhookScene();

        game = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        waves = game != null && game.waves != null ? game.waves : FindAnyObjectByType<WaveManager>();

        if (game != null)
        {
            game.GameEnded += OnGameEnded;
        }
        if (waves != null)
        {
            waves.WaveStarted += OnWaveStarted;
        }
    }

    // Uses the C# references, so it also unsubscribes from objects that were already destroyed with their scene
    void UnhookScene()
    {
        if ((object)game != null)
        {
            game.GameEnded -= OnGameEnded;
            game = null;
        }
        if ((object)waves != null)
        {
            waves.WaveStarted -= OnWaveStarted;
            waves = null;
        }
    }

    void OnEnemyDied(Enemy enemy)
    {
        Play(Sfx.EnemyDeath);
    }

    void OnEnemyReachedBase(Enemy enemy)
    {
        Play(Sfx.BaseHit);
    }

    void OnWaveStarted(int wave, int total)
    {
        Play(Sfx.WaveStart);
    }

    void OnGameEnded(GameState result, GameEndReason reason)
    {
        // Whatever was still ringing would play over the stinger
        StopAllSfx();
        Play(result == GameState.Won ? Sfx.Victory : Sfx.Defeat);
    }

    // ---------- Buttons ----------

    // A small canvas of its own, above every scene's UI, so the buttons show in the menu, in the game and while
    // paused. Sits just above the 1x/2x buttons and matches their look
    void CreateButtons()
    {
        GameObject canvasObject = new GameObject("AudioButtons");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(canvasObject.transform, false);
        row.anchorMin = row.anchorMax = new Vector2(1f, 0f);
        row.pivot = new Vector2(1f, 0f);
        row.anchoredPosition = new Vector2(-24f, 92f);
        row.sizeDelta = new Vector2(170f, 40f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        musicButton = CreateButton(row, "Música", new Vector2(0f, 0f), font, () => SetMusic(!MusicOn), out musicLabel);
        sfxButton = CreateButton(row, "Sons", new Vector2(1f, 0f), font, () => SetSfx(!SfxOn), out sfxLabel);

        RefreshButtons();
    }

    Image CreateButton(RectTransform parent, string text, Vector2 side, Font font,
        UnityEngine.Events.UnityAction onClick, out Text label)
    {
        RectTransform rect = new GameObject(text, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = side;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(82f, 40f);

        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        // A button keeps the focus after a click, which would let Enter or Space press it again
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        RectTransform labelRect = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        label = labelRect.gameObject.AddComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return image;
    }

    void RefreshButtons()
    {
        if (musicButton == null)
        {
            return;
        }
        musicButton.color = MusicOn ? On : Off;
        sfxButton.color = SfxOn ? On : Off;
        musicLabel.color = MusicOn ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        sfxLabel.color = SfxOn ? Color.white : new Color(1f, 1f, 1f, 0.45f);
    }
}
