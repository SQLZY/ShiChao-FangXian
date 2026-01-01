using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconFollowTarget : MonoBehaviour, IResetState
{
    //跟随目标
    public Transform followTarget;
    //图标高度偏移
    public float heightOffset = 0;
    //是否限定显示在屏幕区域内
    public bool isMustInScreen = true;
    //屏幕坐标
    private Vector3 screenPos;

    public void ResetState()
    {
        followTarget = null;
        screenPos = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        //无跟随目标 直接结束逻辑
        if (followTarget == null) return;
        //更新图标位置
        UpdateIconPosition();
    }

    //更新图标位置
    public void UpdateIconPosition()
    {
        //更新屏幕坐标
        screenPos = Camera.main.WorldToScreenPoint(followTarget.position + Vector3.up * heightOffset);

        //正确显示后方Icon屏幕位置
        if (screenPos.z < 0)
        {
            screenPos.x *= -1;
            screenPos.y *= -1;
        }

        if (isMustInScreen)
        {
            //限制图标在视野范围内
            screenPos.x = Mathf.Clamp(screenPos.x, Screen.width * 0.1f, Screen.width * 0.9f);
            screenPos.y = Mathf.Clamp(screenPos.y, Screen.height * 0.1f, Screen.height * 0.9f);
        }

        //屏幕坐标转UI坐标
        RectTransformUtility.ScreenPointToWorldPointInRectangle((this.transform as RectTransform), screenPos, null, out Vector3 worldPoint);
        //设置UI坐标
        transform.position = worldPoint;
    }
}
