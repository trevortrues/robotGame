using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    [SerializeField] private AudioClip musicClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = musicClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (musicClip != null)
        {
            audioSource.Play();
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
}
