using UnityEngine;

/// <summary>
/// PUBG风格角色旋转控制器
/// 配合动画根运动系统，根据相机状态控制角色朝向
/// </summary>
public class PlayerRotationController : MonoBehaviour
{
    [Header("旋转参数")]
    [Tooltip("角色旋转速度")]
    public float rotationSpeed = 610f;

    [Header("引用")]
    [Tooltip("相机控制器引用")]
    public ThirdPersonCamera playerCamera;
    [Tooltip("角色模型旋转枢轴点")]
    public Transform modelPivot;

    // 当前目标旋转方向
    private Quaternion targetRotation;
    // 是否处于自由视角模式
    private bool isFreeLook;
    // 是否处于开镜瞄准模式
    private bool isAiming;
    // 是否处于开火状态
    private bool isFire;

    // 进入自由视角前 是否已旋转玩家至正确位置
    private bool isUpdatePlayerRotationBeforeFreeLook;

    void Start()
    {
        // 自动查找主相机上的控制器
        if (playerCamera == null)
            playerCamera = Camera.main.GetComponent<ThirdPersonCamera>();
        // 自动获取当前挂载角色轴心点
        if (modelPivot == null)
        {
            modelPivot = transform;
        }
        // 初始化目标旋转为当前朝向
        if (modelPivot != null)
        {
            targetRotation = modelPivot.rotation;
        }
    }

    void Update()
    {
        //非游戏进行状态 直接结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        // 获取当前相机状态
        isFreeLook = playerCamera != null && playerCamera.IsFreeLook();
        // 获取开镜与否状态
        isAiming = playerCamera.GetComponent<ThirdPersonCamera>().isAiming;
        // 获取开火与否状态
        isFire = GameDataMgr.Instance.nowPlayerObj.IsFire();

        // 根据相机状态更新角色旋转
        UpdateCharacterRotation();
    }

    /// <summary>
    /// 更新角色旋转逻辑
    /// </summary>
    void UpdateCharacterRotation()
    {
        if (modelPivot == null) return;

        // 非自由视角模式：角色始终面向相机方向
        if (!isFreeLook && playerCamera != null || isAiming || isFire)
        {
            // 获取相机的水平旋转（忽略俯仰角）
            targetRotation = playerCamera.GetCameraYRotation();
        }

        // 应用平滑旋转
        modelPivot.rotation = Quaternion.RotateTowards(modelPivot.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 自由视角前瞬间更正旋转
        if (isFreeLook)
        {
            if (isUpdatePlayerRotationBeforeFreeLook) return;
            else
            {
                modelPivot.rotation = targetRotation;
                isUpdatePlayerRotationBeforeFreeLook = true;
            }
        }
        else isUpdatePlayerRotationBeforeFreeLook = false;
    }

    /// <summary>
    /// 重置旋转参数为默认值
    /// </summary>
    [ContextMenu("重置旋转参数")]
    public void ResetRotationParameters()
    {
        rotationSpeed = 650f;
        Debug.Log("旋转参数已重置为默认值");
    }
}