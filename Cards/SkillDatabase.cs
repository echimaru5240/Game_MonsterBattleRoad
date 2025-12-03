using UnityEngine;
using System.Collections.Generic;

public struct SkillData
{
	public SkillID skillID;
	public string skillName;
	public SkillAction action;
	public SkillCategory category;
	public TargetType targetType;
	public ElementType elementType;
	public int power;
	public float accuracy;
	public float criticalRate;
	public StatusAilmentType statusAilmentType;
	public float ailmentChance;
	public StatusBuffType statusBuffType;
	public int buffAmountStage;
	public int buffDurationTurns;

	public SkillData(
		SkillID skillID,
		string skillName,
		SkillAction action,
		SkillCategory category,
		TargetType targetType,
		ElementType elementType,
		/** パラメータ */
		int power,
		float accuracy,
		float criticalRate,
		/** 状態異常 */
		StatusAilmentType statusAilmentType,
		float ailmentChance,
		/** ステータス補助 */
		StatusBuffType statusBuffType,		/** 攻撃/防御/素早さなど */
		int buffAmountStage,		/** 段階や倍率の指標。+1で小アップ、+2で大アップなど好きに解釈してOK */
		int buffDurationTurns		/** 効果ターン数。0ならこのターンのみ、1ならこのターン+次ターンなど自由に設計 */
	)
	{
		this.skillID = skillID;
		this.skillName = skillName;
		this.action = action;
		this.category = category;
		this.targetType = targetType;
		this.elementType = elementType;
		this.power = power;
		this.accuracy = accuracy;
		this.criticalRate = criticalRate;
		this.statusAilmentType = statusAilmentType;
		this.ailmentChance = ailmentChance;
		this.statusBuffType = statusBuffType;
		this.buffAmountStage = buffAmountStage;
		this.buffDurationTurns = buffDurationTurns;
	}
}

public static class SkillDatabase
{
	private static readonly Dictionary<SkillID, SkillData> skillDict;

	static SkillDatabase()
	{
		skillDict = new Dictionary<SkillID, SkillData>();
		foreach (var s in Skills)
		{
			skillDict[s.skillID] = s;
		}
	}

	public static SkillData Get(SkillID id)
	{
		if (skillDict.TryGetValue(id, out var data))
		{
			return data;
		}
		Debug.LogError($"SkillDatabase: SkillID not found: {id}");
		return default;
	}

	public static readonly SkillData[] Skills = new SkillData[]
	{
        new SkillData(SkillID.SKILL_ID_SLIME_STRIKE, "スラ・ストライク", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.5f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_TOUSAND_NEEDLE, "ニードルダンス", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_ALL, ElementType.NONE, 80, 0.9f, 0f, StatusAilmentType.PARALYSIS, 0.1f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_HEAL, "ヒール", SkillAction.HEAL, SkillCategory.MAGICAL, TargetType.PLAYER_SINGLE, ElementType.NONE, 200, 1f, 0f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_TRAP_BITE, "トラップバイト", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_SPIKE_ROLLING, "ローリングスパイク", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.5f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_CACTUS_LARIAT, "サボテンラリアット", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_NEEDLE_SHOT_1000, "針千本ショット", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_ENERGY_CACTUS, "サボテンジュース", SkillAction.HEAL, SkillCategory.SPECIAL, TargetType.PLAYER_SINGLE, ElementType.NONE, 200, 1f, 0f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_POISON_NEEDLE, "ポイズンニードル", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.1f, StatusAilmentType.POISON, 0.2f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_TREASURE_CRUNCH, "トレジャークランチ", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_FIRE_BREATH, "ファイアブレス", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.FIRE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_ICE_BREATH, "アイスブレス", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.ICE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_THUNDER_BREATH, "サンダーブレス", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_MUSH_CRUSHER, "マッシュクラッシャー", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_FIRE_BURST, "ファイアバースト", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.FIRE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_AQUA_SLASH, "アクアスラッシュ", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.WATER, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_FROST_BITE, "フロストバイト", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_SINGLE, ElementType.ICE, 200, 0.5f, 0.8f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_SLIME_WAVE, "スライムウェーブ", SkillAction.ATTACK, SkillCategory.PHYSICAL, TargetType.ENEMY_ALL, ElementType.NONE, 80, 0.9f, 0f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
        new SkillData(SkillID.SKILL_ID_PSYCO_LASER, "サイコレーザー", SkillAction.ATTACK, SkillCategory.SPECIAL, TargetType.ENEMY_SINGLE, ElementType.NONE, 200, 0.9f, 0.1f, StatusAilmentType.NONE, 0f, StatusBuffType.NONE, 0, 0),
    };
}
