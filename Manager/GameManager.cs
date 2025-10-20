using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    void Start()
    {
        // �퓬BGM�Đ�
        AudioManager.Instance.PlayHomeBGM();
    }

    public void OnClickStartGame()
    {
        AudioManager.Instance.PlayButtonSE();
        SceneManager.LoadScene("BattleScene");
    }
}
