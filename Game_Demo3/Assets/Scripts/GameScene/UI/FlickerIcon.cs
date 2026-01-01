using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickerIcon : MonoBehaviour, IResetState
{
    //闪烁速度
    public float flickerSpeed = 4f;
    //获取组件
    private CanvasGroup canvasGroup;
    private bool isShowing;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isShowing)
        {
            canvasGroup.alpha += flickerSpeed * Time.deltaTime;
            if (canvasGroup.alpha == 1)
            {
                isShowing = false;
            }
        }
        else
        {
            canvasGroup.alpha -= flickerSpeed * Time.deltaTime;
            if (canvasGroup.alpha <= 0.25f)
            {
                isShowing = true;
            }
        }
    }

    public void ResetState()
    {
        //每次对象池回收重置状态
        canvasGroup.alpha = 1;
    }
}
