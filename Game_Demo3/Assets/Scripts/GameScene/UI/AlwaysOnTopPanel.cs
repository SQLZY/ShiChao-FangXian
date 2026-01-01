using UnityEngine.UI;
using UnityEngine;

public class AlwaysOnTopPanel : MonoBehaviour
{
    private Canvas canvas;
    private const int ALWAYS_ON_TOP_ORDER = 999; // 设置一个足够大的值

    void Start()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        // 关键：确保overrideSorting被启用
        canvas.overrideSorting = true;
        canvas.sortingOrder = ALWAYS_ON_TOP_ORDER;

        // 可选：添加Graphic Raycaster以确保交互
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}