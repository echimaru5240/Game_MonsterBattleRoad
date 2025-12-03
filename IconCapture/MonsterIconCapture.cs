using UnityEngine;
using System.IO;

public class MonsterIconCapturer : MonoBehaviour
{
    [Header("Scene References")]
    public Camera captureCamera;
    public Transform monsterAnchor;

    [Header("Capture Settings")]
    public int iconSize = 512;
    public string outputFolder = "MonsterIcons"; // Assets/MonsterIcons に保存される


    public float extraXawOffset = 0f;
    public float extraYawOffset = 0f;
    public float rotZ = 0f;

    // アンカー内を掃除
    public void ClearAnchor()
    {
        for (int i = monsterAnchor.childCount - 1; i >= 0; i--)
        {
            #if UNITY_EDITOR
            DestroyImmediate(monsterAnchor.GetChild(i).gameObject);
            #else
            Destroy(monsterAnchor.GetChild(i).gameObject);
            #endif
        }
    }

    // 1体分アイコン生成（エディタから呼び出す想定）
    public string CaptureIcon(GameObject monsterPrefab, string fileName)
    {
        if (captureCamera == null || monsterAnchor == null || monsterPrefab == null)
        {
            Debug.LogError("MonsterIconCapturer: 設定が不足しています。");
            return null;
        }

        ClearAnchor();

        // モンスター生成
        var instance = Instantiate(monsterPrefab, monsterAnchor);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // カメラ方向へ向ける（必要なら調整）
        var camPos = captureCamera.transform.position;
        var dir = (monsterAnchor.position - camPos);
        Debug.Log($"dir: {dir}");
        // dir.x = rotX;
        // if (dir != Vector3.zero)
        // {
        //     instance.transform.rotation = Quaternion.LookRotation(dir);
        // }
        // ==== ここから向き調整 ====

        // モデルの「前」が +Z を向いている前提
        // カメラの forward とは逆向きにさせると、必ずカメラの方を向く
        Quaternion lookRot = Quaternion.LookRotation(-captureCamera.transform.forward, Vector3.up);
        instance.transform.rotation = lookRot;

        // 必要なら微調整（モデルによって少し横を向かせたいときなど）
        instance.transform.rotation *= Quaternion.Euler(extraXawOffset, extraYawOffset, 0f);

        // ==== ここまで向き調整 ====

        // RenderTexture用意
        var rt = new RenderTexture(iconSize, iconSize, 24);
        captureCamera.targetTexture = rt;

        var tex = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false);

        // 描画して読み取り
        captureCamera.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, iconSize, iconSize), 0, 0);
        tex.Apply();

        // 保存先パス
        string folderPath = Path.Combine(Application.dataPath, outputFolder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, fileName + ".png");
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);

        // 後始末
        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(tex);
        ClearAnchor();

        // Assets からの相対パスを返す
        string assetPath = "Assets/" + outputFolder + "/" + fileName + ".png";
        Debug.Log("Saved icon: " + assetPath);
        return assetPath;
    }
}
