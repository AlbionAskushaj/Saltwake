using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KillZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if (WaveManager.instance != null)
            {
                Level2Progress.SaveWaveAndReloadScene(WaveManager.instance.currentWaveNumber);
                return;
            }

            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Destroy(col.gameObject);
        }
    }
}
