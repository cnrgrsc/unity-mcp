using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast
{
    /// <summary>
    /// Manages all audio playback including music and sound effects.
    /// Singleton pattern for easy access.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip placeSound;
        [SerializeField] private AudioClip clearSound;
        [SerializeField] private AudioClip comboSound;
        [SerializeField] private AudioClip gameOverSound;
        
        private Dictionary<string, AudioClip> soundClips = new Dictionary<string, AudioClip>();
        private float musicVolume = 1f;
        private float sfxVolume = 1f;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SetupAudioSources();
            InitializeSoundDictionary();
            LoadVolumeSettings();
        }
        
        private void Start()
        {
            PlayMusic();
        }
        
        private void SetupAudioSources()
        {
            if (musicSource == null)
            {
                var musicGO = new GameObject("MusicSource");
                musicGO.transform.SetParent(transform);
                musicSource = musicGO.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                var sfxGO = new GameObject("SFXSource");
                sfxGO.transform.SetParent(transform);
                sfxSource = sfxGO.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }
        
        private void InitializeSoundDictionary()
        {
            // Register sounds by name
            RegisterSound("click", clickSound);
            RegisterSound("pickup", pickupSound);
            RegisterSound("place", placeSound);
            RegisterSound("clear", clearSound);
            RegisterSound("combo", comboSound);
            RegisterSound("gameover", gameOverSound);
            
            // Generate default sounds if not assigned
            GenerateDefaultSounds();
        }
        
        private void RegisterSound(string name, AudioClip clip)
        {
            if (clip != null)
                soundClips[name] = clip;
        }
        
        /// <summary>
        /// Generate simple procedural sounds if no clips are assigned
        /// </summary>
        private void GenerateDefaultSounds()
        {
            if (!soundClips.ContainsKey("click"))
                soundClips["click"] = GenerateClickSound();
            if (!soundClips.ContainsKey("place"))
                soundClips["place"] = GeneratePlaceSound();
            if (!soundClips.ContainsKey("clear"))
                soundClips["clear"] = GenerateClearSound();
            if (!soundClips.ContainsKey("combo"))
                soundClips["combo"] = GenerateComboSound();
            if (!soundClips.ContainsKey("gameover"))
                soundClips["gameover"] = GenerateGameOverSound();
        }
        
        private AudioClip GenerateClickSound()
        {
            return CreateTone(0.05f, 800, 0.3f);
        }
        
        private AudioClip GeneratePlaceSound()
        {
            return CreateTone(0.1f, 400, 0.4f);
        }
        
        private AudioClip GenerateClearSound()
        {
            return CreateSweepTone(0.2f, 400, 800, 0.5f);
        }
        
        private AudioClip GenerateComboSound()
        {
            return CreateSweepTone(0.3f, 500, 1000, 0.6f);
        }
        
        private AudioClip GenerateGameOverSound()
        {
            return CreateSweepTone(0.5f, 400, 200, 0.5f);
        }
        
        /// <summary>
        /// Create a simple tone
        /// </summary>
        private AudioClip CreateTone(float duration, float frequency, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration); // Fade out
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * volume;
            }
            
            var clip = AudioClip.Create("GeneratedTone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        
        /// <summary>
        /// Create a frequency sweep tone
        /// </summary>
        private AudioClip CreateSweepTone(float duration, float startFreq, float endFreq, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float frequency = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = 1f - progress; // Fade out
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * volume;
            }
            
            var clip = AudioClip.Create("GeneratedSweep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        
        private void LoadVolumeSettings()
        {
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }
        
        /// <summary>
        /// Play background music
        /// </summary>
        public void PlayMusic()
        {
            if (musicSource == null) return;
            
            if (backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }
        
        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }
        
        /// <summary>
        /// Play a sound effect by name
        /// </summary>
        public void PlaySound(string soundName)
        {
            if (sfxSource == null) return;
            
            if (soundClips.TryGetValue(soundName, out var clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }
        
        /// <summary>
        /// Play a specific audio clip
        /// </summary>
        public void PlayClip(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
        
        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        }
        
        /// <summary>
        /// Set SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        }
        
        /// <summary>
        /// Mute/unmute all audio
        /// </summary>
        public void SetMute(bool muted)
        {
            if (musicSource != null)
                musicSource.mute = muted;
            if (sfxSource != null)
                sfxSource.mute = muted;
        }
    }
}
