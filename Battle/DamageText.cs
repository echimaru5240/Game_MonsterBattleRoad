using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float moveUpSpeed = 30f;
    public float fadeDuration = 1f;

    private float timer = 0f;
    private Color startColor;

    void Start()
    {
        startColor = textMesh.color;
    }

    void Update()
    {
        // ã‚ÉˆÚ“®
        transform.Translate(Vector3.up * moveUpSpeed * Time.deltaTime);

        // ™X‚É“§–¾‚É
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}
