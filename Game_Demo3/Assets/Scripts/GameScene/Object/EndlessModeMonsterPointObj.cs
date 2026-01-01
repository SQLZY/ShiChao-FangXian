using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无尽模式出怪点
/// </summary>
public class EndlessModeMonsterPointObj : MonsterPointObj
{
    protected override void Start()
    {
        //无尽模式 难度增加

        //缩短生成怪物之间的间隔
        creatOffsetTime *= 0.8f;
        //缩短每波怪物之间的间隔
        delayTime *= 0.8f;

        //准备生成第一波怪物
        Invoke("CreatWave", delayTime);
    }

    protected override void CreatWave()
    {
        //更新波数信息
        ++nowWave;
        //更新波数UI信息
        SceneLevelMgr.Instance.CheckAndUpdateWaveInfo();
        //更新本波怪物数量 无尽模式 第几波就出几只怪(每个出怪点)
        nowMonsterNum = nowWave;
        //第40波出怪数量饱和 缩短波与波间隔
        if (nowWave > 40)
        {
            nowMonsterNum = 40;
            delayTime = 10f;
        }
        //出怪逻辑
        CreatMonster();
    }

    protected override void CreatMonster()
    {
        //游戏状态已结束 不再刷新新怪物
        if (!GameDataMgr.Instance.isGaming) return;
        //通过对象池实例化怪物并计数
        ObjectPoolMgr.Instance.Get((SceneLevelMgr.Instance as EndlessModeSceneMgr).EndlessModeGetRandomMonsterKey(), transform.position, Quaternion.identity);
        --nowMonsterNum;
        //关卡管理器记录怪物数量
        SceneLevelMgr.Instance.ChangeMonsterNum(1);

        //更新下一波怪物
        if (nowMonsterNum <= 0) Invoke("CreatWave", delayTime);
        //更新下一只怪物
        else Invoke("CreatMonster", creatOffsetTime);
    }
}
