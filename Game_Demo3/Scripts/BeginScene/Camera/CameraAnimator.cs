using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CameraAnimator : MonoBehaviour
{
    private Animator cameraAnimator;
    //动画结束后执行的函数逻辑
    private UnityAction overAction;

    // Start is called before the first frame update
    void Start()
    {
        cameraAnimator = GetComponent<Animator>();
    }

    /// <summary>
    /// 摄像机播放左转右转动画函数
    /// </summary>
    /// <param name="overAction">动画播放结束后执行的逻辑函数</param>
    /// <param name="isLeft">是否左转 true左转 flase右转</param>
    public void TurnLeftOrRight(UnityAction overAction, bool isLeft)
    {
        this.overAction = overAction;
        if (isLeft)
        {
            cameraAnimator.SetTrigger("Left");
        }
        else
        {
            cameraAnimator.SetTrigger("Right");
        }
    }

    /// <summary>
    /// 摄像机播放向上向下动画函数
    /// </summary>
    /// <param name="overAction">动画播放结束后执行的逻辑函数</param>
    /// <param name="isUp">是否向上 true向上 flase向下</param>
    public void TurnUpOrDown(UnityAction overAction, bool isUp)
    {
        this.overAction = overAction;
        if (isUp)
        {
            cameraAnimator.SetTrigger("Up");
        }
        else
        {
            cameraAnimator.SetTrigger("Down");
        }
    }

    /// <summary>
    /// 摄像机播放向远向近动画函数
    /// </summary>
    /// <param name="overAction">动画播放结束后执行的逻辑函数</param>
    /// <param name="isFar">是否向远 true向远 flase向近</param>
    public void TurnFarOrClose(UnityAction overAction, bool isFar)
    {
        this.overAction = overAction;
        if (isFar)
        {
            cameraAnimator.SetTrigger("Far");
        }
        else
        {
            UIManager.Instance.HidePanel<BeginPanel>(false);
            cameraAnimator.Play("TurnClose");
            cameraAnimator.Update(0f);
        }
    }

    //动画播放完毕后调用
    public void PlayOver()
    {
        overAction?.Invoke();
        overAction = null;
    }
}
