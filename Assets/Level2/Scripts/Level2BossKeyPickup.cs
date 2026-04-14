using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level 2 boss key: touching the player loads the next scene (Level 3 by default build order).
/// Uses <see cref="WaveManager.nextLevelBuildIndex"/> when the serialized override is unset.
/// </summary>
public class Level2BossKeyPickup : MonoBehaviour
{
    [Tooltip("Leave at -1 to use WaveManager.instance.nextLevelBuildIndex (set on WaveManager in Level 2).")]
    [SerializeField] private int nextLevelBuildIndexOverride = -1;

    private bool picked;

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryPickup(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryPickup(other.gameObject);
    }

    void TryPickup(GameObject other)
    {
        if (picked || other == null || !other.CompareTag("Player"))
            return;

        int idx = nextLevelBuildIndexOverride;
        if (idx < 0 && WaveManager.instance != null)
            idx = WaveManager.instance.nextLevelBuildIndex;
        if (idx < 0)
            idx = 2;

        picked = true;
        Level2Progress.ClearResumeWave();
        SceneManager.LoadSceneAsync(idx);
    }
}
