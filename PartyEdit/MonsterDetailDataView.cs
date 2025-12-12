using UnityEngine;
using TMPro;
using System;
using DG.Tweening;
using CandyCoded.HapticFeedback;

public class MonsterDetailDataView : MonoBehaviour
{
    [SerializeField] private GameObject root;          // MonsterDetailPanel 自身でもOK
    [SerializeField] private Transform monsterPosition;

    [Header("UI パラメータ")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI mgcText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI agiText;

    [Header("UI インデックス")]
    [SerializeField] private TextMeshProUGUI indexText;
    [SerializeField] private MonsterDetailDataSlot[] monsterDetailDataSlot;

    [Header("Motion")]
    [SerializeField] private float spacing = 6f;
    [SerializeField] private float moveDuration = 0.3f;

    // 正面の回転（必要なら Inspector で調整してもOK）
    [SerializeField] private Vector3 frontEuler = Vector3.zero;
    [SerializeField] private float turnDuration = 0.12f;

    // ★ 閉じたときに呼び出すコールバック（PartyEditManager から渡す）
    private Action onClosed;

    private GameObject currentObj;
    private OwnedMonster currentMonster;

    private bool isTransitioning;
    public bool IsTransitioning => isTransitioning;

    public void Setup(Action onClosed = null)
    {
        this.onClosed = onClosed;
        root.SetActive(false);
    }

    public void ShowInitial(OwnedMonster prev, OwnedMonster current, OwnedMonster next, int index, int total)
    {
        DestroyAll();

        if (current == null)
        {
            Debug.LogWarning("MonsterDetailDataView.ShowInitial: current is null");
            return;
        }

        currentMonster = current;
        currentObj = Instantiate(current.prefab, monsterPosition);
        currentObj.transform.localPosition = Vector3.zero;

        // UI（slotはUIだけ）
        monsterDetailDataSlot[0].Setup(prev);
        monsterDetailDataSlot[1].Setup(current);
        monsterDetailDataSlot[2].Setup(next);

        UpdateStatusTexts(current, index, total);
        root.SetActive(true);
        isTransitioning = false;
    }

    /// <summary>
    /// direction: 1=次へ（左スワイプ）, -1=前へ（右スワイプ）
    /// nextMonster: nextCurrentMonster
    /// </summary>
    public void PlaySwap(
        int direction,
        OwnedMonster prev,
        OwnedMonster nextCurrent,
        OwnedMonster next,
        int nextIndex,
        int total)
    {
        if (isTransitioning) return;
        if (direction == 0) return;
        if (nextCurrent == null) return;

        isTransitioning = true;

        // ★ どっち向きに歩くか（Y回転）
        // direction == -1 : 次へ(左スワイプ) → 左へ流れる（左向き = Y=-90 と仮定）
        // direction == 1: 前へ(右スワイプ) → 右へ流れる（右向き = Y=+90 と仮定）
        float walkYaw = (direction == 1) ? 90f : -90f;

        // incoming 生成（左右）
        float spawnX = (direction == 1) ? -spacing : spacing;
        var incomingObj = Instantiate(nextCurrent.prefab, monsterPosition);
        incomingObj.transform.localPosition = new Vector3(spawnX, 0f, 0f);

        // 1) 移動開始時：移動方向を向く
        TurnTo(currentObj,  new Vector3(0f, walkYaw, 0f));
        TurnTo(incomingObj, new Vector3(0f, walkYaw, 0f));

        var anim = incomingObj.GetComponentInChildren<Animator>();
        anim?.SetBool("IsMove", true);

        // 移動
        float currentTargetX = (direction == 1) ? spacing : -spacing;

        DOTween.Sequence()
            .Join(currentObj.transform.DOLocalMoveX(currentTargetX, moveDuration).SetEase(Ease.OutQuad))
            .Join(incomingObj.transform.DOLocalMoveX(0f, moveDuration).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                anim?.SetBool("IsMove", false);
                Destroy(currentObj);
                currentObj = incomingObj;
                currentMonster = nextCurrent;
                TurnTo(currentObj, frontEuler);

                // ★ ここで UI を next 状態に更新
                monsterDetailDataSlot[0].Setup(prev);
                monsterDetailDataSlot[1].Setup(nextCurrent);
                monsterDetailDataSlot[2].Setup(next);

                UpdateStatusTexts(nextCurrent, nextIndex, total);

                isTransitioning = false;
            });
    }

    private void TurnTo(GameObject monster, Vector3 euler)
    {
        if (monster == null) return;
        monster.transform.DOLocalRotate(euler, turnDuration).SetEase(Ease.OutQuad);
    }

    public void OnDragPeek(float norm)
    {
        // norm: 左ドラッグで負、右ドラッグで正
        if (Mathf.Abs(norm) < 0.1f) return;
        Peek(norm < 0 ? 1 : -1); // 左ドラッグ→次へ(=1)とみなして右チラ見、など
    }

    public void OnMonsterTap()
    {
        if (currentObj == null) return;
        var animator = currentObj.GetComponentInChildren<Animator>();
        HapticFeedback.LightFeedback();
        animator?.SetTrigger("DoLastHit");
    }

    public void Hide()
    {
        DestroyAll();
        root.SetActive(false);
        onClosed?.Invoke();
    }

    public void OnClickCloseButton() => Hide();

    private void UpdateStatusTexts(OwnedMonster monster, int index, int total)
    {
        hpText.text  = monster.hp.ToString();
        atkText.text = monster.atk.ToString();
        mgcText.text = monster.mgc.ToString();
        defText.text = monster.def.ToString();
        agiText.text = monster.agi.ToString();

        indexText.text = $"{index + 1}/{total}";
    }

    private void DestroyAll()
    {
        if (currentObj != null) Destroy(currentObj);
        currentObj = null;
        currentMonster = null;
        isTransitioning = false;
    }

    private void Peek(int direction)
    {
        var animator = currentObj != null ? currentObj.GetComponentInChildren<Animator>() : null;
        if (animator == null) return;

        if (direction == 1) animator.SetTrigger("LookRight");
        else if (direction == -1) animator.SetTrigger("LookLeft");
    }
}