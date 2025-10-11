using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleFlowManager : MonoBehaviour
{
    public BattleManager battleManager;
    public MonsterCard[] playerTeam;
    public BattleStageData[] stages;   // �� ScriptableObject �z��

    private int currentStageIndex = 0;
    private int carriedCourage = 0; // �����p���p

    void Start()
    {
        StartStage(0, carriedCourage);
    }

    public void StartStage(int index, int initialCourage)
    {
        currentStageIndex = index;
        var stage = stages[index];

        // �X�e�[�W����\���iUI�Ŕw�i��BGM�؂�ւ����\�j
        Debug.Log($"�X�e�[�W�J�n: {stage.stageName}");

        StartCoroutine(battleManager.SetupBattle(playerTeam, stage, OnBattleEnd, initialCourage));

        if (stage.bgm != null)
        {
            AudioSource audio = GetComponent<AudioSource>();
            if (audio != null)
            {
                audio.clip = stage.bgm;
                audio.Play();
            }
        }
    }

    private void OnBattleEnd(bool playerWon, int remainingCourage)
    {
        carriedCourage = remainingCourage; // �E�C�Q�[�W��ۑ�

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
        Debug.Log("�Q�[���N���A�I �� �z�[���ɖ߂�");
        yield return new WaitForSeconds(3f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
    }
}

