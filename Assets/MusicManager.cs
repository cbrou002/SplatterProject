using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Settings")]
    public AudioClip musicClip;
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public bool loop = true;

    private AudioSource _audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        _audioSource.clip = musicClip;
        _audioSource.volume = volume;
        _audioSource.loop = loop;
        _audioSource.playOnAwake = true;

        if (musicClip != null)
        {
            _audioSource.Play();
        }
        }

        public void RestartMusic()
        {
        if (_audioSource != null)
        {
            _audioSource.Stop();
            _audioSource.time = 0;
            _audioSource.Play();
        }
        }

        public void UpdateSettings()
    {
        if (_audioSource != null)
        {
            _audioSource.clip = musicClip;
            _audioSource.volume = volume;
            _audioSource.loop = loop;
            if (!_audioSource.isPlaying && musicClip != null)
            {
                _audioSource.Play();
            }
        }
    }
}
