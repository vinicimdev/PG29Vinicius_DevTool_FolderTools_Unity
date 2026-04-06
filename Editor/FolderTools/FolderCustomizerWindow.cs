// FolderCustomizerWindow.cs
using UnityEditor;
using UnityEngine;

namespace FolderTools
{
    public class FolderCustomizerWindow : EditorWindow
    {
        string              currentGUID;
        FolderColorDatabase database;
        Color               selectedColor;
        string              selectedIcon;

        int  activeTab  = 0;
        bool iconsDirty = true;

        (string icon, string label, Texture2D tex)[] validIcons;

        static readonly (Color color, string label)[] Palette = new[]
        {
            (new Color(0.90f, 0.30f, 0.30f), "Red"),
            (new Color(0.93f, 0.58f, 0.18f), "Orange"),
            (new Color(0.92f, 0.85f, 0.22f), "Yellow"),
            (new Color(0.28f, 0.75f, 0.38f), "Green"),
            (new Color(0.22f, 0.60f, 0.90f), "Blue"),
            (new Color(0.58f, 0.30f, 0.90f), "Purple"),
            (new Color(0.90f, 0.38f, 0.70f), "Pink"),
            (new Color(0.28f, 0.78f, 0.78f), "Cyan"),
            (new Color(0.62f, 0.44f, 0.26f), "Brown"),
            (new Color(0.55f, 0.55f, 0.55f), "Gray"),
        };

        static readonly (string icon, string label)[] IconCatalog = new[]
        {
            ("cs Script Icon",              "C# Script"),
            ("Assembly Icon",               "Assembly"),
            ("Shader Icon",                 "Shader"),
            ("ComputeShader Icon",          "Compute"),
            ("Material Icon",               "Material"),
            ("Texture2D Icon",              "Texture"),
            ("Sprite Icon",                 "Sprite"),
            ("SpriteAtlas Icon",            "Sprite Atlas"),
            ("Font Icon",                   "Font"),
            ("AnimationClip Icon",          "Animation"),
            ("Animator Icon",               "Animator"),
            ("AvatarMask Icon",             "Avatar Mask"),
            ("TimelineAsset Icon",          "Timeline"),
            ("Prefab Icon",                 "Prefab"),
            ("PrefabVariant Icon",          "Prefab Var"),
            ("SceneAsset Icon",             "Scene"),
            ("AudioClip Icon",              "Audio Clip"),
            ("AudioMixerController Icon",   "Audio Mixer"),
            ("PhysicsMaterial Icon",        "Physics Mat"),
            ("PhysicsMaterial2D Icon",      "Physics 2D"),
            ("ScriptableObject Icon",       "Scriptable"),
            ("TextAsset Icon",              "Text Asset"),
            ("MonoScript Icon",             "Mono Script"),
            ("Canvas Icon",                 "Canvas"),
            ("RenderTexture Icon",          "Render Tex"),
            ("Settings Icon",               "Settings"),
            ("BuildSettings.Editor",        "Build"),
            ("console.infoicon",            "Info"),
            ("console.warnicon",            "Warning"),
            ("console.erroricon",           "Error"),
            ("Folder Icon",                 "Folder"),
            ("FolderOpened Icon",           "Folder Open"),
            ("Favorite Icon",               "Favorite"),
            ("d_Favorite Icon",             "Favorite (d)"),
            ("lightMeter/greenLight",       "Green"),
            ("lightMeter/orangeLight",      "Orange"),
            ("lightMeter/redLight",         "Red"),
            ("TestPassed",                  "Test Pass"),
            ("TestFailed",                  "Test Fail"),
            ("TestIgnored",                 "Test Skip"),
            ("Camera Icon",                 "Camera"),
            ("Light Icon",                  "Light"),
            ("ParticleSystem Icon",         "Particles"),
            ("Terrain Icon",                "Terrain"),
            ("d_NavMeshAgent Icon",         "Nav Mesh"),
        };

        public static void Open(string guid, FolderColorDatabase db)
        {
            var w = CreateInstance<FolderCustomizerWindow>();
            w.titleContent  = new GUIContent("Folder Customizer");
            w.minSize       = new Vector2(320f, 360f);
            w.maxSize       = new Vector2(320f, 360f);
            w.currentGUID   = guid;
            w.database      = db;
            w.selectedColor = db.GetColor(guid);
            w.selectedIcon  = db.GetIcon(guid);
            w.ShowUtility();
        }

