using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BattleResultExpBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI requiredExpText;
    [SerializeField] private TextMeshProUGUI levelUpText;

    [Header("Follow")]
    public Transform worldTarget;
    public Vector3 worldOffset = new(0f, 2.2f, 0f);
    public Canvas canvas;
    public Camera worldCamera;

    RectTransform rect;

    void Awake()
    {
        rect = (RectTransform)transform;
        if (levelUpText) levelUpText.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!worldTarget || !canvas || !worldCamera) return;

        Vector3 screen = worldCamera.WorldToScreenPoint(worldTarget.position + worldOffset);

        // ”w–Ê‚È‚ç”ñ•\Ž¦
        if (screen.z < 0f) { if (gameObject.activeSelf) gameObject.SetActive(false); return; }
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        var canvasRect = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screen,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out Vector2 local
        );
        rect.anchoredPosition = local;
    }

    public void SetLevel(int level)
    {
        if (levelText) levelText.text = $"{level}";
    }

    public void SetRange(int requiredExp)
    {
        requiredExp = Mathf.Max(1, requiredExp);
        expSlider.minValue = 0;
        expSlider.maxValue = requiredExp;

        UpdateRequiredText(); // ‰Šú”½‰f
    }

    public void SetValue(int exp)
    {
        expSlider.value = Mathf.Clamp(exp, 0, (int)expSlider.maxValue);
        UpdateRequiredText();
    }

    private void UpdateRequiredText()
    {
        if (!requiredExpText) return;

        int current = Mathf.RoundToInt(expSlider.value);
        int remain  = Mathf.Max(0, (int)expSlider.maxValue - current);

        requiredExpText.text = $"{remain}";
    }

    public Tween AnimateValue(int from, int to, float duration)
    {
        float max = expSlider.maxValue;
        from = Mathf.Clamp(from, 0, (int)max);
        to   = Mathf.Clamp(to,   0, (int)max);

        expSlider.value = from;
        UpdateRequiredText();
        return DOTween.To(() => expSlider.value, v =>
        {
            expSlider.value = v;
            UpdateRequiredText();  // š‚±‚±‚Åí‚É“¯Šú
        }, to, duration).SetEase(Ease.OutQuad);
    }

    public void PlayLevelUp()
    {
        if (!levelUpText) return;

        levelUpText.gameObject.SetActive(true);
        levelUpText.alpha = 1f;
        levelUpText.transform.localScale = Vector3.one * 0.6f;

        Sequence seq = DOTween.Sequence();
        seq.Append(levelUpText.transform.DOScale(1.15f, 0.18f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.35f);
        seq.Append(levelUpText.DOFade(0f, 0.25f));
        seq.OnComplete(() =>
        {
            levelUpText.alpha = 1f;
            levelUpText.gameObject.SetActive(false);
        });
    }
}
