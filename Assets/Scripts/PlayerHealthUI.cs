using UnityEngine;

// Vẽ thanh máu player lên góc trên trái màn hình.
// Dùng OnGUI() thay vì Canvas/Image vì project không cài package com.unity.ugui.
// Gán script này lên Player cùng với PlayerHealth.
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Vị trí & kích thước (pixel trên màn hình)")]
    public float barX      = 20f;   // Khoảng cách từ cạnh trái màn hình
    public float barY      = 20f;   // Khoảng cách từ cạnh trên màn hình
    public float barWidth  = 220f;  // Chiều ngang thanh máu
    public float barHeight = 22f;   // Chiều cao thanh máu

    [Header("Màu sắc")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f); // Màu nền tối
    public Color fillColor       = new Color(0.2f,  0.6f,  0.95f, 1f);    // Màu xanh khi máu cao
    public Color lowColor        = new Color(0.9f,  0.2f,  0.15f, 1f);    // Màu đỏ khi máu thấp

    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.35f; // Dưới 35% máu → đổi sang màu đỏ

    PlayerHealth playerHealth; // Tham chiếu đến script PlayerHealth để đọc HealthPercent
    Texture2D bgTex;           // Texture 1x1 màu nền (tạo 1 lần, dùng mãi)
    Texture2D fillTex;         // Texture 1x1 màu xanh
    Texture2D lowTex;          // Texture 1x1 màu đỏ

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>(); // Lấy PlayerHealth trên cùng GameObject
        // Tạo texture 1 pixel mỗi màu — khi DrawTexture kéo giãn thành thanh màu solid
        bgTex   = MakeTex(backgroundColor);
        fillTex = MakeTex(fillColor);
        lowTex  = MakeTex(lowColor);
    }

    // OnGUI() được Unity gọi tự động mỗi frame để vẽ UI kiểu cũ (IMGUI)
    void OnGUI()
    {
        if (playerHealth == null) return;

        float pct = playerHealth.HealthPercent; // Lấy tỉ lệ máu 0.0 → 1.0

        // Vẽ thanh nền (luôn có độ rộng cố định)
        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), bgTex);

        // Tính độ rộng phần fill theo tỉ lệ máu (trừ 4f = padding 2px mỗi bên)
        float fillW = (barWidth - 4f) * pct;
        if (fillW > 0f)
        {
            // Chọn màu đỏ nếu máu thấp, xanh nếu máu cao
            var tex = pct <= lowHealthThreshold ? lowTex : fillTex;
            // Vẽ phần fill lùi vào 2px so với nền để tạo viền
            GUI.DrawTexture(new Rect(barX + 2f, barY + 2f, fillW, barHeight - 4f), tex);
        }
    }

    // Tạo texture 1x1 pixel với màu bất kỳ — cách đơn giản nhất tạo màu solid trong IMGUI
    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color); // Tô màu pixel duy nhất
        tex.Apply();               // Apply thay đổi vào GPU
        return tex;
    }
}
