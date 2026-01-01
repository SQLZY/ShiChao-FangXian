using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 怪物逻辑
/// </summary>
public class MonsterObj : MonoBehaviour, IResetState
{
    //寻路组件相关
    private NavMeshAgent agent;
    //通往玩家路径数据结构体(寻路管理器统一计算)
    private PlayerPathData playerPath;
    //动画组件相关
    protected Animator animator;

    //怪物ID
    public int ID;
    //怪物信息相关
    protected MonsterInfo monsterInfo;
    //当前血量
    protected int nowHp;
    //最大血量
    protected int maxHp;
    //当前寻路锁定目标 ture玩家 false保护区
    protected bool isTargetPlayer;
    //当前玩家位置
    protected Transform playerTarget;
    //当前攻击目标位置坐标
    private Vector3 nowAtkTarget;

    //基础攻击动画速度系数
    protected float atkSpeedRatio;
    //计算得到的当前怪物攻击动画速度系数
    private float atkSpeedCalResult;
    //怪物基础 移动速度/旋转速度/加速度
    protected float monsterBasicSpeed;
    //攻击玩家时减速系数
    protected float atkingMoveRatio;
    //受到攻击时减速系数
    protected float beAtkedMoveRatio;
    //攻击有效角度范围
    protected float atkAngleRange;
    //上次攻击时间 用于攻击间隔判断
    protected float frontAtkTime;

    //受击状态恢复缓冲延迟
    protected float bufferAtkedTime;
    //受击状态计时器
    private float atkedTimeCount;

    //头部碰撞盒
    public Collider headCollider;
    //身体碰撞盒
    public Collider bodyCollider;

