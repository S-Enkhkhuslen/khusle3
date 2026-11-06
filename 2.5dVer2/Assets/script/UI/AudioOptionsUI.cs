using UnityEngine;
using UnityEngine.UI;

// Both Options screens use these serialized sliders and the same saved settings.
public sealed class AudioOptionsUI : MonoBehaviour
{
    [SerializeField] private Slider musicSlider, effectsSlider;
    [SerializeField] private Text musicValue, effectsValue, muteLabel;
    private MenuAudio menuAudio;
    private GameAudio gameAudio;
    private float Music => menuAudio != null ? menuAudio.music : gameAudio != null ? gameAudio.musicVolume : 0f;
    private float Effects => menuAudio != null ? menuAudio.effects : gameAudio != null ? gameAudio.effectsVolume : 0f;
    private bool Muted => menuAudio != null ? menuAudio.muted : gameAudio != null && gameAudio.IsMuted;

    public void Bind(MenuAudio audio) { menuAudio = audio; gameAudio = null; Refresh(); }
    public void Bind(GameAudio audio) { gameAudio = audio; menuAudio = null; Refresh(); }
    public void SetMusic(float value) { Save(value, Effects, Muted); }
    public void SetEffects(float value) { Save(Music, value, Muted); }
    public void ToggleMute() { Save(Music, Effects, !Muted); }
    public void TestSound() { menuAudio?.Click(); }
    private void Save(float music, float effects, bool muted)
    {
        if (menuAudio != null)
        {
            menuAudio.music = music; menuAudio.effects = effects; menuAudio.muted = muted;
            menuAudio.Apply(); menuAudio.Save();
        }
        else if (gameAudio != null) gameAudio.SetAudioSettings(music, effects, muted);
        Refresh();
    }
    public void Refresh()
    {
        musicSlider.SetValueWithoutNotify(Music); effectsSlider.SetValueWithoutNotify(Effects);
        musicValue.text = Mathf.RoundToInt(Music * 100f) + "%";
        effectsValue.text = Mathf.RoundToInt(Effects * 100f) + "%";
        muteLabel.text = Muted ? "UNMUTE ALL   [M]" : "MUTE ALL   [M]";
    }
    private void OnEnable() { if (menuAudio != null || gameAudio != null) Refresh(); }
}
