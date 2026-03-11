using UnityEngine;
using TMPro;
using System.Collections;

public class RevealBlocksAndDialogueTrigger : MonoBehaviour
{
    [Header("Object To Reveal")]
    [SerializeField] private GameObject objectToReveal;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Message")]
    [TextArea(2, 5)]
    [SerializeField] private string message = "A path opens ahead.";
    [SerializeField] private float displayTime = 3f;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnlyOnce && hasTriggered) return;

        hasTriggered = true;

        if (objectToReveal != null)
        {
            objectToReveal.SetActive(true);
        }

        StartCoroutine(ShowDialogue());
    }

    private IEnumerator ShowDialogue()
    {
        dialogueBox.SetActive(true);
        dialogueText.text = message;

        yield return new WaitForSeconds(displayTime);

        dialogueBox.SetActive(false);
    }
}