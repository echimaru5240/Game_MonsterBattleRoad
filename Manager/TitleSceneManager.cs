using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleSceneManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;

    [Header("Optional UI")]
    [SerializeField] private TextMeshProUGUI continueLabel; // つづきのボタンに「(データなし)」とか付けたい時

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "HomeScene"; // ←あなたのメインシーン名

    void Start()
    {
        // Continue が押せるか更新
        RefreshButtons();

        newGameButton.onClick.RemoveAllListeners();
        newGameButton.onClick.AddListener(OnNewGame);

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinue);
    }

    private void RefreshButtons()
    {
        bool hasSave = GameContext.Instance != null && GameContext.Instance.HasSave();

        if (continueButton != null) continueButton.interactable = hasSave;

        if (continueLabel != null)
            continueLabel.text = hasSave ? "つづきから" : "つづきから（データなし）";
    }

    private void OnNewGame()
    {
        var ctx = GameContext.Instance;
        if (ctx == null) return;

        ctx.DeleteSave();            // 念のため
        ctx.StartNewGameAndSave();   // 初期化→セーブ

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnContinue()
    {
        var ctx = GameContext.Instance;
        if (ctx == null) return;

        if (!ctx.ContinueGame())
        {
            // セーブが壊れてた等：続き不可ならボタン更新して終わる
            RefreshButtons();
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
