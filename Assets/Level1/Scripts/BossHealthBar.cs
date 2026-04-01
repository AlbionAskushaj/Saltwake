using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float fadeOutTime = 1f;

    private static BossHealthBar instance;

    private void Awake()
    {
        instance = this;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public static void Show(string bossName, float maxHealth)
    {
        if (instance == null)
            instance = FindObjectOfType<BossHealthBar>(true);
        if (instance == null) return;
        instance.gameObject.SetActive(true);
        instance.bossNameText.text = bossName;
        instance.healthSlider.maxValue = maxHealth;
        instance.healthSlider.value = maxHealth;
        instance.StartCoroutine(instance.FadeIn());
    }

    public static void UpdateHealth(float currentHealth)
    {
        if (instance == null) return;
        instance.healthSlider.value = Mathf.Max(0f, currentHealth);
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.StartCoroutine(instance.FadeOut());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
