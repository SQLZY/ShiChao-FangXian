using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 攻击怪物方法接口
/// </summary>
public interface I_TowerAtkType
{
    void AtkEvent(TowerObj obj);
}

public static class TowerAtkTypeInfo
{
    public static Dictionary<int, Type> towerAtkTypeInfoDic = new Dictionary<int, Type>()
    {
        { 1, typeof(TowerAtkType01) },
        { 2, typeof(TowerAtkType02) },
        { 3, typeof(TowerAtkType03) },
        { 4, typeof(TowerAtkType04) },
    };
}

public class TowerObj : MonoBehaviour
{
    //防御塔信息
    public TowerInfo towerInfo;
    //攻击方法接口
    private I_TowerAtkType atkType;
    //当前等级
    private int level;
    //实际攻击力
    public int nowAtk;
    //实际攻击范围
    public float nowAtkRange;
    //攻击接口物体实例
    public GameObject atkObj;

    //上次攻击时间
    private float frontAtkTime;
    //可攻击怪物列表
    private List<MonsterObj> monsters;
    //当前攻击怪物目标
    public MonsterObj nowMonsterTarget;

    // 可旋转头部
    public Transform towerHead;
    // 头部旋转速度
    private float headRoundSpeed = 12f;
    // 开火点/开火特效实例化点
    public Transform firePoint;


    /// <summary>
    /// 初始化信息方法
    /// </summary>
    /// <param name="towerInfo">防御塔信息</param>
    /// <param name="atkType">攻击类型</param>
    public void InitInfo(TowerInfo towerInfo, int level)
    {
        //初始化信息
        this.towerInfo = towerInfo;
        //创建攻击接口物体实例
        GameObject atkObj = new GameObject($"AtkType{towerInfo.type}Obj");
        atkObj.transform.position = firePoint.position;
        //添加对应攻击方法接口脚本
        atkType = atkObj.AddComponent(TowerAtkTypeInfo.towerAtkTypeInfoDic[towerInfo.type]) as I_TowerAtkType;
        //记录物体实例用于音效播放点
        this.atkObj = atkObj;
        //记录当前等级
        this.level = level;
        //计算实际攻击力和攻击范围
        if (level < 3)
        {
            nowAtk = towerInfo.atk;
            nowAtkRange = towerInfo.atkRange;
        }
        else
        {
            //升级每级攻击力翻2.5倍(基于原始攻击力)
            nowAtk = (int)((level - 2) * 2.5f * towerInfo.atk);
            //升级每级攻击范围+2
            nowAtkRange = (level - 2) * 2 + towerInfo.atkRange;
        }
        //读取玩家数据 读取增益控制信息
        PlayerData playerData = GameDataMgr.Instance.PlayerData;
        SkinAwardControlInfo skinAwardControlInfo = GameDataMgr.Instance.AllControlInfo.skinAwardControlInfo;
        //应用炮塔攻击力增益
        nowAtk = (int)(nowAtk * (playerData.consumeMoney * skinAwardControlInfo.towerAtkRatio + 1));
        //无尽模式更新攻击力
        EndlessModeUpdateAtkRatio();
        //增加触发器
        CapsuleCollider collider = this.GetComponent<CapsuleCollider>();
        if (collider == null) collider = this.AddComponent<CapsuleCollider>();
        collider.center = Vector3.zero;
        collider.radius = nowAtkRange;
        collider.isTrigger = true;
        //初始化怪物列表
        monsters = new List<MonsterObj>();
    }

    // Update is called once per frame
    void Update()
    {
        //非游戏状态 直接结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;
        //获取一个可攻击的怪物目标
        nowMonsterTarget = GetMonster();
        //无可攻击目标 结束逻辑
        if (nowMonsterTarget == null) return;

        if (towerInfo.lookMonster)
        {
            //看向目标方向
            Vector3 dir = nowMonsterTarget.transform.position - towerHead.transform.position;
            //水平方向看向目标 避免炮台抬头低头
            dir.y = 0;
            towerHead.localRotation = Quaternion.Slerp(towerHead.localRotation, Quaternion.LookRotation(dir), Time.deltaTime * headRoundSpeed);
            //夹角足够小时开火
            if (Vector3.Angle(towerHead.forward, dir) < 8f)
            {
                TowerFire();
            }
        }
        else
        {
            TowerFire();
        }
    }

