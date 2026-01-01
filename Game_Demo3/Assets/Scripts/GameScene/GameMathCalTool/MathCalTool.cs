using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MathCalTool
{
    private static MathCalTool instance = new MathCalTool();
    public static MathCalTool Instance => instance;
    private MathCalTool()
    {

    }

    // 枪械类型武器的最大最小散射
    private float minGunOffset;
    private float maxGunOffset;

    // 计算枪械类型武器最大最小子弹散射
    private void CalMinAndMaxOffset()
    {
        //获取全部枪械类型武器列表
        List<HeroInfo> gunList = new List<HeroInfo>();
        foreach (var obj in GameDataMgr.Instance.HeroList)
        {
            if (obj.atkType == 2)
            {
                gunList.Add(obj);
            }
        }
        //初始化时计算全部枪械武器的最大最小散射并记录
        minGunOffset = CalMin<HeroInfo>("bulletAimOffset", gunList);
        maxGunOffset = CalMax<HeroInfo>("bulletOffset", gunList);
        minGunOffset = CalOffsetValue(minGunOffset, 0, false, true);
        maxGunOffset = CalOffsetValue(maxGunOffset, 1, true, false);
    }

    /// <summary>
    /// 平均数计算器
    /// </summary>
    /// <param name="name">需要计算的变量名</param>
    /// <param name="objList">装载类对象的List列表</param>
    /// <returns>List列表某个字段的平均数</returns>
    public float CalAvg<T>(string name, List<T> objList) where T : class
    {
        Type type = typeof(T);
        FieldInfo fieldInfo = type.GetField(name);
        float count = 0;
        foreach (T obj in objList)
        {
            count += Convert.ToSingle(fieldInfo.GetValue(obj));
        }
        return count / objList.Count;
    }


    /// <summary>
    /// 最大值计算器
    /// </summary>
    /// <param name="name">需要计算的变量名</param>
    /// <param name="objList">装载类对象的List列表</param>
    /// <returns>List列表某个字段的最大值</returns>
    public float CalMax<T>(string name, List<T> objList) where T : class
    {
        Type type = typeof(T);
        FieldInfo fieldInfo = type.GetField(name);
        float count = Convert.ToSingle(fieldInfo.GetValue(objList[0]));
        foreach (T obj in objList)
        {
            if (Convert.ToSingle(fieldInfo.GetValue(obj)) > count)
            {
                count = Convert.ToSingle(fieldInfo.GetValue(obj));
            }
        }
        return count;
    }


    /// <summary>
    /// 最小值计算器
    /// </summary>
    /// <param name="name">需要计算的变量名</param>
    /// <param name="objList">装载类对象的List列表</param>
    /// <returns>List列表某个字段的最小值</returns>
    public float CalMin<T>(string name, List<T> objList) where T : class
    {
        Type type = typeof(T);
        FieldInfo fieldInfo = type.GetField(name);
        float count = Convert.ToSingle(fieldInfo.GetValue(objList[0]));
        foreach (T obj in objList)
        {
            if (Convert.ToSingle(fieldInfo.GetValue(obj)) < count)
            {
                count = Convert.ToSingle(fieldInfo.GetValue(obj));
            }
        }
        return count;
    }


    /// <summary>
    /// 子弹散射大小计算器
    /// </summary>
    /// <param name="basicOffset">基础散射大小</param>
    /// <param name="moveSpeed">移动速度(0-1)</param>
    /// <param name="isJump">是否跳跃中</param>
    /// <param name="isCrouch">是否下蹲中</param>
    /// <returns></returns>
    public float CalOffsetValue(float basicOffset, float moveSpeed, bool isJump, bool isCrouch)
    {
        //根据移速计算散射大小 原则:当前速度占最大移速的百分比 计算散射增大比例
        //例如 最大移速散射系数为1.8f 那么 半速移动散射系数应为1.4f 静止状态散射系数应为1f
        basicOffset *= (moveSpeed / 1) * (GameDataMgr.Instance.AllControlInfo.gunControlInfo.maxSpeedOffsetRatio - 1) + 1;
        //根据跳跃状态计算散射大小
        basicOffset *= isJump ? GameDataMgr.Instance.AllControlInfo.gunControlInfo.jumpOffsetRatio : 1;
        //根据下蹲状态计算散射大小
        basicOffset *= isCrouch ? GameDataMgr.Instance.AllControlInfo.gunControlInfo.crouchOffsetRatio : 1;

        return basicOffset;
    }

    /// <summary>
    /// 子弹散射射线计算器
    /// </summary>
    /// <param name="offset">散射大小</param>
    /// <returns>随机偏移后的子弹射线</returns>
    public Ray CalOffsetFireRay(float offset)
    {
        // 自定义简单规则 后续有复杂需求可优化 避免弹道过于随机
        // 1.有20%概率直接发射精准弹道(散射角度在20%散射区域内)
        if (UnityEngine.Random.Range(0, 100) < 20) offset *= 0.2f;
        // 2.另外有70%概率决定 散射角度在50%散射区域内
        else if (UnityEngine.Random.Range(0, 100) < 70) offset *= 0.5f;

        //基于屏幕中心点 计算一条随机散射后的射线
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        ray.direction = Quaternion.AngleAxis(UnityEngine.Random.Range(-offset, offset), Vector3.up) * ray.direction;
        ray.direction = Quaternion.AngleAxis(UnityEngine.Random.Range(-offset, offset), Vector3.right) * ray.direction;
        //根据相机与玩家的距离前移射线起点 避免命中身后物体
        ray.origin += ray.direction * (Camera.main.GetComponent<ThirdPersonCamera>().distance * 0.88f);
        return ray;
    }

    /// <summary>
    /// 后座力大小计算器
    /// </summary>
    /// <param name="basicRecoil">基础后座力</param>
    /// <param name="isAiming">是否开镜</param>
    /// <param name="isCrouch">是否下蹲</param>
    /// <returns></returns>
    public float CalFireRecoil(float basicRecoil, bool isAiming, bool isCrouch)
    {
        //根据开镜状态计算后座力系数
        basicRecoil *= isAiming ? GameDataMgr.Instance.AllControlInfo.gunControlInfo.aimRecoilRatio : 1;
        //根据下蹲状态计算后座力系数
        basicRecoil *= isCrouch ? GameDataMgr.Instance.AllControlInfo.gunControlInfo.crounchRecoilRatio : 1;
        return basicRecoil;
    }

    /// <summary>
    /// 武器散射大小转UI散射大小
    /// </summary>
    /// <param name="offset">武器散射大小</param>
    /// <returns></returns>
    public float GunOffsetToUIOffset(float offset)
    {
        //如果未初始化最大最小散射值 进行初始化计算
        if (minGunOffset == 0 || maxGunOffset == 0) CalMinAndMaxOffset();
        offset = Mathf.Clamp(offset, minGunOffset, maxGunOffset);
        //计算当前散射大小占最大散射大小的比例
        float ratio = (offset - minGunOffset) / (maxGunOffset - minGunOffset);
        return ratio * 100f;
    }
}
