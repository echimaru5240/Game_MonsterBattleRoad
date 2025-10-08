using UnityEngine;
using TMPro;
using System.Collections;

public class BattleTextManager : MonoBehaviour
{
    [Header("全体メッセージ")]
    [SerializeField] private GameObject mainTextObj;
    [SerializeField] private TextMeshProUGUI battleMainText;

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

    private void Awake()
    {
        battleMainText.text = "";
        battleTurnText.text = "";
        playerTitleText.text = "";
        playerSkillText.text = "";
        enemyTitleText.text = "";
        enemySkillText.text = "";
        // 初期は非表示にしておく
        mainTextObj.SetActive(false);
        turnTextObj.SetActive(false);
        playerTextObj.SetActive(false);
        enemyTextObj.SetActive(false);
    }

    // ================================
    // バトル全体メッセージ表示
    // ================================
    public void ShowMainText(string message, float duration = 2f)
    {
        mainTextObj.SetActive(true);
        StartCoroutine(ShowMainTextRoutine(message, duration));
    }

    private IEnumerator ShowMainTextRoutine(string msg, float duration)
    {
        battleMainText.text = msg;
        yield return new WaitForSeconds(duration);
        mainTextObj.SetActive(false);
        battleMainText.text = "";
    }

    // ================================
    // ターン表示（常時表示・更新）
    // ================================
    public void ShowTurnText(int turn)
    {
        turnTextObj.SetActive(true);
        battleTurnText.text = $"ターン {turn}";
    }

    public void HideTurnText()
    {
        turnTextObj.SetActive(false);
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
