using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SceneBatchProcessor : EditorWindow
{
    private Vector3 targetRotation = Vector3.zero;
    private bool includeInactive = true;
    private string parentSearchText = "A";
    private string childSearchText = "B";

    [MenuItem("Tools/场景批处理/查找并设置旋转")]
    static void ShowWindow()
    {
        GetWindow<SceneBatchProcessor>("场景批处理工具");
    }

    void OnGUI()
    {
        GUILayout.Label("场景对象批处理设置", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // 搜索参数设置
        parentSearchText = EditorGUILayout.TextField("父物体搜索文本", parentSearchText);
        childSearchText = EditorGUILayout.TextField("子物体搜索文本", childSearchText);

        EditorGUILayout.Space();

        // 旋转角度设置
        GUILayout.Label("目标旋转角度 (欧拉角):");
        targetRotation.x = EditorGUILayout.FloatField("X 角度", targetRotation.x);
        targetRotation.y = EditorGUILayout.FloatField("Y 角度", targetRotation.y);
        targetRotation.z = EditorGUILayout.FloatField("Z 角度", targetRotation.z);

        EditorGUILayout.Space();

        includeInactive = EditorGUILayout.Toggle("包含非激活对象", includeInactive);

        EditorGUILayout.Space();

        if (GUILayout.Button("执行批处理", GUILayout.Height(30)))
        {
            ExecuteBatchOperation();
        }

        EditorGUILayout.Space();

        // 显示一些帮助信息
        EditorGUILayout.HelpBox(
            "此工具将：\n" +
            "1. 查找场景中所有名称包含 '" + parentSearchText + "' 的物体\n" +
            "2. 在这些物体的所有子物体中查找名称包含 '" + childSearchText + "' 的物体\n" +
            "3. 设置这些子物体的旋转角度为指定值",
            MessageType.Info);
    }

    void ExecuteBatchOperation()
    {
        if (string.IsNullOrEmpty(parentSearchText) || string.IsNullOrEmpty(childSearchText))
        {
            EditorUtility.DisplayDialog("错误", "搜索文本不能为空！", "确定");
            return;
        }

        // 获取当前场景中的所有游戏对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>(includeInactive);
        List<GameObject> parentObjects = new List<GameObject>();
        List<GameObject> targetChildren = new List<GameObject>();

        // 第一步：查找所有包含指定文本的父物体
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains(parentSearchText.ToLower()))
            {
                parentObjects.Add(obj);
            }
        }

        if (parentObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("结果", $"未找到名称包含 '{parentSearchText}' 的物体", "确定");
            return;
        }

        // 第二步：在每个父物体的子物体中查找目标子物体
        foreach (GameObject parent in parentObjects)
        {
            FindTargetChildrenRecursiveStatic(parent.transform, childSearchText, targetChildren);
        }

        if (targetChildren.Count == 0)
        {
            EditorUtility.DisplayDialog("结果",
                $"找到了 {parentObjects.Count} 个包含 '{parentSearchText}' 的父物体，但未在这些物体的子物体中找到名称包含 '{childSearchText}' 的物体",
                "确定");
            return;
        }

        // 第三步：设置找到的子物体的旋转角度
        Undo.RecordObjects(targetChildren.ToArray(), "Set Child Rotation");

        foreach (GameObject child in targetChildren)
        {
            child.transform.localEulerAngles = targetRotation;
            Debug.Log($"已设置物体 '{child.name}' 的旋转角度为: {targetRotation}", child);
        }

        // 显示结果
        string resultMessage = $"处理完成！\n" +
                              $"找到 {parentObjects.Count} 个包含 '{parentSearchText}' 的父物体\n" +
                              $"设置了 {targetChildren.Count} 个包含 '{childSearchText}' 的子物体的旋转角度";

        EditorUtility.DisplayDialog("批处理完成", resultMessage, "确定");

        Debug.Log($"批处理完成: {resultMessage}");
    }

    // 静态版本的递归查找方法，供菜单项使用
    static void FindTargetChildrenRecursiveStatic(Transform parent, string searchText, List<GameObject> results)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower() == searchText.ToLower())
            {
                results.Add(child.gameObject);
            }

            if (child.childCount > 0)
            {
                FindTargetChildrenRecursiveStatic(child, searchText, results);
            }
        }
    }
}