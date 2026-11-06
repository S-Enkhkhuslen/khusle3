using UnityEngine;
using UnityEngine.UI;

public sealed class MenuHelpTabs : MonoBehaviour
{
    [SerializeField] private GameObject controls, rules;
    [SerializeField] private ScrollRect controlsScroll, rulesScroll;
    public void ShowControls() { controls.SetActive(true); rules.SetActive(false); controlsScroll.verticalNormalizedPosition = 1f; }
    public void ShowRules() { controls.SetActive(false); rules.SetActive(true); rulesScroll.verticalNormalizedPosition = 1f; }
}
