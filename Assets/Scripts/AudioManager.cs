using UnityEngine;

/// <summary>
/// Quản lý âm thanh tổng (BGM).
/// Cấu trúc Singleton bất tử xuyên suốt các màn chơi nhờ lệnh DontDestroyOnLoad.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Nhạc Nền")]
    public AudioClip menuSound;
    public AudioClip soundInGame;

    [Header("Hiệu Ứng Vặt (Lượm đồ / Thắng / Thua)")]
    public AudioClip codeCommitSound;
    public AudioClip coffeeSound;
    public AudioClip bugSound;
    public AudioClip levelUpSound;
    public AudioClip winGameSound;
    public AudioClip gameOverSound;

    // Máy phát nhạc chuyên biệt cho BGM và Cục chớp âm cho SFX
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        // Nếu đã có quản lý âm thanh từ Scene trước chuyển sang, thì tiêu diệt bản sao mới này đi.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Biến vật thể này thành thần thánh: KHÔNG bao giờ bị tiêu huỷ khi dùng LoadScene()
        DontDestroyOnLoad(gameObject);

        // Lắp ráp màng Loa ảo
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true; // Nhạc nền luôn luôn Loop lặp lại
        bgmSource.playOnAwake = false;
        bgmSource.volume = 0.5f; // Chỉnh âm lượng mặc định

        // Sinh ra chiếc loa đài phụ chuyên phát tiếng bíp bíp / bùm bùm
        sfxSource = gameObject.AddComponent<AudioSource>();
    }

    // ─── BỘ API ÉP PHÁT TIẾNG ĐỘNG (Dùng PlayOneShot để có thể đè nhiều tiếng cùng lúc) ───
    public void PlayItemCodeCommit() { if(codeCommitSound) sfxSource.PlayOneShot(codeCommitSound); }
    public void PlayItemCoffee() { if(coffeeSound) sfxSource.PlayOneShot(coffeeSound); }
    public void PlayItemBug() { if(bugSound) sfxSource.PlayOneShot(bugSound); }
    public void PlayItemSkinUp() { if(levelUpSound) sfxSource.PlayOneShot(levelUpSound); }
    public void PlayWin() { if(winGameSound) sfxSource.PlayOneShot(winGameSound); }
    public void PlayLose() { if(gameOverSound) sfxSource.PlayOneShot(gameOverSound); }

    /// <summary>
    /// Phát nhạc ngoài Sảnh (Tự động thông minh không hát lại từ đầu nếu đang hát dở bài Menu)
    /// </summary>
    public void PlayMenuMusic()
    {
        if (menuSound == null) return;
        
        // Tránh giật đĩa nết hát bài trùng bài
        if (bgmSource.clip == menuSound && bgmSource.isPlaying) return;

        bgmSource.clip = menuSound;
        bgmSource.Play();
        Debug.Log("Đang phát nhạc: MenuSound");
    }

    /// <summary>
    /// Phát nhạc Nhịp Độ Cao (Gameplay)
    /// </summary>
    public void PlayInGameMusic()
    {
        if (soundInGame == null) return;

        if (bgmSource.clip == soundInGame && bgmSource.isPlaying) return;

        bgmSource.clip = soundInGame;
        bgmSource.Play();
        Debug.Log("Đang phát nhạc: SoundInGame");
    }

    /// <summary>
    /// Tắt ngúm toàn hệ thống Nhạc Nền (VD: Khi ấn nghỉ game)
    /// </summary>
    public void StopMusic()
    {
        bgmSource.Stop();
    }
}
