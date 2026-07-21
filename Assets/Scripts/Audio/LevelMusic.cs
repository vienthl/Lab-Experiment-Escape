using UnityEngine;

// Nhạc nền lặp cho scene gameplay (Level1/Level2) — gắn lên 1 GameObject bất kỳ tồn tại sẵn
// trong scene (vd chung với LevelHUD). Để trống Music Clip = không phát gì, không lỗi.
public class LevelMusic : MonoBehaviour
{
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.5f;

    void Awake()
    {
        if (musicClip == null) return;

        var source = gameObject.AddComponent<AudioSource>();
        source.clip = musicClip;
        source.volume = volume;
        source.loop = true;
        source.playOnAwake = false;
        source.Play();
    }
}
