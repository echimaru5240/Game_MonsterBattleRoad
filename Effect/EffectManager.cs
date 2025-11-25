using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EffectData
{
    public EffectID effectID;
    public SoundEffectID seID;
    public GameObject prefab;
}

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("攻撃エフェクト一覧")]
    public List<EffectData> effectList = new();

    private Dictionary<EffectID, EffectData> effectDict = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 登録済みリストをDictionary化
            foreach (var e in effectList)
            {
                if (e != null && e.prefab != null)
                    effectDict[e.effectID] = e;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 指定タイプのエフェクトを生成
    /// </summary>
    public GameObject PlayEffect(EffectID effectID, Vector3 position, Quaternion? rotation = null, float lifeTime = 2.5f)
    {
        if (!effectDict.TryGetValue(effectID, out var data))
        {
            Debug.LogWarning($"EffectManager: 指定タイプ {effectID} のエフェクトが見つかりません。");
            return null;
        }

        Quaternion rot = rotation ?? Quaternion.Euler(90f, 0, 0);
        GameObject effect = Instantiate(data.prefab, position, rot);
        Destroy(effect, lifeTime);
        // ? SE 再生（Noneならスキップ）
        if (data.seID != null)
        {
            AudioManager.Instance.PlayActionSE(data.seID);
        }
        return effect;
    }
}
