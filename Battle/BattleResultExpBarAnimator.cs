using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class BattleResultExpBarAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Target
    {
        public Transform monster;          // 頭上追従対象
        public OwnedMonster owned;         // データ
        public BattleResultExpBar ui;      // 生成したUI（未設定なら生成）
    }

    [Header("UI")]
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private BattleResultExpBar expBarPrefab;

    [Header("Anim")]
    [SerializeField] private float segmentDuration = 0.45f;
    [SerializeField] private float levelUpPause = 0.20f;

    public Camera worldCamera;
    public List<Target> targets = new();

    // ----------------------------
    // 内部状態（スキップ用）
    // ----------------------------
    private Coroutine playRoutine;
    private readonly List<Coroutine> runningCoroutines = new();
    private readonly List<Tween> activeTweens = new();

    private bool isPlaying = false;
    private bool isApplied = false;
    private int cachedGainedExp = 0;

    public bool IsPlaying => isPlaying;

    // ----------------------------
    // 外部から呼ぶ入口
    // ----------------------------
    public void Play(int gainedExp)
    {
        if (gainedExp <= 0) return;

        // 途中再生が来たら、まず確定させてから開始（安全）
        if (isPlaying) SkipToEnd();

        if (worldCamera == null) worldCamera = Camera.main;

        cachedGainedExp = gainedExp;
        isApplied = false;
        isPlaying = true;

        playRoutine = StartCoroutine(PlayRoutine(gainedExp));
    }

    /// <summary>
    /// ★ 途中スキップ（次へ押下時に呼ぶ）
    /// - アニメ/コルーチンを止める
    /// - 経験値を即時にOwnedMonsterへ確定反映
    /// - UIも最終状態に更新
    /// </summary>
    public void SkipToEnd()
    {
        if (!isPlaying && isApplied) return;

        // 1) コルーチン停止
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        foreach (var c in runningCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        runningCoroutines.Clear();

        // 2) Tween停止（念のため Kill）
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            var tw = activeTweens[i];
            if (tw != null && tw.IsActive())
            {
                // complete:false で即停止（途中のOnUpdate等を走らせない）
                tw.Kill(complete: false);
            }
        }
        activeTweens.Clear();

        // 3) データ確定反映（ここが最重要）
        ApplyExpIfNeeded();

        // 4) UIも最終状態へ即更新
        RefreshAllUIFromOwned();

        isPlaying = false;
    }

    IEnumerator PlayRoutine(int gainedExp)
    {
        // UI生成＆初期表示
        EnsureUIAndShowCurrent();

        // 3体同時に進めたいなら並列で
        runningCoroutines.Clear();
        foreach (var t in targets)
        {
            var c = StartCoroutine(AnimateOne(t, gainedExp));
            runningCoroutines.Add(c);
        }

        // 全部終わるまで待つ（注意：Coroutine参照を yield return できないのでフラグで待つ）
        // → ここは「全体が終わったら Apply」なので、簡易に WaitUntil でOK
        yield return new WaitUntil(() => AllTweensAndCoroutinesFinished());

        runningCoroutines.Clear();

        // 最後にデータへ確定反映（ここで OnChanged が発火）
        ApplyExpIfNeeded();

        // UIを最終状態に揃える（演出後の微ズレ防止）
        RefreshAllUIFromOwned();

        isPlaying = false;
        playRoutine = null;
    }

    private bool AllTweensAndCoroutinesFinished()
    {
        // Tweenが残ってたら未完
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            var tw = activeTweens[i];
            if (tw == null) { activeTweens.RemoveAt(i); continue; }
            if (tw.IsActive() && tw.IsPlaying()) return false;
            // 終了したTweenは掃除
            if (!tw.IsActive()) activeTweens.RemoveAt(i);
        }

        // コルーチンの「終了検知」は本当は難しいので、
        // この実装では「Tweenが全部止まったら終わり」とみなす（現状のAnimateOne構造なら問題なし）
        return activeTweens.Count == 0;
    }

    private void EnsureUIAndShowCurrent()
    {
        foreach (var t in targets)
        {
            if (t == null) continue;

            if (t.ui == null && expBarPrefab && overlayCanvas)
            {
                var ui = Instantiate(expBarPrefab, overlayCanvas.transform);
                ui.canvas = overlayCanvas;
                ui.worldCamera = worldCamera;
                ui.worldTarget = t.monster;
                t.ui = ui;
            }

            if (t.ui == null || t.owned == null) continue;

            // 現在値で初期表示
            t.ui.SetLevel(t.owned.level);
            t.ui.SetRange(t.owned.RequiredExpToNext);
            t.ui.SetValue(t.owned.exp);
        }
    }

    private void ApplyExpIfNeeded()
    {
        if (isApplied) return;

        int gainedExp = cachedGainedExp;
        if (gainedExp <= 0) { isApplied = true; return; }

        foreach (var t in targets)
        {
            if (t?.owned == null) continue;
            t.owned.GainExp(gainedExp);
        }

        isApplied = true;
    }

    private void RefreshAllUIFromOwned()
    {
        foreach (var t in targets)
        {
            if (t?.ui == null || t.owned == null) continue;

            t.ui.SetLevel(t.owned.level);
            t.ui.SetRange(t.owned.RequiredExpToNext);
            t.ui.SetValue(t.owned.exp);
        }
    }

    struct Segment
    {
        public int level;      // このセグメント開始時のレベル
        public int required;   // このレベルの必要exp
        public int fromExp;    // レベル内exp（0..required）
        public int toExp;      // レベル内exp
        public bool levelUp;   // このセグメントでレベルアップ到達するか
    }

    IEnumerator AnimateOne(Target t, int gainedExp)
    {
        if (t?.owned == null || t.ui == null) yield break;
        if (gainedExp <= 0) yield break;

        // いまの状態をコピーして「セグメント」を作る（OwnedMonster本体は触らない）
        var segs = BuildSegments(
            startLevel: t.owned.level,
            startExp:   t.owned.exp,
            gainedExp:  gainedExp
        );

        foreach (var s in segs)
        {
            if (!isPlaying) yield break; // スキップで停止されたら即終了

            t.ui.SetRange(s.required);

            Tween tw = t.ui.AnimateValue(s.fromExp, s.toExp, segmentDuration);
            if (tw != null)
            {
                activeTweens.Add(tw);
                yield return tw.WaitForCompletion();
                // 終わったTweenは掃除（ここで確実に消す）
                activeTweens.Remove(tw);
            }

            if (!isPlaying) yield break;

            if (s.levelUp)
            {
                t.ui.SetLevel(s.level + 1);
                t.ui.PlayLevelUp();
                yield return new WaitForSeconds(levelUpPause);

                // 次ループでSetRange/SetValueされるけど、気持ちよく0に落としておく
                t.ui.SetValue(0);
            }
        }
    }

    List<Segment> BuildSegments(int startLevel, int startExp, int gainedExp)
    {
        List<Segment> list = new();

        int level = startLevel;
        int exp   = startExp;
        int remaining = gainedExp;

        while (remaining > 0 && level < OwnedMonster.MaxLevel)
        {
            int need = OwnedMonster.GetRequiredExpForNext(level);
            if (need <= 0) break;

            int toNext = need - exp;               // このレベルで残り
            int give   = Mathf.Min(remaining, toNext);

            bool willLevelUp = (exp + give) >= need;

            list.Add(new Segment
            {
                level = level,
                required = need,
                fromExp = exp,
                toExp   = exp + give,
                levelUp = willLevelUp
            });

            remaining -= give;

            if (willLevelUp)
            {
                exp = (exp + give) - need; // 繰り越し対応
                level++;
            }
            else
            {
                exp += give;
            }
        }

        return list;
    }

    public void ClearBars()
    {
        // 途中なら先に止めて確定（事故防止）
        if (isPlaying) SkipToEnd();

        if (targets == null) return;

        foreach (var t in targets)
        {
            if (t?.ui != null)
            {
                Destroy(t.ui.gameObject);
                t.ui = null;
            }
        }

        targets.Clear();
    }
}
