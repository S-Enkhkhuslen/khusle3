using UnityEngine;
using UnityEngine.UI;

public sealed class RocketPromptView : MonoBehaviour
{
    [SerializeField] private GameObject panel, keyBadge, progressTrack;
    [SerializeField] private Text titleText, detailText;
    [SerializeField] private Image progressFill;
    public void Refresh(bool nearby, bool launching, float progress, float distance)
    {
        panel.SetActive(true); keyBadge.SetActive(nearby && !launching); progressTrack.SetActive(launching);
        titleText.text = launching ? "LEAVING THE PLANET" : nearby ? "LAUNCH ROCKET" : "ESCAPE ROCKET READY";
        detailText.text = launching ? "Launch in progress..." : nearby ? "Press E to leave the planet" : "Approach the rocket   " + Mathf.CeilToInt(distance) + " m";
        progressFill.fillAmount = Mathf.Clamp01(progress);
    }
    public void Hide() { panel.SetActive(false); }
}