    /// <summary>
    /// 处理开火事件逻辑
    /// </summary>
    private void TowerFire()
    {
        //不满足开火间隔直接返回
        if (Time.time - frontAtkTime < towerInfo.offsetTime) return;
        //重置开火间隔时间
        frontAtkTime = Time.time;
        //播放开火音效
        GameDataMgr.Instance.PlaySound(towerInfo.audioRes, 0.85f, atkObj.transform);
        //开火事件接口调用方法
        atkType.AtkEvent(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            MonsterObj monsterObj = other.gameObject.GetComponentInParent<MonsterObj>();
            if (monsterObj == null) return;
            //判断是否已经死亡
            if (monsterObj.isDead) return;
            //判断是否已经记录
            if (monsters.Contains(monsterObj)) return;
            else
            {
                monsters.Add(monsterObj);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            MonsterObj monsterObj = other.gameObject.GetComponentInParent<MonsterObj>();
            if (monsterObj == null) return;
            if (monsters.Contains(monsterObj))
            {
                monsters.Remove(monsterObj);
            }
        }
    }

    /// <summary>
    /// 获取一个可攻击怪物对象
    /// </summary>
    /// <returns></returns>
    public MonsterObj GetMonster()
    {
        //清除不合法对象
        monsters.RemoveAll(monster => monster.isDead);
        monsters.RemoveAll(monster => monster.gameObject.activeSelf == false);
        foreach (MonsterObj monsterObj in monsters)
        {
            if (!monsterObj.isDead) return monsterObj;
        }
        return null;
    }

    /// <summary>
    /// 获取全部可攻击怪物对象
    /// </summary>
    /// <returns></returns>
    public List<MonsterObj> GetAllMonsters()
    {
        //清除不合法对象
        monsters.RemoveAll(monster => monster.isDead);
        monsters.RemoveAll(monster => monster.gameObject.activeSelf == false);
        if (monsters.Count == 0) return null;
        else return monsters;
    }

    /// <summary>
    /// 播放怪物受伤效果
    /// </summary>
    public void PlayMonsterWoundEff(Collider collider)
    {
        //播放命中僵尸特效
        GameDataMgr.Instance.PlayEff("Eff/3", collider.bounds.center + collider.transform.up,
                                     Quaternion.LookRotation(firePoint.position - collider.transform.position),
                                     collider.transform);
    }

    /// <summary>
    /// 无尽模式更新攻击力
    /// </summary>
    public void EndlessModeUpdateAtkRatio()
    {
        if (GameDataMgr.Instance.nowGameMode != GameMode.EndlessMode) return;
        if ((SceneLevelMgr.Instance as EndlessModeSceneMgr).IsEggTrigger)
        {
            nowAtk = (int)(nowAtk * 1.2f);
        }
    }

    private void OnDestroy()
    {
        //销毁时连带销毁攻击接口物体实例
        Destroy(atkObj);
    }
}

/// <summary>
/// 攻击类型01
/// 单体瞄准攻击
/// </summary>
public class TowerAtkType01 : MonoBehaviour, I_TowerAtkType
{
    public void AtkEvent(TowerObj obj)
    {
        //防御塔已销毁 直接结束逻辑
        if (obj == null) return;
        //播放开火特效
        GameDataMgr.Instance.PlayEff("Eff/TowersEff/Type1", obj.firePoint.position, Quaternion.LookRotation(-obj.transform.up), obj.firePoint);
        //怪物受伤
        obj.nowMonsterTarget.Wound(obj.nowAtk, false);
        //怪物受伤特效
        obj.PlayMonsterWoundEff(obj.nowMonsterTarget.bodyCollider);
    }
}

/// <summary>
/// 攻击类型02
/// 群体中心冰霜攻击
/// </summary>
public class TowerAtkType02 : MonoBehaviour, I_TowerAtkType
{
    public void AtkEvent(TowerObj obj)
    {
        //防御塔已销毁 直接结束逻辑
        if (obj == null) return;
        //播放开火特效
        GameDataMgr.Instance.PlayEff("Eff/TowersEff/Type2", obj.firePoint.position, Quaternion.identity, obj.firePoint);
        //开启怪物受伤协程
        StartCoroutine(MonsterWoundCoroutine(obj));
    }

