using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("Player Reference")]
    public CharacterController2D player;

    [Header("Heart Images")]
    // Assign these in the Inspector — one Image per heart, left to right
    public Image[] hearts;

    [Header("Sprites")]
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    // Fallback colors if you don't have sprites yet
    [Header("Fallback Colors (used if no sprite assigned)")]
    public Color fullColor  = new Color(0.9f, 0.1f, 0.1f, 1f); // red
    public Color emptyColor = new Color(1f,   1f,   1f,   0.2f); // faded white

    // How much HP each heart represents. Default: 2 HP per heart = 5 hearts for 10 HP
    [Header("Settings")]
    public float hpPerHeart = 2f;

    void Update()
    {
        RefreshHearts();
    }

    void RefreshHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            // How much HP is needed to fill this heart fully
            float threshold = (i + 1) * hpPerHeart;

            if (player.life >= threshold)
            {
                SetHeart(hearts[i], true);
            }
            else
            {
                SetHeart(hearts[i], false);
            }
        }
    }

    void SetHeart(Image heart, bool full)
    {
        if (full)
        {
            heart.sprite = fullHeartSprite != null ? fullHeartSprite : heart.sprite;
            heart.color  = fullHeartSprite != null ? Color.white : fullColor;
        }
        else
        {
            heart.sprite = emptyHeartSprite != null ? emptyHeartSprite : heart.sprite;
            heart.color  = emptyHeartSprite != null ? Color.white : emptyColor;
        }
    }
}
