using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;
using CandyCoded.HapticFeedback;

public class MonsterDetailDataView : MonoBehaviour
{
    [SerializeField] private GameObject root;          // MonsterDetailPanel 自身でもOK
    [SerializeField] private Transform monsterPosition;

    [SerializeField] private StatAllocateView allocateView;

    [Header("UI パラメータ")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI nextExpText;
    [SerializeField] private TextMeshProUGUI totalExpText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI mgcText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI agiText;
    [SerializeField] private Slider          expSlider; // 経験値バー
    [SerializeField] private GameObject      levelUpButton;

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

        // currentMonster = current;
        BindMonster(current, index, total);
        currentObj = Instantiate(current.prefab, monsterPosition);
        currentObj.transform.localPosition = Vector3.zero;

        // UI（slotはUIだけ）
        monsterDetailDataSlot[0].Setup(prev);
        monsterDetailDataSlot[1].Setup(current);
        monsterDetailDataSlot[2].Setup(next);

        UpdateStatusTexts(current, index, total);
        root.SetActive(true);
        isTransitioning = false;
        levelUpButton.SetActive(current.unspentStatPoints > 0);
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
                // currentMonster = nextCurrent;
                BindMonster(nextCurrent, nextIndex, total);
                TurnTo(currentObj, frontEuler);

                // ★ ここで UI を next 状態に更新
                monsterDetailDataSlot[0].Setup(prev);
                monsterDetailDataSlot[1].Setup(nextCurrent);
                monsterDetailDataSlot[2].Setup(next);

                UpdateStatusTexts(nextCurrent, nextIndex, total);
                CloseAllocateView();

                isTransitioning = false;
            });
    }

    private void TurnTo(GameObject monster, Vector3 euler)
    {
        if (monster == null) return;
        monster.transform.DOLocalRotate(euler, turnDuration).SetEase(Ease.OutQuad);
    }

    private void BindMonster(OwnedMonster monster, int index, int total)
    {
        // 前の購読解除
        if (currentMonster != null)
            currentMonster.OnChanged -= OnCurrentMonsterChanged;

        currentMonster = monster;

        if (currentMonster != null)
            currentMonster.OnChanged += OnCurrentMonsterChanged;

        UpdateStatusTexts(currentMonster, index, total);
    }

    private void OnCurrentMonsterChanged()
    {
        if (currentMonster == null) return;

        int requiredExp = currentMonster.RequiredExpToNext;
        int gainedExp = currentMonster.exp;
        int nextLevelExp = requiredExp - gainedExp;
        // index/total は現状だと保持してないので
        // いま表示中の indexText を壊さないならステータスだけ更新でもOK
        levelText.text    = currentMonster.level.ToString();
        nextExpText.text  = nextLevelExp.ToString();
        totalExpText.text = currentMonster.totalExp.ToString();
        expSlider.minValue = 0;
        expSlider.maxValue = requiredExp;
        expSlider.value   = Mathf.Clamp(gainedExp, 0, requiredExp);
        hpText.text  = currentMonster.hp.ToString();
        atkText.text = currentMonster.atk.ToString();
        mgcText.text = currentMonster.mgc.ToString();
        defText.text = currentMonster.def.ToString();
        agiText.text = currentMonster.agi.ToString();
    }

    public void OpenAllocateView()
    {
        if (allocateView == null) return;
        if (currentMonster == null) return;

        if (currentMonster.unspentStatPoints > 0)
        {
            levelUpButton.SetActive(false);
            allocateView.Open(currentMonster, CloseAllocateView);
        }
    }

    public void OnLevelUpButtonClicked()
    {
        OpenAllocateView();
    }

    public void CloseAllocateView()
    {
        if (allocateView == null) return;

        allocateView.Close();
        levelUpButton.SetActive(currentMonster.unspentStatPoints > 0);
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
        int requiredExp  = monster.RequiredExpToNext;
        int gainedExp    = monster.exp;
        int nextLevelExp = requiredExp - gainedExp;

        levelText.text    = monster.level.ToString();
        nextExpText.text  = nextLevelExp.ToString();
        totalExpText.text = monster.totalExp.ToString();
        expSlider.minValue = 0;
        expSlider.maxValue = requiredExp;
        expSlider.value   = Mathf.Clamp(gainedExp, 0, requiredExp);
        hpText.text       = monster.hp.ToString();
        atkText.text      = monster.atk.ToString();
        mgcText.text      = monster.mgc.ToString();
        defText.text      = monster.def.ToString();
        agiText.text      = monster.agi.ToString();

        indexText.text = $"{index + 1}/{total}";
    }

    private void DestroyAll()
    {
        if (currentMonster != null)
            currentMonster.OnChanged -= OnCurrentMonsterChanged;

        if (currentObj != null) Destroy(currentObj);
        currentObj = null;
        currentMonster = null;
        isTransitioning = false;

        // 重畳パネルも閉じる（開いてたら）
        if (allocateView != null) allocateView.Close();
    }

    private void Peek(int direction)
    {
        var animator = currentObj != null ? currentObj.GetComponentInChildren<Animator>() : null;
        if (animator == null) return;

        if (direction == 1) animator.SetTrigger("LookRight");
        else if (direction == -1) animator.SetTrigger("LookLeft");
    }
}