    //全体合法怪物受伤线程
    IEnumerator MonsterWoundCoroutine(TowerObj obj)
    {
        //防御塔已销毁 直接结束逻辑
        if (obj == null) yield break;
        //获得攻击范围内的所有怪物
        List<MonsterObj> monsters = obj.GetAllMonsters();
        if (monsters == null) yield break;
        List<MonsterObj> coroutineMonsters = new List<MonsterObj>(monsters);

        //动画播放0.2s后开始受伤僵尸
        yield return new WaitForSeconds(0.2f);

        //开火点位置 XZ平面
        Vector3 firePos = obj.firePoint.position;
        firePos.y = 0;
        //怪物位置 XZ平面
        Vector3 monsterPos;
        //伤害减免百分比
        float woundRatio;
        //本次伤害值
        int wound;

        //怪物受伤
        foreach (MonsterObj monster in coroutineMonsters)
        {
            //防御塔已销毁 直接结束逻辑
            if (obj == null) yield break;
            //怪物已死亡 不再执行后续逻辑
            if (monster.isDead) continue;
            //怪物位置 XZ平面
            monsterPos = monster.transform.position;
            monsterPos.y = 0;
            //伤害减免百分比
            woundRatio = Mathf.Clamp01(Vector3.Distance(firePos, monsterPos) / obj.nowAtkRange);
            //降低一半伤害减免比例
            woundRatio *= 0.5f;
            //计算伤害值
            wound = (int)(obj.nowAtk * (1 - woundRatio));
            //限定最低伤害比例为25%
            if (wound < obj.nowAtk / 4) wound = obj.nowAtk / 4;
            //施加怪物伤害
            monster.Wound(wound, false);
            //怪物受伤特效
            obj.PlayMonsterWoundEff(monster.bodyCollider);
            //每帧只受伤一只怪物 避免卡顿
            yield return null;
        }
    }
}

/// <summary>
/// 攻击类型03
/// 火炮瞄准造成范围伤害
/// </summary>
public class TowerAtkType03 : MonoBehaviour, I_TowerAtkType
{
    public void AtkEvent(TowerObj obj)
    {
        //防御塔已销毁 直接结束逻辑
        if (obj == null) return;
        //播放开火特效
        GameDataMgr.Instance.PlayEff("Eff/TowersEff/Type1", obj.firePoint.position, Quaternion.LookRotation(-obj.transform.up), obj.firePoint);
        //开启怪物受伤协程
        StartCoroutine(MonsterWoundCoroutine(obj));
    }

    IEnumerator MonsterWoundCoroutine(TowerObj obj)
    {
        //开火音效+特效播放0.2s后 开始播放火炮爆炸特效
        yield return new WaitForSeconds(0.2f);

        //防御塔已销毁 直接结束逻辑
        if (obj == null) yield break;

        //攻击目标不存在 尝试寻找新目标
        if (!obj.nowMonsterTarget) obj.nowMonsterTarget = obj.GetMonster();
        //无法找到新目标 火炮塔攻击中断
        if (!obj.nowMonsterTarget) yield break;

        //爆炸点位置
        Vector3 boomPos = obj.nowMonsterTarget.bodyCollider.bounds.center + Vector3.up * 0.5f;
        //爆炸范围 攻击距离的65%长度作为爆炸半径
        float boomRange = obj.nowAtkRange * 0.65f;

        //播放火炮爆炸特效
        GameDataMgr.Instance.PlayEff("Eff/TowersEff/Type3", boomPos, Quaternion.identity, obj.nowMonsterTarget.transform);

        //爆炸中心范围检测 攻击距离的65%长度作为爆炸半径
        Collider[] colliders = Physics.OverlapSphere(boomPos, boomRange, 1 << LayerMask.NameToLayer("Monster"));

        //爆炸特效播放0.1s后开始受伤僵尸
        yield return new WaitForSeconds(0.1f);

        //获取爆炸点 XZ平面位置
        boomPos.y = 0;
        //怪物位置 XZ平面位置
        Vector3 monsterPos;
        //伤害减免百分比
        float woundRatio;
        //本次伤害值
        int wound;

        foreach (Collider collider in colliders)
        {
            //防御塔已销毁 直接结束逻辑
            if (obj == null) yield break;
            if (collider.CompareTag("Body"))
            {
                //获取怪物OBJ脚本
                MonsterObj monster = collider.GetComponentInParent<MonsterObj>();
                //怪物已死亡 直接结束单次循环逻辑
                if (!monster || monster.isDead) continue;
                //设置怪物XZ平面位置
                monsterPos = collider.transform.position;
                monsterPos.y = 0;
                //计算伤害减免系数
                woundRatio = Mathf.Clamp01(Vector3.Distance(boomPos, monsterPos) / boomRange);
                //降低一半伤害减免比例
                woundRatio *= 0.5f;
                //计算伤害值
                wound = (int)(obj.nowAtk * (1 - woundRatio));
                //限定最低伤害比例为25%
                if (wound < obj.nowAtk / 4) wound = obj.nowAtk / 4;
                //施加怪物伤害
                monster.Wound(wound, false);
                //怪物受伤特效
                obj.PlayMonsterWoundEff(monster.bodyCollider);
                //每帧只受伤一只怪物 避免卡顿
                yield return null;
            }
        }
    }
}

/// <summary>
/// 攻击类型04
/// 闪电链式伤害
/// </summary>
public class TowerAtkType04 : MonoBehaviour, I_TowerAtkType
{
    public void AtkEvent(TowerObj obj)
    {
        //防御塔已销毁 直接结束逻辑
        if (obj == null) return;
        //开启怪物受伤协程
        StartCoroutine(MonsterWoundCoroutine(obj));
    }

