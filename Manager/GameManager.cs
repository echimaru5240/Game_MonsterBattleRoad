using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    public void OnClickStartGame()
    {
        SceneManager.LoadScene("BattleScene");
    }
}
