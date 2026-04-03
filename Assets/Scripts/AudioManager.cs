using UnityEngine;

/// <summary>
/// Quản lý hệ thống âm thanh toàn cục: nhạc nền (BGM) và hiệu ứng (SFX).
/// Sử dụng Singleton và DontDestroyOnLoad để duy trì âm thanh xuyên suốt các màn chơi.
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>
    /// Bản thực thi duy nhất của AudioManager.
    /// </summary>
    public static AudioManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Inspector
    // ═══════════════════════════════════════════════════════════

    [Header("Nhạc Nền (BGM)")]
    public AudioClip menuSound;
    public AudioClip soundInGame;

    [Header("Hiệu Ứng (SFX)")]
    public AudioClip codeCommitSound;
    public AudioClip coffeeSound;
    public AudioClip bugSound;
    public AudioClip levelUpSound;
    public AudioClip winGameSound;
    public AudioClip gameOverSound;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo Loa Nhạc Nền
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = 0.5f;

        // Khởi tạo Loa Hiệu Ứng
        _sfxSource = gameObject.AddComponent<AudioSource>();
    }

    // ═══════════════════════════════════════════════════════════
    //  SFX PLAYER METHODS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Phát tiếng khi nhặt được Code Commit.
    /// </summary>
    public void PlayItemCodeCommit() { PlaySFX(codeCommitSound); }

    /// <summary>
    /// Phát tiếng khi uống Cà phê (Boost).
    /// </summary>
    public void PlayItemCoffee() { PlaySFX(coffeeSound); }

    /// <summary>
    /// Phát tiếng khi va chạm với Bug (Slow).
    /// </summary>
    public void PlayItemBug() { PlaySFX(bugSound); }

    /// <summary>
    /// Phát tiếng khi nâng cấp trình độ (Skin Up).
    /// </summary>
    public void PlayItemSkinUp() { PlaySFX(levelUpSound); }

    /// <summary>
    /// Phát tiếng khi chiến thắng màn chơi.
    /// </summary>
    public void PlayWin() { PlaySFX(winGameSound); }

    /// <summary>
    /// Phát tiếng khi bị Deadline bắt kịp (Lose).
    /// </summary>
    public void PlayLose() { PlaySFX(gameOverSound); }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && _sfxSource != null)
        {
            _sfxSource.PlayOneShot(clip);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  BGM CONTROL METHODS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Phát nhạc nền ngoài Menu chính.
    /// </summary>
    public void PlayMenuMusic()
    {
        if (menuSound == null || _bgmSource == null) return;
        if (_bgmSource.clip == menuSound && _bgmSource.isPlaying) return;

        _bgmSource.clip = menuSound;
        _bgmSource.Play();
    }

    /// <summary>
    /// Phát nhạc nền căng thẳng trong màn chơi.
    /// </summary>
    public void PlayInGameMusic()
    {
        if (soundInGame == null || _bgmSource == null) return;
        if (_bgmSource.clip == soundInGame && _bgmSource.isPlaying) return;

        _bgmSource.clip = soundInGame;
        _bgmSource.Play();
    }

    /// <summary>
    /// Dừng toàn bộ nhạc nền đang phát.
    /// </summary>
    public void StopMusic()
    {
        if (_bgmSource != null) _bgmSource.Stop();
    }
}

