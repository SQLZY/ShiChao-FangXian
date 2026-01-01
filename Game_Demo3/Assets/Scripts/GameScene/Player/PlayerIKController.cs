using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIKController : MonoBehaviour
{
    //玩家看向目标点
    private Vector3 target;
    //玩家当前目标点
    private Vector3 nowTarget;
    //持枪右手瞄准点目标
    private Vector3 rightHandTarget;
    //持枪右手当前瞄准点
    private Vector3 nowRightHandTarget;
    //持枪左手瞄准点目标
    private Vector3 leftHandTarget;
    //持枪左手当前瞄准点
    private Vector3 nowLeftHandTarget;
    //手部IK点向右偏移量 根据武器类型进行配置
    public float rightOffset;
    //开镜状态手部IK点向右偏移量 根据武器类型进行配置
    public float rightAimOffset;
    //手部IK点向上偏移量 根据武器类型进行配置
    public float upOffset;
    //手部IK点瞄准距离 根据武器类型进行配置
    public float aimDistance;
    //左手目标点相对右手偏移量 根据武器类型进行配置
    public float leftHandOffset;
    //开镜状态左手目标点相对右手偏移量 根据武器类型进行配置
    public float leftHandAimOffset;

    //左手IK目标点物体
    public GameObject leftHandTargetObj;
    //右手IK目标点物体
    public GameObject rightHandTargetObj;

    //测试代码
    //有皮肤时左手偏移量
    private Vector2 leftHandSkinOffset = Vector2.one * -0.1f;
    //有皮肤时右手偏移量
    private Vector2 rightHandSkinOffset = Vector2.one * 0.4f;

    //关联动画状态机
    private Animator animator;
    //设置各个部位权重和转向限制
    private float bodyWeight = 0.6f;
    private float headWeight = 0.7f;
    private float eyesWeight = 0.0f;
    private float clampWeight = 0.65f;
    private float handIKWeight = 0.35f;
    private float nowHandIKWeight = 0;
    //用于记录临时IK权重
    private float startValue;

    //屏幕中心点射线
    private Ray ray;

    //开镜过渡效果计时器
    private float nowTime;
    //开镜预期总用时
    private float aimTime = 0.15f;
    //开镜渐变状态 true表示处于开镜状态
    private bool nowState;

    //进入自由视角前 是否已经紧急更新手部IK位置
    private bool isUpdateIKBeforeFreeLook;


    void Start()
    {
        animator = GetComponent<Animator>();
        InitOffset();
        //初始化左右手IK目标点物体
        leftHandTargetObj = new GameObject("playerLeftHandTargetObj");
        rightHandTargetObj = new GameObject("playerRightHandTargetObj");
        //设置玩家坐标为父物体
        leftHandTargetObj.transform.parent = transform;
        rightHandTargetObj.transform.parent = transform;
    }

    // Update is called once per frame
    void Update()
    {
        //非游戏进行状态 直接结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        //插值运算更改IK目标点
        nowLeftHandTarget = Vector3.Lerp(nowLeftHandTarget, leftHandTargetObj.transform.position, Time.deltaTime * 10);
        nowRightHandTarget = Vector3.Lerp(nowRightHandTarget, rightHandTargetObj.transform.position, Time.deltaTime * 10);
        //位置相差过大时直接应用目标IK点
        if (Vector3.Distance(nowLeftHandTarget, leftHandTargetObj.transform.position) > 5f)
            nowLeftHandTarget = leftHandTargetObj.transform.position;
        if (Vector3.Distance(nowRightHandTarget, rightHandTargetObj.transform.position) > 5f)
            nowRightHandTarget = rightHandTargetObj.transform.position;

        //自由视角模式下 不再根据视角更新IK点位置
        if (Camera.main.GetComponent<ThirdPersonCamera>().IsFreeLook())
        {
            if (isUpdateIKBeforeFreeLook)
            {
                return;
            }
            else
            {
                nowLeftHandTarget = leftHandTargetObj.transform.position;
                nowRightHandTarget = rightHandTargetObj.transform.position;
                isUpdateIKBeforeFreeLook = true;
            }
        }
        else isUpdateIKBeforeFreeLook = false;

        //头部相关逻辑
        ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 200) && hit.distance > Camera.main.GetComponent<ThirdPersonCamera>().distance)
        {
            target = hit.point;
        }
        else
        {
            target = ray.origin + ray.direction * 200;
        }
        nowTarget = Vector3.Lerp(nowTarget, target, Time.deltaTime * 12f);


        //手部相关逻辑
        //计算瞄准状态的IK点
        if (Camera.main.GetComponent<ThirdPersonCamera>().isAiming)
        {
            rightHandTarget = ray.origin + ray.direction * aimDistance + this.transform.up * upOffset + this.transform.right * rightAimOffset;
            leftHandTarget = rightHandTarget + this.transform.right * leftHandAimOffset;

            //测试代码 有皮肤时 应用IK偏移量
            if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo != null)
            {
                rightHandTarget += this.transform.right * (rightHandSkinOffset.x + 1.1f) + this.transform.up * rightHandSkinOffset.y;
                leftHandTarget += this.transform.right * (leftHandSkinOffset.x - 0.3f) + this.transform.up * (leftHandSkinOffset.y - 0.2f);
            }
        }
        //计算默认状态的IK点
        else
        {
            rightHandTarget = ray.origin + ray.direction * aimDistance + this.transform.up * upOffset + this.transform.right * rightOffset;
            leftHandTarget = rightHandTarget + this.transform.right * leftHandOffset;

            //测试代码 有皮肤时 应用IK偏移量
            if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo != null)
            {
                rightHandTarget += this.transform.right * rightHandSkinOffset.x + this.transform.up * rightHandSkinOffset.y;
                leftHandTarget += this.transform.right * leftHandSkinOffset.x + this.transform.up * leftHandSkinOffset.y;
            }
        }

        //设置IK点物体位置
        leftHandTargetObj.transform.position = leftHandTarget;
        rightHandTargetObj.transform.position = rightHandTarget;

        if (Camera.main.GetComponent<ThirdPersonCamera>().isAiming || GameDataMgr.Instance.nowPlayerObj.IsFire())
        {
            // 开镜/开火 IK权重渐变逻辑
            if (!nowState)
            {
                nowTime = 0;
                nowState = true;
                startValue = nowHandIKWeight;
            }
            nowTime += Time.deltaTime;
            float t = Mathf.Clamp01(nowTime / aimTime);
            nowHandIKWeight = Mathf.SmoothStep(startValue, handIKWeight, t);
        }
        else
        {
            // 非战斗状态 IK权重渐变逻辑
            if (nowState)
            {
                nowTime = 0;
                nowState = false;
                startValue = nowHandIKWeight;
            }
            nowTime += Time.deltaTime;
            float t = Mathf.Clamp01(nowTime / aimTime);
            nowHandIKWeight = Mathf.SmoothStep(startValue, 0, t);
        }

        // 测试代码 有皮肤时 手部IK永远生效
        if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo != null) nowHandIKWeight = handIKWeight;
    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            //头部设置
            animator.SetLookAtWeight(1, bodyWeight, headWeight, eyesWeight, clampWeight);
            animator.SetLookAtPosition(nowTarget);
            //右手设置
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, nowHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, nowRightHandTarget);
            //左手设置
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, nowHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, nowLeftHandTarget);
        }
    }

    public void InitOffset()
    {
        HeroInfo heroInfo = GameDataMgr.Instance.nowSelHero;
        rightOffset = heroInfo.rightOffset;
        rightAimOffset = heroInfo.rightAimOffset;
        upOffset = heroInfo.upOffset;
        leftHandOffset = heroInfo.leftHandOffset;
        leftHandAimOffset = heroInfo.leftHandAimOffset;
        aimDistance = heroInfo.aimDistance;
    }
}
