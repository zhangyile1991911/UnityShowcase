//
// ShaderGraphExtensions for Unity
// (c) 2020 PH Graphics
// Source code may be used and modified for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 
// *** A NOTE ABOUT PIRACY ***
// 
// If you got this asset from a pirate site, please consider buying it from the Unity asset store. This asset is only legally available from the Unity Asset Store.
// 
// I'm a single indie dev supporting my family by spending hundreds and thousands of hours on this and other assets. It's very offensive, rude and just plain evil to steal when I (and many others) put so much hard work into the software.
// 
// Thank you.
//
// *** END NOTE ABOUT PIRACY ***
//

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

namespace ShaderGraphExtensions
{
    [InitializeOnLoad]
    public class DebugValueGettingStartedWindowShow
    {
        static DebugValueGettingStartedWindowShow()
        {
            var settingsGUID = AssetDatabase.FindAssets("t:DebugValueSettings");
            bool showWindow = settingsGUID == null || settingsGUID.Length < 1;
            if (showWindow)
            {
                EditorApplication.update += OnUpdate;

            }
        }

        private static void OnUpdate()
        {
            EditorApplication.update -= OnUpdate;
            
            DebugValueSettings newSettings = ScriptableObject.CreateInstance<DebugValueSettings>();
            
            // save at the same location as other scripts
            var script = MonoScript.FromScriptableObject(newSettings);
            var scriptPath = AssetDatabase.GetAssetPath(script);
            var directory = Path.GetDirectoryName(scriptPath);
            var path = Path.Combine(directory, "Settings.asset");
            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DebugValueGettingStartedWindow window = (DebugValueGettingStartedWindow)EditorWindow.GetWindow(typeof(DebugValueGettingStartedWindow));
            window.Show();
        }
    }

    public class DebugValueGettingStartedWindow : EditorWindow
    {
        private const string SGEDebugValueVersion = "1.0.2";
        
        private const string LogoFileName = "ShaderGraphExtension_DebugValue_key_128x128";
        private const string ManualFileName = "ShaderGraphExtension_DebugValue_Documentation";
        private const string ChangeLogName = "ShaderGraphExtension_DebugValue_ChangeLog";

        private GUIStyle _wrapLabelStyle;

        private Texture2D _logoTexture;

        private bool _hasError;
        
        // various sizes
        private const int LogoTextureSize = 128;
        private const int Margin = 10;
        private const int ButtonHeight = 30;
        private const int ButtonWidth = 120;
        private const int LargeButtonWidth = 160;
        
        private Vector2 _scrollPosition = Vector2.zero;
        private Vector2 _defaultWindowSize = Vector2.zero;

        private bool _initialized = false;
        
        [MenuItem("Tools/ShaderGraph Extensions/Debug Value/Getting Started")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            DebugValueGettingStartedWindow window = (DebugValueGettingStartedWindow)EditorWindow.GetWindow(typeof(DebugValueGettingStartedWindow));
            window.Show();
        }

        private bool GetInternalFile(string fileName, out string fullPath)
        {
            var fileGUIDs = AssetDatabase.FindAssets(fileName);

            if (fileGUIDs == null || fileGUIDs.Length != 1)
            {
                Debug.LogError("File " + fileName + " not found or exist several time in the asset project. Have you correctly imported the ShaderGraphExtensions DebugValue?");
                _hasError = true;
                fullPath = String.Empty;
                return false;
            }

            fullPath = AssetDatabase.GUIDToAssetPath(fileGUIDs[0]);
            return true;
        }

        private void Awake()
        {
            string fullLogoPath;
            if (!GetInternalFile(LogoFileName, out fullLogoPath))
            {
                return;
            }

            _logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(fullLogoPath);
        }

        private bool OpenFileWithDefaultEditor(string path)
        {
            string fullPath;
            if (!GetInternalFile(path, out fullPath))
            {
                return false;
            }

            fullPath = Path.GetFullPath(fullPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError("File " + fullPath + " doesn't exist. Did you move the ShaderGraphEssentials root folder from Assets/ ? Unfortunately this isn't supported yet.");
                _hasError = true;
                return false;
            }
            
#if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start($@"{fullPath}");
#elif UNITY_EDITOR_OSX
            EditorUtility.RevealInFinder($@"{fullPath}");
#endif

            return true;
        }

        private bool CheckForErrors()
        {
            if (_hasError)
            {
                GUILayout.Label(
                    "There was an error constructing this window. Please check your console for errors. If you can't fix it, please don't hesitate to ask for support");
                return true;
            }

            return false;
        }

