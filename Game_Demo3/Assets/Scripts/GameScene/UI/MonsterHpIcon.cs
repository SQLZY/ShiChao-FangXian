using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 僵尸血条图标组件
/// </summary>
public class MonsterHpIcon : MonoBehaviour
{
    //关联血条前景背景图标
    public Image imgHp;
    public Image imgHpTrue;
    //跟随物体并转UI坐标组件
    public IconFollowTarget followTarget;
    //当前血条所属怪物组件
    public MonsterObj monsterObj;
    //血条长度
    public float hpw = 100f;
    //目标血条长度
    private float targetHpw;
    //当前血条长度
    private float nowHpw;
    //控制透明度组件
    private CanvasGroup canvasGroup;

    private void Start()
    {
        //初始化CanvasGroup
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        //怪物组件已被销毁 直接结束逻辑
        if (!monsterObj) return;

        if (!monsterObj.gameObject.activeSelf)
        {
            canvasGroup.alpha = 0;
            return;
        }
        if (monsterObj.isBeAtked)
        {
            canvasGroup.alpha = 1;
        }
        else
        {
            canvasGroup.alpha -= Time.deltaTime * 0.3f;
        }
        //计算目标血条长度
        targetHpw = hpw * monsterObj.GetNowHpRatio();
        //即时更新真实血条背景图
        (imgHpTrue.transform as RectTransform).sizeDelta = new Vector2(targetHpw, (imgHpTrue.transform as RectTransform).sizeDelta.y);
        //动画前景血量更新效果
        nowHpw = Mathf.Lerp((imgHp.transform as RectTransform).sizeDelta.x, targetHpw, Time.deltaTime * 3f);
        (imgHp.transform as RectTransform).sizeDelta = new Vector2(nowHpw, (imgHp.transform as RectTransform).sizeDelta.y);
    }

    /// <summary>
    /// 初始化怪物信息
    /// </summary>
    /// <param name="monsterObj"></param>
    public void InitMonster(MonsterObj monsterObj)
    {
        this.monsterObj = monsterObj;
        followTarget = GetComponent<IconFollowTarget>();
        followTarget.followTarget = monsterObj.headCollider.transform;
        followTarget.heightOffset = 0.4f;
    }
}
