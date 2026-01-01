using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// PUBG风格第三人称相机控制器
/// 实现角色在屏幕左侧显示、自由视角切换和中心准星
/// </summary>
[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("核心参数")]
    [Tooltip("相机与玩家之间的距离")]
    [Range(1f, 10f)] public float distance = 4f;
    [Tooltip("相机枢轴点相对于玩家的高度")]
    [Range(1f, 5f)] public float height = 2;
    [Tooltip("角色在屏幕中的水平偏移（正值=角色在左侧）")]
    [Range(-1f, 1f)] public float horizontalOffset = 0.6f;
    [Tooltip("垂直视角限制范围")]
    public Vector2 verticalClamp = new Vector2(-50f, 75f);
    [Tooltip("普通模式下的旋转速度")]
    public float rotationSpeed = 180f;
    [Tooltip("自由视角下的旋转速度")]
    public float freeLookRotationSpeed = 120f;
    [Tooltip("相机跟随平滑度")]
    public float followSharpness = 50f;
    [Tooltip("注视点高度比例（相对于相机枢轴高度）")]
    [Range(0.1f, 1f)] public float lookAtHeightRatio = 0.725f;

    [Header("高级参数")]
    [Tooltip("自由视角下的距离缩放")]
    [Range(0.4f, 1f)] public float freeLookDistanceMultiplier = 0.84f;
    [Tooltip("瞄准状态下的距离缩放")]
    [Range(0.4f, 1f)] public float aimingDistanceMultiplier = 0.5f;
    [Tooltip("遮挡状态下的最小距离缩放")]
    [Range(0.4f, 1f)] public float obstacleDistanceMultiplier = 0.6f;
    [Tooltip("准星射线最大检测距离")]
    public float crosshairRayDistance = 1000f;

    [Tooltip("所有需要检测的环境遮挡物层级")]
    public LayerMask layers;

    // 私有变量
    private Vector3 currentRotation;        // 当前相机旋转角度
    private bool isFreeLook;                // 是否处于自由视角模式
    private float originalDistance;         // 初始相机距离
    private float targetCameraDistance;     // 目标相机距离
    private Vector3 lookAtOffset;           // 注视点偏移量
    private Vector3 cameraTargetPosition;   // 每帧计算更新后的相机目标位置
    private int noObstacleCount;            // 没有障碍物的每帧检测计数

    private Transform playerTarget;          // 玩家目标位置
    private Transform cameraPivot;           // 相机枢轴点

    private float xRecoilSpeed;             // 目标水平后座力速度
    private float nowXRecoilSpeed;          // 当前水平后座力速度
    private float yRecoilSpeed;             // 目标垂直后座力速度
    private float nowYRecoilSpeed;          // 当前垂直后座力速度
    private float recoilStayTime = 0.3f;    // 后座力持续时间
    private float nowRecoilStayTime;        // 后座力持续时间计时器

    private float nowLookAtHeightRatio;     // 当前视角高度系数
    private float targetLookAtHeightRatio;  // 目标视角高度系数
    private float nowShakeCameraOffset;     // 玩家状态(下蹲/落地瞬间)导致的相机高度偏移当前值 正数为向下偏移
    private float targetShakeCameraOffset;  // 相机高度偏移目标值 正数为向下偏移

    // 公有变量
    public bool isAiming;                   // 是否处于瞄准状态


    void Start()
    {
        // 寻找场景中的玩家
        if (playerTarget == null)
            playerTarget = GameDataMgr.Instance.nowPlayerObj?.transform;

        // 确保相机枢轴点存在
        if (cameraPivot == null)
        {
            CreateCameraPivot();
        }

        // 初始化变量
        currentRotation = Vector3.zero;
        originalDistance = distance;

        // 初始化遮挡物检测层级
        layers = -1;
        // 剔除 玩家/怪物/UI 相关层
        layers &= ~(1 << LayerMask.NameToLayer("Player")) &
                  ~(1 << LayerMask.NameToLayer("Monster")) &
                  ~(1 << LayerMask.NameToLayer("Weapon")) &
                  ~(1 << LayerMask.NameToLayer("UI")) &
                  ~(1 << LayerMask.NameToLayer("UIMapIcon"));

        // 初始化相机视距
        this.GetComponent<Camera>().nearClipPlane = GameDataMgr.Instance.nowGameMode == GameMode.TrainingMode ? 0.4f : 0.7f;
        this.GetComponent<Camera>().farClipPlane = 300f;
        // 初始化相机视野
        this.GetComponent<Camera>().fieldOfView = 50f;

        // 锁定鼠标到屏幕中心
        Cursor.lockState = CursorLockMode.Locked;

        // 计算初始注视点偏移
        RecalculateLookAtOffset();
    }

    /// <summary>
    /// 创建相机枢轴点
    /// </summary>
    void CreateCameraPivot()
    {
        cameraPivot = new GameObject("Camera Pivot").transform;
        cameraPivot.SetParent(playerTarget);
        cameraPivot.localPosition = Vector3.up * height;
    }

    /// <summary>
    /// 重新计算注视点偏移量（关键修正）
    /// </summary>
    void RecalculateLookAtOffset()
    {
        // 关键：创建水平偏移使角色显示在屏幕左侧
        lookAtOffset = new Vector3(horizontalOffset, 0, 0);
    }

    void LateUpdate()
    {
        // 寻找场景中的玩家
        if (playerTarget == null)
            playerTarget = GameDataMgr.Instance.nowPlayerObj?.transform;
        // 确保相机枢轴点存在
        if (cameraPivot == null)
            CreateCameraPivot();

        if (playerTarget == null || cameraPivot == null) return;

        // 检测ALT键切换自由视角模式
        if (!GameDataMgr.Instance.FightSettingsData.freeControlMode)
        {
            isFreeLook = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            {
                isFreeLook = !isFreeLook;
            }
        }
        // 瞄准状态/开火状态 退出自由视角
        if (isAiming || GameDataMgr.Instance.nowPlayerObj && GameDataMgr.Instance.nowPlayerObj.IsFire()) isFreeLook = false;


        // 不同状态下调整适应相机距离

        // 1.处于瞄准状态
        if (isAiming)
            targetCameraDistance = originalDistance * aimingDistanceMultiplier;
        // 2.存在遮挡障碍物状态(利用球体投射检测)
        else if (Physics.SphereCast(cameraPivot.position, 0.3f, (cameraTargetPosition - cameraPivot.position).normalized,
                                    out RaycastHit hitInfo, originalDistance, layers))
        {
            // 有障碍物检测计数
            --noObstacleCount;
            noObstacleCount = Mathf.Clamp(noObstacleCount, -20, 0);
            // 连续3帧检测到有障碍物 且 距离差值较大时 才更新相机目标距离 避免相机抖动
            if (noObstacleCount < -3 && Mathf.Abs(targetCameraDistance - hitInfo.distance) > 0.1f)
            {
                targetCameraDistance = Mathf.Clamp(hitInfo.distance,
                                                   originalDistance * obstacleDistanceMultiplier,
                                                   originalDistance);
            }
        }
        // 3.处于自由视角状态
        else if (isFreeLook)
            targetCameraDistance = originalDistance * freeLookDistanceMultiplier;
        // 4.常规状态 恢复原始相机距离
        else
        {
            // 无障碍物检测帧计数
            ++noObstacleCount;
            noObstacleCount = Mathf.Clamp(noObstacleCount, 0, 20);
            // 连续3帧确定无障碍物 才恢复相机目标距离 避免相机抖动
            if (noObstacleCount > 3)
                targetCameraDistance = originalDistance;
        }

        // 应用相机距离
        distance = Mathf.Lerp(distance, targetCameraDistance, Time.deltaTime * 12);

        //// 更新注视点偏移
        //if (Mathf.Abs(horizontalOffset) > 0.01f)
        //{
        //    RecalculateLookAtOffset();
        //}

        // 应用并计时重置后座力速度
        ApplyAndResetRecoilSpeed();

        // 更新相机注视点高度系数 更新相机高度偏移值
        UpdateLookAtHeightRatio();

        // 获取鼠标输入
        Vector2 mouseInput = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );

        // 视角旋转控制
        float speed;
        // 不在游戏进行状态 不应用鼠标旋转
        if (!GameDataMgr.Instance.isGaming)
        {
            speed = 0;
        }
        // 游戏进行状态 根据是否自由视角模式改变视角旋转速度
        else
        {
            speed = isFreeLook ? freeLookRotationSpeed : rotationSpeed;
        }

        currentRotation.x += mouseInput.x * speed * Time.deltaTime;
        // 限制Y轴最大抬头低头角度
        currentRotation.y = Mathf.Clamp(
            currentRotation.y - mouseInput.y * speed * Time.deltaTime,
            verticalClamp.x,
            verticalClamp.y
        );

        // 根据欧拉角计算旋转四元数
        Quaternion rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);

        // 计算相机目标位置（相机后方）
        Vector3 baseOffset = rotation * Vector3.back * distance;
        cameraTargetPosition = cameraPivot.position + baseOffset + Vector3.down * nowShakeCameraOffset;

        // 应用相机位置
        transform.position = Vector3.Lerp(transform.position, cameraTargetPosition, followSharpness * Time.deltaTime);

        // 关键修正：设置偏移注视点
        Vector3 lookAtPoint = cameraPivot.position +
                              playerTarget.TransformDirection(lookAtOffset) +
                              Vector3.up * height * (nowLookAtHeightRatio - 0.5f);

        // 使相机注视偏移点
        transform.LookAt(lookAtPoint);
    }

    /// <summary>
    /// 获取相机的水平旋转角度（仅Y轴）
    /// </summary>
    public Quaternion GetCameraYRotation()
    {
        return Quaternion.Euler(0, currentRotation.x, 0);
    }

    /// <summary>
    /// 获取当前是否为自由视角模式
    /// </summary>
    public bool IsFreeLook()
    {
        return isFreeLook;
    }

    /// <summary>
    /// 后座力应用
    /// </summary>
    /// <param name="xRecoil">水平后座力</param>
    /// <param name="yRecoil">垂直后座力</param>
    public void ShakeCamera(float xRecoil, float yRecoil)
    {
        //随机水平后座力方向
        if (Random.Range(0, 2) == 0) xRecoil *= -1;
        //后座力基准系数(水平系数2.5f 垂直系数7.0f)调整
        xRecoil *= 2.5f;
        yRecoil *= 7.5f;
        //应用水平后座力
        xRecoilSpeed = xRecoil;
        //应用垂直后座力
        yRecoilSpeed = yRecoil;
        //重置后座力时间
        nowRecoilStayTime = recoilStayTime;
    }

    // 应用并计时重置后座力速度
    private void ApplyAndResetRecoilSpeed()
    {
        // 渐变后座力速度大小
        nowXRecoilSpeed = Mathf.Lerp(nowXRecoilSpeed, xRecoilSpeed, Time.deltaTime * 0.8f);
        nowYRecoilSpeed = Mathf.Lerp(nowYRecoilSpeed, yRecoilSpeed, Time.deltaTime * 0.8f);

        if (nowRecoilStayTime > 0)
        {
            // 后座力缓冲时间计时
            nowRecoilStayTime -= Time.deltaTime;
            // 应用后座力旋转
            currentRotation.x += nowXRecoilSpeed * Time.deltaTime;
            currentRotation.y -= nowYRecoilSpeed * Time.deltaTime;
        }
        else
        {
            // 完全重置后座力
            xRecoilSpeed = 0;
            yRecoilSpeed = 0;
            nowXRecoilSpeed = 0;
            nowYRecoilSpeed = 0;
        }
    }

    // 视角高度控制更新
    private void UpdateLookAtHeightRatio()
    {
        // 目标视角高度系数
        targetLookAtHeightRatio = lookAtHeightRatio;
        // 目标相机高度向下偏移量
        targetShakeCameraOffset = 0;

        if (!GameDataMgr.Instance.nowPlayerObj) return;

        // 下蹲状态 降低目标高度系数 增加高度向下偏移值
        if (GameDataMgr.Instance.nowPlayerObj.crouchValue > 0.5f)
        {
            targetLookAtHeightRatio *= GameDataMgr.Instance.PlayerData.nowSelSkinInfo != null ? 0.08f : 0.68f;
            targetShakeCameraOffset += GameDataMgr.Instance.PlayerData.nowSelSkinInfo != null ? 1.2f : 0.9f;
        }

        // 落地瞬间 降低目标高度系数 增加高度向下偏移值
        if (!GameDataMgr.Instance.nowPlayerObj.isEndJump)
        {
            targetLookAtHeightRatio *= 0.5f;
            targetShakeCameraOffset += 1.8f;
        }

        // 插值运算渐变目标高度系数
        nowLookAtHeightRatio = Mathf.Lerp(nowLookAtHeightRatio, targetLookAtHeightRatio, Time.deltaTime * 5f);
        nowShakeCameraOffset = Mathf.Lerp(nowShakeCameraOffset, targetShakeCameraOffset, Time.deltaTime * 5f);
    }

    /// <summary>
    /// 重置所有参数为默认值（在Inspector中调用）
    /// </summary>
    [ContextMenu("重置为默认值")]
    public void ResetToDefaults()
    {
        distance = 4f;
        height = 2f;
        horizontalOffset = 0.6f;
        verticalClamp = new Vector2(-50f, 75f);
        rotationSpeed = 180f;
        freeLookRotationSpeed = 120f;
        followSharpness = 50f;
        lookAtHeightRatio = 0.725f;
        freeLookDistanceMultiplier = 0.84f;
        aimingDistanceMultiplier = 0.5f;
        obstacleDistanceMultiplier = 0.6f;
        crosshairRayDistance = 1000f;

        // 重新计算偏移
        RecalculateLookAtOffset();
    }

    /// <summary>
    /// 有皮肤状态下重置参数
    /// </summary>
    public void ResetToDefaultsWithSkin()
    {
        distance = 4f;
        height = 2f;
        horizontalOffset = 0.55f;
        verticalClamp = new Vector2(-50f, 75f);
        rotationSpeed = 180f;
        freeLookRotationSpeed = 120f;
        followSharpness = 50f;
        lookAtHeightRatio = 0.335f;
        freeLookDistanceMultiplier = 0.84f;
        aimingDistanceMultiplier = 0.5f;
        obstacleDistanceMultiplier = 0.6f;
        crosshairRayDistance = 1000f;

        // 重新计算偏移
        RecalculateLookAtOffset();
    }
}