        private void InitializeWindow()
        {
            titleContent.text = "Getting Started";
            minSize = new Vector2(250, 270);
            
            _defaultWindowSize = new Vector2(450, 270);
            Vector2 initialPosition = 0.5f * (new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) - _defaultWindowSize);
            position = new Rect(initialPosition, _defaultWindowSize);
            
            _wrapLabelStyle = new GUIStyle(EditorStyles.label) {wordWrap = true};
        }

        void OnGUI()
        {
            if (!_initialized)
            {
                InitializeWindow();
                _initialized = true;
            }

            if (CheckForErrors()) return;

            _scrollPosition = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), _scrollPosition,
                new Rect(0, 0, _defaultWindowSize.x - 10, _defaultWindowSize.y - 10), false, false);

            float yOffset = 0;
            float defaultXSize = position.width - Margin - Margin;
            
            // Header
            GUI.BeginGroup(new Rect(Margin, Margin, defaultXSize, LogoTextureSize));

            float xOffset = 0;
            GUI.DrawTexture(new Rect(xOffset, 0, LogoTextureSize, LogoTextureSize), _logoTexture);
            xOffset += LogoTextureSize + Margin;
            
            GUI.Label(new Rect(xOffset, 0, 100, 30), "Version: " + SGEDebugValueVersion);
            if (GUI.Button(new Rect(xOffset, 30, ButtonWidth, ButtonHeight), "View Changelog"))
            {
                OpenChangelog();
                if (CheckForErrors()) return;
            }
            
            if (GUI.Button(new Rect(xOffset, 30 + ButtonHeight + Margin, ButtonWidth, ButtonHeight), "View Manual"))
            {
                OpenManual();
                if (CheckForErrors()) return;
            }

            xOffset += ButtonWidth + Margin;
            
            if (GUI.Button(new Rect(xOffset, 30, LargeButtonWidth, ButtonHeight), "View Offline Changelog"))
            {
                OpenFileWithDefaultEditor(ChangeLogName);
                if (CheckForErrors()) return;
            }
            
            if (GUI.Button(new Rect(xOffset, 30 + ButtonHeight + Margin, LargeButtonWidth, ButtonHeight), "View Offline Manual"))
            {
                OpenFileWithDefaultEditor(ManualFileName);
                if (CheckForErrors()) return;
            }
            
            GUI.EndGroup();

            yOffset += Margin + LogoTextureSize; 
            
            GUI.Label(new Rect(Margin, yOffset + Margin, defaultXSize, 10), "", GUI.skin.horizontalSlider);

            yOffset += Margin + Margin;
            
            // Getting started title
            yOffset += Margin;
            GUI.BeginGroup(new Rect(Margin, yOffset, defaultXSize, 20));
            
            GUI.Label(new Rect(defaultXSize / 2f - 150, 0, 300, 20), "SG Debug Value is already ready to be used!", EditorStyles.boldLabel);

            GUI.EndGroup();

            yOffset += Margin;
            
            GUI.Label(new Rect(Margin, yOffset + Margin, defaultXSize, 10), "", GUI.skin.horizontalSlider);

            yOffset += Margin + Margin;

            // Help title
            yOffset += Margin;
            GUI.BeginGroup(new Rect(Margin, yOffset, defaultXSize, 30));
            
            GUI.Label(new Rect(defaultXSize / 2f - 15, 0, 100, 20), "Help", EditorStyles.largeLabel);
            
            GUI.EndGroup();

            yOffset += Margin + 20;
            
            // Help
            GUI.BeginGroup(new Rect(Margin, yOffset, defaultXSize, ButtonHeight));
            
            if (GUI.Button(new Rect(0, 0, defaultXSize / 2 - Margin, ButtonHeight), "Discord"))
            {
                OpenDiscordHelp();
            }
            
            if (GUI.Button(new Rect(defaultXSize / 2 + Margin, 0, defaultXSize / 2 - Margin, ButtonHeight), "Email"))
            {
                OpenEmailHelp();
            }
            
            
            GUI.EndGroup();
            
            GUI.EndScrollView();
        }

        private void OpenEmailHelp()
        {
            Application.OpenURL("mailto:ph.graphics.unity@gmail.com");
        }

        private void OpenDiscordHelp()
        {
            Application.OpenURL("https://discord.gg/ksURBah");
        }

        private void OpenManual()
        {
            Application.OpenURL("http://assetstore.phbarralis.com/sgex/debug_value.html");
        }

        private void OpenChangelog()
        {
            Application.OpenURL("http://assetstore.phbarralis.com/sgex/debug_value_changelog.html");
        }
    }
}
