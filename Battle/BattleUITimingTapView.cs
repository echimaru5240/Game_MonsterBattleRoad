using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;
using DG.Tweening;

public class BattleUITimingTapView : MonoBehaviour
{
    [Header("Rings")]
    [SerializeField] private RectTransform activeRing;   // 青リング（縮む）
    [SerializeField] private Image activeRingImage;      // 青リングのImage（フラッシュ用）
    [SerializeField] private CanvasGroup ringsGroup;     // 任意：リング全体をまとめてフラッシュしたい場合
    [SerializeField] private TextMeshProUGUI rankText;   // 任意
    [SerializeField] private CanvasGroup textGroup;      // 任意：テキストをフラッシュしたい場合

    [Header("Scale Animation")]
    [SerializeField] private float startScale = 2.2f;
    [SerializeField] private float endScale   = 0.6f;
    [SerializeField] private float duration  = 1.0f;

    [Header("Judge Scale Range (target = 1.0)")]
    [SerializeField] private float perfectMin = 0.97f;
    [SerializeField] private float perfectMax = 1.03f;

    [SerializeField] private float greatMin   = 0.90f;
    [SerializeField] private float greatMax   = 1.10f;

    [SerializeField] private float goodMin    = 0.82f;
    [SerializeField] private float goodMax    = 1.18f;

    [Header("Damage Multipliers")]
    [SerializeField] private float perfectMul = 1.5f;
    [SerializeField] private float greatMul   = 1.25f;
    [SerializeField] private float goodMul    = 1.1f;
    [SerializeField] private float missMul    = 0.85f;

    [Header("Rank Colors")]
    [SerializeField] private Color perfectColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color greatColor   = new Color(0.3f, 1f, 0.5f, 1f);
    [SerializeField] private Color goodColor    = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color missColor    = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Flash (on tap)")]
    [SerializeField] private float flashTime = 0.08f;
    [SerializeField] private float flashScaleUp = 1.15f;
    [SerializeField] private int flashLoops = 2;            // 2回点滅（行って戻ってで1ループ）
    [SerializeField] private float afterDelayToDestroy = 0.20f;

    private float timer;
    private bool running;
    private bool decided;

    private Action<TimingResult> onFinished;

    private Sequence flashSeq;

    // ================================
    // 外部から呼ぶ
    // ================================
    public void Play(Action<TimingResult> onFinished)
    {
        this.onFinished = onFinished;
        timer = 0f;
        running = true;
        decided = false;

        if (rankText)
        {
            rankText.text = "";
            rankText.color = goodColor;
            rankText.transform.localScale = Vector3.one;
        }

        if (ringsGroup) ringsGroup.alpha = 1f;
        if (textGroup)  textGroup.alpha  = 1f;

        SetActiveScale(startScale);
        if (activeRingImage) activeRingImage.color = new Color(activeRingImage.color.r, activeRingImage.color.g, activeRingImage.color.b, 1f);
    }

    void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        float scale = Mathf.Lerp(startScale, endScale, t);
        SetActiveScale(scale);

        // 入力
        if (!decided && IsTap())
        {
            Decide(Evaluate(scale));
            return;
        }

        // 時間切れ
        if (t >= 1f && !decided)
        {
            Decide(Miss());
        }
    }

    // ================================
    // 判定
    // ================================
    private TimingResult Evaluate(float scale)
    {
        if (scale >= perfectMin && scale <= perfectMax)
            return Result(TimingRank.Perfect, perfectMul);

        if (scale >= greatMin && scale <= greatMax)
            return Result(TimingRank.Great, greatMul);

        if (scale >= goodMin && scale <= goodMax)
            return Result(TimingRank.Good, goodMul);

        return Miss();
    }

    private TimingResult Miss()
    {
        return Result(TimingRank.Miss, missMul);
    }

    private TimingResult Result(TimingRank rank, float mul)
    {
        return new TimingResult
        {
            rank = rank,
            multiplier = mul
        };
    }

    // ================================
    // 終了処理
    // ================================
    private void Decide(TimingResult result)
    {
        decided = true;
        running = false;

        // ランク表示＆色
        if (rankText)
        {
            rankText.text = result.rank.ToString();
            rankText.color = GetRankColor(result.rank);
        }

        onFinished?.Invoke(result);

        // タップ時フラッシュ（リング＆テキスト）
        PlayFlash(result.rank);

        // 演出後に消す
        Destroy(gameObject, Mathf.Max(0.01f, afterDelayToDestroy));
    }

    private Color GetRankColor(TimingRank rank)
    {
        return rank switch
        {
            TimingRank.Perfect => perfectColor,
            TimingRank.Great   => greatColor,
            TimingRank.Good    => goodColor,
            _                  => missColor,
        };
    }

    private void PlayFlash(TimingRank rank)
    {
        flashSeq?.Kill();

        // 「当たった感」を強めたいので、Missは少し控えめにする例
        float scaleUp = (rank == TimingRank.Miss) ? 1.05f : flashScaleUp;
        int loops     = (rank == TimingRank.Miss) ? 1 : flashLoops;

        flashSeq = DOTween.Sequence();

        // テキスト：ポン＋点滅
        if (rankText)
        {
            flashSeq.Join(rankText.transform.DOScale(scaleUp, flashTime).SetEase(Ease.OutQuad));
            flashSeq.Join(rankText.transform.DOScale(1f, flashTime).SetEase(Ease.InQuad).SetDelay(flashTime));

            if (textGroup)
            {
                // alpha点滅（0.2 ? 1.0）
                flashSeq.Join(textGroup.DOFade(0.2f, flashTime).SetLoops(loops, LoopType.Yoyo));
            }
            else
            {
                // CanvasGroupが無い場合は色のalphaで点滅
                flashSeq.Join(rankText.DOFade(0.2f, flashTime).SetLoops(loops, LoopType.Yoyo));
            }
        }

        // リング：フラッシュ（白っぽく）＋点滅
        if (activeRingImage)
        {
            // 色フラッシュ（白に寄せる）
            Color baseCol = activeRingImage.color;
            Color flashCol = Color.white;
            flashCol.a = 1f;

            flashSeq.Join(activeRingImage.DOColor(flashCol, flashTime));
            flashSeq.Join(activeRingImage.DOColor(baseCol, flashTime).SetDelay(flashTime));

            // alpha点滅（CanvasGroupあるならそっち優先）
            if (ringsGroup)
                flashSeq.Join(ringsGroup.DOFade(0.25f, flashTime).SetLoops(loops, LoopType.Yoyo));
            else
                flashSeq.Join(activeRingImage.DOFade(0.25f, flashTime).SetLoops(loops, LoopType.Yoyo));
        }

        // ちょい余韻
        flashSeq.SetUpdate(true);
    }

    // ================================
    // Utility
    // ================================
    private void SetActiveScale(float s)
    {
        activeRing.localScale = Vector3.one * s;
    }

    private bool IsTap()
    {
    #if ENABLE_INPUT_SYSTEM
        // New Input System
        // PC: 左クリック / Mobile: primaryTouch
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Touchscreen.current != null)
        {
            // primaryTouch.press は「押された瞬間」を wasPressedThisFrame で取れる
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
        }

        return false;
    #else
        // Old Input Manager（Active Input Handling が Both の時用）
        if (Input.GetMouseButtonDown(0)) return true;
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    #endif
    }

}
