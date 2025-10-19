using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// バトルロード共通サウンド管理
/// シーンをまたいでも破棄されない（BGM/SE一元管理）
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Range(0f, 1f)] public float seVolume = 1.0f;

    [Header("BGM Clips")]
    public AudioClip homeBGM;
    public AudioClip battleBGM;
    public AudioClip bossBGM;
    public AudioClip victoryBGM;
    public AudioClip defeatBGM;

    [Header("SE Clips")]
    public AudioClip buttonSE;
    public AudioClip actionSE;
    public AudioClip walkSE;
    public AudioClip attackSE;
    public AudioClip hitSE;
    public AudioClip finisherSE;
    public AudioClip courageMaxSE;

    [Header("個別SE登録")]
    [SerializeField] private List<ActionSEData> seDataList = new List<ActionSEData>();
    private Dictionary<ActionSE, AudioClip> seDictionary = new();

    public float fadeDuration = 1.0f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSourceがシーンにない場合、自動生成
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
        }

        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.loop = false;
        }

        // 辞書へ変換
        foreach (var data in seDataList)
        {
            Debug.Log($"オーディオデータ:{data.actionSE}");
            if (!seDictionary.ContainsKey(data.actionSE) && data.clip != null)
                seDictionary.Add(data.actionSE, data.clip);
        }
    }

    // ================================
    // ? BGM 再生（フェード対応）
    // ================================
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeInBGM(clip, loop));
    }

    private IEnumerator FadeInBGM(AudioClip clip, bool loop)
    {
        if (bgmSource.isPlaying)
        {
            yield return StartCoroutine(FadeOutBGM());
        }

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();

        // float t = 0f;
        // while (t < fadeDuration)
        // {
        //     t += Time.deltaTime;
        //     bgmSource.volume = Mathf.Lerp(0f, bgmVolume, t / fadeDuration);
        //     yield return null;
        // }

        // bgmSource.volume = bgmVolume;
    }

    public IEnumerator FadeOutBGM()
    {
        float startVol = bgmSource.volume;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }

        bgmSource.Stop();
    }

    public void StopBGM(bool fade = true)
    {
        if (fade)
            StartCoroutine(FadeOutBGM());
        else
            bgmSource.Stop();
    }

    // ================================
    // ? SE 再生
    // ================================
    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;
        seSource.PlayOneShot(clip, seVolume);
    }


    public void PlayActionSE(ActionSE type)
    {
        if (seDictionary.TryGetValue(type, out AudioClip clip))
        {
            seSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"未登録のSE: {type}");
        }
    }

    // ================================
    // ? 汎用呼び出しヘルパー
    // ================================
    public void PlayButtonSE() => PlaySE(buttonSE);
    public void PlayActionSE() => PlaySE(actionSE);
    public void PlayWalkSE() => PlaySE(walkSE);
    public void PlayAttackSE() => PlaySE(attackSE);
    public void PlayHitSE() => PlaySE(hitSE);
    public void PlayFinisherSE() => PlaySE(finisherSE);
    public void PlayCourageMaxSE() => PlaySE(courageMaxSE);
}
