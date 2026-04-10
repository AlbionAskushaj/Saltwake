using UnityEngine;
using UnityEngine.UI;

// Tracks the player's CharacterController2D.life and drives a filled UI Image.
public class SimpleHealthBar : MonoBehaviour
{
    private Image fillImage;
    private CharacterController2D player;

    private void Start()
    {
        TryBindPlayer();

        // Find the Fill child image
        Transform fill = transform.Find("Fill");
        if (fill != null)
        {
            fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                // Force the image to Filled mode so fillAmount works visually
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = 0; // left
                fillImage.fillAmount = 1f;
            }
        }
    }

    private void Update()
    {
        if (player == null)
        {
            TryBindPlayer();
            return;
        }
        if (fillImage == null) return;
        if (player.maxLife <= 0f) return;

        float ratio = Mathf.Clamp01(player.life / player.maxLife);
        fillImage.fillAmount = ratio;

        // Green when healthy, red when low
        fillImage.color = Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(0.15f, 0.85f, 0.15f), ratio);
    }

    private void TryBindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.GetComponent<CharacterController2D>();
    }
}
