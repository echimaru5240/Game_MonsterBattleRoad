using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MonsterSortButtonItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private GameObject checkMark; // 選択中表示（任意）
    [SerializeField] private GameObject ascendingIconUp;
    [SerializeField] private GameObject ascendingIconDown;

    private MonsterSortType sortType;
    private Action<MonsterSortType> onClicked;

    public void Setup(
        MonsterSortType type,
        string label,
        bool isSelected,
        bool isAscending,
        Action<MonsterSortType> onClicked
    )
    {
        sortType = type;
        this.onClicked = onClicked;

        if (labelText != null) labelText.text = label;
        if (checkMark != null) checkMark.SetActive(isSelected);

        // ↑↓ は「選択中の行だけ」表示
        if (ascendingIconUp != null)
            ascendingIconUp.SetActive(isSelected && isAscending);

        if (ascendingIconDown != null)
            ascendingIconDown.SetActive(isSelected && !isAscending);


        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClicked?.Invoke(sortType));
        }
    }
}
