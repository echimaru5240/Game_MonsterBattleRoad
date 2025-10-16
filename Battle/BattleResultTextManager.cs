using UnityEngine;
using TMPro;
using System.Collections;

public class BattleResultTextManager : MonoBehaviour
{
    [Header("全体メッセージ")]
    [SerializeField] private GameObject mainTextObj;
    [SerializeField] private TextMeshProUGUI resultMainText;


    private void Awake()
    {
        resultMainText.text = "";
        // 初期は非表示にしておく
        mainTextObj.SetActive(false);
    }

    // ================================
    // バトル全体メッセージ表示
    // ================================
    public void ShowMainText(string message)
    {
        mainTextObj.SetActive(true);
        resultMainText.text = message;
    }

}
