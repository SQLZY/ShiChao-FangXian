using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class BasePanel : MonoBehaviour
{
    //整体控制透明度组件
    private CanvasGroup canvasGroup;
    //淡入淡出速度
    private float alphaSpeed = 6;
    //是否显示
    private bool isShow;

    //淡出隐藏面板后回调函数
    private UnityAction hideCallBack;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        Init();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //淡入
        if (isShow && canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += alphaSpeed * Time.deltaTime;
        }
        //淡出
        if (!isShow && canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= alphaSpeed * Time.deltaTime;
            if (canvasGroup.alpha == 0)
            {
                hideCallBack?.Invoke();
            }
        }
    }

    /// <summary>
    /// 初始化注册控件事件方法
    /// </summary>
    protected abstract void Init();

    public virtual void ShowMe()
    {
        isShow = true;
        canvasGroup.alpha = 0;
    }

    public virtual void HideMe(UnityAction hideCallBack)
    {
        // 禁用Button 防止连点
        StartCoroutine(DisableAllButtonCoroutine());

        this.hideCallBack = hideCallBack;
        isShow = false;
        canvasGroup.alpha = 1;
    }

    /// <summary>
    /// 禁用全部Button按钮协程
    /// </summary>
    IEnumerator DisableAllButtonCoroutine()
    {
        yield return null;
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button button in buttons) button.interactable = false;
    }
}
