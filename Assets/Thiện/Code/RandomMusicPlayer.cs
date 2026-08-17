using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomMusicPlayer : MonoBehaviour
{
    [Header("Danh sách nhạc")]
    public AudioClip[] musicPlaylist;

    [Header("Cài đặt")]
    public bool playOnStart = true;
    public bool loopPlaylist = true; // Phát tiếp bài ngẫu nhiên khác khi bài hiện tại hết

    private AudioSource audioSource;
    private int lastPlayedIndex = -1; // Tránh phát lặp lại đúng 1 bài 2 lần liên tiếp

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayRandomMusic();
        }
    }

    private void Update()
    {
        // Tự động chuyển bài ngẫu nhiên mới khi bài cũ kết thúc
        if (loopPlaylist && !audioSource.isPlaying && musicPlaylist.Length > 0)
        {
            PlayRandomMusic();
        }
    }

    // Hàm gọi để phát bài nhạc ngẫu nhiên
    public void PlayRandomMusic()
    {
        if (musicPlaylist == null || musicPlaylist.Length == 0)
        {
            Debug.LogWarning("Chưa gán bài hát nào vào musicPlaylist!");
            return;
        }

        int randomIndex;

        // Chọn index ngẫu nhiên (nếu có hơn 1 bài thì ưu tiên chọn bài khác bài vừa phát)
        if (musicPlaylist.Length > 1)
        {
            do
            {
                randomIndex = Random.Range(0, musicPlaylist.Length);
            }
            while (randomIndex == lastPlayedIndex);
        }
        else
        {
            randomIndex = 0;
        }

        lastPlayedIndex = randomIndex;

        // Gán nhạc và phát
        audioSource.clip = musicPlaylist[randomIndex];
        audioSource.Play();
    }
}