    IEnumerator MonsterWoundCoroutine(TowerObj obj)
    {
        //防御塔已销毁 直接结束逻辑
        if (obj == null) yield break;
        //已访问过的怪物列表
        List<MonsterObj> visitedMonsterList = new List<MonsterObj>();
        //待访问怪物队列
        Queue<MonsterObj> monsterQueue = new Queue<MonsterObj>();
        //父节点映射关系记录
        Dictionary<MonsterObj, Transform> fatherTransDic = new Dictionary<MonsterObj, Transform>();

        //第一只怪物入列
        monsterQueue.Enqueue(obj.nowMonsterTarget);
        //记录开火点为父对象
        fatherTransDic.Add(obj.nowMonsterTarget, obj.firePoint);

        //广度优先搜索遍历
        while (monsterQueue.Count > 0)
        {
            //防御塔已销毁 直接结束逻辑
            if (obj == null) yield break;
            //获取当前队列怪物
            MonsterObj monster = monsterQueue.Dequeue();
            if (visitedMonsterList.Contains(monster)) { continue; }
            //播放雷电特效
            GameDataMgr.Instance.PlayEff("Eff/TowersEff/Type4", fatherTransDic[monster].position,
                                         Quaternion.LookRotation(monster.bodyCollider.transform.position - fatherTransDic[monster].position));
            //雷电开火音效
            GameDataMgr.Instance.PlaySound(obj.towerInfo.audioRes, 0.3f, fatherTransDic[monster]);

            //雷电特效播放0.2s后开始受伤僵尸
            yield return new WaitForSeconds(0.2f);

            //应用怪物受伤
            monster.Wound(obj.nowAtk, false);
            //怪物受伤溅血特效
            obj.PlayMonsterWoundEff(monster.bodyCollider);
            //记录当前怪物为已访问对象
            visitedMonsterList.Add(monster);

            //范围检测 攻击距离的65%长度链式反应距离上限
            Collider[] colliders = Physics.OverlapSphere(monster.transform.position + Vector3.up, obj.nowAtkRange * 0.65f, 1 << LayerMask.NameToLayer("Monster"));
            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag("Body"))
                {
                    //获取怪物脚本
                    MonsterObj newMonsterObj = collider.GetComponentInParent<MonsterObj>();
                    if (newMonsterObj.isDead || visitedMonsterList.Contains(newMonsterObj)) continue;
                    //记录合法怪物脚本
                    monsterQueue.Enqueue(newMonsterObj);
                    //记录父对象
                    if (!fatherTransDic.ContainsKey(newMonsterObj))
                    {
                        fatherTransDic.Add(newMonsterObj, monster.bodyCollider.transform);
                    }
                }
            }
            //每帧只受伤一只怪物 避免卡顿
            yield return null;
        }
    }
}
