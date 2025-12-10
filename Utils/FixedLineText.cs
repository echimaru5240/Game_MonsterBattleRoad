using UnityEngine;
using TMPro;

public class FixedLineText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmpText; // インスペクターで割り当て
    [SerializeField] private int fixedLines = 1;      // 固定したい行数

    void Start()
    {
        if (tmpText != null)
        {
            tmpText.maxVisibleLines = fixedLines;
        }
    }
}