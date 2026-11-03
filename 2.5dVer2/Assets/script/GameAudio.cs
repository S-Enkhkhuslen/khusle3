using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene-local audio service. Sources live here so destroying a projectile,
// pickup, generator or boarded player does not cut its sound off.
[DefaultExecutionOrder(200)]
public sealed class GameAudio : MonoBehaviour
{
    [Range(0f, 1f)] public float effectsVolume = 0.8f;
    [Range(0f, 1f)] public float warningVolume = 0.55f;
    [Range(0f, 1f)] public float musicVolume = 0.22f;
    [Range(0f, 1f)] public float ambienceVolume = 0.18f;
    private const int VoiceCount = 20;
    private const float MusicTrackGain = 1.25f;
    private static GameAudio instance;
    private static readonly System.Random variation = new System.Random();
    private readonly AudioSource[] voices = new AudioSource[VoiceCount];
    private readonly double[] voiceEnds = new double[VoiceCount];
    private readonly float[] voiceLevels = new float[VoiceCount];
    private AudioClip[] playerShots, enemyShots, impacts;
    private AudioClip[] dustSteps, hardSteps, punches;
    private AudioClip ignition, jetLoop, field, pickup, latch, boom, rocketLoop, warning;
    private AudioClip victory, defeat, swing;
    private AudioSource jetSource, warningSource, rocketSource;
    private AudioSource musicSource, windSource, combatSource;
    private AudioSource sprintSource;
    private PlayerHealth player;
    private Oxygen oxygen;
    private Jetpack jetpack;
    private Sprint sprint;
    private CharacterController playerBody;
    private bool wasJetting, rocketActive, gameFinished, paused, muted, settingsDirty;
    private readonly List<Enemy> battleEnemies = new List<Enemy>();
    private bool battleActive;
    private float combatMix, nextBattleCheck, previousTimeScale;
    private bool inputWasActive, aimWasEnabled, cursorWasVisible;
    private CursorLockMode cursorWasLocked;
    private GUIStyle menuTitle, menuText, menuButton;
    private Unity.Cinemachine.CinemachineInputAxisController[] cameraInputs;
    private bool[] cameraInputStates;
    private int warningTier;
    private float nextWarning, nextHurt;

    private static GameAudio Current
    {
        get
        {
            if (instance == null)
                instance = new GameObject("Game Audio").AddComponent<GameAudio>();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Khusle.Audio.Music", musicVolume));
        effectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Khusle.Audio.Effects", effectsVolume));
        muted = PlayerPrefs.GetInt("Khusle.Audio.Muted", 0) != 0;
        playerShots = LoadVariants("laserLarge", 3);
        enemyShots = LoadVariants("laserSmall", 3);
        impacts = LoadVariants("impactMetal", 3);
        ignition = Load("thrusterFire_000");
        jetLoop = Load("spaceEngineSmall_000");
        field = Load("forceField_000");
        pickup = Load("forceField_001");
        latch = Load("doorClose_000");
        boom = Load("lowFrequency_explosion_000");
        rocketLoop = Load("launch_roar", "Rocket", ".wav");
        dustSteps = LoadVariants("footstep_snow", 3, "Foley");
        hardSteps = LoadVariants("footstep_concrete", 3, "Foley");
        punches = LoadVariants("impactPunch_medium", 3, "Foley");
        victory = Load("escape_success", "Original", ".wav");
        defeat = Load("mission_failed", "Original", ".wav");
        swing = Load("melee_swing", "Original", ".wav");
        for (int i = 0; i < VoiceCount; i++) voices[i] = CreateSource("Effect " + i, 128);
        jetSource = CreateSource("Jetpack Loop", 64);
        jetSource.loop = true;
        jetSource.volume = 0f;
        jetSource.clip = jetLoop;
        warningSource = CreateSource("Oxygen Warning", 16);
        warning = CreateWarning();
        warningSource.clip = warning;
        rocketSource = CreateSource("Rocket Engine", 32);
        rocketSource.loop = true;
        rocketSource.volume = 0f;
        rocketSource.clip = rocketLoop;
        musicSource = CreateLoop("Space Music", "Music/DeadShip", 200);
        windSource = CreateLoop("Desert Wind", "Original/desert_wind", 220);
        combatSource = CreateLoop("Battle Music", "Original/combat_music", 180);
        sprintSource = CreateSource("Sprint Ground Scrape", 80);
        sprintSource.loop = true;
        sprintSource.volume = 0f;
        sprintSource.clip = Load("sprint_scrape", "Original", ".wav");
    }

    private AudioSource CreateLoop(string name, string resource, int priority)
    {
        AudioSource source = CreateSource(name, priority);
        source.clip = Resources.Load<AudioClip>("Audio/" + resource);
        source.loop = true;
        source.volume = 0f;
        if (source.clip != null) source.Play();
        else Debug.LogWarning("GameAudio: missing Audio/" + resource);
        return source;
    }

    private AudioSource CreateSource(string sourceName, int priority)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.mute = muted;
        source.priority = priority;
        source.dopplerLevel = 0f;
        source.minDistance = 8f;
        source.maxDistance = 80f;
        source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }

