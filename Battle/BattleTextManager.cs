using UnityEngine;
using TMPro;
using System.Collections;

public class BattleTextManager : MonoBehaviour
{
    [Header("�S�̃��b�Z�[�W")]
    [SerializeField] private GameObject mainTextObj;
    [SerializeField] private TextMeshProUGUI battleMainText;

    [Header("�^�[���\��")]
    [SerializeField] private GameObject turnTextObj;
    [SerializeField] private TextMeshProUGUI battleTurnText;

    [Header("�v���C���[��")]
    [SerializeField] private GameObject playerTextObj;
    [SerializeField] private TextMeshProUGUI playerTitleText;
    [SerializeField] private TextMeshProUGUI playerSkillText;

    [Header("�G��")]
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
        // �����͔�\���ɂ��Ă���
        mainTextObj.SetActive(false);
        turnTextObj.SetActive(false);
        playerTextObj.SetActive(false);
        enemyTextObj.SetActive(false);
    }

    // ================================
    // �o�g���S�̃��b�Z�[�W�\��
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
    // �^�[���\���i�펞�\���E�X�V�j
    // ================================
    public void ShowTurnText(int turn)
    {
        turnTextObj.SetActive(true);
        battleTurnText.text = $"�^�[�� {turn}";
    }

    public void HideTurnText()
    {
        turnTextObj.SetActive(false);
    }

    // ================================
    // �v���C���[�U���e�L�X�g
    // ================================
    public void ShowPlayerAttackText(string monsterName, string skillName)
    {
        playerTextObj.SetActive(true);
        playerTitleText.text = $"{monsterName} �̍U���I";
        playerSkillText.text = skillName;
    }

    public void HidePlayerAttackText()
    {
        playerTextObj.SetActive(false);
    }

    // ================================
    // �G�U���e�L�X�g
    // ================================
    public void ShowEnemyAttackText(string monsterName, string skillName)
    {
        enemyTextObj.SetActive(true);
        enemyTitleText.text = $"{monsterName} �̍U���I";
        enemySkillText.text = skillName;
    }

    public void HideEnemyAttackText()
    {
        enemyTextObj.SetActive(false);
    }
}
