using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AimStarPanel : BasePanel
{
    //准星图片
    public Image centre;
    public Image up;
    public Image down;
    public Image left;
    public Image right;
    //命中提示
    public GameObject hitTip;
    //命中提示滞留时间
    private float hitTipTime = 0.25f;
    //命中提示显示计时器
    private float hitTipTimeCount;

    //最大扩散程度系数
    public float maxDegreeRatio = 5f;

    //目标扩散程度
    [Range(0f, 100f)]
    public float targetOpenDegree = 0;

    //准星变化速度
    public float changeSpeed = 10f;

    //目标扩散位置
    private Vector2 upTarget;
    private Vector2 downTarget;
    private Vector2 leftTarget;
    private Vector2 rightTarget;
    //初始扩散位置
    private Vector2 basicUpPos;
    private Vector2 basicDownPos;
    private Vector2 basicLeftPos;
    private Vector2 basicRightPos;

    //战斗相关设置
    private FightSettingsData fightSettings;

    protected override void Init()
    {
        basicUpPos = (up.transform as RectTransform).localPosition;
        basicDownPos = (down.transform as RectTransform).localPosition;
        basicLeftPos = (left.transform as RectTransform).localPosition;
        basicRightPos = (right.transform as RectTransform).localPosition;
        //关联战斗相关设置
        fightSettings = GameDataMgr.Instance.FightSettingsData;

        //全部准星图片
        List<Image> images = new List<Image>() { centre, up, down, left, right };
        //设置准星颜色
        switch (fightSettings.aimStarColor)
        {
            case 1:
                foreach (Image image in images) image.color = Color.white;
                break;
            case 2:
                foreach (Image image in images) image.color = Color.red;
                break;
            case 3:
                foreach (Image image in images) image.color = Color.green;
                break;
            case 4:
                foreach (Image image in images) image.color = Color.blue;
                break;
        }
        //中心点显隐设置
        centre.gameObject.SetActive(fightSettings.aimStarCentre);
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        //动态准星
        if (fightSettings.aimStarDynamic)
        {
            //更新目标扩散位置
            upTarget = basicUpPos + Vector2.up * targetOpenDegree * maxDegreeRatio;
            downTarget = basicDownPos + Vector2.down * targetOpenDegree * maxDegreeRatio;
            leftTarget = basicLeftPos + Vector2.left * targetOpenDegree * maxDegreeRatio;
            rightTarget = basicRightPos + Vector2.right * targetOpenDegree * maxDegreeRatio;
            //缓动渐变目标位置
            (up.transform as RectTransform).localPosition = Vector2.Lerp((up.transform as RectTransform).localPosition,
                                                            upTarget, Time.deltaTime * changeSpeed);
            (down.transform as RectTransform).localPosition = Vector2.Lerp((down.transform as RectTransform).localPosition,
                                                            downTarget, Time.deltaTime * changeSpeed);
            (left.transform as RectTransform).localPosition = Vector2.Lerp((left.transform as RectTransform).localPosition,
                                                            leftTarget, Time.deltaTime * changeSpeed);
            (right.transform as RectTransform).localPosition = Vector2.Lerp((right.transform as RectTransform).localPosition,
                                                            rightTarget, Time.deltaTime * changeSpeed);
        }

        //计时决定命中反馈图标是否隐藏
        hitTipTimeCount -= Time.deltaTime;
        if (hitTipTimeCount < 0) hitTip.SetActive(false);
    }

    /// <summary>
    /// 改变UI准星散布大小
    /// </summary>
    /// <param name="offset">散布大小</param>
    public void ChangeTargetOffset(float offset)
    {
        targetOpenDegree = Mathf.Clamp(offset, 0f, 100f);
    }

    /// <summary>
    /// 显示命中提示图标
    /// </summary>
    /// <param name="isHead">是否命中头部</param>
    public void ShowHitTip(bool isHead)
    {
        //显示命中反馈图标
        hitTip.SetActive(true);
        Image[] images = hitTip.GetComponentsInChildren<Image>();
        foreach (Image image in images)
        {
            if (isHead)
            {
                image.color = Color.red;
            }
            else
            {
                image.color = Color.white;
            }
        }
        //重置命中反馈滞留计时器
        hitTipTimeCount = hitTipTime;
    }
}
