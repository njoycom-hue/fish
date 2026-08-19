using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 효과음/배경음을 한 곳에서 재생한다. AudioClip 이 비어 있어도 안전하게 무시하므로
    /// 사운드 에셋 없이도 프로젝트가 그대로 돌아간다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("클립")]
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip coinClip;
        [SerializeField] private AudioClip crashClip;
        [SerializeField] private AudioClip musicClip;

        [Header("볼륨")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        private AudioSource sfxSource;
        private AudioSource musicSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            sfxSource = GetComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.clip = musicClip;
            musicSource.volume = musicVolume;
        }

        private void Start()
        {
            if (SaveSystem.Data.musicEnabled)
            {
                PlayMusic();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayJump() => PlaySfx(jumpClip);

        public void PlayCoin() => PlaySfx(coinClip, Random.Range(0.95f, 1.1f));

        public void PlayCrash() => PlaySfx(crashClip);

        public void PlayMusic()
        {
            if (musicSource.clip != null && !musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void StopMusic() => musicSource.Stop();

        private void PlaySfx(AudioClip clip, float pitch = 1f)
        {
            if (clip == null || !SaveSystem.Data.sfxEnabled)
            {
                return;
            }

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
