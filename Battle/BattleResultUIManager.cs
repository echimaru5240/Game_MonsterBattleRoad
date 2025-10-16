using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class BattleResultUIManager : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;     // BattleResultPanel �v���n�u
    [Header("�e�L�X�g�Ǘ�")]
    [SerializeField] private BattleResultTextManager textManager;

    [Header("Result Buttons")]
    [SerializeField] private GameObject resultButtonObj;

    private Action onNextPressed; // �������̃R�[���o�b�N

    /// <summary>
    /// �������i�{�^���C�x���g�o�^�j
    /// </summary>
    void Start()
    {
        if (resultButtonObj != null) {
            Button resultButton = resultButtonObj.GetComponentInChildren<Button>();
            resultButton.onClick.AddListener(OnNextButtonClicked);
        }
        else {

            Debug.Log("resultButtonObj null");
        }

        // �ŏ��͔�\��
        resultPanel?.SetActive(false);
    }

    /// <summary>
    /// ���ʕ\�����J�n
    /// </summary>
    public void ShowResult(bool playerWon, Action onNext)
    {
        onNextPressed = onNext;

        resultPanel.SetActive(true);
        string msg = playerWon ? "�����I" : "�s�k�c";
        textManager.ShowMainText(msg);
    }

    private void OnNextButtonClicked()
    {
        Debug.Log("resultButton Clicked!");

        onNextPressed?.Invoke();
        resultPanel.SetActive(false);
    }
}
