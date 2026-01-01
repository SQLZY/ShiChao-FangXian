using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

public class ClassDiagramGenerator : EditorWindow
{
    private List<ClassInfo> classInfos = new List<ClassInfo>();
    private Dictionary<string, List<ClassInfo>> inheritanceMap = new Dictionary<string, List<ClassInfo>>();
    private Vector2 scrollPosition;
    private bool initialized = false;
    private int totalCodeLines = 0;

    // 分类定义
    private enum ClassCategory
    {
        MonoBehaviour,
        DataInfo,      // Info结尾的配置数据信息
        PlayerData,    // Data结尾的玩家数据信息
        Manager,       // 管理器类
        Other          // 其他
    }

    private class ClassInfo
    {
        public Type Type { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string BaseClassName { get; set; }
        public bool IsMonoBehaviour { get; set; }
        public ClassCategory Category { get; set; }
        public int CodeLines { get; set; }
        public string FilePath { get; set; }
    }

    [MenuItem("Tools/生成类关系图")]
    public static void ShowWindow()
    {
        GetWindow<ClassDiagramGenerator>("类图生成器").minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        initialized = false;
        LoadScriptsWithLineCount();
    }

    private int CountCodeLines(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return 0;

            int lineCount = 0;
            bool inMultiLineComment = false;

            foreach (string line in File.ReadAllLines(filePath))
            {
                string trimmedLine = line.Trim();

                // 跳过空行
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // 处理多行注释
                if (inMultiLineComment)
                {
                    if (trimmedLine.Contains("*/"))
                    {
                        inMultiLineComment = false;
                        // 检查注释结束后是否有代码
                        string afterComment = trimmedLine.Substring(trimmedLine.IndexOf("*/") + 2);
                        if (!string.IsNullOrWhiteSpace(afterComment))
                            lineCount++;
                    }
                    continue;
                }

                // 跳过单行注释
                if (trimmedLine.StartsWith("//"))
                    continue;

                // 检查多行注释开始
                if (trimmedLine.Contains("/*"))
                {
                    inMultiLineComment = true;
                    // 检查注释开始前是否有代码
                    string beforeComment = trimmedLine.Substring(0, trimmedLine.IndexOf("/*"));
                    if (!string.IsNullOrWhiteSpace(beforeComment))
                        lineCount++;
                    continue;
                }

                lineCount++;
            }

            return lineCount;
        }
        catch
        {
            return 0;
        }
    }

    private ClassCategory DetermineCategory(ClassInfo classInfo)
    {
        if (classInfo.IsMonoBehaviour)
            return ClassCategory.MonoBehaviour;
        else if (classInfo.Name.EndsWith("Info", StringComparison.OrdinalIgnoreCase))
            return ClassCategory.DataInfo;
        else if (classInfo.Name.EndsWith("Data", StringComparison.OrdinalIgnoreCase))
            return ClassCategory.PlayerData;
        else if (classInfo.Name.EndsWith("Manager", StringComparison.OrdinalIgnoreCase) ||
                 classInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ||
                 classInfo.Name.Contains("Manager") ||
                 classInfo.Name.Contains("Controller"))
            return ClassCategory.Manager;
        else
            return ClassCategory.Other;
    }

    private void LoadScriptsWithLineCount()
    {
        classInfos.Clear();
        inheritanceMap.Clear();
        totalCodeLines = 0;

        // 获取所有程序集
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        // 查找Assets/Scripts/目录下的所有.cs文件
        string scriptsPath = Application.dataPath + "/Scripts";
        if (!Directory.Exists(scriptsPath))
        {
            Debug.LogWarning($"Scripts目录不存在: {scriptsPath}");
            return;
        }

        string[] scriptFiles = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);

