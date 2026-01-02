using UnityEngine;
using System.Collections;

public class TimingTapManager : MonoBehaviour
{
    public static TimingTapManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private BattleUITimingTapView timingTapPrefab;

    void Awake()
    {
        Instance = this;
    }

    public IEnumerator PlayTimingTap(System.Action<TimingResult> onDone, Vector2? screenPos = null)
    {
        if (overlayCanvas == null || timingTapPrefab == null)
        {
            // UI‚ª–³‚¢‚È‚çƒm[ƒ}ƒ‹ˆµ‚¢
            onDone?.Invoke(new TimingResult { rank = TimingRank.Good, multiplier = 1.0f });
            yield break;
        }

        var view = Instantiate(timingTapPrefab, overlayCanvas.transform);

        if (screenPos.HasValue)
        {
            var rt = (RectTransform)view.transform;
            rt.position = screenPos.Value;
        }

        bool finished = false;
        TimingResult result = default;

        view.Play(r =>
        {
            result = r;
            finished = true;
            onDone?.Invoke(r);
        });

        // Š®—¹‘Ò‚¿
        while (!finished)
            yield return null;
    }
}
