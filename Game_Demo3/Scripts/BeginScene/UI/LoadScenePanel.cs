using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadScenePanel : BasePanel
{
    //过场景进度条加载速度
    private float loadSpeed = 0.3f;

    //场景文字信息 背景图 进度条控件关联
    public Text txtSceneName;
    public Scrollbar scrollbarLoad;
    public RawImage rawImageBK;
    public Text tip;

    //随机游戏提示词列表
    private string[] tips = new string[]
    {
        "机枪塔拥有最高的单体伤害 对付Boss战役至关重要",
        "冰棱塔以自身为中心溅射伤害 尝试把怪物引到它的附近",
        "火炮塔适合攻击怪物密度较大的小范围区域",
        "雷电塔的伤害可以无限传递 适合怪物数量较多且站位靠近的场景",
        "感觉手上的武器伤害较低 请多解锁高价角色增加伤害",
        "解锁角色获得的各项增益可以累加 大大降低游戏难度",
        "瞄准和蹲姿状态下 枪械武器的后座力和子弹散布会更小",
        "善用瞄准和下蹲来控制枪械武器弹道",
        "本游戏中的全部武器都可以通过长按来连续攻击",
        "怪物出现危险提示 善用前后翻滚来快速躲开攻击",
        "翻滚方向与玩家原始跑动方向一致 可以翻滚更远距离",
        "翻滚过后有一定的缓冲时间 无法立刻再次翻滚",
        "在没有怪物时 记得主动按R键换弹",
        "键盘按键M可以开关大地图俯瞰全局",
        "善用小地图和Alt自由视角来观察周围怪物位置",
        "打开Esc退出游戏面板 可以用于暂停游戏",
        "击败怪物是关卡过程中获得筑晶的唯一途径",
        "血量不佳时 多用跑动引导怪物 把输出交给防御塔",
        "玩家处于怪物附近时 怪物会优先攻击玩家 利用好这点保护基地",
        "冒险模式游戏失败获得的金钱数量会大幅减少",
        "据说收集完毕全部角色 家园会出现惊喜哦",
        "据说无尽模式的某个房子里藏有惊喜哦",
        "无尽模式下的怪物会得到增强 挑战难度更高",
        "不熟悉键位 不知道可以跳舞 请查看百科中的键位指南",
        "不是自己常用的操作方法或准星样式 可以在设置界面修改哦",
        "命中怪物头部的伤害远远大于其它部位",
        "手枪的伤害较低 精准度和射程较差 只适合用于前期过渡",
        "狙击枪是伤害、射程、精准度最高的武器 但不适合大量怪物",
        "重机枪虽然有着很好的输出续航 但是玩家移动速度大幅降低",
        "百科中的成就统计可以知晓当前游戏进度",
        "怪物无法攻击到身后的玩家 对转身较慢的怪物可以多加利用",
        "怪物攻击玩家时可能会击中基地 尽量吸引怪物远离基地",
        "无尽模式下的出怪间隔更小 节奏更紧凑",
        "无尽模式赚取金钱的效率会更高哦",
        "遇到卡关情况 试试挑战上个场景的Boss关卡 也有不错的收益呢",
        "如果您在游戏过程中遇到Bug 请说谢谢老中医并受着",
    };

    //通过场景信息加载场景
    public void InitInfo(SceneInfo sceneInfo, UnityAction callBack = null)
    {
        //更新场景文字和背景图
        txtSceneName.text = sceneInfo.name;
        rawImageBK.texture = Resources.Load<Texture>(sceneInfo.imgRes + "BK");
        //增加切换背景音乐委托
        callBack += () => { BKMusic.Instance.ChangeBKMusic(sceneInfo.sceneName); };
        //异步加载场景协程
        StartCoroutine(ChangeSceneCoroutine(sceneInfo.sceneName, callBack));
    }

    //通过场景名加载场景
    public void InitInfo(string sceneName, string txtInfo = "", UnityAction callBack = null, string music = "", string imgPath = "")
    {
        //更新场景文字
        txtSceneName.text = txtInfo;

        //更新背景图
        if (imgPath == "")
        {
            rawImageBK.texture = null;
            rawImageBK.color = new Color(0, 0, 0, 0);
        }
        else
        {
            rawImageBK.texture = Resources.Load<Texture>(imgPath);
        }

        //增加切换背景音乐委托(☆通过场景名加载默认切换基础背景音乐)
        if (music == "")
        {
            callBack += () => { BKMusic.Instance.ChangeBeginMusic(); };
        }
        else
        {
            callBack += () => { BKMusic.Instance.ChangeBKMusic(music); };
        }

        //异步加载场景协程
        StartCoroutine(ChangeSceneCoroutine(sceneName, callBack));
    }

    protected override void Init()
    {
        // 随机过场景提示文字
        tip.text = tips[Random.Range(0, tips.Length)];
    }

    //异步加载场景协程
    IEnumerator ChangeSceneCoroutine(string sceneName, UnityAction callBack = null)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;
        scrollbarLoad.size = 0f;

        while (scrollbarLoad.size != 1)
        {
            scrollbarLoad.size += loadSpeed * Time.deltaTime;
            yield return null;
        }

        while (asyncOperation.progress < 0.9f)
        {
            yield return null;
        }

        asyncOperation.allowSceneActivation = true;

        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        UIManager.Instance.HidePanel<LoadScenePanel>();

        callBack?.Invoke();
    }
}
