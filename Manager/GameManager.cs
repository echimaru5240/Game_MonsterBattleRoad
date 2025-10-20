using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    void Start()
    {
        // í“¬BGMÄ¶
        AudioManager.Instance.PlayHomeBGM();
    }

    public void OnClickStartGame()
    {
        AudioManager.Instance.PlayButtonSE();
        SceneManager.LoadScene("BattleScene");
    }
}
