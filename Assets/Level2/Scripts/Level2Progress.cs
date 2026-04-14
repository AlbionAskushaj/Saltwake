using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persists Level 2 wave across a scene reload (death) so music and scene state reset
/// while gameplay resumes at the wave the player died on.
/// </summary>
public static class Level2Progress
{
    const string ResumeWaveKey = "Saltwake_L2_ResumeWave";

    public static void ClearResumeWave()
    {
        PlayerPrefs.DeleteKey(ResumeWaveKey);
        PlayerPrefs.Save();
    }

    /// <summary> Returns 0 if the next load should start a fresh run. </summary>
    public static int ConsumeResumeWave()
    {
        if (!PlayerPrefs.HasKey(ResumeWaveKey))
            return 0;
        int w = PlayerPrefs.GetInt(ResumeWaveKey, 0);
        PlayerPrefs.DeleteKey(ResumeWaveKey);
        PlayerPrefs.Save();
        return w;
    }

    public static void SaveWaveAndReloadScene(int waveNumber)
    {
        if (waveNumber < 1)
            waveNumber = 1;
        if (waveNumber > 5)
            waveNumber = 5;
        PlayerPrefs.SetInt(ResumeWaveKey, waveNumber);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
