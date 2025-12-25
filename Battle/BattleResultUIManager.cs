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
    [SerializeField] private BattleResultExpBarAnimator expBarAnimator;

    [Header("Result Buttons")]
    [SerializeField] private GameObject resultButtonObj;

    private Action onNextPressed; // 押下時のコールバック
    private List<MonsterController> monsterControllers = new();

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
    public void ShowResult(bool playerWon, List<MonsterController> monsterControllers, int turn, int gainExp, Action onNext)
    {
        onNextPressed = onNext;
        this.monsterControllers.Clear();
        this.monsterControllers.AddRange(monsterControllers);
        int currentPartyIndex = GameContext.Instance.CurrentPartyIndex;
        var party = GameContext.Instance.partyList[currentPartyIndex];

        if (expBarAnimator.targets == null) expBarAnimator.targets = new List<BattleResultExpBarAnimator.Target>();

        expBarAnimator.targets.Clear();
        for (int i = 0; i < monsterControllers.Count; i++)
        {
            monsterControllers[i].PlayResult(playerWon ? 1 : 2); // 1 = win, 2 = lose
            var monsterObj = monsterControllers[i];
            var owned      = party.members[i];
            if (monsterObj == null || owned == null) continue;

            expBarAnimator.targets.Add(new BattleResultExpBarAnimator.Target
            {
                monster = monsterObj.transform,
                owned   = owned,
                ui      = null, // nullでOK。ResultExpAnimator側が生成する
            });
        }

        resultPanel.SetActive(true);
        textManager.ShowMainText(playerWon);
        textManager.ShowBattleResult(turn, gainExp);
        // resultExpAnimator.targets に、表示したい3体を入れておく（monster Transform + owned）
        if (playerWon) expBarAnimator.Play(gainedExp: gainExp);
    }

    // private void GrantExpToParty(int exp)
    // {

    //     int currentPartyIndex = GameContext.Instance.CurrentPartyIndex;
    //     var party = GameContext.Instance.partyList[currentPartyIndex];
    //     if (party == null)
    //     {
    //         Debug.LogWarning("CurrentParty が null です");
    //     }
    //     else
    //     {
    //         for (int i = 0; i < party.members.Length; i++)
    //         {
    //             var owned = party.members[i];
    //             if (owned != null)
    //             {
    //                 owned.GainExp(exp);
    //             }
    //         }
    //     }
    // }

    private void OnNextButtonClicked()
    {
        Debug.Log("resultButton Clicked!");
        foreach (var monster in monsterControllers )
            monster.PlayResult(0); // 1 = win, 2 = lose
        expBarAnimator.SkipToEnd();

        // ③ 経験値バーを消す（超重要）
        expBarAnimator.ClearBars();

        onNextPressed?.Invoke();
        resultPanel.SetActive(false);
    }
}
