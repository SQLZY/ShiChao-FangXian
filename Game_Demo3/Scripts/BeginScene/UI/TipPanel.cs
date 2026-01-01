using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TipPanel : BasePanel
{
    //确认与取消按钮
    public Button btnYes;
    public Button btnNo;
    //提示信息
    public Text txtTipInfo;
    //不同按钮的回调函数
    private UnityAction<bool> clickCallBack;

    protected override void Init()
    {
        btnYes.onClick.AddListener(() =>
        {
            if (clickCallBack == null)
                UIManager.Instance.HidePanel<TipPanel>();
            else
                clickCallBack?.Invoke(true);
        });
        btnNo.onClick.AddListener(() =>
        {
            if (clickCallBack == null)
                UIManager.Instance.HidePanel<TipPanel>();
            else
                clickCallBack?.Invoke(false);
        });
    }
    /// <summary>
    /// 初始化信息方法
    /// </summary>
    /// <param name="tip">提示信息</param>
    /// <param name="isOneBtn">是否为一个按钮 true一个 false两个</param>
    public void InitInfo(string tip, bool isOneBtn = true)
    {
        txtTipInfo.text = tip;
        if (isOneBtn)
        {
            btnNo.gameObject.SetActive(false);
            Vector3 pos = btnYes.GetComponent<RectTransform>().localPosition;
            btnYes.GetComponent<RectTransform>().localPosition = new Vector3(0, pos.y, pos.z);
        }
    }
    /// <summary>
    /// 传入回调函数
    /// </summary>
    /// <param name="unityAction">回调函数</param>
    public void InitAction(UnityAction<bool> unityAction)
    {
        clickCallBack = unityAction;
    }
    /// <summary>
    /// 清除回调函数
    /// </summary>
    public void ClearAction()
    {
        clickCallBack = null;
    }

    public override void ShowMe()
    {
        base.ShowMe();
        Cursor.lockState = CursorLockMode.None;
    }

    public override void HideMe(UnityAction hideCallBack)
    {
        if (GameDataMgr.Instance.isGaming) Cursor.lockState = CursorLockMode.Locked;
        base.HideMe(hideCallBack);
    }
}
