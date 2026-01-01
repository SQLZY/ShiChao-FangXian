using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个出怪点逻辑
/// </summary>
public class MonsterPointObj : MonoBehaviour
{
    //场景关卡等级信息
    SceneLevelMonsterInfo sceneLevelMonsterInfo;
    //单只怪物创建间隔时间
    protected float creatOffsetTime = 2.3f;
    //每波怪物创建间隔时间
    protected float delayTime = 26f;
    //当前波数
    public int nowWave;
    //是否出怪结束
    public bool isMonsterOver;
    //当前阶段
    private int nowStage = 1;
    //本波剩余怪物数量
    protected int nowMonsterNum;

    protected virtual void Start()
    {
        sceneLevelMonsterInfo = GameDataMgr.Instance.nowSelSceneLevel;
        Invoke("CreatWave", delayTime);
    }

    protected virtual void CreatWave()
    {
        //更新波数信息
        ++nowWave;
        //更新波数UI信息
        SceneLevelMgr.Instance.CheckAndUpdateWaveInfo();
        //更新当前阶段
        if (nowWave > sceneLevelMonsterInfo.waveNum / 2f) nowStage = 2;
        //更新本波怪物数量 根据增长系数计算 增加一个极小值补偿 避免因精度问题导致截断退位
        nowMonsterNum = sceneLevelMonsterInfo.basicMonsterNum +
            (int)((sceneLevelMonsterInfo.addMonsterRatio * sceneLevelMonsterInfo.basicMonsterNum * (nowWave - 1)) + 0.001f);
        //出怪逻辑
        CreatMonster();
    }

    //出怪逻辑
    protected virtual void CreatMonster()
    {
        //游戏状态已结束 不再刷新新怪物
        if (!GameDataMgr.Instance.isGaming) return;
        //通过对象池实例化怪物并计数
        ObjectPoolMgr.Instance.Get(SceneLevelMgr.Instance.GetRandomMonsterKey(nowStage), transform.position, Quaternion.identity);
        --nowMonsterNum;
        //关卡管理器记录怪物数量
        SceneLevelMgr.Instance.ChangeMonsterNum(1);

        if (nowMonsterNum <= 0)
        {
            if (nowWave >= sceneLevelMonsterInfo.waveNum)
            {
                isMonsterOver = true;
            }
            else
            {
                //更新下一波怪物
                Invoke("CreatWave", delayTime);
            }
        }
        //更新下一只怪物
        else Invoke("CreatMonster", creatOffsetTime);
    }
}
