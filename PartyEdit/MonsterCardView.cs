using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MonsterCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI mgcText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI agiText;
    [SerializeField] private Button button;

    private OwnedMonster ownedData;
    private Action<OwnedMonster> onClicked;

    public void Setup(OwnedMonster data, Action<OwnedMonster> onClicked)
    {
        Debug.Log("Card SetUp");
        ownedData = data;
        this.onClicked = onClicked;

        if (data != null && data.master != null)
        {
            Debug.Log("Card DataSet");
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = data.master.monsterFarSprite;
            nameText.text    = data.master.Name;
            hpText.text      = data.master.hp.ToString();
            atkText.text     = data.master.atk.ToString();
            mgcText.text     = data.master.mgc.ToString();
            defText.text     = data.master.def.ToString();
            agiText.text     = data.master.agi.ToString();
        }
        else
        {
            Debug.Log("Card Data Null");
            // 空スロット表示など、必要ならここで
            iconImage.gameObject.SetActive(false);
            nameText.text    = "－－－";
            hpText.text = atkText.text = mgcText.text = defText.text = agiText.text = "-";
        }

        if (button != null)
        {
            Debug.Log("Button != Null");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        Debug.Log("OnClick Card");
        onClicked?.Invoke(ownedData);
    }
}
