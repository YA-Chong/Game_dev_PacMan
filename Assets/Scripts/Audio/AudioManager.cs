using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Level 01 Background Music")]
    public AudioClip bgmLevelIntro;
    public AudioClip bgmGhostNormal;
    public AudioClip bgmGhostScared;
    public AudioClip bgmGhostDead;

    [Header("Level 01 Sound Effects")]
    public AudioClip sfxMove;
    public AudioClip sfxEatPellet;
    public AudioClip sfxCollideWall;
    public AudioClip sfxPacDeath;

    private MusicState _currentMusicState;
    private Coroutine _musicTransitionCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public enum MusicState
    {
        None,
        LevelIntro,
        GhostNormal,
        GhostScared,
        GhostDead,
    }

    public void StartLevelMusic()
    {
        if (bgmLevelIntro == null || bgmGhostNormal == null)
        {
            Debug.LogError("AudioManager: Intro or Normal BGM is not assigned!");
            return;
        }

        PlayMusic(bgmLevelIntro, MusicState.LevelIntro, false);

        float delayBeforeTransition = Mathf.Min(3f, bgmLevelIntro.length);
        Invoke(nameof(TransitionToNormalBGM), delayBeforeTransition);
    }

    private void TransitionToNormalBGM()
    {
        PlayMusic(bgmGhostNormal, MusicState.GhostNormal);
    }

    public void SwitchToScaredBGM()
    {
        if (_currentMusicState != MusicState.GhostScared)
        {
            PlayMusic(bgmGhostScared, MusicState.GhostScared);
        }
    }

    public void SwitchToDeadBGM()
    {
        PlayMusic(bgmGhostDead, MusicState.GhostDead);
    }

    public void SwitchBackToNormalBGM()
    {
        PlayMusic(bgmGhostNormal, MusicState.GhostNormal);
    }

    private void PlayMusic(AudioClip clip, MusicState newState, bool loop = true)
    {
        if (musicSource.isPlaying && musicSource.clip == clip)
            return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();

        _currentMusicState = newState;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayMoveSFX() => PlaySFX(sfxMove);

    public void PlayEatPelletSFX() => PlaySFX(sfxEatPellet);

    public void PlayCollideWallSFX() => PlaySFX(sfxCollideWall);

    public void PlayPacDeathSFX() => PlaySFX(sfxPacDeath);

    private void OnDestroy()
    {
        CancelInvoke();
    }
}
