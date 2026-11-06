using UnityEngine;

// Uses the same saved settings as the in-game pause menu.
public sealed class MenuAudio
{
    public float music = Mathf.Clamp01(PlayerPrefs.GetFloat("Khusle.Audio.Music", 0.22f));
    public float effects = Mathf.Clamp01(PlayerPrefs.GetFloat("Khusle.Audio.Effects", 0.8f));
    public bool muted = PlayerPrefs.GetInt("Khusle.Audio.Muted", 0) != 0;
    private readonly AudioSource musicSource, effectSource;

    public MenuAudio(GameObject owner)
    {
        musicSource = owner.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.clip = Resources.Load<AudioClip>("Audio/Music/DeadShip");
        effectSource = owner.AddComponent<AudioSource>();
        effectSource.playOnAwake = false;
        effectSource.clip = Resources.Load<AudioClip>("Audio/SciFi/doorClose_000");
        Apply();
        if (musicSource.clip != null) musicSource.Play();
    }

    public void Apply()
    {
        musicSource.volume = Mathf.Clamp01(music * 1.25f);
        effectSource.volume = effects * 0.18f;
        musicSource.mute = effectSource.mute = muted;
    }
    public void Click() { if (effectSource.clip != null) effectSource.Play(); }
    public void Save()
    {
        PlayerPrefs.SetFloat("Khusle.Audio.Music", music);
        PlayerPrefs.SetFloat("Khusle.Audio.Effects", effects);
        PlayerPrefs.SetInt("Khusle.Audio.Muted", muted ? 1 : 0);
        PlayerPrefs.Save();
    }
}
