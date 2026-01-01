using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景等级与出怪相关对应信息
/// </summary>
public class SceneLevelMonsterInfo
{
    // 场景ID
    public int sceneID;
    // 难度等级
    public int sceneLevel;
    // 基础(第一波)怪物数量
    public int basicMonsterNum;
    // 怪物数量递增系数
    public float addMonsterRatio;
    // 怪物波数
    public int waveNum;
    // 怪物点数量
    public int monsterPointNum;
    // 阶段一怪物ID字符串
    public string monsterStage1;
    // 阶段二怪物ID字符串
    public string monsterStage2;
    // 关卡末BossID 0代表非Boss关卡
    public int bossID;
}
