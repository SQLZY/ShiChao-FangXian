using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterInfo
{
    // ID
    public int id;
    // 预制体资源路径
    public string res;
    // 动画状态机资源路径
    public string animator;
    // 攻击力
    public int atk;
    // 攻击距离
    public float atkRange;
    // 移动速度系数
    public float moveSpeedRatio;
    // 生命值
    public int hp;
    // 攻击时间间隔
    public float atkCd;
    // 奖励金币
    public int awardMoney;
    // 怪物图鉴名称
    public string tipsName;
    // 怪物图鉴危害等级
    public int tipsDmgLevel;
    // 是否是Boss
    public bool isBoss;
}
