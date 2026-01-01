using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色数据
/// </summary>
public class HeroInfo
{
    // ID
    public int id;
    //模型预制体路径
    public string res;
    //攻击力
    public int atk;
    //爆头伤害系数
    public float headAtkRatio;
    //攻击检测类型
    public int atkType;
    //攻击速度(每秒几次)
    public float shootSpeed;
    //弹夹容量
    public int bulletCount;
    //腰射子弹散布
    public float bulletOffset;
    //开镜子弹散布
    public float bulletAimOffset;
    //水平后座力
    public float xRecoil;
    //垂直后座力
    public float yRecoil;
    //玩家移动速度系数
    public float playerMoveSpeed;
    //攻击距离
    public int atkDistance;
    //解锁金额
    public int lockMoney;
    //枪声音效资源路径
    public string soundPath;
    //武器提示信息
    public string tips;

    // IK控制相关配置信息
    public float rightOffset;
    public float rightAimOffset;
    public float upOffset;
    public float aimDistance;
    public float leftHandOffset;
    public float leftHandAimOffset;

    // 开火动画播放速度
    public float fireSpeed;
}