        void OnGUI()
        {
            activeTab = GUILayout.Toolbar(activeTab, new[] { "Color", "Icon" });
            GUILayout.Space(8f);

            if (activeTab == 0) DrawColorTab();
            else                DrawIconTab();

            GUILayout.FlexibleSpace();
            DrawFooter();
        }

        void DrawColorTab()
        {
            GUILayout.Label("Quick palette", EditorStyles.boldLabel);
            GUILayout.Space(4f);

            const float btnSize = 26f;
            const float gap     = 4f;
            Rect row = GUILayoutUtility.GetRect(0, btnSize + 8f);

            for (int i = 0; i < Palette.Length; i++)
            {
                Rect btn = new Rect(10f + i * (btnSize + gap), row.y + 4f, btnSize, btnSize);

                if (ColorApproxEqual(selectedColor, Palette[i].color))
                    EditorGUI.DrawRect(new Rect(btn.x - 2, btn.y - 2, btn.width + 4, btn.height + 4), Color.white);

                EditorGUI.DrawRect(btn, Palette[i].color);

                if (GUI.Button(btn, new GUIContent("", Palette[i].label), GUIStyle.none))
                    selectedColor = Palette[i].color;
            }

            GUILayout.Space(12f);

            EditorGUI.BeginChangeCheck();
            Color picked = EditorGUILayout.ColorField("Custom color", selectedColor);
            if (EditorGUI.EndChangeCheck()) selectedColor = picked;

            GUILayout.Space(8f);

            if (GUILayout.Button("Clear color")) selectedColor = Color.clear;
        }

        Vector2 iconScroll;

        void LoadIcons()
        {
            iconsDirty = false;
            var list = new System.Collections.Generic.List<(string, string, Texture2D)>();

            foreach (var (icon, label) in IconCatalog)
            {
                Texture2D tex = EditorGUIUtility.FindTexture(icon);
                if (tex == null)
                {
                    try { tex = EditorGUIUtility.IconContent(icon)?.image as Texture2D; }
                    catch { }
                }
                if (tex != null) list.Add((icon, label, tex));
            }

            validIcons = list.ToArray();
        }

        void DrawIconTab()
        {
            if (iconsDirty) LoadIcons();

            GUILayout.Label("Choose an icon  (click again to deselect)", EditorStyles.boldLabel);
            GUILayout.Space(4f);

            const float cellSize = 52f;
            const float padding  = 6f;
            int cols = Mathf.FloorToInt((320f - 20f) / (cellSize + padding));

            iconScroll = GUILayout.BeginScrollView(iconScroll, GUILayout.Height(236f));

            int col = 0;
            GUILayout.BeginHorizontal();

            foreach (var (icon, label, tex) in validIcons)
            {
                bool isSelected = selectedIcon == icon;
                Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize + 16f,
                    GUILayout.Width(cellSize), GUILayout.Height(cellSize + 16f));

                if (isSelected)
                    EditorGUI.DrawRect(
                        new Rect(cellRect.x - 2, cellRect.y - 2, cellSize + 4, cellSize + 4),
                        new Color(0.25f, 0.55f, 1f, 0.5f));

                GUI.DrawTexture(
                    new Rect(cellRect.x + (cellSize - 32f) * 0.5f, cellRect.y + 4f, 32f, 32f),
                    tex, ScaleMode.ScaleToFit);

                GUI.Label(
                    new Rect(cellRect.x, cellRect.y + 38f, cellSize, 16f), label,
                    new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });

                if (Event.current.type == EventType.MouseDown
                    && cellRect.Contains(Event.current.mousePosition))
                {
                    selectedIcon = isSelected ? "" : icon;
                    Event.current.Use();
                    Repaint();
                }

                col++;
                if (col >= cols)
                {
                    col = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            if (GUILayout.Button("Clear icon")) selectedIcon = "";
        }

        void DrawFooter()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply"))
            {
                database.SetColor(currentGUID, selectedColor);
                database.SetIcon(currentGUID, selectedIcon);
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                EditorApplication.RepaintProjectWindow();
                Close();
            }

            if (GUILayout.Button("Cancel")) Close();

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        static bool ColorApproxEqual(Color a, Color b, float t = 0.02f) =>
            Mathf.Abs(a.r - b.r) < t &&
            Mathf.Abs(a.g - b.g) < t &&
            Mathf.Abs(a.b - b.b) < t;
    }
}
