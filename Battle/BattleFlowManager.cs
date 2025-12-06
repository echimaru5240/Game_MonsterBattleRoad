using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleFlowManager : MonoBehaviour
{
    public BattleManager battleManager;
    public BattleStageData[] stages;   // ← ScriptableObject 配列

    private int currentStageIndex = 0;
    private int carriedCourage = 0; // 引き継ぎ用

    private MonsterBattleData[] partyBattleData = new MonsterBattleData[0];

    void Start()
    {
        int currentPartyIndex = GameContext.Instance.CurrentPartyIndex;
        var party = GameContext.Instance.partyList[currentPartyIndex];
        if (party == null)
        {
            Debug.LogWarning("CurrentParty が null です");
            partyBattleData = new MonsterBattleData[0];
        }
        else
        {
            partyBattleData = new MonsterBattleData[party.members.Length];

            for (int i = 0; i < party.members.Length; i++)
            {
                var owned = party.members[i];
                if (owned == null)
                {
                    partyBattleData[i] = null; // 空スロット
                    continue;
                }

                partyBattleData[i] = MonsterBattleData.CreateBattleFromOwnedData(owned);
            }
        }
        StartStage(0, carriedCourage);
    }

    public void StartStage(int index, int initialCourage)
    {
        currentStageIndex = index;
        var stage = stages[index];

        // ステージ情報を表示（UIで背景やBGM切り替えも可能）
        Debug.Log($"ステージ開始: {stage.stageName}");

        StartCoroutine(battleManager.SetupBattle(partyBattleData, stage, OnBattleEnd, initialCourage));

        // if (stage.bgm != null)
        // {
        //     AudioSource audio = GetComponent<AudioSource>();
        //     if (audio != null)
        //     {
        //         audio.clip = stage.bgm;
        //         audio.Play();
        //     }
        // }
    }

    private void OnBattleEnd(bool playerWon, int remainingCourage)
    {
        carriedCourage = remainingCourage; // 勇気ゲージを保存

        if (!playerWon)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
            return;
        }

        if (currentStageIndex + 1 < stages.Length)
        {
            StartStage(currentStageIndex + 1, carriedCourage);
        }
        else
        {
            StartCoroutine(GoHomeAfterDelay());
        }
    }

    private System.Collections.IEnumerator GoHomeAfterDelay()
    {
        Debug.Log("ゲームクリア！ → ホームに戻る");
        yield return new WaitForSeconds(3f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
    }
}

