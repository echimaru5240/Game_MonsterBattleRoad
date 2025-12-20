using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class BattleTextManager : MonoBehaviour
{
    [Header("全体メッセージ")]
    [SerializeField] private RectTransform mainTextObj;
    [SerializeField] private CanvasGroup canvasGroup;     // フェード用（無ければ追加してOK）
    [SerializeField] private TextMeshProUGUI battleMainText;
    [SerializeField] private float appearDuration = 0.18f;
    [SerializeField] private float holdDuration   = 0.45f;
    [SerializeField] private float exitDuration   = 0.18f;
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float peakScale  = 1.18f;

    [Header("Optional Move")]
    [SerializeField] private bool useSlideOut = true;
    [SerializeField] private Vector2 slideOutOffset = new Vector2(220f, -80f); // 斜めに抜ける


    [Header("ターン表示")]
    [SerializeField] private GameObject turnTextObj;
    [SerializeField] private TextMeshProUGUI battleTurnText;

    [Header("プレイヤー側")]
    [SerializeField] private GameObject playerTextObj;
    [SerializeField] private TextMeshProUGUI playerTitleText;
    [SerializeField] private TextMeshProUGUI playerSkillText;

    [Header("敵側")]
    [SerializeField] private GameObject enemyTextObj;
    [SerializeField] private TextMeshProUGUI enemyTitleText;
    [SerializeField] private TextMeshProUGUI enemySkillText;

    private Sequence seq;

    private void Awake()
    {
        battleMainText.text = "";
        battleTurnText.text = "";
        playerTitleText.text = "";
        playerSkillText.text = "";
        enemyTitleText.text = "";
        enemySkillText.text = "";
        // 初期は非表示にしておく
        mainTextObj.gameObject.SetActive(false);
        playerTextObj.SetActive(false);
        enemyTextObj.SetActive(false);
        turnTextObj.SetActive(true);
    }

    // ================================
    // バトル全体メッセージ表示
    // ================================
    public void ShowMainText(string message, float duration = 2f)
    {
        // mainTextObj.SetActive(true);
        // StartCoroutine(ShowMainTextRoutine(message, duration));
        Play(message);
    }

    private IEnumerator ShowMainTextRoutine(string msg, float duration)
    {
        battleMainText.text = msg;
        yield return new WaitForSeconds(duration);
        mainTextObj.gameObject.SetActive(false);
        battleMainText.text = "";
    }

    public void Play(string message)
    {
        if (battleMainText == null || mainTextObj == null) return;

        // 既存演出を止める
        seq?.Kill();
        seq = null;

        // 初期化
        battleMainText.text = message;

        mainTextObj.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        mainTextObj.anchoredPosition = Vector2.zero;
        mainTextObj.localScale = Vector3.one * startScale;

        // 演出
        seq = DOTween.Sequence();

        // 0.2 -> 1.18 (ドン！) -> 1.0
        seq.Append(mainTextObj.DOScale(peakScale, appearDuration).SetEase(Ease.OutBack));
        seq.Append(mainTextObj.DOScale(1.0f, 0.08f).SetEase(Ease.OutQuad));

        // しばらく表示
        seq.AppendInterval(holdDuration);

        // 消える
        if (useSlideOut)
        {
            seq.Join(mainTextObj.DOAnchorPos(slideOutOffset, exitDuration).SetEase(Ease.InQuad));
        }
        seq.Append(canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InQuad));

        seq.OnComplete(HideImmediate);
    }

    public void HideImmediate()
    {
        seq?.Kill();
        seq = null;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (mainTextObj != null)
        {
            mainTextObj.localScale = Vector3.one;
            mainTextObj.anchoredPosition = Vector2.zero;
            mainTextObj.gameObject.SetActive(false);
        }
    }

    // ================================
    // ターン表示（常時表示・更新）
    // ================================
    public void ShowTurnText(int turn)
    {
        battleTurnText.text = $"{turn}";
    }


    // ================================
    // プレイヤー攻撃テキスト
    // ================================
    public void ShowPlayerAttackText(string monsterName, string skillName)
    {
        playerTextObj.SetActive(true);
        playerTitleText.text = $"{monsterName} の攻撃！";
        playerSkillText.text = skillName;
    }

    public void HidePlayerAttackText()
    {
        playerTextObj.SetActive(false);
    }

    // ================================
    // 敵攻撃テキスト
    // ================================
    public void ShowEnemyAttackText(string monsterName, string skillName)
    {
        enemyTextObj.SetActive(true);
        enemyTitleText.text = $"{monsterName} の攻撃！";
        enemySkillText.text = skillName;
    }

    public void HideEnemyAttackText()
    {
        enemyTextObj.SetActive(false);
    }
}
