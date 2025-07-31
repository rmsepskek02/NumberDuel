using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ProjectScriptAnalyzer : EditorWindow
{
    private Vector2 scrollPosition;
    private bool[] scriptSelections;
    private string[] allScriptPaths;
    private string[] scriptNames;
    private string searchFilter = "";

    [MenuItem("Tools/Analyze Project Scripts")]
    public static void ShowWindow()
    {
        GetWindow<ProjectScriptAnalyzer>("Script Analyzer");
    }

    [MenuItem("Tools/Export ALL Scripts to Console")]
    public static void ExportAllScriptsToConsole()
    {
        Debug.Log("=== ALL SCRIPTS ANALYSIS START ===");

        // Find all .cs files in Scripts folder
        string[] scriptFiles = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);

        Debug.Log($"Found {scriptFiles.Length} script files");

        foreach (string filePath in scriptFiles)
        {
            ExportSingleScript(filePath);
        }

        Debug.Log("=== ALL SCRIPTS ANALYSIS COMPLETE ===");
    }

    [MenuItem("Tools/Export Core Game Scripts")]
    public static void ExportCoreGameScripts()
    {
        string[] coreScripts = {
            "Assets/Scripts/Managers/NetworkGameManager.cs",
            "Assets/Scripts/Managers/InGameManager.cs",
            "Assets/Scripts/Managers/InGameUIManager.cs",
            "Assets/Scripts/Managers/TurnManager.cs",
            "Assets/Scripts/Managers/PhotonManager.cs",
            "Assets/Scripts/Objects/UIItem/CardModeSelector.cs",
            "Assets/Scripts/Objects/Card/CardZone.cs"
        };

        Debug.Log("=== CORE GAME SCRIPTS ANALYSIS START ===");

        foreach (string filePath in coreScripts)
        {
            ExportSingleScript(filePath);
        }

        Debug.Log("=== CORE GAME SCRIPTS ANALYSIS COMPLETE ===");
    }

    [MenuItem("Tools/Export Manager Scripts Only")]
    public static void ExportManagerScripts()
    {
        Debug.Log("=== MANAGER SCRIPTS ANALYSIS START ===");

        string[] managerFiles = Directory.GetFiles("Assets/Scripts/Managers", "*.cs", SearchOption.TopDirectoryOnly);

        foreach (string filePath in managerFiles)
        {
            ExportSingleScript(filePath);
        }

        Debug.Log("=== MANAGER SCRIPTS ANALYSIS COMPLETE ===");
    }

    [MenuItem("Tools/Export UI Scripts Only")]
    public static void ExportUIScripts()
    {
        Debug.Log("=== UI SCRIPTS ANALYSIS START ===");

        // Search UI-related folders
        string[] uiFolders = {
            "Assets/Scripts/Objects/UIItem",
            "Assets/Scripts/Objects/Joker"
        };

        foreach (string folder in uiFolders)
        {
            if (Directory.Exists(folder))
            {
                string[] uiFiles = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
                foreach (string filePath in uiFiles)
                {
                    ExportSingleScript(filePath);
                }
            }
        }

        Debug.Log("=== UI SCRIPTS ANALYSIS COMPLETE ===");
    }

    private static void ExportSingleScript(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                string content = File.ReadAllText(filePath);

                Debug.Log($"=== SCRIPT: {fileName} ===");

                // Split content if too long
                if (content.Length > 8000)
                {
                    Debug.Log("=== CONTENT (PART 1) ===");
                    Debug.Log(content.Substring(0, 8000));
                    Debug.Log("=== CONTENT (PART 2) ===");
                    Debug.Log(content.Substring(8000));
                }
                else
                {
                    Debug.Log(content);
                }

                Debug.Log($"=== END: {fileName} ===");
                Debug.Log("");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error reading {filePath}: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"File not found: {filePath}");
        }
    }

    void OnEnable()
    {
        RefreshScriptList();
    }

    void RefreshScriptList()
    {
        allScriptPaths = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);
        scriptNames = new string[allScriptPaths.Length];
        scriptSelections = new bool[allScriptPaths.Length];

        for (int i = 0; i < allScriptPaths.Length; i++)
        {
            scriptNames[i] = Path.GetFileName(allScriptPaths[i]);
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Project Script Analyzer", EditorStyles.boldLabel);

        // Quick action buttons
        GUILayout.Label("Quick Actions:", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("All Scripts"))
        {
            ExportAllScriptsToConsole();
        }
        if (GUILayout.Button("Core Game Scripts"))
        {
            ExportCoreGameScripts();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Manager Scripts"))
        {
            ExportManagerScripts();
        }
        if (GUILayout.Button("UI Scripts"))
        {
            ExportUIScripts();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        // Individual script selection
        GUILayout.Label("Individual Script Selection:", EditorStyles.boldLabel);

        // Search filter
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = GUILayout.TextField(searchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchFilter = "";
        }
        GUILayout.EndHorizontal();

        // Select all/none buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All Visible"))
        {
            for (int i = 0; i < scriptNames.Length; i++)
            {
                if (string.IsNullOrEmpty(searchFilter) ||
                    scriptNames[i].ToLower().Contains(searchFilter.ToLower()))
                {
                    scriptSelections[i] = true;
                }
            }
        }
        if (GUILayout.Button("Deselect All"))
        {
            for (int i = 0; i < scriptSelections.Length; i++)
            {
                scriptSelections[i] = false;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Script list
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < scriptNames.Length; i++)
        {
            if (string.IsNullOrEmpty(searchFilter) ||
                scriptNames[i].ToLower().Contains(searchFilter.ToLower()))
            {
                scriptSelections[i] = GUILayout.Toggle(scriptSelections[i], scriptNames[i]);
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(10);

        // Export selected scripts
        if (GUILayout.Button("Export Selected Scripts"))
        {
            Debug.Log("=== SELECTED SCRIPTS ANALYSIS START ===");

            for (int i = 0; i < scriptSelections.Length; i++)
            {
                if (scriptSelections[i])
                {
                    ExportSingleScript(allScriptPaths[i]);
                }
            }

            Debug.Log("=== SELECTED SCRIPTS ANALYSIS COMPLETE ===");
        }

        GUILayout.Space(20);
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("• Use Quick Actions for common script groups");
        GUILayout.Label("• Or select individual scripts and export them");
        GUILayout.Label("• Claude will read console logs via MCP");
        GUILayout.Label("• Results appear in Console window");
    }
}