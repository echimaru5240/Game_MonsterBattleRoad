using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class MonsterSortButtonView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;               // 全体（Panel）
    [SerializeField] private Button closeButton;            // ×ボタン（任意）
    [SerializeField] private Button backgroundCloseButton;  // 背景タップで閉じる（任意）

    [Header("List")]
    [SerializeField] private Transform contentParent;       // VerticalLayoutGroup の中身
    [SerializeField] private MonsterSortButtonItemView itemPrefab;

    // 表示名をいじりたい時用
    private static readonly (MonsterSortType type, string label)[] Items =
    {
        (MonsterSortType.ID,    "ID順"),
        (MonsterSortType.Name,  "名前順"),
        (MonsterSortType.Level, "レベル順"),
        (MonsterSortType.HP,    "HP順"),
        (MonsterSortType.ATK,   "攻撃力順"),
        (MonsterSortType.MGC,   "魔力順"),
        (MonsterSortType.DEF,   "防御力順"),
        (MonsterSortType.AGI,   "速度順"),
    };

    private Action<MonsterSortType> onSelected;
    private bool isAscending;


    public bool IsVisible
    {
        get
        {
            if (root != null) return root.activeSelf;
            return gameObject.activeSelf;
        }
    }

    void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
        if (backgroundCloseButton != null)
        {
            backgroundCloseButton.onClick.RemoveAllListeners();
            backgroundCloseButton.onClick.AddListener(Hide);
        }

        Hide();
    }

    public void Show(MonsterSortType current, bool isAscending, Action<MonsterSortType> onSelected)
    {
        this.onSelected = onSelected;
        this.isAscending = isAscending;

        if (root != null) root.SetActive(true);
        gameObject.SetActive(true);

        Rebuild(current);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }


    private void Rebuild(MonsterSortType current)
    {
        if (contentParent == null || itemPrefab == null) return;

        // 既存を全削除
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        foreach (var it in Items)
        {
            var item = Instantiate(itemPrefab, contentParent);
            bool selected = (it.type == current);

            item.Setup(it.type, it.label, selected, isAscending, type =>
            {
                onSelected?.Invoke(type);
                Hide(); // 選んだら閉じる
            });
        }
    }
}
