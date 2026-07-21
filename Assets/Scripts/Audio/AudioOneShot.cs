using UnityEngine;

// Phát 1 âm thanh ngắn tại 1 vị trí, dùng chung cho mọi SFX one-shot trong game
// (đánh trúng, chết, nhặt đồ, mở cửa...). AudioSource.PlayClipAtPoint tự tạo GameObject tạm
// riêng rồi tự hủy sau khi phát xong — an toàn kể cả khi object phát ra bị Destroy/SetActive(false)
// ngay sau đó (vd quái chết, item bị nhặt).
public static class AudioOneShot
{
    public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}
