using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class BattleResultUIManager : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;     // BattleResultPanel プレハブ
    [Header("テキスト管理")]
    [SerializeField] private BattleResultTextManager textManager;

    [Header("Result Buttons")]
    [SerializeField] private GameObject resultButtonObj;

    private Action onNextPressed; // 押下時のコールバック

    /// <summary>
    /// 初期化（ボタンイベント登録）
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

        // 最初は非表示
        resultPanel?.SetActive(false);
    }

    /// <summary>
    /// 結果表示を開始
    /// </summary>
    public void ShowResult(bool playerWon, Action onNext)
    {
        onNextPressed = onNext;

        resultPanel.SetActive(true);
        string msg = playerWon ? "勝利！" : "敗北…";
        textManager.ShowMainText(msg);
    }

    private void OnNextButtonClicked()
    {
        Debug.Log("resultButton Clicked!");

        onNextPressed?.Invoke();
        resultPanel.SetActive(false);
    }
}
