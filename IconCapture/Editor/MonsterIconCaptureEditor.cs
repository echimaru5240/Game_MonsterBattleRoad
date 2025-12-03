#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonsterIconCapturer))]
public class MonsterIconCapturerEditor : Editor
{
    private MonsterIconDatabase database;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        // MonsterIconDatabase をインスペクタで選べるように
        database = (MonsterIconDatabase)EditorGUILayout.ObjectField(
            "Monster Database",
            database,
            typeof(MonsterIconDatabase),
            false);

        if (GUILayout.Button("選択中のMonsterDataのアイコンを撮影（1体）"))
        {
            CaptureSelectedMonster();
        }

        if (GUILayout.Button("Database内の全モンスターのアイコンを一括撮影"))
        {
            CaptureAllFromDatabase();
        }
    }

    private void CaptureSelectedMonster()
    {
        var capturer = (MonsterIconCapturer)target;
        var obj = Selection.activeObject as MonsterIconData;
        if (obj == null)
        {
            Debug.LogWarning("MonsterIconData を選択してから実行してください。");
            return;
        }

        CaptureAndAssign(capturer, obj);
    }

    private void CaptureAllFromDatabase()
    {
        var capturer = (MonsterIconCapturer)target;
        if (database == null)
        {
            Debug.LogWarning("MonsterIconDatabase を指定してください。");
            return;
        }

        foreach (var monster in database.monsters)
        {
            if (monster == null) continue;
            CaptureAndAssign(capturer, monster);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("全モンスターのアイコン生成完了！");
    }

    private void CaptureAndAssign(MonsterIconCapturer capturer, MonsterIconData data)
    {
        if (data.monsterPrefab == null)
        {
            Debug.LogWarning($"{data.name} に monsterPrefab が設定されていません。");
            return;
        }

        // ファイル名はIDか名前
        string safeName = string.IsNullOrEmpty(data.id) ? data.name : data.id;
        safeName = MakeSafeFileName(safeName);

        string assetPath = capturer.CaptureIcon(data.monsterPrefab, safeName);
        if (string.IsNullOrEmpty(assetPath)) return;

        // インポート設定：Spriteにする
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }

        // Sprite を読み込んで MonsterIconData にセット
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        data.iconSprite = sprite;
        EditorUtility.SetDirty(data);

        Debug.Log($"Set iconSprite for {data.displayName}");
    }

    private string MakeSafeFileName(string name)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
#endif
