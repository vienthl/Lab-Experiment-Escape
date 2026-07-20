using UnityEngine;

public class BossHealthBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private UnityEngine.UI.Image healthImage;

    [Header("HP Sprites")]
    [SerializeField] private Sprite hp0;
    [SerializeField] private Sprite hp16;
    [SerializeField] private Sprite hp33;
    [SerializeField] private Sprite hp50;
    [SerializeField] private Sprite hp66;
    [SerializeField] private Sprite hp83;
    [SerializeField] private Sprite hp100;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetHealth(float currentHealth, float maxHealth)
    {
        if (healthImage == null || maxHealth <= 0f)
        {
            return;
        }

        float percent = Mathf.Clamp01(currentHealth / maxHealth);

        if (percent <= 0f)
        {
            healthImage.sprite = hp0;
        }
        else if (percent <= 0.16f)
        {
            healthImage.sprite = hp16;
        }
        else if (percent <= 0.33f)
        {
            healthImage.sprite = hp33;
        }
        else if (percent <= 0.50f)
        {
            healthImage.sprite = hp50;
        }
        else if (percent <= 0.66f)
        {
            healthImage.sprite = hp66;
        }
        else if (percent <= 0.83f)
        {
            healthImage.sprite = hp83;
        }
        else
        {
            healthImage.sprite = hp100;
        }
    }
}