    private static AudioClip Load(string name, string folder = "SciFi", string extension = ".ogg")
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + folder + "/" + name);
        if (clip == null) Debug.LogWarning("GameAudio: missing Audio/" + folder + "/" + name + extension);
        return clip;
    }

    private static AudioClip[] LoadVariants(string prefix, int count, string folder = "SciFi")
    {
        var clips = new AudioClip[count];
        for (int i = 0; i < count; i++) clips[i] = Load(prefix + "_" + i.ToString("000"), folder);
        return clips;
    }

    private static AudioClip Pick(AudioClip[] clips) { return clips[variation.Next(clips.Length)]; }
    private static float VaryPitch(float min, float max) { return min + (float)variation.NextDouble() * (max - min); }

    private void Play(AudioClip clip, Vector3 position, float volume, float spatial = 1f,
        int priority = 128, float limit = 0f, float pitch = 1f, float maxDistance = 80f)
    {
        if (clip == null || effectsVolume <= 0f) return;
        int slot = -1;
        int leastImportant = priority;
        for (int i = 0; i < VoiceCount; i++)
        {
            if (!voices[i].isPlaying) { slot = i; break; }
            if (voices[i].priority > leastImportant)
            {
                leastImportant = voices[i].priority;
                slot = i;
            }
        }
        if (slot < 0) return; // Do not drown out important cues with distant combat.
        AudioSource source = voices[slot];
        source.Stop();
        source.transform.position = position;
        source.clip = clip;
        source.mute = muted;
        source.volume = volume * effectsVolume;
        voiceLevels[slot] = volume;
        source.spatialBlend = spatial;
        source.maxDistance = maxDistance;
        source.minDistance = Mathf.Min(8f, maxDistance * 0.15f);
        source.priority = priority;
        source.pitch = pitch;
        float duration = clip.length / pitch;
        voiceEnds[slot] = AudioSettings.dspTime + (limit > 0f ? Mathf.Min(limit, duration) : duration);
        source.Play();
    }

    public static void BindPlayer(PlayerHealth health)
    {
        GameAudio audio = Current;
        audio.player = health;
        audio.oxygen = health.GetComponent<Oxygen>();
        audio.jetpack = health.GetComponent<Jetpack>();
        audio.sprint = health.GetComponent<Sprint>();
        audio.playerBody = health.GetComponent<CharacterController>();
    }

    public static void Shot(Vector3 position, bool playerShot)
    {
        GameAudio audio = Current;
        if (audio.rocketActive || audio.gameFinished || audio.paused) return;
        audio.Play(Pick(playerShot ? audio.playerShots : audio.enemyShots), position,
            playerShot ? 0.7f : 0.35f, playerShot ? 0f : 1f, playerShot ? 60 : 150,
            0f, VaryPitch(0.96f, 1.04f));
    }

    public static void Impact(Vector3 position)
    {
        GameAudio audio = Current;
        if (!audio.rocketActive && !audio.gameFinished && !audio.paused)
            audio.Play(Pick(audio.impacts), position, 0.35f, 1f, 170, 0f, VaryPitch(0.94f, 1.06f));
    }

    public static void PlayerHurt()
    {
        GameAudio audio = Current;
        if (Time.unscaledTime < audio.nextHurt || audio.rocketActive || audio.gameFinished) return;
        audio.nextHurt = Time.unscaledTime + 0.18f;
        audio.Play(Pick(audio.punches), Vector3.zero, 0.6f, 0f, 40, 0f, 0.9f);
    }

    public static void PlayerDied()
    {
        GameAudio audio = Current;
        if (audio.paused) audio.SetPaused(false);
        audio.gameFinished = true;
        foreach (AudioSource source in audio.voices) source.Stop();
        audio.jetSource.Stop();
        audio.sprintSource.Stop();
        audio.sprintSource.volume = 0f;
        audio.warningSource.Stop();
        audio.wasJetting = false;
        audio.Play(audio.boom, Vector3.zero, 0.2f, 0f, 30, 0.5f);
        audio.Play(audio.defeat, Vector3.zero, 0.7f, 0f, 20);
    }

    public static void Footstep(Vector3 position, bool isPlayer, bool hardSurface)
    {
        GameAudio audio = Current;
        if (audio.gameFinished || audio.rocketActive || audio.paused) return;
        if (isPlayer && audio.sprint != null && audio.sprint.IsSprinting) return;
        Camera camera = StatusBarUI.FindGameCamera();
        if (!isPlayer && camera != null && (camera.transform.position - position).sqrMagnitude > 18f * 18f) return;
        audio.Play(Pick(hardSurface ? audio.hardSteps : audio.dustSteps), position,
            isPlayer ? 0.23f : 0.14f, isPlayer ? 0f : 1f, isPlayer ? 110 : 210,
            0f, VaryPitch(0.92f, 1.08f), 18f);
    }

    public static void Landed(Vector3 position)
    {
        GameAudio audio = Current;
        if (!audio.gameFinished && !audio.rocketActive)
            audio.Play(Pick(audio.hardSteps), position, 0.45f, 0f, 100, 0f, 0.8f);
    }

    public static void MeleeSwing(Vector3 position)
    {
        GameAudio audio = Current;
        if (!audio.gameFinished && !audio.rocketActive && !audio.paused)
            audio.Play(audio.swing, position, 0.48f, 1f, 120, 0f, VaryPitch(0.95f, 1.05f), 30f);
    }

    public static void EnemyDied(Vector3 position)
    {
        GameAudio audio = Current;
        if (audio.gameFinished || audio.rocketActive) return;
        audio.Play(audio.boom, position, 0.34f, 1f, 100, 0.85f, VaryPitch(0.9f, 1.1f), 55f);
        audio.RefreshBattleState();
    }

    public static void CollectOxygen(bool upgrade = false)
    {
        GameAudio audio = Current;
        audio.Play(audio.pickup, Vector3.zero, 0.55f, 0f, 35, 0f, upgrade ? 1.2f : 1f);
        audio.Play(audio.latch, Vector3.zero, 0.2f, 0f, 80, 0.25f);
        if (audio.oxygen != null && audio.oxygen.CurrentOxygen / Mathf.Max(1f, audio.oxygen.Capacity) > 0.25f)
            audio.warningSource.Stop();
    }

    public static void RegisterBattle(IEnumerable<Enemy> enemies)
    {
        GameAudio audio = Current;
        foreach (Enemy enemy in enemies)
            if (enemy != null && !enemy.IsDead && !audio.battleEnemies.Contains(enemy))
                audio.battleEnemies.Add(enemy);
        audio.RefreshBattleState();
    }

    private void RefreshBattleState()
    {
        float nearest = float.PositiveInfinity;
        for (int i = battleEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = battleEnemies[i];
            if (enemy == null || enemy.IsDead) { battleEnemies.RemoveAt(i); continue; }
            if (player != null && enemy.isActiveAndEnabled)
                nearest = Mathf.Min(nearest, (enemy.transform.position - player.transform.position).sqrMagnitude);
        }
        // Two distances prevent the track changing back and forth at a boundary.
        float range = battleActive ? 70f : 55f;
        battleActive = nearest <= range * range;
        nextBattleCheck = Time.unscaledTime + 0.25f;
    }

    public static void GeneratorActivated(Vector3 position)
    {
        GameAudio audio = Current;
        audio.Play(audio.latch, position, 0.3f, 0.5f, 70, 0.3f);
        audio.Play(audio.field, position, 0.65f, 0.5f, 50);
    }

    public static void RocketReady()
    {
        GameAudio audio = Current;
        audio.Play(audio.pickup, Vector3.zero, 0.7f, 0f, 30, 0f, 1.25f);
    }

    public static void BeginLaunch()
    {
        GameAudio audio = Current;
        foreach (AudioSource source in audio.voices) source.Stop();
        audio.jetSource.Stop();
        audio.sprintSource.Stop();
        audio.sprintSource.volume = 0f;
        audio.warningSource.Stop();
        audio.wasJetting = false;
        audio.rocketActive = true;
        audio.gameFinished = false;
        audio.rocketSource.volume = 0f;
        if (audio.rocketSource.clip != null) audio.rocketSource.Play();
        audio.Play(audio.latch, Vector3.zero, 0.35f, 0f, 30);
    }

    public static void CompleteLaunch()
    {
        GameAudio audio = Current;
        audio.rocketActive = false;
        audio.gameFinished = true;
        audio.Play(audio.victory, Vector3.zero, 0.7f, 0f, 20);
    }

    private void LateUpdate()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.mKey.wasPressedThisFrame)
            { muted = !muted; settingsDirty = true; }
            if (Keyboard.current.escapeKey.wasPressedThisFrame && !gameFinished && !rocketActive
                && player != null && !player.IsDead && player.gameObject.activeInHierarchy)
                SetPaused(!paused);
        }
        double now = AudioSettings.dspTime;
        for (int i = 0; i < VoiceCount; i++)
        {
            voices[i].mute = muted;
            if (!voices[i].isPlaying) continue;
            // Stop the last boot's tail as soon as the player starts sliding.
            if (sprint != null && sprint.IsSprinting && voices[i].spatialBlend == 0f
                && voices[i].clip != null && voices[i].clip.name.StartsWith("footstep_"))
            { voices[i].Stop(); continue; }
            double remaining = voiceEnds[i] - now;
            if (remaining <= 0d) voices[i].Stop();
            else voices[i].volume = voiceLevels[i] * effectsVolume * Mathf.Clamp01((float)remaining / 0.025f);
        }

        float fade = Time.unscaledDeltaTime * 3f;
        rocketSource.volume = Mathf.MoveTowards(rocketSource.volume,
            rocketActive ? effectsVolume * 0.9f : 0f, fade);
        if (!rocketActive && rocketSource.volume <= 0f) rocketSource.Stop();
        rocketSource.mute = muted;

        bool alive = player != null && player.isActiveAndEnabled && !player.IsDead;
        bool gameplay = alive && !rocketActive && !gameFinished && !paused && Time.timeScale > 0f;
        bool groundedSprint = gameplay && sprint != null && sprint.IsSprinting
            && playerBody != null && playerBody.enabled && playerBody.isGrounded
            && (jetpack == null || !jetpack.IsJetting);
        bool jetting = gameplay && ((jetpack != null && jetpack.IsJetting)
            || (sprint != null && sprint.IsSprinting)) && oxygen != null && oxygen.HasOxygen;
        jetSource.pitch = sprint != null && sprint.IsSprinting ? 1.12f : 1f;
        if (jetting && !wasJetting)
        {
            Play(ignition, Vector3.zero, 0.32f, 0f, 65, 0.22f);
            if (jetSource.clip != null && !jetSource.isPlaying) jetSource.Play();
        }
        jetSource.volume = Mathf.MoveTowards(jetSource.volume,
            jetting ? effectsVolume * (groundedSprint ? 0.18f : 0.32f) : 0f, fade);
        if (!jetting && jetSource.volume <= 0f) jetSource.Stop();
        wasJetting = jetting;
        jetSource.mute = muted;

        Vector3 groundVelocity = playerBody != null && playerBody.enabled ? playerBody.velocity : Vector3.zero;
        groundVelocity.y = 0f;
        float groundSpeed = groundVelocity.magnitude;
        bool scraping = groundedSprint && groundSpeed > 0.3f;
        float scrapeTarget = scraping ? effectsVolume * 0.6f * Mathf.Clamp01(groundSpeed / 6f) : 0f;
        sprintSource.pitch = Mathf.Clamp(groundSpeed / 8f, 0.85f, 1.2f);
        if (scraping && !sprintSource.isPlaying && sprintSource.clip != null) sprintSource.Play();
        sprintSource.volume = Mathf.MoveTowards(sprintSource.volume, scrapeTarget, fade);
        if (!scraping && sprintSource.volume <= 0f) sprintSource.Stop();
        sprintSource.mute = muted;

        if (gameplay && Time.unscaledTime >= nextBattleCheck) RefreshBattleState();
        float battleTarget = (gameplay || paused) && battleActive ? 1f : 0f;
        combatMix = Mathf.MoveTowards(combatMix, battleTarget, Time.unscaledDeltaTime * 0.7f);
        float musicLevel = gameFinished ? 0f : Mathf.Clamp01(musicVolume * MusicTrackGain)
            * (rocketActive ? 0.35f : paused ? 0.45f : 1f);
        float musicTarget = musicLevel * (1f - combatMix);
        if (musicTarget > 0f && !musicSource.isPlaying && musicSource.clip != null) musicSource.Play();
        float musicFade = rocketActive || battleActive || combatMix > 0f ? 0.6f : 0.15f;
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicTarget, Time.unscaledDeltaTime * musicFade);
        windSource.volume = Mathf.MoveTowards(windSource.volume, gameplay ? musicVolume * ambienceVolume : 0f, fade * 0.1f);
        combatSource.volume = Mathf.MoveTowards(combatSource.volume, !rocketActive ? musicLevel * 0.95f * combatMix : 0f, fade * 0.2f);
        musicSource.mute = windSource.mute = combatSource.mute = muted;

        float ratio = oxygen != null ? oxygen.CurrentOxygen / Mathf.Max(1f, oxygen.Capacity) : 1f;
        int tier = !gameplay || ratio > 0.25f ? 0 : ratio <= 0.1f ? 2 : 1;
        if (tier == 0) warningSource.Stop();
        else if (tier != warningTier || Time.unscaledTime >= nextWarning)
        {
            warningSource.pitch = tier == 2 ? 1.15f : 1f;
            warningSource.volume = warningVolume * effectsVolume;
            warningSource.Play();
            nextWarning = Time.unscaledTime + (tier == 2 ? 1.2f : 3f);
        }
        warningTier = tier;
        warningSource.volume = warningVolume * effectsVolume;
        warningSource.mute = muted;
    }

    private void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        var input = player != null ? player.GetComponent<PlayerInput>() : null;
        var aim = player != null ? player.GetComponent<TPSAim>() : null;
        if (paused)
        {
            previousTimeScale = Time.timeScale;
            cursorWasLocked = Cursor.lockState; cursorWasVisible = Cursor.visible;
            inputWasActive = input != null && input.inputIsActive;
            aimWasEnabled = aim != null && aim.enabled;
            cameraInputs = Object.FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>();
            cameraInputStates = new bool[cameraInputs.Length];
            for (int i = 0; i < cameraInputs.Length; i++)
            { cameraInputStates[i] = cameraInputs[i].enabled; cameraInputs[i].enabled = false; }
            input?.DeactivateInput();
            if (aim != null) aim.enabled = false;
            sprint?.StopSprinting(); jetpack?.StopJetting();
            foreach (AudioSource source in voices) source.Stop();
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        else
        {
            Time.timeScale = previousTimeScale;
            if (inputWasActive && input != null && player != null && !player.IsDead) input.ActivateInput();
            if (aim != null && player != null && !player.IsDead) aim.enabled = aimWasEnabled;
            if (cameraInputs != null)
                for (int i = 0; i < cameraInputs.Length; i++)
                    if (cameraInputs[i] != null) cameraInputs[i].enabled = cameraInputStates[i];
            Cursor.lockState = cursorWasLocked; Cursor.visible = cursorWasVisible;
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        if (!settingsDirty) return;
        PlayerPrefs.SetFloat("Khusle.Audio.Music", musicVolume);
        PlayerPrefs.SetFloat("Khusle.Audio.Effects", effectsVolume);
        PlayerPrefs.SetInt("Khusle.Audio.Muted", muted ? 1 : 0);
        PlayerPrefs.Save(); settingsDirty = false;
    }

    private void OnGUI()
    {
        if (gameFinished || rocketActive || player == null || player.IsDead) return;
        if (menuTitle == null)
        {
            menuTitle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 28 };
            menuText = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 18 };
            menuButton = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        }
        if (!paused)
        {
            GUI.Label(new Rect(12f, Screen.height - 28f, 240f, 25f), muted ? "ESC  SETTINGS    M  UNMUTE" : "ESC  SETTINGS    M  MUTE");
            return;
        }
        Matrix4x4 matrix = GUI.matrix; Color color = GUI.color; int depth = GUI.depth;
        GUI.depth = -2500;
        float scale = Mathf.Max(0.65f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
        float width = Mathf.Min(480f, (Screen.width - 24f) / scale);
        float top = Screen.height / scale * 0.5f - 170f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width * 0.5f, 0f), Quaternion.identity, Vector3.one * scale);
        GUI.color = new Color(0.025f, 0.04f, 0.065f, 0.96f);
        GUI.DrawTexture(new Rect(-width * 0.5f, top, width, 340f), Texture2D.whiteTexture);
        GUI.color = new Color(0.2f, 0.85f, 1f);
        GUI.Label(new Rect(-width * 0.5f, top + 16f, width, 45f), "PAUSED", menuTitle);
        GUI.color = Color.white;
        float inner = width - 36f;
        GUI.Label(new Rect(-inner * 0.5f, top + 76f, inner, 30f), "MUSIC  " + Mathf.RoundToInt(musicVolume * 100f) + "%", menuText);
        float music = GUI.HorizontalSlider(new Rect(-inner * 0.5f, top + 112f, inner, 24f), musicVolume, 0f, 1f);
        GUI.Label(new Rect(-inner * 0.5f, top + 151f, inner, 30f), "SOUND EFFECTS  " + Mathf.RoundToInt(effectsVolume * 100f) + "%", menuText);
        float effects = GUI.HorizontalSlider(new Rect(-inner * 0.5f, top + 187f, inner, 24f), effectsVolume, 0f, 1f);
        if (music != musicVolume || effects != effectsVolume) settingsDirty = true;
        musicVolume = music; effectsVolume = effects;
        if (GUI.Button(new Rect(-inner * 0.5f, top + 228f, inner, 38f), muted ? "UNMUTE  (M)" : "MUTE ALL  (M)", menuButton))
        { muted = !muted; settingsDirty = true; }
        bool resume = GUI.Button(new Rect(-inner * 0.5f, top + 280f, inner, 38f), "RESUME  (ESC)", menuButton);
        GUI.matrix = matrix; GUI.color = color; GUI.depth = depth;
        if (resume) SetPaused(false);
    }

    // This pack has no warning beep; synthesize an original two-pulse suit alarm.
    private static AudioClip CreateWarning()
    {
        const int rate = 44100;
        var samples = new float[(int)(rate * 0.38f)];
        for (int i = 0; i < samples.Length; i++)
        {
            float time = i / (float)rate;
            float pulse = time < 0.14f ? time : time >= 0.23f ? time - 0.23f : -1f;
            if (pulse < 0f || pulse >= 0.14f) continue;
            float envelope = Mathf.Clamp01(pulse / 0.01f) * Mathf.Clamp01((0.14f - pulse) / 0.025f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * time) * envelope * 0.3f;
        }
        AudioClip clip = AudioClip.Create("Suit Oxygen Warning", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (paused)
        {
            Time.timeScale = previousTimeScale;
            Cursor.lockState = cursorWasLocked; Cursor.visible = cursorWasVisible;
        }
        SaveSettings();
        if (warning != null) Destroy(warning);
        if (instance == this) instance = null;
    }
}
