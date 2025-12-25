using UnityEngine;

public static class BoundsUtil
{
    // 見た目の全体Bounds（ワールド空間）を取得
    public static bool TryGetVisualBounds(GameObject root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds; // Renderer.bounds はワールドAABB
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    // 「だいたいの半径」（カメラ距離計算に便利）
    public static float GetRadius(Bounds b)
    {
        // AABBの対角線の半分＝球で包む時の半径の近似
        return b.extents.magnitude;
    }
}
