using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RespawnPoint : MonoBehaviour
{
    [SerializeField] private bool oneTime = true;
    [SerializeField] private string activateDialogue = "Checkpoint reached.";
    [SerializeField] private float dialogueDuration = 2f;

    private bool activated = false;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneTime && activated) return;

        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null) return;

        if (respawn.GetRespawnPoint() == transform) return;

        respawn.SetRespawnPoint(transform);
        activated = true;

        if (!string.IsNullOrEmpty(activateDialogue))
            DialogueBox.Show(activateDialogue, dialogueDuration);
    }
}
