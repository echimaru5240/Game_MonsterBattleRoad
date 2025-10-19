using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// �o�g�����[�h���ʃT�E���h�Ǘ�
/// �V�[�����܂����ł��j������Ȃ��iBGM/SE�ꌳ�Ǘ��j
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

    [Header("��SE�o�^")]
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

        // AudioSource���V�[���ɂȂ��ꍇ�A��������
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

        // �����֕ϊ�
        foreach (var data in seDataList)
        {
            Debug.Log($"�I�[�f�B�I�f�[�^:{data.actionSE}");
            if (!seDictionary.ContainsKey(data.actionSE) && data.clip != null)
                seDictionary.Add(data.actionSE, data.clip);
        }
    }

    // ================================
    // ? BGM �Đ��i�t�F�[�h�Ή��j
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
    // ? SE �Đ�
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
            Debug.LogWarning($"���o�^��SE: {type}");
        }
    }

    // ================================
    // ? �ėp�Ăяo���w���p�[
    // ================================
    public void PlayButtonSE() => PlaySE(buttonSE);
    public void PlayActionSE() => PlaySE(actionSE);
    public void PlayWalkSE() => PlaySE(walkSE);
    public void PlayAttackSE() => PlaySE(attackSE);
    public void PlayHitSE() => PlaySE(hitSE);
    public void PlayFinisherSE() => PlaySE(finisherSE);
    public void PlayCourageMaxSE() => PlaySE(courageMaxSE);
}
