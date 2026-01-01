using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharactarAnimatorUpdateController : MonoBehaviour
{
    // 动画状态机帧更新频率
    private int animatorUpdateFrequency;
    // 上一次的帧更新频率
    private int frontAnimatorUpdateFrequency;
    // 更新帧偏移
    private int frameOffset;

    // 玩家位置
    private Transform playerTarget;
    // 自身位置参照物 若为空则使用自身Transform位置
    public Transform posTarget;
    // 动画状态机
    private Animator animator;
    // 质量设置检查时间间隔
    private float checkTimeOffset = 1.5f;
    // 下次检测时间
    private float nextCheckTime;
    // 玩家相机与自身的距离
    private float distance;

    // A质量距离
    private float lodDistance1 = 12f;
    // B质量距离
    private float lodDistance2 = 24f;
    // C质量距离
    private float lodDistance3 = 36f;

    // Start is called before the first frame update
    void Start()
    {
        // 初始化组件
        playerTarget = Camera.main.transform;
        animator = GetComponent<Animator>();

        // 禁用自动更新
        animator.enabled = false;

        // 相同位置目标角色动画同一帧更新 确保同步
        if (posTarget) frameOffset = 0;
        // 其余角色随机更新帧偏移 避免角色在同一帧更新动画
        else frameOffset = Random.Range(0, 20);

        // 设置默认A质量
        SetALevelQuality();
    }

    // Update is called once per frame
    void Update()
    {
        // 检测玩家距离设置动画质量
        UpdateAnimatorQuality();
        // 根据动画帧更新频率更新动画
        UpdateAnimatorState();
    }

    // 检测玩家距离设置动画质量
    private void UpdateAnimatorQuality()
    {
        if (Time.time > nextCheckTime)
        {
            // 记录上次的更新频率
            frontAnimatorUpdateFrequency = animatorUpdateFrequency;
            // 计算玩家距离
            if (posTarget) distance = Vector3.Distance(posTarget.position, playerTarget.position);
            else distance = Vector3.Distance(transform.position, playerTarget.position);

            // 根据玩家距离设置动画质量
            if (distance < lodDistance1)
            {
                SetALevelQuality();
            }
            else if (distance < lodDistance2)
            {
                SetBLevelQuality();
            }
            else if (distance < lodDistance3)
            {
                SetCLevelQuality();
            }
            else
            {
                SetDLevelQuality();
            }

            // 如果更新帧频率改变 立刻同步动画状态机
            if (animatorUpdateFrequency != frontAnimatorUpdateFrequency)
            {
                // 立刻同步状态
                animator.Update(0f);
            }

            // 计算下次更新动画质量时间
            nextCheckTime = Time.time + checkTimeOffset;
        }
    }

    // 根据动画帧更新频率更新动画
    private void UpdateAnimatorState()
    {
        if ((Time.frameCount + frameOffset) % animatorUpdateFrequency == 0)
        {
            animator.Update(Time.deltaTime * animatorUpdateFrequency);
        }
    }

    // A质量
    private void SetALevelQuality()
    {
        animatorUpdateFrequency = 1;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        animator.speed = 1.0f;
    }
    // B质量
    private void SetBLevelQuality()
    {
        animatorUpdateFrequency = 2;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        animator.speed = 1.0f;
    }
    // C质量
    private void SetCLevelQuality()
    {
        animatorUpdateFrequency = 4;
        animator.cullingMode = AnimatorCullingMode.CullCompletely;
        animator.speed = 0.8f;
    }
    // D质量
    private void SetDLevelQuality()
    {
        animatorUpdateFrequency = 10;
        animator.cullingMode = AnimatorCullingMode.CullCompletely;
        animator.speed = 0f;
    }
}
