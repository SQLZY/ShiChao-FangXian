using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Events;

public class UIEditorToolWindow : EditorWindow
{
    private string targetFolderPath = "Assets/UI/Panels";
    private string hoverSoundPath = "";
    private string clickSoundPath = "";
    private bool includeSubfolders = true;
    private Vector2 scrollPosition;

    // 颜色设置
    private Color normalColor = Color.white;                              // 默认状态颜色
    private Color highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);    // 鼠标经过颜色
    private Color pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);        // 点击按下颜色
    private Color selectedColor = new Color(0.7f, 0.7f, 0.7f, 1f);       // 选中状态颜色
    private Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);     // 禁用状态颜色

    [MenuItem("Tools/UI编辑器工具")]
    public static void ShowWindow()
    {
        GetWindow<UIEditorToolWindow>("UI编辑器工具");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI面板批量处理工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 文件夹设置
        EditorGUILayout.LabelField("目标文件夹设置", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        targetFolderPath = EditorGUILayout.TextField("目标文件夹", targetFolderPath);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择UI面板文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                targetFolderPath = "Assets" + path.Replace(Application.dataPath, "");
            }
        }
        EditorGUILayout.EndHorizontal();

        // 包含子文件夹选项
        includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("颜色渐变设置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("为Button控件设置自定义颜色渐变效果。", MessageType.Info);

        // 颜色设置区域
        EditorGUILayout.BeginVertical(GUI.skin.box);
        normalColor = EditorGUILayout.ColorField("默认状态颜色 (Normal)", normalColor);
        highlightedColor = EditorGUILayout.ColorField("鼠标经过颜色 (Highlighted)", highlightedColor);
        pressedColor = EditorGUILayout.ColorField("点击按下颜色 (Pressed)", pressedColor);
        selectedColor = EditorGUILayout.ColorField("选中状态颜色 (Selected)", selectedColor);
        disabledColor = EditorGUILayout.ColorField("禁用状态颜色 (Disabled)", disabledColor);
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("设置Button颜色渐变", GUILayout.Height(30)))
        {
            SetButtonsColorTransition();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("音效设置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("请输入Resources文件夹下的音效路径（无需扩展名），例如：Sounds/click", MessageType.Info);

        // 音效路径设置
        EditorGUILayout.BeginVertical(GUI.skin.box);
        hoverSoundPath = EditorGUILayout.TextField("鼠标经过音效路径", hoverSoundPath);
        clickSoundPath = EditorGUILayout.TextField("鼠标点击音效路径", clickSoundPath);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        if (GUILayout.Button("设置控件音效", GUILayout.Height(30)))
        {
            SetUIElementsSound();
        }

        // 预览当前设置的音效
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("音效预览", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("预览经过音效"))
        {
            PreviewSound(hoverSoundPath);
        }
        if (GUILayout.Button("预览点击音效"))
        {
            PreviewSound(clickSoundPath);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void SetButtonsColorTransition()
    {
        if (!Directory.Exists(targetFolderPath))
        {
            EditorUtility.DisplayDialog("错误", "指定文件夹不存在！", "确定");
            return;
        }

        SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] prefabPaths = Directory.GetFiles(targetFolderPath, "*.prefab", searchOption);
        int processedCount = 0;

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                bool modified = ProcessPrefabButtons(prefab);
                if (modified)
                {
                    processedCount++;
                    EditorUtility.SetDirty(prefab);
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已处理 {processedCount} 个预制体中的Button颜色渐变", "确定");
    }

    private bool ProcessPrefabButtons(GameObject prefab)
    {
        bool modified = false;
        Button[] buttons = prefab.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            // 设置颜色渐变
            ColorBlock colors = button.colors;

            // 使用用户设置的颜色
            colors.normalColor = normalColor;              // 默认状态
            colors.highlightedColor = highlightedColor;    // 鼠标经过
            colors.pressedColor = pressedColor;            // 点击按下
            colors.selectedColor = selectedColor;          // 选中状态
            colors.disabledColor = disabledColor;          // 禁用状态

            // 设置颜色过渡时间
            colors.fadeDuration = 0.1f;

            button.colors = colors;
            modified = true;

            // 标记场景中的对象为已修改（如果是场景中的实例）
            if (PrefabUtility.IsPartOfPrefabInstance(button))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            }
        }

        return modified;
    }

    private void SetUIElementsSound()
    {
        if (!Directory.Exists(targetFolderPath))
        {
            EditorUtility.DisplayDialog("错误", "指定文件夹不存在！", "确定");
            return;
        }

        if (string.IsNullOrEmpty(hoverSoundPath) && string.IsNullOrEmpty(clickSoundPath))
        {
            EditorUtility.DisplayDialog("错误", "请至少设置一个音效路径！", "确定");
            return;
        }

        SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] prefabPaths = Directory.GetFiles(targetFolderPath, "*.prefab", searchOption);
        int processedCount = 0;

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                bool modified = ProcessPrefabSounds(prefab);
                if (modified)
                {
                    processedCount++;
                    EditorUtility.SetDirty(prefab);
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已为 {processedCount} 个预制体设置音效", "确定");
    }

    private bool ProcessPrefabSounds(GameObject prefab)
    {
        bool modified = false;

        // 处理Button控件
        Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            // 移除原有的点击事件监听器（避免重复）
            //SerializedObject so = new SerializedObject(button);
            //so.FindProperty("m_OnClick").ClearArray();
            //so.ApplyModifiedProperties();

            // 添加新的点击事件，播放音效
            if (!string.IsNullOrEmpty(clickSoundPath))
            {
                UnityEventTools.AddPersistentListener(button.onClick, () =>
                {
                    if (Application.isPlaying)
                    {
                        GameDataMgr.Instance.PlaySound(clickSoundPath);
                    }
                });
            }

            // 为Button添加EventTrigger来处理鼠标经过事件
            if (!string.IsNullOrEmpty(hoverSoundPath))
            {
                AddHoverSoundToComponent(button.gameObject);
            }
            modified = true;
        }

        // 处理Toggle控件（单选框/多选框）
        Toggle[] toggles = prefab.GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            // 移除原有的值改变事件监听器
            //SerializedObject so = new SerializedObject(toggle);
            //so.FindProperty("m_OnValueChanged").ClearArray();
            //so.ApplyModifiedProperties();

            // 添加点击音效 - 每次点击都播放，不区分状态
            if (!string.IsNullOrEmpty(clickSoundPath))
            {
                UnityEventTools.AddPersistentListener(toggle.onValueChanged, (bool isOn) =>
                {
                    if (Application.isPlaying)
                    {
                        GameDataMgr.Instance.PlaySound(clickSoundPath);
                    }
                });
            }

            // 添加鼠标经过音效
            if (!string.IsNullOrEmpty(hoverSoundPath))
            {
                AddHoverSoundToComponent(toggle.gameObject);
            }
            modified = true;
        }

        return modified;
    }

    private void AddHoverSoundToComponent(GameObject target)
    {
        // 添加EventTrigger组件来处理鼠标事件
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = target.AddComponent<EventTrigger>();

            // 确保新添加的组件被标记为已修改
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        // 使用SerializedObject来正确修改EventTrigger的triggers
        SerializedObject so = new SerializedObject(eventTrigger);
        SerializedProperty triggersProperty = so.FindProperty("m_Delegates");

        // 检查是否已存在PointerEnter事件
        bool hasPointerEnter = false;
        for (int i = 0; i < triggersProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = triggersProperty.GetArrayElementAtIndex(i);
            SerializedProperty eventIDProperty = entryProperty.FindPropertyRelative("eventID");
            if (eventIDProperty.enumValueIndex == (int)EventTriggerType.PointerEnter)
            {
                hasPointerEnter = true;

                // 清空现有回调
                SerializedProperty callbackProperty = entryProperty.FindPropertyRelative("callback");
                callbackProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();

                // 添加新的回调
                AddSoundCallbackToEvent(callbackProperty, hoverSoundPath);
                break;
            }
        }

        // 如果不存在PointerEnter事件，则创建一个
        if (!hasPointerEnter)
        {
            int index = triggersProperty.arraySize;
            triggersProperty.arraySize++;

            SerializedProperty entryProperty = triggersProperty.GetArrayElementAtIndex(index);
            SerializedProperty eventIDProperty = entryProperty.FindPropertyRelative("eventID");
            eventIDProperty.enumValueIndex = (int)EventTriggerType.PointerEnter;

            SerializedProperty callbackProperty = entryProperty.FindPropertyRelative("callback");
            AddSoundCallbackToEvent(callbackProperty, hoverSoundPath);
        }

        so.ApplyModifiedProperties();

        // 确保修改被记录
        if (PrefabUtility.IsPartOfPrefabInstance(eventTrigger))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(eventTrigger);
        }
    }

    private void AddSoundCallbackToEvent(SerializedProperty callbackProperty, string soundPath)
    {
        SerializedProperty callsProperty = callbackProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
        callsProperty.arraySize = 1;

        SerializedProperty callProperty = callsProperty.GetArrayElementAtIndex(0);

        // 设置目标对象为GameDataMgr.Instance
        callProperty.FindPropertyRelative("m_Target").objectReferenceValue = null; // 运行时动态获取

        // 设置方法名
        callProperty.FindPropertyRelative("m_MethodName").stringValue = "PlaySound";

        // 设置参数
        SerializedProperty argumentsProperty = callProperty.FindPropertyRelative("m_Arguments");
        argumentsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = null;
        argumentsProperty.FindPropertyRelative("m_StringArgument").stringValue = soundPath;
        argumentsProperty.FindPropertyRelative("m_IntArgument").intValue = 0;
        argumentsProperty.FindPropertyRelative("m_FloatArgument").floatValue = 0f;
        argumentsProperty.FindPropertyRelative("m_BoolArgument").boolValue = false;

        // 设置调用模式为动态
        callProperty.FindPropertyRelative("m_Mode").intValue = 1; // PersistentListenerMode.String
        callProperty.FindPropertyRelative("m_CallState").intValue = 2; // UnityEngine.Events.UnityEventCallState.EditorAndRuntime
    }

    private void PreviewSound(string soundPath)
    {
        if (string.IsNullOrEmpty(soundPath))
        {
            EditorUtility.DisplayDialog("提示", "请先设置音效路径", "确定");
            return;
        }

        // 使用Resources.Load加载音效
        AudioClip clip = Resources.Load<AudioClip>(soundPath);
        if (clip != null)
        {
            AudioUtility.PlayClip(clip);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"无法加载音效文件: {soundPath}，请检查路径是否正确", "确定");
        }
    }
}

// 音频预览工具
public static class AudioUtility
{
    public static void PlayClip(AudioClip clip)
    {
        System.Reflection.Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        System.Type audioUtilType = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        System.Reflection.MethodInfo method = audioUtilType.GetMethod(
            "PlayClip",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
            null,
            new System.Type[] { typeof(AudioClip) },
            null
        );

        if (method != null)
        {
            method.Invoke(null, new object[] { clip });
        }
    }
}