        foreach (string file in scriptFiles)
        {
            string className = Path.GetFileNameWithoutExtension(file);
            int lineCount = CountCodeLines(file);
            totalCodeLines += lineCount;

            // 在所有程序集中查找对应的类型
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type type = assembly.GetType(className);
                    if (type == null)
                    {
                        // 尝试带有命名空间的类型
                        string relativePath = file.Replace(scriptsPath, "").Replace("\\", "/");
                        string namespacePath = relativePath.Replace("/" + Path.GetFileName(file), "").Trim('/');
                        namespacePath = namespacePath.Replace("/", ".");
                        string fullTypeName = string.IsNullOrEmpty(namespacePath) ? className : $"{namespacePath}.{className}";
                        type = assembly.GetType(fullTypeName);
                    }

                    if (type != null && !type.IsAbstract && !type.IsInterface && !type.IsGenericType)
                    {
                        ClassInfo info = new ClassInfo
                        {
                            Type = type,
                            Name = type.Name,
                            Namespace = type.Namespace,
                            BaseClassName = type.BaseType?.Name ?? "无",
                            IsMonoBehaviour = type.IsSubclassOf(typeof(MonoBehaviour)),
                            CodeLines = lineCount,
                            FilePath = file
                        };

                        info.Category = DetermineCategory(info);
                        classInfos.Add(info);
                        break;
                    }
                }
                catch { }
            }
        }

        Debug.Log($"找到 {classInfos.Count} 个脚本，总代码行数: {totalCodeLines}");

        // 建立继承关系
        foreach (ClassInfo info in classInfos)
        {
            if (!string.IsNullOrEmpty(info.BaseClassName) && info.BaseClassName != "object" && info.BaseClassName != "Object")
            {
                if (!inheritanceMap.ContainsKey(info.BaseClassName))
                    inheritanceMap[info.BaseClassName] = new List<ClassInfo>();

                inheritanceMap[info.BaseClassName].Add(info);
            }
        }

        initialized = true;
    }

    private void OnGUI()
    {
        if (!initialized)
            return;

        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("类图生成器", EditorStyles.boldLabel, GUILayout.Height(30));

        EditorGUILayout.Space(10);

        if (GUILayout.Button("重新加载脚本"))
        {
            LoadScriptsWithLineCount();
            Repaint();
        }

        if (GUILayout.Button("生成HTML类图"))
        {
            ExportAsHTML();
        }

        EditorGUILayout.Space(20);

        EditorGUILayout.LabelField($"已加载 {classInfos.Count} 个类");
        EditorGUILayout.LabelField($"总代码行数: {totalCodeLines}");
        EditorGUILayout.LabelField($"继承关系: {CalculateTotalInheritanceRelations()} 条");

        // 显示分类统计
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("分类统计:", EditorStyles.boldLabel);

        var categoryGroups = classInfos.GroupBy(c => c.Category).OrderBy(g => g.Key);
        foreach (var group in categoryGroups)
        {
            string categoryName = GetCategoryDisplayName(group.Key);
            EditorGUILayout.LabelField($"  {categoryName}: {group.Count()} 个类");
        }

        EditorGUILayout.Space(10);

        // 显示类列表
        EditorGUILayout.LabelField("脚本列表:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        foreach (ClassInfo info in classInfos.OrderBy(c => c.Name))
        {
            string categoryIcon = GetCategoryIcon(info.Category);
            EditorGUILayout.LabelField($"{categoryIcon} {info.Name} ({info.CodeLines}行)");
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private string GetCategoryDisplayName(ClassCategory category)
    {
        switch (category)
        {
            case ClassCategory.MonoBehaviour: return "MonoBehaviour类";
            case ClassCategory.DataInfo: return "配置数据类(Info)";
            case ClassCategory.PlayerData: return "玩家数据类(Data)";
            case ClassCategory.Manager: return "管理器类";
            case ClassCategory.Other: return "其他类";
            default: return "未知";
        }
    }

    private string GetCategoryIcon(ClassCategory category)
    {
        switch (category)
        {
            case ClassCategory.MonoBehaviour: return "🎮";
            case ClassCategory.DataInfo: return "📄";
            case ClassCategory.PlayerData: return "💾";
            case ClassCategory.Manager: return "⚙️";
            case ClassCategory.Other: return "📦";
            default: return "❓";
        }
    }

    private void ExportAsHTML()
    {
        try
        {
            // 创建输出目录
            string outputDir = Application.dataPath + "/ArtRes/ClassPhoto";
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string outputPath = outputDir + "/岐黄镇略游戏类关系图.html";

            // 生成HTML内容
            string html = GenerateEnhancedHTML();
            File.WriteAllText(outputPath, html);

            EditorUtility.DisplayDialog("成功", $"HTML类图已生成:\n{outputPath}", "确定");

            // 在默认浏览器中打开
            Application.OpenURL("file://" + outputPath);
            Debug.Log($"HTML类图已生成: {outputPath}");

            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"生成失败: {e.Message}", "确定");
        }
    }

    private string GenerateEnhancedHTML()
    {
        StringBuilder html = new StringBuilder();

        // HTML头部
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang='zh-CN'>");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset='UTF-8'>");
        html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        html.AppendLine("    <title>岐黄镇略游戏类关系图</title>");

        // CSS样式
        html.AppendLine(@"    <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    
    body {
        font-family: 'Segoe UI', 'Microsoft YaHei', sans-serif;
        line-height: 1.6;
        color: #333;
        background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
        min-height: 100vh;
        padding: 20px;
    }
    
    .container {
        max-width: 1600px;
        margin: 0 auto;
        background: white;
        border-radius: 15px;
        box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
        overflow: hidden;
    }
    
    .header {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        padding: 30px;
        text-align: center;
    }
    
    .header h1 {
        font-size: 2.5rem;
        margin-bottom: 10px;
        text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.2);
    }
    
    .header .subtitle {
        font-size: 1.1rem;
        opacity: 0.9;
    }
    
    .stats {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 20px;
        padding: 25px;
        background: #f8f9fa;
        border-bottom: 1px solid #e9ecef;
    }
    
    .stat-card {
        background: white;
        padding: 20px;
        border-radius: 10px;
        text-align: center;
        box-shadow: 0 3px 10px rgba(0, 0, 0, 0.08);
        transition: transform 0.3s ease;
    }
    
    .stat-card:hover {
        transform: translateY(-5px);
        box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
    }
    
    .stat-value {
        font-size: 2.5rem;
        font-weight: bold;
        color: #667eea;
        margin-bottom: 5px;
    }
    
    .stat-label {
        color: #6c757d;
        font-size: 0.9rem;
        text-transform: uppercase;
        letter-spacing: 1px;
    }
    
    .stat-card.total-lines .stat-value {
        color: #28a745;
    }
    
    .category-stats {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: 15px;
        padding: 20px;
        background: #f0f8ff;
        margin: 20px;
        border-radius: 10px;
    }
    
    .category-stat {
        text-align: center;
        padding: 15px;
        background: white;
        border-radius: 8px;
        box-shadow: 0 2px 5px rgba(0,0,0,0.05);
    }
    
    .category-icon {
        font-size: 1.5rem;
        margin-bottom: 5px;
    }
    
    .category-name {
        font-weight: bold;
        color: #495057;
        margin-bottom: 5px;
    }
    
    .category-count {
        font-size: 1.2rem;
        color: #667eea;
        font-weight: bold;
    }
    
    .main-content {
        display: grid;
        grid-template-columns: 1fr 2fr;
        gap: 30px;
        padding: 30px;
    }
    
    @media (max-width: 1200px) {
        .main-content {
            grid-template-columns: 1fr;
        }
    }
    
    .class-list-section {
        background: #f8f9fa;
        padding: 25px;
        border-radius: 10px;
    }
    
    .class-list-section h2 {
        color: #495057;
        margin-bottom: 20px;
        padding-bottom: 10px;
        border-bottom: 2px solid #667eea;
    }
    
    .class-list {
        max-height: 600px;
        overflow-y: auto;
    }
    
    .class-item {
        background: white;
        margin-bottom: 10px;
        padding: 15px;
        border-radius: 8px;
        border-left: 4px solid #667eea;
        transition: all 0.2s ease;
        cursor: pointer;
    }
    
    .class-item:hover {
        background: #e9ecef;
        transform: translateX(5px);
    }
    
    .class-item.mono {
        border-left-color: #28a745;
    }
    
    .class-item.info {
        border-left-color: #17a2b8;
    }
    
    .class-item.data {
        border-left-color: #ffc107;
    }
    
    .class-item.manager {
        border-left-color: #dc3545;
    }
    
    .class-item.other {
        border-left-color: #6c757d;
    }
    
    .class-name {
        font-weight: bold;
        color: #495057;
        margin-bottom: 5px;
    }
    
    .class-info {
        font-size: 0.85rem;
        color: #6c757d;
    }
    
    .class-lines {
        font-size: 0.8rem;
        color: #adb5bd;
        margin-top: 3px;
    }
    
    .diagram-section {
        background: white;
        padding: 25px;
        border-radius: 10px;
        border: 1px solid #e9ecef;
    }
    
    .diagram-section h2 {
        color: #495057;
        margin-bottom: 20px;
        padding-bottom: 10px;
        border-bottom: 2px solid #764ba2;
    }
    
    .inheritance-diagram {
        background: #f8f9fa;
        border-radius: 8px;
        padding: 20px;
        min-height: 500px;
        overflow-x: auto;
        position: relative;
    }
    
    .diagram-area {
        position: relative;
        width: 100%;
        min-height: 600px;
    }
    
    .category-area {
        position: absolute;
        border-radius: 10px;
        padding: 10px;
        min-width: 200px;
    }
    
    .category-area.mono {
        background: rgba(40, 167, 69, 0.1);
        border: 2px dashed #28a745;
        top: 20px;
        left: 20px;
    }
    
    .category-area.info {
        background: rgba(23, 162, 184, 0.1);
        border: 2px dashed #17a2b8;
        top: 20px;
        right: 20px;
    }
    
    .category-area.data {
        background: rgba(255, 193, 7, 0.1);
        border: 2px dashed #ffc107;
        bottom: 20px;
        left: 20px;
    }
    
    .category-area.manager {
        background: rgba(220, 53, 69, 0.1);
        border: 2px dashed #dc3545;
        bottom: 20px;
        right: 20px;
    }
    
    .category-area.other {
        background: rgba(108, 117, 125, 0.1);
        border: 2px dashed #6c757d;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
    }
    
    .category-label {
        font-weight: bold;
        text-align: center;
        margin-bottom: 10px;
        padding: 5px;
        border-radius: 5px;
        color: white;
    }
    
    .category-label.mono {
        background: #28a745;
    }
    
    .category-label.info {
        background: #17a2b8;
    }
    
    .category-label.data {
        background: #ffc107;
        color: #333;
    }
    
    .category-label.manager {
        background: #dc3545;
    }
    
    .category-label.other {
        background: #6c757d;
    }
    
    .class-node {
        background: white;
        border: 2px solid #667eea;
        border-radius: 8px;
        padding: 15px;
        margin: 10px;
        min-width: 180px;
        text-align: center;
        box-shadow: 0 3px 6px rgba(0, 0, 0, 0.1);
        position: absolute;
        z-index: 2;
        transition: all 0.3s ease;
        cursor: pointer;
    }
    
    .class-node:hover {
        box-shadow: 0 5px 15px rgba(0, 0, 0, 0.2);
        transform: translateY(-2px) scale(1.02);
    }
    
    .class-node.mono {
        border-color: #28a745;
        background: #f8fff9;
    }
    
    .class-node.info {
        border-color: #17a2b8;
        background: #f0fcff;
    }
    
    .class-node.data {
        border-color: #ffc107;
        background: #fffdf0;
    }
    
    .class-node.manager {
        border-color: #dc3545;
        background: #fff0f0;
    }
    
    .class-node.other {
        border-color: #6c757d;
        background: #f8f9fa;
    }
    
    .class-node .node-name {
        font-weight: bold;
        color: #495057;
        margin-bottom: 5px;
        font-size: 14px;
    }
    
    .class-node .node-lines {
        font-size: 11px;
        color: #888;
        margin-bottom: 5px;
        font-style: italic;
    }
    
    .class-node .node-parent {
        font-size: 11px;
        color: #6c757d;
        margin-bottom: 3px;
    }
    
    .class-node .node-namespace {
        font-size: 10px;
        color: #adb5bd;
        margin-top: 5px;
    }
    
    .inheritance-connections {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        pointer-events: none;
        z-index: 1;
    }
    
    .inheritance-legend {
        display: flex;
        justify-content: center;
        flex-wrap: wrap;
        gap: 20px;
        margin-top: 20px;
        padding: 15px;
        background: #f8f9fa;
        border-radius: 8px;
    }
    
    .legend-item {
        display: flex;
        align-items: center;
        gap: 8px;
    }
    
    .legend-color {
        width: 20px;
        height: 20px;
        border-radius: 4px;
    }
    
    .legend-color.mono {
        background: #f8fff9;
        border: 2px solid #28a745;
    }
    
    .legend-color.info {
        background: #f0fcff;
        border: 2px solid #17a2b8;
    }
    
    .legend-color.data {
        background: #fffdf0;
        border: 2px solid #ffc107;
    }
    
    .legend-color.manager {
        background: #fff0f0;
        border: 2px solid #dc3545;
    }
    
    .legend-color.other {
        background: #f8f9fa;
        border: 2px solid #6c757d;
    }
    
    .legend-color.line {
        background: linear-gradient(to right, #667eea, #764ba2);
        height: 2px;
        width: 30px;
        margin-top: 2px;
    }
    
    .footer {
        text-align: center;
        padding: 20px;
        background: #f8f9fa;
        color: #6c757d;
        border-top: 1px solid #e9ecef;
        font-size: 0.9rem;
    }
    
    .search-box {
        margin-bottom: 20px;
    }
    
    .search-box input {
        width: 100%;
        padding: 10px 15px;
        border: 1px solid #dee2e6;
        border-radius: 5px;
        font-size: 1rem;
    }
    
    .no-classes {
        text-align: center;
        padding: 40px;
        color: #6c757d;
        font-style: italic;
    }
    
    svg {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        pointer-events: none;
    }
    
    .line {
        stroke: #667eea;
        stroke-width: 2;
        stroke-dasharray: 5, 5;
        fill: none;
    }
    
    .solid-line {
        stroke: #667eea;
        stroke-width: 2;
        fill: none;
    }
    
    .controls {
        margin-bottom: 20px;
        padding: 15px;
        background: #f8f9fa;
        border-radius: 8px;
        display: flex;
        gap: 10px;
        flex-wrap: wrap;
    }
    
    .control-btn {
        padding: 8px 15px;
        background: #667eea;
        color: white;
        border: none;
        border-radius: 5px;
        cursor: pointer;
        font-size: 0.9rem;
    }
    
    .control-btn:hover {
        background: #5a67d8;
    }
</style>");

        // JavaScript
        html.AppendLine(@"    <script>
    let nodePositions = {};
    let classNodes = [];
    
    function filterClasses() {
        const searchTerm = document.getElementById('classSearch').value.toLowerCase();
        const classItems = document.querySelectorAll('.class-item');
        const classNodes = document.querySelectorAll('.class-node');
        
        classItems.forEach(item => {
            const className = item.querySelector('.class-name').textContent.toLowerCase();
            if (className.includes(searchTerm)) {
                item.style.display = 'block';
            } else {
                item.style.display = 'none';
            }
        });
        
        classNodes.forEach(node => {
            const className = node.getAttribute('data-class').toLowerCase();
            if (searchTerm === '' || className.includes(searchTerm)) {
                node.style.opacity = '1';
            } else {
                node.style.opacity = '0.3';
            }
        });
    }
    
    function highlightClass(className) {
        // 移除之前的高亮
        document.querySelectorAll('.class-node').forEach(node => {
            node.style.boxShadow = '0 3px 6px rgba(0, 0, 0, 0.1)';
            node.style.transform = '';
            node.style.zIndex = '2';
        });
        
        // 添加新的高亮
        const targetNode = document.querySelector(`.class-node[data-class='\${className}']`);
        if (targetNode) {
            targetNode.style.boxShadow = '0 0 0 3px rgba(255, 107, 107, 0.5), 0 5px 15px rgba(0, 0, 0, 0.2)';
            targetNode.style.transform = 'scale(1.1)';
            targetNode.style.zIndex = '100';
            
            // 高亮父类和子类
            const parentClass = targetNode.getAttribute('data-parent');
            if (parentClass && parentClass !== '无' && parentClass !== 'object') {
                const parentNode = document.querySelector(`.class-node[data-class='\${parentClass}']`);
                if (parentNode) {
                    parentNode.style.boxShadow = '0 0 0 3px rgba(78, 205, 196, 0.5), 0 5px 15px rgba(0, 0, 0, 0.2)';
                    parentNode.style.zIndex = '50';
                }
            }
            
            // 高亮子类
            document.querySelectorAll('.class-node').forEach(node => {
                if (node.getAttribute('data-parent') === className) {
                    node.style.boxShadow = '0 0 0 3px rgba(255, 230, 109, 0.5), 0 5px 15px rgba(0, 0, 0, 0.2)';
                    node.style.zIndex = '50';
                }
            });
        }
    }
    
    function arrangeNodesByCategory() {
        const diagramArea = document.querySelector('.diagram-area');
        if (!diagramArea) return;
        
        const width = diagramArea.clientWidth;
        const height = diagramArea.clientHeight;
        
        // 定义各分类区域的位置和大小
        const areas = {
            'mono': { x: 50, y: 50, width: width * 0.35, height: height * 0.35 },
            'info': { x: width * 0.6, y: 50, width: width * 0.35, height: height * 0.35 },
            'data': { x: 50, y: height * 0.6, width: width * 0.35, height: height * 0.35 },
            'manager': { x: width * 0.6, y: height * 0.6, width: width * 0.35, height: height * 0.35 },
            'other': { x: width * 0.3, y: height * 0.3, width: width * 0.4, height: height * 0.4 }
        };
        
        // 按分类分组节点
        const nodesByCategory = {
            'mono': [],
            'info': [],
            'data': [],
            'manager': [],
            'other': []
        };
        
        document.querySelectorAll('.class-node').forEach(node => {
            const category = node.classList[1]; // 第二个类是分类
            if (nodesByCategory[category]) {
                nodesByCategory[category].push(node);
            } else {
                nodesByCategory['other'].push(node);
            }
        });
        
        // 在各自区域内随机分布节点
        Object.keys(nodesByCategory).forEach(category => {
            const area = areas[category];
            const nodes = nodesByCategory[category];
            
            if (nodes.length > 0) {
                const cols = Math.ceil(Math.sqrt(nodes.length));
                const rows = Math.ceil(nodes.length / cols);
                
                const cellWidth = area.width / cols;
                const cellHeight = area.height / rows;
                
                nodes.forEach((node, index) => {
                    const row = Math.floor(index / cols);
                    const col = index % cols;
                    
                    const x = area.x + col * cellWidth + Math.random() * 20;
                    const y = area.y + row * cellHeight + Math.random() * 20;
                    
                    node.style.left = x + 'px';
                    node.style.top = y + 'px';
                    
                    // 保存位置信息
                    const className = node.getAttribute('data-class');
                    nodePositions[className] = {
                        x: x + node.offsetWidth / 2,
                        y: y + node.offsetHeight
                    };
                });
            }
        });
        
        // 重新绘制连线
        setTimeout(drawInheritanceLines, 100);
    }
    
    function drawInheritanceLines() {
        const svg = document.getElementById('inheritance-svg');
        if (!svg) return;
        
        // 清空之前的连线
        svg.innerHTML = '';
        
        // 绘制所有继承关系
        document.querySelectorAll('.class-node').forEach(node => {
            const className = node.getAttribute('data-class');
            const parentClass = node.getAttribute('data-parent');
            
            if (parentClass && parentClass !== '无' && parentClass !== 'object' && 
                nodePositions[className] && nodePositions[parentClass]) {
                
                const childPos = nodePositions[className];
                const parentPos = nodePositions[parentClass];
                
                // 创建连线
                const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
                line.setAttribute('x1', parentPos.x);
                line.setAttribute('y1', parentPos.y);
                line.setAttribute('x2', childPos.x);
                line.setAttribute('y2', childPos.y);
                
                // 根据父类类别设置颜色
                const parentNode = document.querySelector(`.class-node[data-class='\${parentClass}']`);
                let strokeColor = '#667eea';
                
                if (parentNode) {
                    if (parentNode.classList.contains('mono')) strokeColor = '#28a745';
                    else if (parentNode.classList.contains('info')) strokeColor = '#17a2b8';
                    else if (parentNode.classList.contains('data')) strokeColor = '#ffc107';
                    else if (parentNode.classList.contains('manager')) strokeColor = '#dc3545';
                    else if (parentNode.classList.contains('other')) strokeColor = '#6c757d';
                }
                
                line.setAttribute('stroke', strokeColor);
                line.setAttribute('stroke-width', '2');
                line.setAttribute('stroke-dasharray', '5,5');
                
                // 添加箭头标记
                const markerId = 'arrow-' + className + '-' + parentClass;
                const marker = document.createElementNS('http://www.w3.org/2000/svg', 'marker');
                marker.setAttribute('id', markerId);
                marker.setAttribute('viewBox', '0 0 10 10');
                marker.setAttribute('refX', '5');
                marker.setAttribute('refY', '5');
                marker.setAttribute('markerWidth', '6');
                marker.setAttribute('markerHeight', '6');
                marker.setAttribute('orient', 'auto');
                
                const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
                path.setAttribute('d', 'M 0 0 L 10 5 L 0 10 z');
                path.setAttribute('fill', strokeColor);
                
                marker.appendChild(path);
                svg.appendChild(marker);
                line.setAttribute('marker-end', 'url(#' + markerId + ')');
                
                svg.appendChild(line);
            }
        });
    }
    
    function resetView() {
        document.querySelectorAll('.class-node').forEach(node => {
            node.style.boxShadow = '0 3px 6px rgba(0, 0, 0, 0.1)';
            node.style.transform = '';
            node.style.zIndex = '2';
            node.style.opacity = '1';
        });
        
        document.getElementById('classSearch').value = '';
    }
    
    // 页面加载完成后执行
    document.addEventListener('DOMContentLoaded', function() {
        // 初始布局
        arrangeNodesByCategory();
        
        // 为类列表项添加点击事件
        document.querySelectorAll('.class-item').forEach(item => {
            item.addEventListener('click', function() {
                const className = this.querySelector('.class-name').textContent.split(' (')[0];
                highlightClass(className);
            });
        });
        
        // 为类节点添加点击事件
        document.querySelectorAll('.class-node').forEach(node => {
            node.addEventListener('click', function() {
                const className = this.getAttribute('data-class');
                highlightClass(className);
            });
        });
        
        // 为搜索框添加输入事件
        document.getElementById('classSearch').addEventListener('input', filterClasses);
        
        // 窗口大小改变时重新布局
        window.addEventListener('resize', function() {
            setTimeout(arrangeNodesByCategory, 100);
        });
    });
</script>");

        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // 容器开始
        html.AppendLine("<div class='container'>");

        // 头部
        html.AppendLine("    <div class='header'>");
        html.AppendLine($"        <h1>岐黄镇略游戏类关系图</h1>");
        html.AppendLine($"        <div class='subtitle'>生成时间: {DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss")}</div>");
        html.AppendLine("    </div>");

        // 统计信息
        html.AppendLine("    <div class='stats'>");
        html.AppendLine("        <div class='stat-card'>");
        html.AppendLine($"            <div class='stat-value'>{classInfos.Count}</div>");
        html.AppendLine("            <div class='stat-label'>总类数</div>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class='stat-card'>");
        html.AppendLine($"            <div class='stat-value'>{inheritanceMap.Count}</div>");
        html.AppendLine("            <div class='stat-label'>父类数量</div>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class='stat-card'>");
        html.AppendLine($"            <div class='stat-value'>{CalculateTotalInheritanceRelations()}</div>");
        html.AppendLine("            <div class='stat-label'>继承关系</div>");
        html.AppendLine("        </div>");
        html.AppendLine($"        <div class='stat-card total-lines'>");
        html.AppendLine($"            <div class='stat-value'>{totalCodeLines}</div>");
        html.AppendLine("            <div class='stat-label'>总代码行数</div>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        // 分类统计
        var categoryStats = classInfos
            .GroupBy(c => c.Category)
            .Select(g => new { Category = g.Key, Count = g.Count(), TotalLines = g.Sum(c => c.CodeLines) })
            .OrderBy(s => s.Category)
            .ToList();

        html.AppendLine("    <div class='category-stats'>");
        foreach (var stat in categoryStats)
        {
            string categoryName = GetCategoryDisplayName(stat.Category);
            string categoryIcon = GetCategoryIcon(stat.Category);
            string categoryClass = stat.Category.ToString().ToLower();

            html.AppendLine("        <div class='category-stat'>");
            html.AppendLine($"            <div class='category-icon'>{categoryIcon}</div>");
            html.AppendLine($"            <div class='category-name'>{categoryName}</div>");
            html.AppendLine($"            <div class='category-count'>{stat.Count} 个类</div>");
            html.AppendLine($"            <div style='font-size: 0.9rem; color: #6c757d;'>{stat.TotalLines} 行代码</div>");
            html.AppendLine("        </div>");
        }
        html.AppendLine("    </div>");

        // 主要内容区域
        html.AppendLine("    <div class='main-content'>");

        // 左侧：类列表
        html.AppendLine("        <div class='class-list-section'>");
        html.AppendLine("            <h2>📋 类列表</h2>");
        html.AppendLine("            <div class='search-box'>");
        html.AppendLine("                <input type='text' id='classSearch' placeholder='搜索类名...' />");
        html.AppendLine("            </div>");

        if (classInfos.Count == 0)
        {
            html.AppendLine("            <div class='no-classes'>");
            html.AppendLine("                <p>未找到任何脚本类</p >");
            html.AppendLine("                <p>请确保脚本存放在 Assets/Scripts/ 目录下</p >");
            html.AppendLine("            </div>");
        }
        else
        {
            html.AppendLine("            <div class='class-list'>");
            foreach (ClassInfo info in classInfos.OrderBy(c => c.Name))
            {
                string categoryClass = info.Category.ToString().ToLower();
                string categoryIcon = GetCategoryIcon(info.Category);

                html.AppendLine($"            <div class='class-item {categoryClass}'>");
                html.AppendLine($"                <div class='class-name'>{categoryIcon} {info.Name}</div>");
                html.AppendLine($"                <div class='class-info'>");
                html.AppendLine($"                    父类: {info.BaseClassName}<br>");
                html.AppendLine($"                    命名空间: {info.Namespace ?? "全局"}");
                html.AppendLine($"                </div>");
                html.AppendLine($"                <div class='class-lines'>📝 {info.CodeLines} 行代码</div>");
                html.AppendLine("            </div>");
            }
            html.AppendLine("            </div>");
        }
        html.AppendLine("        </div>");

        // 右侧：继承关系图
        html.AppendLine("        <div class='diagram-section'>");
        html.AppendLine("            <h2>🏗️ 继承关系图</h2>");
        html.AppendLine("            <div class='controls'>");
        html.AppendLine("                <button class='control-btn' onclick='arrangeNodesByCategory()'>重新布局</button>");
        html.AppendLine("                <button class='control-btn' onclick='resetView()'>重置视图</button>");
        html.AppendLine("                <button class='control-btn' onclick='drawInheritanceLines()'>重新绘制连线</button>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class='inheritance-diagram'>");

        // 创建SVG容器用于绘制连线
        html.AppendLine("                <svg id='inheritance-svg'></svg>");

        // 创建分类区域
        html.AppendLine("                <div class='diagram-area'>");

        // 添加分类区域标签
        html.AppendLine("                    <div class='category-area mono'>");
        html.AppendLine("                        <div class='category-label mono'>🎮 MonoBehaviour类</div>");
        html.AppendLine("                    </div>");

        html.AppendLine("                    <div class='category-area info'>");
        html.AppendLine("                        <div class='category-label info'>📄 配置数据类(Info)</div>");
        html.AppendLine("                    </div>");

        html.AppendLine("                    <div class='category-area data'>");
        html.AppendLine("                        <div class='category-label data'>💾 玩家数据类(Data)</div>");
        html.AppendLine("                    </div>");

        html.AppendLine("                    <div class='category-area manager'>");
        html.AppendLine("                        <div class='category-label manager'>⚙️ 管理器类</div>");
        html.AppendLine("                    </div>");

        html.AppendLine("                    <div class='category-area other'>");
        html.AppendLine("                        <div class='category-label other'>📦 其他类</div>");
        html.AppendLine("                    </div>");

        // 生成类节点
        int nodeId = 0;
        foreach (ClassInfo info in classInfos)
        {
            string categoryClass = info.Category.ToString().ToLower();
            string categoryIcon = GetCategoryIcon(info.Category);

            html.AppendLine($"                <div class='class-node {categoryClass}' id='node-{nodeId}' data-class='{info.Name}' data-parent='{info.BaseClassName}'>");
            html.AppendLine($"                    <div class='node-name'>{categoryIcon} {info.Name}</div>");
            html.AppendLine($"                    <div class='node-lines'>📝 {info.CodeLines} 行</div>");

            if (info.BaseClassName != null && info.BaseClassName != "无" && info.BaseClassName != "object" && info.BaseClassName != "Object")
            {
                html.AppendLine($"                    <div class='node-parent'>继承自: {info.BaseClassName}</div>");
            }
            else
            {
                html.AppendLine($"                    <div class='node-parent'>无父类</div>");
            }

            if (!string.IsNullOrEmpty(info.Namespace))
            {
                html.AppendLine($"                    <div class='node-namespace'>{info.Namespace}</div>");
            }

            html.AppendLine("                </div>");
            nodeId++;
        }

        html.AppendLine("                </div>"); // diagram-area结束
        html.AppendLine("            </div>"); // inheritance-diagram结束

        // 图例
        html.AppendLine("            <div class='inheritance-legend'>");
        html.AppendLine("                <div class='legend-item'>");
        html.AppendLine("                    <div class='legend-color mono'></div>");
        html.AppendLine("                    <span>MonoBehaviour类</span>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class='legend-item'>");
        html.AppendLine("                    <div class='legend-color info'></div>");
        html.AppendLine("                    <span>配置数据类(Info)</span>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class='legend-item'>");
        html.AppendLine("                    <div class='legend-color data'></div>");
        html.AppendLine("                    <span>玩家数据类(Data)</span>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class='legend-item'>");
        html.AppendLine("                    <div class='legend-color manager'></div>");
        html.AppendLine("                    <span>管理器类</span>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class='legend-item'>");
        html.AppendLine("                    <div class='legend-color other'></div>");
        html.AppendLine("                    <span>其他类</span>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class='legend-item'>");
        html.AppendLine("                    <div class='legend-color line'></div>");
        html.AppendLine("                    <span>继承关系线</span>");
        html.AppendLine("                </div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");

        // 底部
        html.AppendLine("    <div class='footer'>");
        html.AppendLine("        <p>使用说明：</p >");
        html.AppendLine("        <p>1. 点击左侧类列表或右侧类节点可以高亮显示该类及其继承关系</p >");
        html.AppendLine("        <p>2. 使用搜索框可以过滤类和节点</p >");
        html.AppendLine("        <p>3. 点击控制按钮可以重新布局、重置视图或重新绘制连线</p >");
        html.AppendLine($"        <p>统计: 总类数 {classInfos.Count} | MonoBehaviour类 {classInfos.Count(c => c.Category == ClassCategory.MonoBehaviour)} | 配置数据类 {classInfos.Count(c => c.Category == ClassCategory.DataInfo)} | 玩家数据类 {classInfos.Count(c => c.Category == ClassCategory.PlayerData)} | 管理器类 {classInfos.Count(c => c.Category == ClassCategory.Manager)} | 其他类 {classInfos.Count(c => c.Category == ClassCategory.Other)} | 总代码行数 {totalCodeLines}</p >");
        html.AppendLine("    </div>");

        html.AppendLine("</div>"); // 容器结束

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private int CalculateTotalInheritanceRelations()
    {
        int total = 0;
        foreach (var kvp in inheritanceMap)
        {
            total += kvp.Value.Count;
        }
        return total;
    }
}