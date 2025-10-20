using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EffectData
{
    public AttackEffectType type;
    public GameObject prefab;
}

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("攻撃エフェクト一覧")]
    public List<EffectData> effectList = new();

    private Dictionary<AttackEffectType, GameObject> effectDict = new();

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
                    effectDict[e.type] = e.prefab;
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
    public GameObject PlayEffect(AttackEffectType type, Vector3 position, Quaternion? rotation = null, float lifeTime = 2.5f)
    {
        if (!effectDict.ContainsKey(type))
        {
            Debug.LogWarning($"EffectManager: 指定タイプ {type} のエフェクトが見つかりません。");
            return null;
        }

        var prefab = effectDict[type];
        Quaternion rot = rotation ?? Quaternion.Euler(90f, 0, 0);
        GameObject effect = Instantiate(prefab, position, rot);
        Destroy(effect, lifeTime);
        return effect;
    }
}
