using System.Collections.Generic;
using UnityEngine;

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
    public static GameAudio Existing => instance;
    public bool IsPaused => paused;
    public bool IsMuted => muted;
    public bool HasActivePlayer => player != null && player.isActiveAndEnabled && !player.IsDead && !gameFinished && !rocketActive;
    private static readonly System.Random variation = new System.Random();
    private readonly AudioSource[] voices = new AudioSource[VoiceCount];
    private readonly double[] voiceEnds = new double[VoiceCount];
    private readonly float[] voiceLevels = new float[VoiceCount];
    private GameSounds sounds;
    private readonly PauseState pauseState = new PauseState();
    private readonly UIKeyActions keys = new UIKeyActions();
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
    private float combatMix, nextBattleCheck;
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
        keys.Add("escape", () => { if (HasActivePlayer) SetPaused(!paused); });
        keys.Add("m", () => { muted = !muted; settingsDirty = true; });
        sounds = new GameSounds();
        for (int i = 0; i < VoiceCount; i++) voices[i] = CreateSource("Effect " + i, 128);
        jetSource = CreateSource("Jetpack Loop", 64);
        jetSource.loop = true;
        jetSource.volume = 0f;
        jetSource.clip = sounds.jetLoop;
        warningSource = CreateSource("Oxygen Warning", 16);
        warningSource.clip = sounds.warning;
        rocketSource = CreateSource("Rocket Engine", 32);
        rocketSource.loop = true;
        rocketSource.volume = 0f;
        rocketSource.clip = sounds.rocketLoop;
        musicSource = CreateLoop("Space Music", "Music/DeadShip", 200);
        windSource = CreateLoop("Desert Wind", "Original/desert_wind", 220);
        combatSource = CreateLoop("Battle Music", "Original/combat_music", 180);
        sprintSource = CreateSource("Sprint Ground Scrape", 80);
        sprintSource.loop = true;
        sprintSource.volume = 0f;
        sprintSource.clip = sounds.sprintScrape;
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
        audio.Play(Pick(playerShot ? audio.sounds.playerShots : audio.sounds.enemyShots), position,
            playerShot ? 0.7f : 0.35f, playerShot ? 0f : 1f, playerShot ? 60 : 150,
            0f, VaryPitch(0.96f, 1.04f));
    }

    public static void Impact(Vector3 position)
    {
        GameAudio audio = Current;
        if (!audio.rocketActive && !audio.gameFinished && !audio.paused)
            audio.Play(Pick(audio.sounds.impacts), position, 0.35f, 1f, 170, 0f, VaryPitch(0.94f, 1.06f));
    }

    public static void PlayerHurt()
    {
        GameAudio audio = Current;
        if (Time.unscaledTime < audio.nextHurt || audio.rocketActive || audio.gameFinished) return;
        audio.nextHurt = Time.unscaledTime + 0.18f;
        audio.Play(Pick(audio.sounds.punches), Vector3.zero, 0.6f, 0f, 40, 0f, 0.9f);
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
        audio.Play(audio.sounds.boom, Vector3.zero, 0.2f, 0f, 30, 0.5f);
        audio.Play(audio.sounds.defeat, Vector3.zero, 0.7f, 0f, 20);
    }

    public static void Footstep(Vector3 position, bool isPlayer, bool hardSurface)
    {
        GameAudio audio = Current;
        if (audio.gameFinished || audio.rocketActive || audio.paused) return;
        if (isPlayer && audio.sprint != null && audio.sprint.IsSprinting) return;
        Camera camera = StatusBarUI.FindGameCamera();
        if (!isPlayer && camera != null && (camera.transform.position - position).sqrMagnitude > 18f * 18f) return;
        audio.Play(Pick(hardSurface ? audio.sounds.hardSteps : audio.sounds.dustSteps), position,
            isPlayer ? 0.23f : 0.14f, isPlayer ? 0f : 1f, isPlayer ? 110 : 210,
            0f, VaryPitch(0.92f, 1.08f), 18f);
    }

    public static void Landed(Vector3 position)
    {
        GameAudio audio = Current;
        if (!audio.gameFinished && !audio.rocketActive)
            audio.Play(Pick(audio.sounds.hardSteps), position, 0.45f, 0f, 100, 0f, 0.8f);
    }

    public static void MeleeSwing(Vector3 position)
    {
        GameAudio audio = Current;
        if (!audio.gameFinished && !audio.rocketActive && !audio.paused)
            audio.Play(audio.sounds.swing, position, 0.48f, 1f, 120, 0f, VaryPitch(0.95f, 1.05f), 30f);
    }

    public static void EnemyDied(Vector3 position)
    {
        GameAudio audio = Current;
        if (audio.gameFinished || audio.rocketActive) return;
        audio.Play(audio.sounds.boom, position, 0.34f, 1f, 100, 0.85f, VaryPitch(0.9f, 1.1f), 55f);
        audio.RefreshBattleState();
    }

    public static void CollectOxygen(bool upgrade = false)
    {
        GameAudio audio = Current;
        audio.Play(audio.sounds.pickup, Vector3.zero, 0.55f, 0f, 35, 0f, upgrade ? 1.2f : 1f);
        audio.Play(audio.sounds.latch, Vector3.zero, 0.2f, 0f, 80, 0.25f);
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
        audio.Play(audio.sounds.latch, position, 0.3f, 0.5f, 70, 0.3f);
        audio.Play(audio.sounds.field, position, 0.65f, 0.5f, 50);
    }

    public static void RocketReady()
    {
        GameAudio audio = Current;
        audio.Play(audio.sounds.pickup, Vector3.zero, 0.7f, 0f, 30, 0f, 1.25f);
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
        audio.Play(audio.sounds.latch, Vector3.zero, 0.35f, 0f, 30);
    }

    public static void CompleteLaunch()
    {
        GameAudio audio = Current;
        audio.rocketActive = false;
        audio.gameFinished = true;
        audio.Play(audio.sounds.victory, Vector3.zero, 0.7f, 0f, 20);
    }

    private void LateUpdate()
    {
        UpdateEffects();
        bool gameplay = player != null && player.isActiveAndEnabled && !player.IsDead
            && !rocketActive && !gameFinished && !paused && Time.timeScale > 0f;
        FadeLoop(rocketSource, rocketActive ? effectsVolume * 0.9f : 0f);
        UpdateMovementAudio(gameplay);
        UpdateMusic(gameplay);
        UpdateOxygenWarning(gameplay);
    }

    private void UpdateEffects()
    {
        double now = AudioSettings.dspTime;
        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource source = voices[i]; source.mute = muted;
            if (!source.isPlaying) continue;
            if (sprint != null && sprint.IsSprinting && source.spatialBlend == 0f
                && source.clip != null && source.clip.name.StartsWith("footstep_"))
            { source.Stop(); continue; }
            double remaining = voiceEnds[i] - now;
            if (remaining <= 0d) source.Stop();
            else source.volume = voiceLevels[i] * effectsVolume * Mathf.Clamp01((float)remaining / 0.025f);
        }
    }

    private void UpdateMovementAudio(bool gameplay)
    {
        bool sprinting = sprint != null && sprint.IsSprinting;
        bool flying = jetpack != null && jetpack.IsJetting;
        bool groundedSprint = gameplay && sprinting && !flying && playerBody != null
            && playerBody.enabled && playerBody.isGrounded;
        bool jetting = gameplay && (flying || sprinting) && oxygen != null && oxygen.HasOxygen;
        jetSource.pitch = sprinting ? 1.12f : 1f;
        if (jetting && !wasJetting) Play(sounds.ignition, Vector3.zero, 0.32f, 0f, 65, 0.22f);
        FadeLoop(jetSource, jetting ? effectsVolume * (groundedSprint ? 0.18f : 0.32f) : 0f);
        wasJetting = jetting;
        Vector3 velocity = playerBody != null && playerBody.enabled ? playerBody.velocity : Vector3.zero;
        velocity.y = 0f; float speed = velocity.magnitude;
        bool scraping = groundedSprint && speed > 0.3f;
        sprintSource.pitch = Mathf.Clamp(speed / 8f, 0.85f, 1.2f);
        FadeLoop(sprintSource, scraping ? effectsVolume * 0.6f * Mathf.Clamp01(speed / 6f) : 0f);
    }

    private void UpdateMusic(bool gameplay)
    {
        if (gameplay && Time.unscaledTime >= nextBattleCheck) RefreshBattleState();
        float target = (gameplay || paused) && battleActive ? 1f : 0f;
        combatMix = Mathf.MoveTowards(combatMix, target, Time.unscaledDeltaTime * 0.7f);
        float level = gameFinished ? 0f : Mathf.Clamp01(musicVolume * MusicTrackGain)
            * (rocketActive ? 0.35f : paused ? 0.45f : 1f);
        float fade = rocketActive || battleActive || combatMix > 0f ? 0.6f : 0.15f;
        FadeLoop(musicSource, level * (1f - combatMix), fade, false);
        FadeLoop(windSource, gameplay ? musicVolume * ambienceVolume : 0f, 0.3f, false);
        FadeLoop(combatSource, !rocketActive ? level * 0.95f * combatMix : 0f, 0.6f, false);
    }

    private void UpdateOxygenWarning(bool gameplay)
    {
        warningSource.volume = warningVolume * effectsVolume; warningSource.mute = muted;
        float ratio = oxygen != null ? oxygen.CurrentOxygen / Mathf.Max(1f, oxygen.Capacity) : 1f;
        int tier = !gameplay || ratio > 0.25f ? 0 : ratio <= 0.1f ? 2 : 1;
        if (tier == 0) warningSource.Stop();
        else if (tier != warningTier || Time.unscaledTime >= nextWarning)
        {
            warningSource.pitch = tier == 2 ? 1.15f : 1f;
            warningSource.Play(); nextWarning = Time.unscaledTime + (tier == 2 ? 1.2f : 3f);
        }
        warningTier = tier;
    }

    // All loops use unscaled fades, including the frozen-world rocket sequence.
    private void FadeLoop(AudioSource source, float target, float rate = 3f, bool stopWhenSilent = true)
    {
        if (target > 0f && !source.isPlaying && source.clip != null) source.Play();
        source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * rate);
        source.mute = muted;
        if (stopWhenSilent && target <= 0f && source.volume <= 0f) source.Stop();
    }

    public void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        if (paused)
        {
            pauseState.Pause(player);
            sprint?.StopSprinting(); jetpack?.StopJetting();
            foreach (AudioSource source in voices) source.Stop();
        }
        else { pauseState.Resume(); SaveSettings(); }
    }

    private void SaveSettings()
    {
        if (!settingsDirty) return;
        PlayerPrefs.SetFloat("Khusle.Audio.Music", musicVolume);
        PlayerPrefs.SetFloat("Khusle.Audio.Effects", effectsVolume);
        PlayerPrefs.SetInt("Khusle.Audio.Muted", muted ? 1 : 0);
        PlayerPrefs.Save(); settingsDirty = false;
    }

    public void SetAudioSettings(float music, float effects, bool mute)
    {
        musicVolume = Mathf.Clamp01(music); effectsVolume = Mathf.Clamp01(effects); muted = mute;
        settingsDirty = true; SaveSettings();
    }

    private void OnDestroy()
    {
        if (paused) pauseState.Resume();
        keys.Dispose(); SaveSettings(); sounds?.Dispose();
        if (instance == this) instance = null;
    }
}
