using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PartyEditPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject popupObj;
    [SerializeField] private TextMeshProUGUI popupText;

    public void Setup()
    {
        popupObj.SetActive(false);
    }

    public IEnumerator ShowErrorPopup()
    {
        popupText.text = "パーティメンバーがいません";
        popupObj.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        popupObj.gameObject.SetActive(false);
    }

    public void SetReplacePopup(bool visible)
    {
        popupText.text = "入れ替えるモンスターを選択してください";
        popupObj.gameObject.SetActive(visible);
    }
}
