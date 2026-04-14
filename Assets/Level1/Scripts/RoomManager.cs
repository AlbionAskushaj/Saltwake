using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField] private List<GameObject> enemies = new List<GameObject>();

    [Header("Gates")]
    [SerializeField] private RoomGate entranceGate;
    [SerializeField] private RoomGate exitGate;

    [Header("Rewards")]
    [SerializeField] private GameObject clearRewardPrefab;
    [SerializeField] private Transform rewardSpawnPoint;

    [Header("Boss Room")]
    [SerializeField] private bool isBossRoom = false;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;
    [Tooltip("If true, boss is instantiated at scene Start instead of when the player enters.")]
    [SerializeField] private bool spawnBossOnStart = false;

    [Header("Boss Scene References (Brinewyrm)")]
    [Tooltip("Scene transforms the boss surfaces from. Brinewyrm only.")]
    [SerializeField] private Transform[] bossSurfacePoints;
    [Tooltip("Scene-side rising tide hazard. Brinewyrm only.")]
    [SerializeField] private RisingTideHazard bossTideHazard;
    [Tooltip("Scene transform where the boss death reward spawns. Brinewyrm only.")]
    [SerializeField] private Transform bossRewardSpawnPoint;

    [Header("Dialogue")]
    [TextArea(2, 5)]
    [SerializeField] private string roomEntryDialogue = "";
    [SerializeField] private float entryDialogueDuration = 3f;
    [TextArea(2, 5)]
    [SerializeField] private string roomClearDialogue = "";
    [SerializeField] private float clearDialogueDuration = 3f;

    [Header("Level Transition")]
    [SerializeField] private bool loadNextLevelOnClear = false;
    [SerializeField] private int nextLevelBuildIndex = -1;

    private bool roomCleared = false;
    private bool playerInside = false;
    private bool hasEnemies = false;
    private bool bossSpawned = false;
    private GameObject spawnedBoss;

    private void Start()
    {
        // A room with no enemies assigned (and not a boss room) has nothing to gate
        hasEnemies = enemies.Count > 0 || isBossRoom;

        // Gates always start open. ActivateRoom() closes them when the player enters
        // a room that still has enemies; OnRoomCleared() reopens them on clear.
        if (entranceGate != null) entranceGate.Open();
        if (exitGate != null) exitGate.Open();

        if (!hasEnemies)
        {
            roomCleared = true;
        }

        if (isBossRoom && spawnBossOnStart && bossPrefab != null && spawnedBoss == null)
        {
            SpawnBoss();
        }
    }

    private void SpawnBoss()
    {
        Vector3 spawnPos = bossSpawnPoint != null ? bossSpawnPoint.position : transform.position;
        spawnedBoss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        bossSpawned = true;
        enemies.Add(spawnedBoss);

        CrowBoss bossComponent = spawnedBoss.GetComponent<CrowBoss>();
        if (bossComponent != null)
            bossComponent.SetRoomManager(this);

        BrinewyrmBoss brinewyrm = spawnedBoss.GetComponent<BrinewyrmBoss>();
        if (brinewyrm != null)
        {
            brinewyrm.SetRoomManager(this);
            brinewyrm.Initialize(bossSurfacePoints, bossTideHazard, bossRewardSpawnPoint);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || playerInside) return;

        playerInside = true;

        if (roomCleared) return;

        StartCoroutine(ActivateRoom());
    }

    private IEnumerator ActivateRoom()
    {
        yield return new WaitForSeconds(3f);

        if (roomCleared) yield break;

        if (entranceGate != null)
            entranceGate.Close();

        if (exitGate != null)
            exitGate.Close();

        if (isBossRoom && bossPrefab != null && spawnedBoss == null)
        {
            SpawnBoss();
            DialogueBox.Show("The Stormcrow descends...", 3f);
        }
        else if (isBossRoom && spawnedBoss != null)
        {
            DialogueBox.Show("The Stormcrow descends...", 3f);
        }
        else if (!string.IsNullOrEmpty(roomEntryDialogue))
        {
            DialogueBox.Show(roomEntryDialogue, entryDialogueDuration);
        }
    }

    private void Update()
    {
        if (roomCleared || !playerInside) return;

        // Don't evaluate enemy list until boss has actually spawned
        if (isBossRoom && !bossSpawned) return;

        enemies.RemoveAll(e => e == null);

        if (enemies.Count == 0)
        {
            roomCleared = true;
            OnRoomCleared();
        }
    }

    private void OnRoomCleared()
    {
        if (exitGate != null)
            exitGate.Open();

        if (entranceGate != null)
            entranceGate.Open();

        if (!string.IsNullOrEmpty(roomClearDialogue))
            DialogueBox.Show(roomClearDialogue, clearDialogueDuration);

        if (clearRewardPrefab != null && rewardSpawnPoint != null)
        {
            Instantiate(clearRewardPrefab, rewardSpawnPoint.position, Quaternion.identity);
        }

        if (loadNextLevelOnClear && nextLevelBuildIndex >= 0)
        {
            StartCoroutine(LoadNextLevel());
        }
    }

    private IEnumerator LoadNextLevel()
    {
        yield return new WaitForSeconds(3f);
        Level2Progress.ClearResumeWave();
        SceneManager.LoadSceneAsync(nextLevelBuildIndex);
    }

    public void TrackEnemy(GameObject enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }
}
