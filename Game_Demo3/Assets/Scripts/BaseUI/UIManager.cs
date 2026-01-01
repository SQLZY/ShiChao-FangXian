using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager
{
    private static UIManager instance = new UIManager();
    public static UIManager Instance => instance;
    private UIManager()
    {
        canvas = GameObject.Instantiate(Resources.Load<GameObject>("UI/Canvas")).transform;
        GameObject.DontDestroyOnLoad(canvas);
    }
    //记录场景面板的字典
    private Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();
    //场景上的Canvans
    private Transform canvas;

    //显示面板
    public T ShowPanel<T>() where T : BasePanel
    {
        //需要保证面板类名和面板预设体名一致
        string name = typeof(T).Name;
        if (panelDic.ContainsKey(name))
        {
            return (T)panelDic[name];
        }
        // 不存在已有面板 实例化面板
        GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>("UI/" + name));
        obj.transform.SetParent(canvas, false);
        T panel = obj.GetComponent<T>();
        // 字典记录面板
        panelDic.Add(name, panel);
        // 显示面板
        panel.ShowMe();
        // 返回面板
        return panel;
    }

    //隐藏面板
    /// <summary>
    /// 隐藏面板
    /// </summary>
    /// <typeparam name="T">面板类名</typeparam>
    /// <param name="isFade">是否有淡出效果</param>
    public void HidePanel<T>(bool isFade = true) where T : BasePanel
    {
        //需要保证面板类名和面板预设体名一致
        string name = typeof(T).Name;
        if (panelDic.ContainsKey(name))
        {
            if (isFade)
            {
                panelDic[name].HideMe(() =>
                {
                    GameObject.Destroy(panelDic[name].gameObject);
                    panelDic.Remove(name);
                });
            }
            else
            {
                GameObject.Destroy(panelDic[name].gameObject);
                panelDic.Remove(name);
            }
        }
    }

    //隐藏所有面板
    public void HideAllPanel(bool isFade = true)
    {
        //获取所有面板列表
        List<BasePanel> panelList = new List<BasePanel>(panelDic.Values);
        //遍历删除所有面板
        foreach (var panel in panelList)
        {
            if (isFade)
            {
                panel.HideMe(() =>
                {
                    GameObject.Destroy(panel.gameObject);
                });
            }
            else
            {
                GameObject.Destroy(panel.gameObject);
            }
        }
        //清空面板记录字典
        panelDic.Clear();
    }

    //获取面板
    public T GetPanel<T>() where T : BasePanel
    {
        if (panelDic.ContainsKey(typeof(T).Name))
        {
            return (T)panelDic[typeof(T).Name];
        }
        return null;
    }

}
