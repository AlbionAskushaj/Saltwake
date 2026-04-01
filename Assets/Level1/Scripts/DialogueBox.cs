using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueBox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Animation")]
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private float charsPerSecond = 30f;

    private Coroutine activeRoutine;

    private static DialogueBox instance;

    private void Awake()
    {
        instance = this;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public static void Show(string message, float holdDuration = 3f)
    {
        if (instance == null)
            instance = FindObjectOfType<DialogueBox>(true);
        if (instance == null) return;
        instance.gameObject.SetActive(true);

        if (instance.activeRoutine != null)
            instance.StopCoroutine(instance.activeRoutine);

        instance.activeRoutine = instance.StartCoroutine(instance.ShowRoutine(message, holdDuration));
    }

    private IEnumerator ShowRoutine(string message, float holdDuration)
    {
        dialogueText.text = "";

        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        for (int i = 0; i < message.Length; i++)
        {
            dialogueText.text = message.Substring(0, i + 1);
            yield return new WaitForSeconds(1f / charsPerSecond);
        }

        yield return new WaitForSeconds(holdDuration);

        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        activeRoutine = null;
    }
}
