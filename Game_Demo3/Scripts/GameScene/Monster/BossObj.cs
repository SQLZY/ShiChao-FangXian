using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 怪物ID对应信息
/// </summary>
public static class BossAtkDic
{
    //招式种类数量
    public static Dictionary<int, int> bossIDToAtkMaxNum = new Dictionary<int, int>()
    {
        {13,3},
        {14,6},
        {15,2},
        {16,4},
        {17,6},
    };

    //攻击动画速度
    public static Dictionary<int, float> bossIDToAtkSpeed = new Dictionary<int, float>()
    {
        {13,1.10f},
        {14,1.30f},
        {15,0.24f},
        {16,0.36f},
        {17,0.48f},
    };
}

public class BossObj : MonsterObj
{
    //上次攻击命中判断帧时间
    private float frontAtkEventTime;
    //场上玩家脚本
    private PlayerObj playerObj;

    protected override void Awake()
    {
        base.Awake();
        //Boss攻击玩家时只少量减速
        atkingMoveRatio = 0.68f;
        //Boss受到攻击时只少量减速
        beAtkedMoveRatio = 0.88f;
        //Boss受到攻击时更快恢复非受击状态
        bufferAtkedTime *= 0.38f;
        //设置攻击动画播放速度
        animator.SetFloat("AtkSpeed", BossAtkDic.bossIDToAtkSpeed[ID]);
        //关联场上玩家脚本
        playerObj = GameDataMgr.Instance.nowPlayerObj;
    }

    protected override void UpdateAtkState()
    {
        //已经在攻击 不再进行攻击逻辑判断
        if (isAtking) return;

        if (isTargetPlayer)
        {
            if (Vector3.Distance(this.transform.position, playerTarget.position) < monsterInfo.atkRange
                && Time.time - frontAtkTime > monsterInfo.atkCd)
            {
                frontAtkTime = Time.time;
                animator.SetLayerWeight(1, 1);
                animator.SetInteger("Atk", Random.Range(1, BossAtkDic.bossIDToAtkMaxNum[monsterInfo.id] + 1));
            }
        }
        else
        {
            if (Vector3.Distance(this.transform.position, MainTowerObj.Instance.transform.position) < monsterInfo.atkRange
                && Time.time - frontAtkTime > monsterInfo.atkCd)
            {
                frontAtkTime = Time.time;
                animator.SetLayerWeight(1, 1);
                animator.SetInteger("Atk", Random.Range(1, BossAtkDic.bossIDToAtkMaxNum[monsterInfo.id] + 1));
            }
        }
    }

    public override void AtkEvent()
    {
        // 播放攻击音效
        GameDataMgr.Instance.PlaySound("Music/Gun/AtkEvent", 0.7f, transform);
        // 更新攻击帧时间
        frontAtkEventTime = Time.time;

        // 攻击保护区域忽略夹角
        if ((Vector3.Dot(this.transform.forward, MainTowerObj.Instance.transform.position - this.transform.position) > 0
          && Vector3.Distance(this.transform.position, MainTowerObj.Instance.transform.position) <= monsterInfo.atkRange)
          || Vector3.Distance(this.transform.position, MainTowerObj.Instance.transform.position) <= monsterInfo.atkRange * 0.25f)
        {
            //保护区受到攻击逻辑
            MainTowerObj.Instance.Wound(monsterInfo.atk);
            //播放音效
            GameDataMgr.Instance.PlaySound("Music/Eat", 0.5f);
        }
    }

    public override void EndAtkEvent()
    {
        base.EndAtkEvent();
        animator.SetInteger("Atk", 0);
    }

    private void OnTriggerStay(Collider other)
    {
        //攻击判断帧0.4s内命中有效
        if (other.CompareTag("Player") && isAtking && Time.time - frontAtkEventTime < 0.4f)
        {
            //玩家处于受击缓冲状态 不再重复受伤
            if (playerObj.isBeAtked) return;

            //玩家翻滚/腾空状态 减少伤害判定区域(不再完全依赖碰撞检测)
            if (playerObj.isRolling || playerObj.isJumping)
            {
                //玩家不在前方 不受伤
                if (Vector3.Dot(transform.forward, playerTarget.position - transform.position) < 0) return;
                //玩家超过距离 不受伤
                if (Vector3.Distance(transform.position, playerTarget.position) > monsterInfo.atkRange) return;
            }

            //玩家受到攻击逻辑
            playerObj.Wound(monsterInfo.atk);
            //播放音效
            GameDataMgr.Instance.PlaySound("Music/Gun/PlayerWound", 0.6f);
        }
    }
}
