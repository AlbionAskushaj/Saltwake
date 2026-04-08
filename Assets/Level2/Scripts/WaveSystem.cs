using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
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



    // Make currentWaveNumber publicly accessible
    public int CurrentWaveNumber => currentWaveNumber;


    public TMP_Text waveText; // Add this line

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
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (currentWaveNumber < 5) // Check for waves 1 through 5
        {
            currentWaveNumber++;

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

            }

            batsAlive = 1; // Mark the boss as alive for game logic
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
            StartNextWave();
        }
    }

    IEnumerator LoadNextLevel()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadSceneAsync(nextLevelBuildIndex);
    }
}

