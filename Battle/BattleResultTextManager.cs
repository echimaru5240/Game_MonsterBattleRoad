using UnityEngine;
using TMPro;
using System.Collections;

public class BattleResultTextManager : MonoBehaviour
{
    [Header("�S�̃��b�Z�[�W")]
    [SerializeField] private GameObject mainTextObj;
    [SerializeField] private TextMeshProUGUI resultMainText;


    private void Awake()
    {
        resultMainText.text = "";
        // �����͔�\���ɂ��Ă���
        mainTextObj.SetActive(false);
    }

    // ================================
    // �o�g���S�̃��b�Z�[�W�\��
    // ================================
    public void ShowMainText(string message)
    {
        mainTextObj.SetActive(true);
        resultMainText.text = message;
    }

}
