using UnityEngine;

// Loads the clips once; GameAudio decides when to play them.
public sealed class GameSounds
{
    public readonly AudioClip[] playerShots, enemyShots, impacts, dustSteps, hardSteps, punches;
    public readonly AudioClip ignition, jetLoop, field, pickup, latch, boom, rocketLoop, warning;
    public readonly AudioClip victory, defeat, swing, sprintScrape;

    public GameSounds()
    {
        playerShots = Variants("laserLarge"); enemyShots = Variants("laserSmall"); impacts = Variants("impactMetal");
        dustSteps = Variants("footstep_snow", "Foley"); hardSteps = Variants("footstep_concrete", "Foley");
        punches = Variants("impactPunch_medium", "Foley");
        ignition = Load("thrusterFire_000"); jetLoop = Load("spaceEngineSmall_000");
        field = Load("forceField_000"); pickup = Load("forceField_001"); latch = Load("doorClose_000");
        boom = Load("lowFrequency_explosion_000"); rocketLoop = Load("launch_roar", "Rocket");
        victory = Load("escape_success", "Original"); defeat = Load("mission_failed", "Original");
        swing = Load("melee_swing", "Original"); sprintScrape = Load("sprint_scrape", "Original");
        warning = CreateWarning();
    }
    private static AudioClip Load(string name, string folder = "SciFi")
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + folder + "/" + name);
        if (clip == null) Debug.LogWarning("GameAudio: missing Audio/" + folder + "/" + name);
        return clip;
    }
    private static AudioClip[] Variants(string prefix, string folder = "SciFi")
    {
        var clips = new AudioClip[3];
        for (int i = 0; i < clips.Length; i++) clips[i] = Load(prefix + "_" + i.ToString("000"), folder);
        return clips;
    }
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
        clip.SetData(samples, 0); return clip;
    }
    public void Dispose() { if (warning != null) Object.Destroy(warning); }
}
