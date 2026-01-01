using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainingModeMonster : MonsterObj
{
    // 移动数组
    public Vector3[] moveVector3s;
    // 是否为移动怪物
    private bool isMoveMonster;
    // 移动速度
    private float moveSpeed;
    // 移动进度
    private float moveProgress;

    protected override void Awake()
    {
        // 关联动画状态机
        animator = GetComponent<Animator>();
        // 关联场景中心点
        centrePos = GameObject.Find("CentrePos").transform.position;
        // 随机移动速度
        moveSpeed = Random.Range(0.2f, 0.7f);
    }

    public override void BornOver() { }

    protected override void OnEnable() { }

    protected override void Update()
    {
        //受伤状态更新
        UpdateWoundState();
        //看向场景中心点
        LookAtCentrePos();
        //控制怪物移动
        MoveMonsterControl();
    }

    protected override void Dead()
    {
        //死亡状态更新
        isDead = true;
        //死亡动画更新
        animator.SetBool("Dead", true);
        //播放死亡音效
        GameDataMgr.Instance.PlaySound("Music/Dead", 0.3f, this.transform);
        //对象池回收对象
        if (!monsterInfo.isBoss) Invoke("ReleaseMe", 3.5f);
    }

    protected override void ReleaseMe()
    {
        //通知训练场管理器生成新怪物
        TrainingModeMgr.Instance.DeadMonster(this);
        //父类对象池回收方法
        base.ReleaseMe();
    }

    public override void ResetState()
    {
        base.ResetState();
        // 随机移动速度
        moveSpeed = Random.Range(0.2f, 0.7f);
        // 重置怪物种类
        isMoveMonster = false;
        // 重置移动数组
        moveVector3s = null;
    }

    /// <summary>
    /// 初始化怪物信息
    /// </summary>
    public void InitInfo(int ID)
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
        this.ID = ID;
        monsterInfo = GameDataMgr.Instance.MonsterList[ID - 1];
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(monsterInfo.animator);
        nowHp = maxHp = monsterInfo.hp;
        // 关联自身头部/身体碰撞盒
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Head")) headCollider = collider;
            else if (collider.CompareTag("Body")) bodyCollider = collider;
        }
        // 获取血条图标
        UIManager.Instance.GetPanel<GamePanel>()?.ShowMonsterHpIcon(this);
    }

    // 训练场景中心点
    private Vector3 centrePos;

    /// <summary>
    /// 看向场景中心点
    /// </summary>
    private void LookAtCentrePos()
    {
        Quaternion quaternion = Quaternion.LookRotation(centrePos - transform.position);
        transform.rotation = quaternion;
    }

    // 初始化移动数组
    public void InitMovePos(Vector3[] pos)
    {
        isMoveMonster = true;
        moveVector3s = pos;
    }

    /// <summary>
    /// 控制怪物移动
    /// </summary>
    private void MoveMonsterControl()
    {
        if (isMoveMonster)
        {
            //更新移动进度
            moveProgress += moveSpeed * Time.deltaTime;
            //移动到头反向移动
            if (moveProgress > 1 || moveProgress < 0)
            {
                moveSpeed = -moveSpeed;
                moveProgress = Mathf.Clamp01(moveProgress);
            }
            //球形插值运算控制移动
            this.transform.position = Vector3.Slerp(moveVector3s[0], moveVector3s[1], moveProgress);
        }
    }
}
