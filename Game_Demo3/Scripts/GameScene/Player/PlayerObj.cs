using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerObj : MonoBehaviour
{
    //玩家当前生命值
    private int nowHp;
    //玩家最大生命值
    private int maxHp;
    //玩家武器攻击力
    private int atk;
    //玩家关卡金钱
    public int money { get; private set; }
    //玩家奖励金钱（用于解锁新装备和皮肤）
    public int awardMoney { get; private set; }
    //子弹射速（每秒多少发）
    private float shootSpeed;
    //玩家移动速度加速系数
    private float playerMoveSpeed;
    //玩家基础移速单位
    private float basicMoveSpeed;
    //武器有效射程
    private int atkDistance;
    //武器剩余子弹数量
    private int bulletCount;
    //攻击检测类型 1刀具 2枪械
    private int atkType;
    //基础腰射散布
    private float bulletOffset;
    //开镜腰射散布
    private float bulletAimOffset;
    //水平后座力
    private float xRecoil;
    //垂直后座力
    private float yRecoil;
    //爆头伤害系数
    private float headAtkRatio;
    //玩家信息原始参数记录
    private HeroInfo heroInfo;
    //武器游戏物体记录(有皮肤角色)
    public GameObject weapon;

    //当前移动轴向
    private Vector2 vMove;
    //当前移速  (最小值为0 最大值为1)
    public float moveDirMagnitude;
    //当前移动方向(是否向前)
    public bool isMovingForward;
    //角色控制器脚本
    private CharacterController characterController;

    //玩家身上的动画状态机脚本
    private Animator animator;
    //玩家身上的IK控制脚本
    private PlayerIKController playerIKController;
    //当前动画2D混合XY坐标
    private Vector2 nowAnimationValue;

    //是否触发疾跑
    private bool isShiftDown;
    //是否触发蹲下
    private bool isCtrlDown;
    //蹲下缓变效果记录值
    public float crouchValue;

    //最小翻滚时间间隔
    private float minRollTime;
    //翻滚持续时间
    private float rollingTime;
    //站立最大翻滚速度
    private float normalRollSpeed;
    //蹲姿最大翻滚速度
    private float crouchRollSpeed;
    //当前翻滚时间冷却计时器
    private float nowMinRollTime;
    //是否翻滚中
    public bool isRolling;
    //翻滚方向
    private bool isForward;
    //翻滚计时器
    private float rollTime;

    //瞄准按键是否按下
    private bool isAimButtonDown;
    //是否处于瞄准状态
    private bool isAiming;
    //处于开镜瞄准状态移速减速系数(独立控制 不受姿态等因素影响)
    private float aimingSpeed;

    //非疾跑状态减速系数
    private float walkSpeedRatio;
    //处于非全速状态(蹲下/开火/换弹/后退)减速系数
    private float nonfullSpeedRatio;

    //是否按下跳跃键
    private bool isSpaceDown;
    //是否处于跳跃状态
    public bool isJumping;
    //期望最大跳跃高度
    private float jumpHeight;
    //重力大小
    private float gravity;
    //当前垂直方向速度
    private float verticalSpeed;

    //跳跃缓冲机制时间 当处于无法跳跃状态时按下跳跃键 若缓冲时间内恢复可跳跃状态 立刻进入跳跃逻辑
    private float bufferJumpTime;
    //当前缓冲跳跃时间计数器
    private float nowbufferJumpTime;
    //土狼时间机制 当离开地面平台的短暂时间内 仍然可以起跳成功
    private float coyoteJumpTime;
    //当前土狼时间计数器
    private float nowCoyoteJumpTime;

    //是否结束落地僵直状态(刚落地静止移动)
    public bool isEndJump = true;

    //是否处于开火状态
    private bool isFire;
    //上一颗子弹发射时间
    private float frontBulletFireTime;
    //开火特效未命中怪物最大显示距离
    private float maxFireEffDistance;
    //当前开火子弹散射大小
    private float gunOffset;
    //枪口火焰位置
    public Transform gunFirePoint;

    //是否处于换弹状态
    private bool isReloading;

    //受到攻击状态
    public bool isBeAtked;
    //受伤状态恢复时间
    private float atkedTime;
    //受伤状态计时器
    private float nowAtkedTime;

    //是否处于舞蹈状态
    public bool isDancing;
    //是否允许进入舞蹈状态(玩家行为相关)
    private bool isAllowDance;
    //动画状态机是否播放Move动画
    private bool isPlayingMove;

    //玩家是否死亡
    private bool isDead;

    //战斗设置
    private FightSettingsData fightSettingsData;


    // Start is called before the first frame update
    void Awake()
    {
        //获得动画状态机脚本 
        animator = GetComponent<Animator>();
        //确保关闭动画根运动
        animator.applyRootMotion = false;
        //获得角色控制器脚本
        characterController = GetComponent<CharacterController>();
        //获得IK控制器脚本
        playerIKController = GetComponent<PlayerIKController>();
        //获取战斗设置
        fightSettingsData = GameDataMgr.Instance.FightSettingsData;
    }

    // 玩家控制状态切换原则梳理
    // 第一条：跳跃状态 不允许蹲下/翻滚/开镜 落地瞬间不允许移动
    // 第二条：开镜状态 不允许跳跃 移动速度受限
    // 第三条：蹲下状态 不允许跳跃 翻滚距离受限 移动速度受限
    // 第四条：翻滚状态 不允许开镜/跳跃/蹲下/开火/换弹 移动控制不再改变速度 不允许IK控制
    // 第五条：开火状态 不允许换弹 移动速度受限
    // 第六条：换弹状态 不允许开镜/翻滚/开火 不允许IK控制 刀具不允许换弹 移动速度受限
    // 第七条：自由视角状态/刀具武器 不允许IK控制
    // 第八条：舞蹈状态 静止不动允许进入 任何输入操作与受到攻击将会打断 不允许IK控制

    // Update is called once per frame
    void Update()
    {
        //非游戏进行状态停止所有状态更新
        if (!GameDataMgr.Instance.isGaming) return;
        //如果死亡停止所有状态更新
        if (isDead) return;
        //玩家移动更新
        UpdatePlayerMove();
        //开火检测更新
        UpdateFire();
        //蹲下检测更新
        UpdateCrouchState();
        //打滚检测更新
        UpdateRollState();
        //应用翻滚位移
        RollThePlayer();
        //开镜检测更新
        UpdateAimingState();
        //跳跃检测更新
        UpdateJumpState();
        //换弹状态更新
        UpdateReloadState();
        //受伤状态更新
        UpdateWoundState();
        //舞蹈状态更新
        UpdateDanceState();
        //控制IK脚本是否激活
        UpdateIKState();
    }

    /// <summary>
    /// 初始化玩家信息
    /// </summary>
    /// <param name="atk">攻击力</param>
    /// <param name="shootSpeed">子弹射速</param>
    public void InitPlayerInfo(HeroInfo heroInfo)
    {
        //读取玩家数据 读取玩家控制类 读取皮肤增益控制信息
        PlayerData playerData = GameDataMgr.Instance.PlayerData;
        PlayerControlInfo playerControlInfo = GameDataMgr.Instance.AllControlInfo.playerControlInfo;
        SkinAwardControlInfo skinAwardControlInfo = GameDataMgr.Instance.AllControlInfo.skinAwardControlInfo;

        //初始化玩家控制数据
        basicMoveSpeed = playerControlInfo.basicMoveSpeed;
        minRollTime = playerControlInfo.minRollTime;
        rollingTime = playerControlInfo.rollingTime;
        normalRollSpeed = playerControlInfo.normalRollSpeed;
        crouchRollSpeed = playerControlInfo.crouchRollSpeed;
        aimingSpeed = playerControlInfo.aimingSpeed;
        walkSpeedRatio = playerControlInfo.walkSpeedRatio;
        nonfullSpeedRatio = playerControlInfo.nonfullSpeedRatio;
        jumpHeight = playerControlInfo.jumpHeight;
        gravity = playerControlInfo.gravity;
        bufferJumpTime = playerControlInfo.bufferJumpTime;
        coyoteJumpTime = playerControlInfo.coyoteJumpTime;
        maxFireEffDistance = playerControlInfo.maxFireEffDistance;
        atkedTime = playerControlInfo.atkedTime;

        //初始化武器相关数据
        atk = (int)(heroInfo.atk * (playerData.consumeMoney * skinAwardControlInfo.playerAtkRatio + 1));
        shootSpeed = heroInfo.shootSpeed;
        playerMoveSpeed = heroInfo.playerMoveSpeed;
        atkDistance = heroInfo.atkDistance;
        bulletCount = heroInfo.bulletCount;
        atkType = heroInfo.atkType;
        bulletOffset = heroInfo.bulletOffset;
        bulletAimOffset = heroInfo.bulletAimOffset;
        xRecoil = heroInfo.xRecoil;
        yRecoil = heroInfo.yRecoil;
        headAtkRatio = heroInfo.headAtkRatio;
        this.heroInfo = heroInfo;

        //初始化玩家血量
        nowHp = maxHp = playerData.playerBasicHp + playerData.consumeMoney * skinAwardControlInfo.playerHp;
        //初始化关卡金钱
        money = playerData.basicMoney + playerData.consumeMoney * skinAwardControlInfo.basicMoney;
        UpdateUIInfo();
        //初始化玩家血条
        UIManager.Instance.GetPanel<GamePanel>().UpdatePlayerHp(nowHp, maxHp);

        //如果是刀具武器 关闭IK控制
        if (atkType == 1) Destroy(this.GetComponent<PlayerIKController>());

        //根据角色移动速度系数设置动画播放速度
        animator.speed = heroInfo.playerMoveSpeed;
        //根据角色移动速度系数设置最大跳跃高度
        jumpHeight *= heroInfo.playerMoveSpeed;
        //根据角色移动速度系数设置基础移速数值
        basicMoveSpeed *= heroInfo.playerMoveSpeed;
        //根据角色移动速度系数设置翻滚速度数值
        normalRollSpeed *= heroInfo.playerMoveSpeed;
        crouchRollSpeed *= heroInfo.playerMoveSpeed;
        //根据角色武器配置信息设置开火动画播放速度
        animator.SetFloat("FireSpeed", heroInfo.fireSpeed);
    }
    //玩家移动更新
    private void UpdatePlayerMove()
    {
        //落地瞬间/翻滚过程 禁止移动
        if (isEndJump && !isRolling)
        {
            //加速键是否按下
            if (!fightSettingsData.runControlMode)
            {
                isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                {
                    isShiftDown = !isShiftDown;
                }
            }

            //移动相关逻辑
            //获取移动轴向
            vMove.x = Input.GetAxis("Horizontal");
            vMove.y = Input.GetAxis("Vertical");
            // 角色移动方向 向量正则化归一化速度 避免斜向跑动加速
            Vector3 vMoveDir = this.transform.TransformDirection(new Vector3(vMove.x, 0, vMove.y).normalized);
            // 开镜状态  独立计算移速 忽略加速键
            if (isAiming)
            {
                //开镜减速
                vMoveDir *= aimingSpeed;
            }
            // 非开镜状态
            else
            {
                //后退或侧向移动减速 通过向量点乘判断前进后退
                vMoveDir *= Vector3.Dot(vMoveDir, transform.forward) < 0.1f ? nonfullSpeedRatio : 1;
                //非疾跑状态减速
                vMoveDir *= !isShiftDown ? walkSpeedRatio : 1;
                //蹲下减速
                vMoveDir *= crouchValue > 0.5f ? nonfullSpeedRatio : 1;
                //开火减速
                vMoveDir *= isFire ? nonfullSpeedRatio : 1;
                //换弹减速
                vMoveDir *= isReloading ? nonfullSpeedRatio : 1;
            }
            //根据实际移速渐变动画速度
            nowAnimationValue = Vector2.Lerp(nowAnimationValue, vMove * vMoveDir.magnitude, Time.deltaTime * 6);
            animator.SetFloat("XSpeed", nowAnimationValue.x);
            animator.SetFloat("YSpeed", nowAnimationValue.y);
            //记录当前移速 用于子弹散射等计算
            moveDirMagnitude = vMoveDir.magnitude;
            //记录当前移动方向 用于翻滚速度等判断
            isMovingForward = Vector3.Dot(vMoveDir, transform.forward) > 0;
            //更新跑步音频播放速度
            PlayerSoundMgr.Instance.UpdatePicth(moveDirMagnitude * playerMoveSpeed);
            //角色控制器控制移动
            characterController.SimpleMove(vMoveDir * basicMoveSpeed);
        }
    }
    //开火检测更新
    private void UpdateFire()
    {
        // 无论开火与否 计算并更新当前状态子弹散射大小
        gunOffset = MathCalTool.Instance.CalOffsetValue
            (isAiming ? bulletAimOffset : bulletOffset, moveDirMagnitude, (isJumping || !isEndJump), crouchValue > 0.5f);
        // 更新准星大小UI显示 利用写好的武器散射大小转UI散射大小API
        UIManager.Instance.GetPanel<AimStarPanel>()?.ChangeTargetOffset(MathCalTool.Instance.GunOffsetToUIOffset(gunOffset));

        if (Input.GetMouseButton(0))
        {
            // 翻滚状态/换弹状态/弹夹打空 不允许开火
            if (Time.time - frontBulletFireTime >= 1f / shootSpeed && !isRolling && !isReloading && (bulletCount > 0 || atkType == 1))
            {
                //第一发子弹
                if (!isFire)
                    frontBulletFireTime = Time.time;
                //非第一发子弹
                else frontBulletFireTime += 1f / shootSpeed;

                //设置动画 更改状态
                animator.SetBool("Fire", true);
                isFire = true;

                //计算伤害逻辑
                CalAtkType();
            }

        }
        if (Input.GetMouseButtonUp(0) || (bulletCount <= 0 && atkType == 2))
        {
            //设置动画 更改状态
            animator.SetBool("Fire", false);
            isFire = false;
        }
    }

    // 结束换弹音效播放
    private void PlayLoadEnd()
    {
        GameDataMgr.Instance.PlaySound("Music/Gun/LoadEnd", 1.2f);
    }

    /// <summary>
    /// 伤害检测判断
    /// </summary>
    private void CalAtkType()
    {
        // 播放对应武器音效
        GameDataMgr.Instance.PlaySound(heroInfo.soundPath, 0.5f);

        // 狙击枪每发装填音效
        if (heroInfo.id == 5)
        {
            Invoke("PlayLoadEnd", 1f / shootSpeed);
        }

        switch (atkType)
        {
            //刀具类型
            case 1:

                // 根据攻击距离生成球形检测区域
                Collider[] colliders = Physics.OverlapSphere(transform.position + transform.forward * atkDistance / 2 + Vector3.up,
                                                             atkDistance / 1.8f, 1 << LayerMask.NameToLayer("Monster"));
                foreach (Collider collider in colliders)
                {
                    if (collider.GetComponentInParent<MonsterObj>())
                    {
                        //命中怪物计算伤害
                        if (collider.CompareTag("Body"))
                        {
                            //僵尸受伤扣血逻辑
                            collider.GetComponentInParent<MonsterObj>().Wound(atk, false, true);
                            //播放刀具命中音效
                            GameDataMgr.Instance.PlaySound("Music/Knife", 0.7f);
                            //播放命中僵尸特效
                            PlayEff(3, collider.bounds.center + collider.transform.up * 1.2f,
                                    Quaternion.LookRotation(this.transform.position - collider.transform.position),
                                    collider.transform);
                        }
                    }
                }
                break;

            //枪械类型
            case 2:

                // 更新剩余子弹数量
                --bulletCount;
                // 更新子弹数量界面显示
                UIManager.Instance.GetPanel<GamePanel>().ChangeBulletCount(bulletCount);
                // 训练场模式界面显示更新
                if (GameDataMgr.Instance.nowGameMode == GameMode.TrainingMode)
                    UIManager.Instance.GetPanel<GamePanel>().AddFireCount();
                // 播放枪口火花特效
                GameDataMgr.Instance.PlayEff("Eff/GunFireEff", gunFirePoint.position,
                                             Quaternion.LookRotation(gunFirePoint.up),
                                             gunFirePoint.transform);

                // 根据子弹散射大小计算一条随机的射线
                Ray ray = MathCalTool.Instance.CalOffsetFireRay(gunOffset);
                // 应用武器后座力
                Camera.main.GetComponent<ThirdPersonCamera>().ShakeCamera
                    (MathCalTool.Instance.CalFireRecoil(xRecoil, isAiming, crouchValue > 0.5f),
                     MathCalTool.Instance.CalFireRecoil(yRecoil, isAiming, crouchValue > 0.5f));

                // 检测射线碰撞
                if (Physics.Raycast(ray, out RaycastHit hit, atkDistance))
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Monster"))
                    {
                        if (hit.collider.CompareTag("Head"))
                        {
                            //僵尸受伤扣血逻辑
                            hit.collider.gameObject.GetComponentInParent<MonsterObj>().Wound((int)(atk * headAtkRatio), true, true);
                            //播放命中僵尸特效
                            PlayEff(3, hit.point, Quaternion.LookRotation(-ray.direction), hit.collider.transform);

                            // 训练场模式界面显示更新
                            if (GameDataMgr.Instance.nowGameMode == GameMode.TrainingMode)
                                UIManager.Instance.GetPanel<GamePanel>().AddHitCount(true, (int)(atk * headAtkRatio));
                        }
                        else if (hit.collider.CompareTag("Body"))
                        {
                            //僵尸受伤扣血逻辑
                            hit.collider.gameObject.GetComponentInParent<MonsterObj>().Wound(atk, false, true);
                            //播放命中僵尸特效
                            PlayEff(3, hit.point, Quaternion.LookRotation(-ray.direction), hit.collider.transform);

                            // 训练场模式界面显示更新
                            if (GameDataMgr.Instance.nowGameMode == GameMode.TrainingMode)
                                UIManager.Instance.GetPanel<GamePanel>().AddHitCount(false, atk);
                        }
                    }

                    else
                    {
                        //播放命中物体特效
                        if (hit.distance < maxFireEffDistance)
                        {
                            PlayEff(4, hit.point, Quaternion.LookRotation(-ray.direction));
                            //命中物体音效播放
                            if (!ObjectPoolMgr.Instance.HasPool("HitPoint"))
                            {
                                //若无3D音效物体对象池 则注册
                                ObjectPoolMgr.Instance.RegisterPool("HitPoint", Resources.Load<GameObject>("Music/HitPoint"));
                            }
                            GameDataMgr.Instance.PlaySound("Music/Gun/HitPointSound", 0.1f,
                                                ObjectPoolMgr.Instance.Get("HitPoint", hit.point, Quaternion.identity).transform);
                        }
                        else
                        {
                            //子弹最终位置播放 未命中任何东西特效
                            PlayEff(5, ray.origin + ray.direction * maxFireEffDistance, Quaternion.LookRotation(-ray.direction));
                        }
                    }
                }
                else
                {
                    //子弹最终位置播放 未命中任何东西特效
                    PlayEff(5, ray.origin + ray.direction * (atkDistance < maxFireEffDistance ? atkDistance : maxFireEffDistance), Quaternion.LookRotation(-ray.direction));
                }
                break;
        }
    }

    //播放对应命中特效
    private void PlayEff(int type, Vector3 pos, Quaternion quat, Transform parent = null)
    {
        // 3子弹命中僵尸
        // 4子弹命中物体
        // 5子弹未命中
        GameDataMgr.Instance.PlayEff($"Eff/{type}", pos, quat, parent);
    }

    public bool IsFire()
    {
        return isFire;
    }

    /// <summary>
    /// 受伤方法
    /// </summary>
    /// <param name="dmg">伤害值</param>
    public void Wound(int dmg)
    {
        //已经死亡退出逻辑
        if (isDead) return;
        //播放受伤动画
        PlayWoundAnimation();
        //显示受伤图标
        UIManager.Instance.GetPanel<GamePanel>().ShowPlayerHurt();
        //计算伤害
        nowHp -= dmg;
        //更新血条
        UIManager.Instance.GetPanel<GamePanel>().UpdatePlayerHp(nowHp > 0 ? nowHp : 0, maxHp);
        //更新死亡状态
        if (nowHp <= 0) Dead();
    }
    /// <summary>
    /// 死亡方法
    /// </summary>
    private void Dead()
    {
        DeadPlayer();
        //游戏结束逻辑
        SceneLevelMgr.Instance.GameOverLose(1);
    }

    //受伤动画播放
    private void PlayWoundAnimation()
    {
        //若已经处于受伤状态 不再重复播放动画
        if (isBeAtked) return;
        //播放动画并设置权重
        animator.SetTrigger("Wound");
        animator.SetLayerWeight(3, 0.6f);
        //更新受击状态和计时器
        isBeAtked = true;
        nowAtkedTime = atkedTime;
    }

    //受伤状态更新
    private void UpdateWoundState()
    {
        if (isBeAtked)
        {
            nowAtkedTime -= Time.deltaTime;
            if (nowAtkedTime <= 0)
            {
                //恢复受伤动画层权重
                animator.SetLayerWeight(3, 0);
                isBeAtked = false;
            }
        }
    }

    /// <summary>
    /// 游戏失败时外部调用使玩家死亡
    /// </summary>
    public void DeadPlayer()
    {
        //恢复受伤动画层权重
        animator.SetLayerWeight(3, 0);
        //播放死亡动画
        animator.SetBool("Dead", true);
        //更新状态
        isDead = true;
        //结束游戏状态
        GameDataMgr.Instance.isGaming = false;
    }

    //蹲下检测更新
    private void UpdateCrouchState()
    {
        // (跳跃状态/翻滚状态)进行时 不允许改变现有蹲下状态 蹲下时无法跳跃但是可以翻滚
        if (!isJumping && isEndJump && !isRolling)
        {
            // 下蹲按键检测
            if (!fightSettingsData.crouchControlMode)
            {
                isCtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.C);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.C))
                {
                    isCtrlDown = !isCtrlDown;
                }
            }

            // 插值渐变下蹲动画层权重
            if (isCtrlDown)
            {
                crouchValue = Mathf.Lerp(crouchValue, 1, Time.deltaTime * 8);
            }
            else
            {
                crouchValue = Mathf.Lerp(crouchValue, 0, Time.deltaTime * 8);
            }
            // 渐变蹲下动画层权重实现逐渐蹲下效果
            animator.SetLayerWeight(1, crouchValue);
        }
    }
    //打滚检测更新
    private void UpdateRollState()
    {
        nowMinRollTime -= Time.deltaTime;
        // 跳跃状态/换弹状态/冷却时间未到 不允许翻滚
        if (nowMinRollTime > 0 || isJumping || !isEndJump || isReloading) return;
        if (Input.mouseScrollDelta.y > 0)
        {
            animator.SetTrigger("Roll");
            //播放翻滚音效
            PlayerSoundMgr.Instance.ChangeState(E_MoveState.Roll);
            //设置翻滚相关参数
            isRolling = true;
            isForward = true;
            rollTime = rollingTime;
            //重置翻滚冷却时间
            nowMinRollTime = minRollTime;
            //冷却时间UI信息提示
            UIManager.Instance.GetPanel<GamePanel>().RollCdTip(minRollTime);
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            animator.SetTrigger("RollBack");
            //播放翻滚音效
            PlayerSoundMgr.Instance.ChangeState(E_MoveState.Roll);
            //设置翻滚相关参数
            isRolling = true;
            isForward = false;
            rollTime = rollingTime;
            //重置翻滚冷却时间
            nowMinRollTime = minRollTime;
            //冷却时间UI信息提示
            UIManager.Instance.GetPanel<GamePanel>().RollCdTip(minRollTime);
        }
    }
    //应用翻滚位移
    private void RollThePlayer()
    {
        if (rollTime > 0 && isRolling)
        {
            //若处于蹲下状态 翻滚距离减少
            if (isForward)
            {
                characterController.SimpleMove(this.transform.TransformDirection(Vector3.forward) *
                                              (crouchValue > 0.5f ? crouchRollSpeed : normalRollSpeed) *
                                              (moveDirMagnitude > 0.4f && isMovingForward ? moveDirMagnitude : 0.4f));
            }
            //若处于蹲下状态 翻滚距离减少
            else
            {
                characterController.SimpleMove(this.transform.TransformDirection(Vector3.back) *
                                              (crouchValue > 0.5f ? crouchRollSpeed : normalRollSpeed) *
                                              (moveDirMagnitude > 0.4f && !isMovingForward ? moveDirMagnitude : 0.4f));
            }
        }
        else
        {
            isRolling = false;
        }
        rollTime -= Time.deltaTime;
    }

    /// <summary>
    /// 界面显示更新
    /// </summary>
    private void UpdateUIInfo()
    {
        UIManager.Instance.GetPanel<GamePanel>().UpdateMoneyNum(money);
    }
    /// <summary>
    /// 增加关卡金钱
    /// </summary>
    /// <param name="money">关卡金钱</param>
    public void AddMoney(int money)
    {
        this.money += money;
        awardMoney += money;
        UpdateUIInfo();
    }
    /// <summary>
    /// 减少关卡金钱
    /// </summary>
    /// <param name="money">关卡金钱</param>
    public void RemoveMoney(int money)
    {
        this.money -= money;
        UpdateUIInfo();
    }

    //开镜检测更新
    private void UpdateAimingState()
    {
        if (!fightSettingsData.aimControlMode)
        {
            isAimButtonDown = Input.GetMouseButton(1);
        }
        else
        {
            if (Input.GetMouseButtonDown(1))
            {
                isAimButtonDown = !isAimButtonDown;
            }
        }

        // 翻滚状态/换弹状态/跳跃状态 不允许开镜
        if (isAimButtonDown && !isRolling && !isReloading && !isJumping && isEndJump)
        {
            isAiming = true;
            Camera.main.GetComponent<ThirdPersonCamera>().isAiming = true;
        }
        else
        {
            isAiming = false;
            Camera.main.GetComponent<ThirdPersonCamera>().isAiming = false;
        }
    }
    //跳跃检测更新
    private void UpdateJumpState()
    {
        isSpaceDown = Input.GetKeyDown(KeyCode.Space);

        //跳跃输入缓冲机制
        if (isSpaceDown)
        {
            //重置缓冲计数时间
            nowbufferJumpTime = bufferJumpTime;
        }
        else
        {
            nowbufferJumpTime -= Time.deltaTime;
        }

        //土狼时间机制
        if (characterController.isGrounded)
        {
            //重置土狼时间计数
            nowCoyoteJumpTime = coyoteJumpTime;
        }
        else
        {
            nowCoyoteJumpTime -= Time.deltaTime;
        }

        //开镜状态/蹲下状态/翻滚状态/落地僵直状态 不允许进入跳跃状态
        if (!isJumping && nowCoyoteJumpTime > 0 && nowbufferJumpTime > 0 && !isAiming && !isCtrlDown && !isRolling && isEndJump)
        {
            isJumping = true;
            //根据重力公式和期望高度 计算初始向上的速度
            verticalSpeed = Mathf.Sqrt(2 * gravity * jumpHeight);
            animator.SetTrigger("Jump");
            //播放起跳音效
            PlayerSoundMgr.Instance.ChangeState(E_MoveState.Jump);
        }
        if (isJumping)
        {
            //处理跳跃移动 由于Move方法后调用 会覆盖掉SimpleMove方法
            //因此 实现了角色在空中无法SimpleMove移动 符合真实物理表现
            characterController.Move(new Vector3(0, verticalSpeed, 0) * Time.deltaTime);
            verticalSpeed -= gravity * Time.deltaTime;
            if (verticalSpeed < 0 && characterController.isGrounded)
            {
                isJumping = false;
                isEndJump = false;
                animator.SetTrigger("EndJump");
            }
        }
    }

    //动画状态机跳跃落地僵直状态结束事件
    public void EndJumpEvent()
    {
        //播放落地音效
        PlayerSoundMgr.Instance.ChangeState(E_MoveState.Land);
        //重置恢复各项状态
        isEndJump = true;
        animator.ResetTrigger("EndJump");
    }

    //换弹状态更新
    private void UpdateReloadState()
    {
        // 刀具不换弹
        if (atkType == 1) return;
        // 开火状态/翻滚状态/子弹满容量 不允许换弹
        // 按下换弹键/子弹打空 都应该尝试进入换弹流程
        if ((Input.GetKeyDown(KeyCode.R) || (bulletCount <= 0 && atkType == 2))
            && !isFire && !isRolling && bulletCount != heroInfo.bulletCount && !isReloading)
        {
            if (animator.GetCurrentAnimatorStateInfo(2).IsName("Null"))
            {
                //设置开始换弹动画
                animator.SetTrigger("Reload");
                isReloading = true;
                //播放开始换弹音效
                GameDataMgr.Instance.PlaySound("Music/Gun/LoadStart");
            }
        }
    }
    //换弹动画完全结束调用事件
    public void EndReloadEvent()
    {
        //换弹结束
        isReloading = false;
        //补充子弹
        bulletCount = heroInfo.bulletCount;
        //更新子弹数量界面显示
        UIManager.Instance.GetPanel<GamePanel>()?.ChangeBulletCount(bulletCount);
        //播放换弹结束音效
        GameDataMgr.Instance.PlaySound("Music/Gun/LoadEnd", 1.5f);
    }

    //舞蹈状态更新
    private void UpdateDanceState()
    {
        //检测当前是否允许舞蹈 跳跃/开火/开镜/翻滚/换弹/下蹲/移动/受伤 状态 不允许舞蹈
        isAllowDance = !isJumping && isEndJump && !isFire && !isAiming && !isRolling && !isReloading
                                  && crouchValue < 0.3f && moveDirMagnitude < 0.2f && !isBeAtked;
        //获取动画状态机状态是否正在播放移动动画
        isPlayingMove = animator.GetCurrentAnimatorStateInfo(0).IsName("Move");

        if (!isDancing)
        {
            //输入检测
            for (int i = 0; i < 9; ++i)
            {
                if (Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo == null)
                    {
                        UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("默认角色无法使用舞蹈动作", false);
                    }
                    else if (isAllowDance && isPlayingMove)
                    {
                        animator.SetInteger("Dance", i + 1);
                        isDancing = true;
                        weapon.SetActive(false);
                    }
                    else
                    {
                        UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("当前状态无法使用舞蹈动作", false);
                    }
                }
            }
        }
        else
        {
            if (!isAllowDance) EndDanceEvent();
        }
    }
    //结束舞蹈事件
    public void EndDanceEvent()
    {
        animator.SetInteger("Dance", 0);
        isDancing = false;
        weapon.SetActive(true);
    }

    //控制IK脚本是否激活
    private void UpdateIKState()
    {
        // 刀具武器无IK控制 
        if (atkType == 1) return;

        // 有皮肤时 仅在翻滚/舞蹈状态禁用IK
        if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo != null)
        {
            if (isRolling || isDancing)
            {
                playerIKController.enabled = false;
            }
            else
            {
                playerIKController.enabled = true;
            }
            return;
        }

        // 翻滚状态/换弹状态/自由视角状态 禁止IK控制
        if (isRolling || isReloading || Camera.main.GetComponent<ThirdPersonCamera>().IsFreeLook())
        {
            playerIKController.enabled = false;
        }
        else
        {
            playerIKController.enabled = true;
        }
    }

    // 无尽模式彩蛋 增加攻击力
    public void AddAtkRatio(float ratio)
    {
        atk = (int)(atk * ratio);
    }
}
