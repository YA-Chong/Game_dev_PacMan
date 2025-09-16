// Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    // 单例实例
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource; // 用于播放关卡BGM
    public AudioSource sfxSource;   // 用于播放音效

    [Header("Level 01 Background Music")]
    public AudioClip bgmLevelIntro;     // 关卡开场序曲 (必须赋值)
    public AudioClip bgmGhostNormal;    // 幽灵正常状态BGM (必须赋值)
    public AudioClip bgmGhostScared;    // 幽灵害怕状态BGM (必须赋值)
    public AudioClip bgmGhostDead;      // 幽灵死亡状态BGM (必须赋值)

    [Header("Level 01 Sound Effects")]
    public AudioClip sfxMove;           // PacStudent移动音效（空转）
    public AudioClip sfxEatPellet;      // 吃豆子
    public AudioClip sfxCollideWall;    // 撞墙
    public AudioClip sfxPacDeath;       // PacStudent死亡
    //public AudioClip sfxGhostEaten;     // (可选) 吃掉幽灵的音效

    private MusicState _currentMusicState;
    private Coroutine _musicTransitionCoroutine;

    private void Awake()
    {
        // 实现单例，但不使用 DontDestroyOnLoad
        // 这样当从Level01切换回StartScene时，此管理器会被销毁，符合我们的新设计
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 这行被注释掉了
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
        GhostDead
    }

    // ========== BGM 核心控制方法 ========== //

    /// <summary>
    /// 在Level01场景开始时调用，播放Intro并自动切换至Normal状态BGM
    /// 严格满足作业要求：播放3秒或音频长度（取较短者），然后切换
    /// </summary>
    public void StartLevelMusic()
    {
        if (bgmLevelIntro == null || bgmGhostNormal == null)
        {
            Debug.LogError("AudioManager: Intro or Normal BGM is not assigned!");
            return;
        }

        PlayMusic(bgmLevelIntro, MusicState.LevelIntro, false);
        
        // 计算延迟时间：3秒 或 音频长度（取更短者）
        float delayBeforeTransition = Mathf.Min(3f, bgmLevelIntro.length);
        Invoke(nameof(TransitionToNormalBGM), delayBeforeTransition);
    }

    private void TransitionToNormalBGM()
    {
        PlayMusic(bgmGhostNormal, MusicState.GhostNormal);
    }

    /// <summary>
    /// 当PacStudent吃到能量豆时，由GameManager调用
    /// </summary>
    public void SwitchToScaredBGM()
    {
        if (_currentMusicState != MusicState.GhostScared)
        {
            PlayMusic(bgmGhostScared, MusicState.GhostScared);
        }
    }

    /// <summary>
    /// 当有幽灵被吃掉时，由GameManager调用
    /// </summary>
    public void SwitchToDeadBGM()
    {
        PlayMusic(bgmGhostDead, MusicState.GhostDead);
    }

    /// <summary>
    /// 当所有幽灵结束死亡状态或能量豆效果结束时，切换回Normal BGM
    /// </summary>
    public void SwitchBackToNormalBGM()
    {
        PlayMusic(bgmGhostNormal, MusicState.GhostNormal);
    }

    // 内部通用的音乐播放方法
    private void PlayMusic(AudioClip clip, MusicState newState, bool loop = true)
    {
        if (musicSource.isPlaying && musicSource.clip == clip) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();

        _currentMusicState = newState;
        // Debug.Log("BGM switched to: " + newState); // 可用于调试
    }

    // ========== SFX 音效方法 ========== //

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // 为每个音效提供便捷方法
    public void PlayMoveSFX() => PlaySFX(sfxMove);
    public void PlayEatPelletSFX() => PlaySFX(sfxEatPellet);
    public void PlayCollideWallSFX() => PlaySFX(sfxCollideWall);
    public void PlayPacDeathSFX() => PlaySFX(sfxPacDeath);
    //public void PlayGhostEatenSFX() => PlaySFX(sfxGhostEaten);

    // ========== 工具方法 ========== //

    private void OnDestroy()
    {
        // 确保当对象被销毁时，所有Invoke调用都被取消
        CancelInvoke();
    }
}