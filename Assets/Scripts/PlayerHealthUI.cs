using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tạo thanh máu UI góc trên trái màn hình cho nhân vật chính.
/// Gán script này lên Player cùng với PlayerHealth.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Vị trí & kích thước (pixel)")]
    public Vector2 position = new Vector2(20f, -20f);
    public Vector2 size = new Vector2(220f, 22f);

    [Header("Màu sắc")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    public Color fillColor       = new Color(0.2f,  0.6f,  0.95f, 1f);
    public Color lowColor        = new Color(0.9f,  0.2f,  0.15f, 1f);

    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.35f;

    PlayerHealth playerHealth;
    Image fillImage;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        BuildUI();
    }

    void BuildUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────
        var canvasGo = new GameObject("PlayerHealthCanvas");
        DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Nền (background) ──────────────────────────────────────────────
        var bgGo = new GameObject("HP_Background");
        bgGo.transform.SetParent(canvasGo.transform, false);

        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0f, 1f);
        bgRect.anchorMax        = new Vector2(0f, 1f);
        bgRect.pivot            = new Vector2(0f, 1f);
        bgRect.anchoredPosition = position;
        bgRect.sizeDelta        = size;

        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = backgroundColor;

        // ── Fill (thanh máu) ───────────────────────────────────────────────
        var fillGo = new GameObject("HP_Fill");
        fillGo.transform.SetParent(bgGo.transform, false);

        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f,  2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        fillImage = fillGo.AddComponent<Image>();
        fillImage.color      = fillColor;
        fillImage.type       = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
    }

    void Update()
    {
        if (playerHealth == null || fillImage == null) return;

        float pct = playerHealth.HealthPercent;
        fillImage.fillAmount = pct;
        fillImage.color = pct <= lowHealthThreshold ? lowColor : fillColor;
    }
}
