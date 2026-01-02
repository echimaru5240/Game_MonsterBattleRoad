using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    void Start()
    {
        // êÌì¨BGMçƒê∂
        AudioManager.Instance.PlayHomeBGM();
    }

    public void OnClickStartGame()
    {
        AudioManager.Instance.PlayButtonSE();
        SceneManager.LoadScene("BattleScene");
    }

    public void OnClickPartyFormation()
    {
        AudioManager.Instance.PlayButtonSE();
        SceneManager.LoadScene("PartyEditScene");
    }

    public void OnClickTitle()
    {
        AudioManager.Instance.PlayButtonSE();
        SceneManager.LoadScene("TitleScene");
    }
}
