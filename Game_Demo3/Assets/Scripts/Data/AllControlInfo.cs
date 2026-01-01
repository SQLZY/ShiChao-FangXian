using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制信息相关类
/// </summary>
public class AllControlInfo
{
    public GunControlInfo gunControlInfo;
    public SkinAwardControlInfo skinAwardControlInfo;
    public PlayerControlInfo playerControlInfo;
    public MonsterControlInfo monsterControlInfo;
}

/// <summary>
/// 枪械控制相关
/// </summary>
public class GunControlInfo
{
    //全速移动 散射扩大系数
    public float maxSpeedOffsetRatio;
    //跳跃状态 散射扩大系数
    public float jumpOffsetRatio;
    //下蹲状态 散射缩小系数
    public float crouchOffsetRatio;
    //开镜状态 后座力缩小系数
    public float aimRecoilRatio;
    //下蹲状态 后座力缩小系数
    public float crounchRecoilRatio;
}

/// <summary>
/// 皮肤增益控制类
/// 单位为每消费$3000增益
/// </summary>
public class SkinAwardControlInfo
{
    //玩家生命值
    public int playerHp;
    //基地生命值
    public int mainTowerHp;
    //基础关卡金钱
    public int basicMoney;
    //玩家伤害系数
    public float playerAtkRatio;
    //炮塔伤害系数
    public float towerAtkRatio;
}

/// <summary>
/// 玩家控制类
/// </summary>
public class PlayerControlInfo
{
    //玩家基础移速单位
    public float basicMoveSpeed;
    //最小翻滚时间间隔
    public float minRollTime;
    //翻滚持续时间
    public float rollingTime;
    //站立最大翻滚速度
    public float normalRollSpeed;
    //蹲姿最大翻滚速度
    public float crouchRollSpeed;
    //处于开镜瞄准状态移速减速系数(独立控制 不受姿态等因素影响)
    public float aimingSpeed;
    //非疾跑状态减速系数
    public float walkSpeedRatio;
    //处于非全速状态(蹲下/开火/换弹/后退)减速系数
    public float nonfullSpeedRatio;
    //期望最大跳跃高度
    public float jumpHeight;
    //重力大小
    public float gravity;
    //跳跃缓冲机制时间 当处于无法跳跃状态时按下跳跃键 若缓冲时间内恢复可跳跃状态 立刻进入跳跃逻辑
    public float bufferJumpTime;
    //土狼时间机制 当离开地面平台的短暂时间内 仍然可以起跳成功
    public float coyoteJumpTime;
    //开火特效未命中怪物最大显示距离
    public float maxFireEffDistance;
    //受伤状态恢复时间
    public float atkedTime;
}

/// <summary>
/// 怪物控制类
/// </summary>
public class MonsterControlInfo
{
    //基础攻击动画速度系数
    public float atkSpeedRatio;
    //怪物基础 移动速度/旋转速度/加速度
    public float monsterBasicSpeed;
    //攻击玩家时减速系数
    public float atkingMoveRatio;
    //受到攻击时减速系数
    public float beAtkedMoveRatio;
    //攻击有效角度范围
    public float atkAngleRange;
    //受击状态恢复缓冲延迟
    public float bufferAtkedTime;
}