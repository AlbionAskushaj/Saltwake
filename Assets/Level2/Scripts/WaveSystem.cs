using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;

    public GameObject batPrefab;
    public Transform[] spawnPoints;
    private int batsRemainingToSpawn = 0;
    private int batsAlive = 0;
    public int currentWaveNumber = 0;

    public GameObject bossPrefab;

    [Header("Level Transition")]
    public int nextLevelBuildIndex = -1;

    [Header("Checkpoints")]
    [Tooltip("Respawn position when Level 2 loads (before wave 1). Required so deaths stay in this scene.")]
    [SerializeField] private Transform levelEntryCheckpoint;
    [Tooltip("Optional per-wave respawn positions; null entries use levelEntryCheckpoint.")]
    [SerializeField] private Transform[] waveCheckpointTransforms = new Transform[5];
    [SerializeField] private string[] checkpointMessages = new string[5]
    {
        "Checkpoint reached.",
        "Checkpoint reached.",
        "Checkpoint reached.",
        "Checkpoint reached.",
        "Checkpoint reached."
    };
    [SerializeField] private float dialogueDuration = 2f;

    [Header("Flavor dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string entryFlavorLine = "Oh… how'd I get up here?";
    [SerializeField] private float entryFlavorDelay = 3.5f;
    [SerializeField] private float entryFlavorHold = 3f;

    [TextArea(2, 4)]
    [SerializeField] private string wave2FlavorLine = "Oh great, there's more!";
    [TextArea(2, 4)]
    [SerializeField] private string wave4FlavorLine = "You've got to be kidding—again?!";
    [SerializeField] private float waveFollowUpDelay = 3.5f;
    [SerializeField] private float waveFlavorHold = 3f;

    [TextArea(2, 4)]
    [SerializeField] private string wave1FirstShotLine = "Whoa—what's that coming at me?!";
    [TextArea(2, 4)]
    [SerializeField] private string wave3FirstShotLine = "What?! The bullets are coming right at me!";
    [SerializeField] private float projectileReactionHold = 3f;

    [TextArea(2, 4)]
    [SerializeField] private string bossSpawnLine = "Oh no… why is there an angry cloud?!";
    [SerializeField] private float bossSpawnHold = 3f;

    [TextArea(2, 4)]
    [SerializeField] private string bossDefeatFirstLine = "Phew, that was scary..";
    [SerializeField] private float bossDefeatFirstHold = 3f;
    [SerializeField] private float bossDefeatBetweenLinesDelay = 4.5f;
    [TextArea(2, 4)]
    [SerializeField] private string bossDefeatKeyLine = "Oh, it dropped a yellow key, let me grab it!";
    [SerializeField] private float bossDefeatKeyHold = 3f;
    [SerializeField] private float bossDefeatLevelLoadDelay = 4f;

    private bool wave1ShotLineDone;
    private bool wave3ShotLineDone;

    // Make currentWaveNumber publicly accessible
    public int CurrentWaveNumber => currentWaveNumber;


    public TMP_Text waveText; // Add this line

    private PlayerRespawn cachedPlayerRespawn;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        int resumeWave = Level2Progress.ConsumeResumeWave();
        if (resumeWave >= 1 && resumeWave <= 5)
            StartCoroutine(StartFromResumeWave(resumeWave));
        else
        {
            ApplyEntryRespawnOnly();
            StartNextWave();
            if (!string.IsNullOrEmpty(entryFlavorLine))
                StartCoroutine(ShowEntryFlavorRoutine());
        }
    }

    IEnumerator ShowEntryFlavorRoutine()
    {
        yield return new WaitForSeconds(entryFlavorDelay);
        DialogueBox.Show(entryFlavorLine, entryFlavorHold);
    }

    IEnumerator ShowDelayedFlavorLine(string line, float delay, float hold)
    {
        if (string.IsNullOrEmpty(line))
            yield break;
        yield return new WaitForSeconds(delay);
        DialogueBox.Show(line, hold);
    }

    IEnumerator StartFromResumeWave(int resumeWave)
    {
        yield return null;
        yield return null;

        PreApplyCloudSpeedForResume(resumeWave);
        currentWaveNumber = resumeWave - 1;
        PositionPlayerAtWaveCheckpoint(resumeWave);
        StartNextWave(showCheckpointDialogue: false);
    }

    private void PreApplyCloudSpeedForResume(int targetWave)
    {
        if (targetWave < 3)
            return;
        int extraIncreases = targetWave - 2;
        Cloud[] clouds = FindObjectsOfType<Cloud>();
        for (int i = 0; i < extraIncreases; i++)
        {
            foreach (Cloud cloud in clouds)
                cloud.IncreaseSpeed(0.5f);
        }
    }

    private Transform GetCheckpointTransformForWave(int waveNumber)
    {
        if (waveNumber < 1 || waveNumber > 5)
            return null;
        int idx = waveNumber - 1;
        Transform t = levelEntryCheckpoint;
        if (waveCheckpointTransforms != null && idx < waveCheckpointTransforms.Length && waveCheckpointTransforms[idx] != null)
            t = waveCheckpointTransforms[idx];
        return t;
    }

    private void PositionPlayerAtWaveCheckpoint(int waveNumber)
    {
        Transform t = GetCheckpointTransformForWave(waveNumber);
        if (t == null)
            return;
        PlayerRespawn pr = GetPlayerRespawn();
        if (pr == null)
            return;
        pr.SetRespawnPoint(t);
        pr.Respawn();
    }

    private PlayerRespawn GetPlayerRespawn()
    {
        if (cachedPlayerRespawn != null)
            return cachedPlayerRespawn;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;
        cachedPlayerRespawn = player.GetComponent<PlayerRespawn>();
        return cachedPlayerRespawn;
    }

    private void ApplyEntryRespawnOnly()
    {
        if (levelEntryCheckpoint == null)
        {
            Debug.LogWarning("WaveManager: assign levelEntryCheckpoint so the player respawns in Level 2.");
            return;
        }
        PlayerRespawn pr = GetPlayerRespawn();
        if (pr != null)
            pr.SetRespawnPoint(levelEntryCheckpoint);
    }

    private void ApplyCheckpointForWave(int waveNumber, bool showDialogue = true)
    {
        if (waveNumber < 1 || waveNumber > 5)
            return;
        int idx = waveNumber - 1;
        Transform t = GetCheckpointTransformForWave(waveNumber);
        if (t == null)
            return;
        PlayerRespawn pr = GetPlayerRespawn();
        if (pr != null)
            pr.SetRespawnPoint(t);

        if (!showDialogue)
            return;

        string msg = "Checkpoint reached.";
        if (checkpointMessages != null && idx < checkpointMessages.Length && !string.IsNullOrEmpty(checkpointMessages[idx]))
            msg = checkpointMessages[idx];
        DialogueBox.Show(msg, dialogueDuration);
    }

    public void NotifyProjectileSpawned()
    {
        if (currentWaveNumber == 1 && !wave1ShotLineDone && !string.IsNullOrEmpty(wave1FirstShotLine))
        {
            wave1ShotLineDone = true;
            DialogueBox.Show(wave1FirstShotLine, projectileReactionHold);
        }
        else if (currentWaveNumber == 3 && !wave3ShotLineDone && !string.IsNullOrEmpty(wave3FirstShotLine))
        {
            wave3ShotLineDone = true;
            DialogueBox.Show(wave3FirstShotLine, projectileReactionHold);
        }
    }

    public void StartNextWave(bool showCheckpointDialogue = true)
    {
        if (currentWaveNumber < 5) // Check for waves 1 through 5
        {
            currentWaveNumber++;

            ApplyCheckpointForWave(currentWaveNumber, showCheckpointDialogue);

            if (showCheckpointDialogue)
            {
                if (currentWaveNumber == 2)
                    StartCoroutine(ShowDelayedFlavorLine(wave2FlavorLine, waveFollowUpDelay, waveFlavorHold));
                else if (currentWaveNumber == 4)
                    StartCoroutine(ShowDelayedFlavorLine(wave4FlavorLine, waveFollowUpDelay, waveFlavorHold));
            }

            // Increase cloud speed starting from Wave 2
            if (currentWaveNumber >= 2)
            {
                Cloud[] clouds = FindObjectsOfType<Cloud>();
                foreach (Cloud cloud in clouds)
                {
                    cloud.IncreaseSpeed(0.5f);
                }
            }


            // Differentiate the text for the final wave
            if (currentWaveNumber == 5)
            {
                if (waveText != null)
                {
                    waveText.text = "Final Wave!";
                    waveText.alpha = 1;
                    StartCoroutine(HideWaveTextAfterDelay(5f));
                }
            }
            else
            {
                if (waveText != null)
                {
                    waveText.text = "WAVE " + currentWaveNumber + "!";
                    waveText.alpha = 1;
                    StartCoroutine(HideWaveTextAfterDelay(5f));
                }
            }

            // Flickering effects for Clouds and Skys
            Cloud[] cloudFlickers = FindObjectsOfType<Cloud>();
            foreach (Cloud cloud in cloudFlickers)
            {
                cloud.StartFlickering();
            }

            Sky[] skys = FindObjectsOfType<Sky>();
            foreach (Sky sky in skys)
            {
                sky.StartFlickering();
            }

            if (currentWaveNumber <= 4)
            {
                batsRemainingToSpawn = (currentWaveNumber == 2 || currentWaveNumber == 4) ? 3 : 1;
                batsAlive = batsRemainingToSpawn;
                StartCoroutine(SpawnWave());
            }
            else // For Wave 5, spawn the final boss
            {
                SpawnBoss();
            }
        }
        else
        {
            Debug.Log("All waves complete!");
            if (nextLevelBuildIndex >= 0)
            {
                StartCoroutine(LoadNextLevel());
            }
        }
    }






    void SpawnBoss()
    {
        if (bossPrefab != null)
        {
            GameObject bossGO = Instantiate(bossPrefab, spawnPoints[0].position, Quaternion.identity);
            BatEnemy bossScript = bossGO.GetComponent<BatEnemy>();

            if (bossScript != null)
            {
                bossScript.isBoss = true; // Flag this instance as the boss
                                          // Adjust endPosition for the boss to ensure it stops when fully on-screen
                float bossWidth = bossScript.GetComponent<SpriteRenderer>().bounds.size.x;
                float screenRightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;
                bossScript.SetEndPosition(new Vector3(screenRightEdge - bossWidth / 2, bossScript.transform.position.y, bossScript.transform.position.z));

                bossScript.OnDeath += BatDefeated;
                bossScript.OnDeath += () => bossScript.OnDeath -= BatDefeated;
            }

            batsAlive = 1; // Mark the boss as alive for game logic

            if (!string.IsNullOrEmpty(bossSpawnLine))
                DialogueBox.Show(bossSpawnLine, bossSpawnHold);
        }
        else
        {
            Debug.LogError("Boss prefab not assigned in WaveManager.");
        }
    }



    IEnumerator HideWaveTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        waveText.alpha = 0; // Assuming the text component's alpha property controls visibility
    }


    IEnumerator SpawnWave()
    {
        if (currentWaveNumber == 1)
        {
            batsRemainingToSpawn = 1; // Only one bat in the center for wave 1
        }
        else if (currentWaveNumber == 2 || currentWaveNumber == 4)
        {
            batsRemainingToSpawn = 3; // Three bats for waves 2 and 4
        }
        else if (currentWaveNumber == 3)
        {
            batsRemainingToSpawn = 1; // One bat in the center for wave 3, scaled up
        }

        batsAlive = batsRemainingToSpawn; // Update batsAlive to reflect the number of bats to spawn

        while (batsRemainingToSpawn > 0)
        {
            Transform spawnPoint = currentWaveNumber == 1 || currentWaveNumber == 3 ?
                                   spawnPoints[0] : // Center spawn point for waves 1 and 3
                                   spawnPoints[batsRemainingToSpawn - 1]; // Iterate through spawn points for waves 2 and 4

            bool scaleUp = currentWaveNumber == 3 || currentWaveNumber == 4;
            SpawnBat(spawnPoint, scaleUp);

            batsRemainingToSpawn--;
            yield return new WaitForSeconds(1f); // Adjust spawn timing as needed
        }
    }



    void SpawnBat(Transform spawnPoint, bool scaleUp)
    {
        GameObject batGO = Instantiate(batPrefab, spawnPoint.position, Quaternion.identity);
        BatEnemy bat = batGO.GetComponent<BatEnemy>();

        // Conditionally set the bat's scale based on the scaleUp parameter
        if (scaleUp)
        {
            batGO.transform.localScale = new Vector3(3f, 3f, 3f); // Adjust this scale to your preference for scaled-up bats
        }
        else
        {
            batGO.transform.localScale = new Vector3(2f, 2f, 2f); // Normal scale
        }

        bat.OnDeath += BatDefeated;
        bat.OnDeath += () => bat.OnDeath -= BatDefeated; // Ensure to unsubscribe correctly to avoid potential memory leaks
    }




    public void BatDefeated()
    {
        batsAlive--;
        if (batsAlive <= 0 && batsRemainingToSpawn <= 0)
        {
            if (currentWaveNumber == 5)
                StartCoroutine(BossDefeatDialogueThenLoadLevel());
            else
                StartNextWave();
        }
    }

    IEnumerator BossDefeatDialogueThenLoadLevel()
    {
        if (!string.IsNullOrEmpty(bossDefeatFirstLine))
            DialogueBox.Show(bossDefeatFirstLine, bossDefeatFirstHold);
        yield return new WaitForSeconds(bossDefeatBetweenLinesDelay);

        if (!string.IsNullOrEmpty(bossDefeatKeyLine))
            DialogueBox.Show(bossDefeatKeyLine, bossDefeatKeyHold);
        yield return new WaitForSeconds(bossDefeatLevelLoadDelay);

        if (nextLevelBuildIndex >= 0)
        {
            Level2Progress.ClearResumeWave();
            SceneManager.LoadSceneAsync(nextLevelBuildIndex);
        }
    }

    IEnumerator LoadNextLevel()
    {
        yield return new WaitForSeconds(3f);
        Level2Progress.ClearResumeWave();
        SceneManager.LoadSceneAsync(nextLevelBuildIndex);
    }
}

