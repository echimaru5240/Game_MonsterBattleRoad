using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class BattleResultTextManager : MonoBehaviour
{
    [Header("全体メッセージ")]
    [SerializeField] private RectTransform mainTextObj;
    [SerializeField] private Image mainTextBG;
    [SerializeField] private Color mainTextObjColorWin;
    [SerializeField] private Color mainTextObjColorLose;
    [SerializeField] private TextMeshProUGUI resultMainText;
    [SerializeField] private float appearDuration = 0.18f;
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float peakScale  = 1.50f;
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI gainExpText;

    private Sequence seq;

    private void Awake()
    {
        resultMainText.text = "";
        // 初期は非表示にしておく
        mainTextObj.gameObject.SetActive(false);
        turnText.gameObject.SetActive(false);
        gainExpText.gameObject.SetActive(false);
    }

    // ================================
    // バトル全体メッセージ表示
    // ================================
    public void ShowMainText(bool battleWon, int turn, int gainExp)
    {
        Play(battleWon);
        turnText.text = turn.ToString();
        turnText.gameObject.SetActive(true);
        gainExpText.text = battleWon ? gainExp.ToString() : "0";
        gainExpText.gameObject.SetActive(true);
    }

    public void Play(bool battleWon)
    {
        if (resultMainText == null || mainTextObj == null) return;

        // 既存演出を止める
        seq?.Kill();
        seq = null;

        // 初期化
        resultMainText.text = battleWon ? "勝利！" : "敗北...";
        mainTextBG.color = battleWon ? mainTextObjColorWin : mainTextObjColorLose;

        mainTextObj.gameObject.SetActive(true);

        mainTextObj.anchoredPosition = Vector2.zero;
        mainTextObj.localScale = Vector3.one * startScale;

        // 演出
        seq = DOTween.Sequence();

        // 0.2 -> 1.18 (ドン！) -> 1.0
        seq.Append(mainTextObj.DOScale(peakScale, appearDuration).SetEase(Ease.OutBack));
        seq.Append(mainTextObj.DOScale(1.0f, 0.08f).SetEase(Ease.OutQuad));
    }

    public void ShowBattleResult(int turn, int gainExp)
    {
        turnText.text = turn.ToString();
        turnText.gameObject.SetActive(true);
        gainExpText.text = gainExp.ToString();
        gainExpText.gameObject.SetActive(true);
    }

}
