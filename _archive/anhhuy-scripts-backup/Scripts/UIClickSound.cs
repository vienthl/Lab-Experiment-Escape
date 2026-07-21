using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIClickSound : MonoBehaviour
{
    public AudioClip clickClip;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void Play()
    {
        if (clickClip != null)
            _source.PlayOneShot(clickClip);
    }
}