    //状态相关
    public bool isDead; //是否死亡
    public bool isAtking; //是否正在攻击
    public bool isBeAtked; //是否正在受击
    public bool isAllowMove; //是否允许移动

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        InitInfo();
    }

    /// <summary>
    /// 初始化怪物信息
    /// </summary>
    public virtual void InitInfo()
    {
        // 初始化怪物控制信息数据相关
        MonsterControlInfo monsterControlInfo = GameDataMgr.Instance.AllControlInfo.monsterControlInfo;
        atkSpeedRatio = monsterControlInfo.atkSpeedRatio;
        monsterBasicSpeed = monsterControlInfo.monsterBasicSpeed;
        atkingMoveRatio = monsterControlInfo.atkingMoveRatio;
        beAtkedMoveRatio = monsterControlInfo.beAtkedMoveRatio;
        atkAngleRange = monsterControlInfo.atkAngleRange;
        bufferAtkedTime = monsterControlInfo.bufferAtkedTime;

        // 初始化怪物信息 动画状态机信息等
        monsterInfo = GameDataMgr.Instance.MonsterList[ID - 1];
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(monsterInfo.animator);
        nowHp = maxHp = monsterInfo.hp;
        // 根据怪物平均攻击CD间隔 动态改变攻击动画播放速度
        // 比如,怪物平均攻击间隔是1.5s(配置信息计算得到),那么间隔<1.5s的攻击动画更快,间隔>1.5s的攻击动画更慢
        float avg = MathCalTool.Instance.CalAvg<MonsterInfo>("atkCd", GameDataMgr.Instance.MonsterList);
        atkSpeedCalResult = avg / monsterInfo.atkCd * atkSpeedRatio;
        animator.SetFloat("AtkSpeed", atkSpeedCalResult);
        // 设置寻路组件 移动速度/旋转速度/加速度
        ResetBasicMoveSpeed();
        // 设置寻路组件停止移动距离
        agent.stoppingDistance = monsterInfo.atkRange / 3;
        // 允许寻路自动更新动态障碍新路径
        agent.autoRepath = true;
        // 寻找场景中的玩家
        if (GameDataMgr.Instance.nowPlayerObj) playerTarget = GameDataMgr.Instance.nowPlayerObj.transform;
        // 关联自身头部/身体碰撞盒
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Head")) headCollider = collider;
            else if (collider.CompareTag("Body")) bodyCollider = collider;
        }
        // 获取血条图标
        if (!monsterInfo.isBoss) UIManager.Instance.GetPanel<GamePanel>()?.ShowMonsterHpIcon(this);
        else UIManager.Instance.GetPanel<GamePanel>()?.InitBossArea(monsterInfo.tipsName);

        // 无尽模式下 增强怪物
        if (GameDataMgr.Instance.nowGameMode == GameMode.EndlessMode)
        {
            // 血量增加70%
            nowHp = maxHp = (int)(monsterInfo.hp * 1.7f);
            // 移速增加25%
            monsterBasicSpeed *= 1.25f;
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //受伤状态更新
        UpdateWoundState();
        // 死亡/非游戏状态直接返回
        if (isDead || !GameDataMgr.Instance.isGaming) return;
        //移动状态更新
        UpdateMoveState();
        //攻击状态更新
        UpdateAtkState();
    }

    //受伤
    public void Wound(int dmg, bool isHead, bool isPlayerHit = false)
    {
        if (isDead) return;
        //受击状态更新
        isBeAtked = true;
        //受击缓冲时间重置
        atkedTimeCount = bufferAtkedTime;
        //受击动画更新
        animator.SetBool("Wound", true);
        //播放受伤音效
        if (isHead)
        {
            //怪物受伤音效
            GameDataMgr.Instance.PlaySound("Music/Wound01", 0.8f, this.transform);
        }
        else
        {
            //怪物受伤音效
            GameDataMgr.Instance.PlaySound("Music/Wound02", 0.2f, this.transform);
        }
        if (isPlayerHit)
        {
            //播放受击提示
            UIManager.Instance.GetPanel<AimStarPanel>()?.ShowHitTip(isHead);
            if (isHead)
            {
                //命中僵尸受伤反馈音效
                GameDataMgr.Instance.PlaySound("Music/Gun/HitHead", 1.4f);
            }
            else
            {
                //命中僵尸受伤反馈音效
                GameDataMgr.Instance.PlaySound("Music/Gun/HitBody", 2f);
            }
        }
        //扣血并判断死亡
        nowHp -= dmg;
        //如果是Boss 通知UI更新血条
        if (monsterInfo.isBoss)
        {
            UIManager.Instance.GetPanel<GamePanel>().UpdateBossHp(GetNowHpRatio());
        }
        if (nowHp <= 0) Dead();
    }

    //死亡
    protected virtual void Dead()
    {
        //死亡状态更新
        isDead = true;
        //死亡动画更新
        animator.SetBool("Dead", true);
        //播放死亡音效
        GameDataMgr.Instance.PlaySound("Music/Dead", 0.3f, this.transform);
        //停止移动
        agent.Warp(this.transform.position);
        agent.isStopped = true;
        //玩家对象加钱
        if (monsterInfo.isBoss)
        {
            if (!GameDataMgr.Instance.PlayerData.killBoss.Contains(monsterInfo.id))
            {
                //首次击败Boss加钱
                GameDataMgr.Instance.nowPlayerObj.AddMoney(monsterInfo.awardMoney);
                //记录击败Boss
                GameDataMgr.Instance.PlayerData.killBoss.Add(monsterInfo.id);
            }
            else
            {
                //非首次击败Boss加钱10%
                GameDataMgr.Instance.nowPlayerObj.AddMoney((int)(monsterInfo.awardMoney * 0.1f));
            }
        }
        else
        {
            GameDataMgr.Instance.nowPlayerObj?.AddMoney(monsterInfo.awardMoney);
        }
        //记录玩家杀敌数量
        ++GameDataMgr.Instance.PlayerData.killMonsterCount;
        //关卡管理器更新怪物数量
        SceneLevelMgr.Instance.ChangeMonsterNum(-1);
        //判断游戏是否通关
        SceneLevelMgr.Instance.CheckGameOverWin();
        //对象池回收对象
        if (!monsterInfo.isBoss) Invoke("ReleaseMe", 3.5f);
    }

    //回收对象方法
    protected virtual void ReleaseMe()
    {
        //对象池回收对象
        ObjectPoolMgr.Instance.ReleaseObj(this.gameObject);
    }

    //受伤状态更新
    protected void UpdateWoundState()
    {
        atkedTimeCount -= Time.deltaTime;
        if (atkedTimeCount <= 0)
        {
            //受击状态更新
            isBeAtked = false;
            //受击动画更新
            animator.SetBool("Wound", false);
        }
    }

    //出生动画播放完毕后开始移动
    public virtual void BornOver()
    {
        isAllowMove = true;
        //更换寻路目标为保护区
        ResetTargetToMainTower();
    }

    //移动状态更新
    private void UpdateMoveState()
    {
        if (isAllowMove)
        {
            //动画更新
            animator.SetBool("Run", agent.velocity != Vector3.zero);

            //目标更新
            if (Vector3.Distance(this.transform.position, playerTarget.transform.position) <= 20 ||
                Vector3.Distance(this.transform.position, playerTarget.transform.position) <= 40 && isBeAtked)
            {
                MoveToPlayerIfAllow();
            }
            else if (Vector3.Distance(this.transform.position, playerTarget.transform.position) > 40)
            {
                //更换寻路目标为保护区
                ResetTargetToMainTower();
            }

            if (isTargetPlayer && !MoveToPlayerIfAllow())
            {
                //更换寻路目标为保护区
                ResetTargetToMainTower();
            }

            //速度更新
            if (isBeAtked || isAtking)
            {
                //恢复默认移动速度
                ResetBasicMoveSpeed();
                //位移动画播放速度更新
                animator.SetFloat("RunSpeed", (isBeAtked ? beAtkedMoveRatio : 1) * (isAtking ? atkingMoveRatio : 1));
                //受击状态位移减速
                if (isBeAtked)
                {
                    agent.speed *= beAtkedMoveRatio;
                    agent.angularSpeed *= beAtkedMoveRatio;
                    agent.acceleration *= beAtkedMoveRatio;
                }
                //攻击状态位移减速
                if (isAtking)
                {
                    agent.speed *= atkingMoveRatio;
                    agent.angularSpeed *= atkingMoveRatio;
                    agent.acceleration *= atkingMoveRatio;
                }
            }
            else
            {
                //恢复默认移动速度
                ResetBasicMoveSpeed();
            }
        }
    }

    //恢复默认移动速度
    private void ResetBasicMoveSpeed()
    {
        //移动动画播放速度恢复
        animator.SetFloat("RunSpeed", 1);
        //重置寻路组件 移动速度/旋转速度/加速度
        agent.speed = monsterBasicSpeed * monsterInfo.moveSpeedRatio;
        agent.angularSpeed = monsterBasicSpeed * monsterInfo.moveSpeedRatio * 45f;
        agent.acceleration = monsterBasicSpeed * monsterInfo.moveSpeedRatio * 2f;
    }

    //更换寻路目标为保护区
    private void ResetTargetToMainTower()
    {
        isTargetPlayer = false;
        //保护区位置固定 因此只用进行一次目标更新
        agent.SetDestination(MainTowerObj.Instance.transform.position);
    }

    //如果条件允许 更改寻路目标为玩家
    private bool MoveToPlayerIfAllow()
    {
        //if (agent.CalculatePath(playerTarget.transform.position, path))
        //{
        //    //路径完全可达 切换目标为玩家
        //    if (path.status == NavMeshPathStatus.PathComplete)
        //    {
        //        isTargetPlayer = true;
        //        agent.SetDestination(playerTarget.transform.position);
        //        return true;
        //    }

        //    //路径部分可达 但最终点在怪物攻击范围内 切换目标为玩家
        //    if (path.status == NavMeshPathStatus.PathPartial)
        //    {
        //        //虽然最后一个路径拐点 在极少数特殊情况下 不是距离目标最近的点
        //        //但是相比遍历拐点数组 节约大量性能
        //        if (Vector3.Distance(path.corners[path.corners.Length - 1], playerTarget.transform.position) < monsterInfo.atkRange * 0.9f)
        //        {
        //            isTargetPlayer = true;
        //            agent.SetDestination(path.corners[path.corners.Length - 1]);
        //            return true;
        //        }
        //    }
        //}

        //return false;


        //获取通往玩家寻路数据
        playerPath = CalPathMgr.Instance.GetPlayerPathData();

        //路径可达或部分可达
        if (playerPath.isPlayerReachable)
        {
            //攻击范围内
            if (Vector3.Distance(playerPath.reachablePlayerPos, playerTarget.transform.position) < monsterInfo.atkRange * 0.85f)
            {
                isTargetPlayer = true;
                agent.SetDestination(playerPath.reachablePlayerPos);
                return true;
            }
        }

        //路径不可达 或 不在攻击范围内
        return false;
    }

    //攻击状态更新
    protected virtual void UpdateAtkState()
    {
        if (isTargetPlayer)
        {
            if (Vector3.Distance(this.transform.position, playerTarget.transform.position) < monsterInfo.atkRange
                && Time.time - frontAtkTime > monsterInfo.atkCd)
            {
                frontAtkTime = Time.time;
                animator.SetTrigger("Atk");
            }
        }
        else
        {
            if (Vector3.Distance(this.transform.position, MainTowerObj.Instance.transform.position) < monsterInfo.atkRange
                && Time.time - frontAtkTime > monsterInfo.atkCd)
            {
                frontAtkTime = Time.time;
                animator.SetTrigger("Atk");
            }
        }
    }

    //两段攻击动画开始调用事件
    public void StartAtkEvent()
    {
        // 已经结束游戏状态 直接结束开始攻击逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        //每次攻击开始时 根据当前目标 修正朝向
        nowAtkTarget = isTargetPlayer ? playerTarget.transform.position : MainTowerObj.Instance.transform.position;
        Vector3 dir = nowAtkTarget - this.transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
            this.transform.rotation = Quaternion.LookRotation(dir);
        //变更攻击状态
        isAtking = true;

        //若未脱离攻击范围 显示危险图标提示玩家
        if (isTargetPlayer && Vector3.Distance(transform.position, playerTarget.position) <= monsterInfo.atkRange * 1.3f)
            UIManager.Instance.GetPanel<GamePanel>().ShowWarningIcon(headCollider.transform);
    }
    //两段攻击关键帧伤害检测调用事件
    public virtual void AtkEvent()
    {
        // 已经结束游戏状态 直接结束攻击判断逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        // 播放攻击音效
        GameDataMgr.Instance.PlaySound("Music/Gun/AtkEvent", 0.7f, transform);

        // 满足成功攻击的条件
        // 1.攻击目标所在角度夹角在最大角度范围内
        // 2.攻击目标处于怪物前方
        // 3.攻击目标处于当前怪物最大攻击范围内
        // 或
        // 4.玩家距离怪物太近
        Vector3 playerDir = playerTarget.transform.position - this.transform.position;
        playerDir.y = 0;
        if (Vector3.Distance(this.transform.position, playerTarget.transform.position) <= monsterInfo.atkRange / 3f ||
            Vector3.Angle(this.transform.forward, playerDir) <= atkAngleRange
            && Vector3.Dot(this.transform.forward, playerDir) > 0
            && Vector3.Distance(this.transform.position, playerTarget.transform.position) <= monsterInfo.atkRange)
        {
            //玩家受到攻击逻辑
            GameDataMgr.Instance.nowPlayerObj.Wound(monsterInfo.atk);
            //播放音效
            GameDataMgr.Instance.PlaySound("Music/Gun/PlayerWound", 0.6f);
        }
        // 攻击保护区域忽略夹角
        if (Vector3.Dot(this.transform.forward, MainTowerObj.Instance.transform.position - this.transform.position) > 0
            && Vector3.Distance(this.transform.position, MainTowerObj.Instance.transform.position) <= monsterInfo.atkRange)
        {
            //保护区受到攻击逻辑
            MainTowerObj.Instance.Wound(monsterInfo.atk);
            //播放音效
            GameDataMgr.Instance.PlaySound("Music/Eat", 0.5f);
        }
    }

    //二段攻击动画结束调用事件
    public virtual void EndAtkEvent()
    {
        //恢复非攻击状态
        isAtking = false;
    }

    //对象池回收对象 重置状态操作
    public virtual void ResetState()
    {
        //重置血量
        nowHp = maxHp;
        //重置动画状态机
        animator.Rebind();
        animator.Update(0f);
        //重置状态
        isDead = false;
        isAtking = false;
        isBeAtked = false;
        isAllowMove = false;
        //重置旋转避免动画出错
        this.transform.rotation = Quaternion.identity;
    }

    //对象激活时调用
    protected virtual void OnEnable()
    {
        //恢复正确攻击动画速度系数
        if (!monsterInfo.isBoss) animator.SetFloat("AtkSpeed", atkSpeedCalResult);
    }

    /// <summary>
    /// 获取当前血量百分比
    /// </summary>
    /// <returns>血量百分比</returns>
    public float GetNowHpRatio()
    {
        return Mathf.Clamp01((float)nowHp / maxHp);
    }
}
