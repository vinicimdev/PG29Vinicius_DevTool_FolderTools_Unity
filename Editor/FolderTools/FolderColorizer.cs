// FolderColorizer.cs
using UnityEditor;
using UnityEngine;

namespace FolderTools
{
    [InitializeOnLoad]
    public static class FolderColorizer
    {
        static FolderColorDatabase database;
        static Texture2D folderTex;

        static FolderColorizer()
        {
            EditorApplication.projectWindowItemOnGUI += OnGUI;
            LoadDatabase();
        }

        static void LoadDatabase()
        {
            EnsureDataFolder();
            database = AssetDatabase.LoadAssetAtPath<FolderColorDatabase>(FolderColorDatabase.AssetPath);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<FolderColorDatabase>();
                AssetDatabase.CreateAsset(database, FolderColorDatabase.AssetPath);
                AssetDatabase.SaveAssets();
            }
        }

        static void EnsureDataFolder()
        {
            if (!AssetDatabase.IsValidFolder(FolderColorDatabase.DataPath))
                AssetDatabase.CreateFolder("Assets", "FolderToolsData");
        }

        static void OnGUI(string guid, Rect selectionRect)
        {
            if (database == null) LoadDatabase();

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path)) return;

            Color  color    = database.GetColor(guid);
            string iconName = database.GetIcon(guid);

            bool hasColor = color.a > 0f;
            bool hasIcon  = !string.IsNullOrEmpty(iconName);

            if (!hasColor && !hasIcon) return;

            if (folderTex == null)
                folderTex = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            if (folderTex == null) return;

            bool isListView = selectionRect.height <= 20f;
            Rect iconRect;

            if (isListView)
            {
                float size = selectionRect.height;
                iconRect = new Rect(selectionRect.x, selectionRect.y, size, size);
            }
            else
            {
                float iconHeight = selectionRect.height - 16f;
                iconRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, iconHeight);
            }

            Color prev = GUI.color;

            if (hasColor)
            {
                Color bgColor = EditorGUIUtility.isProSkin
                    ? new Color(0.2196f, 0.2196f, 0.2196f, 1f)
                    : new Color(0.7843f, 0.7843f, 0.7843f, 1f);
                EditorGUI.DrawRect(iconRect, bgColor);

                GUI.color = color;
                GUI.DrawTexture(iconRect, folderTex, ScaleMode.ScaleToFit);

                GUI.color = new Color(1f, 1f, 1f, 0.25f);
                GUI.DrawTexture(iconRect, folderTex, ScaleMode.ScaleToFit);
            }

            if (hasIcon)
            {
                Texture2D tex = EditorGUIUtility.FindTexture(iconName);
                if (tex != null)
                {
                    float badgeSize;
                    Rect  badgeRect;

                    if (isListView)
                    {
                        badgeSize = iconRect.height * 0.55f;
                        badgeRect = new Rect(
                            iconRect.xMax - badgeSize + 2f,
                            iconRect.yMax - badgeSize + 2f,
                            badgeSize, badgeSize);
                    }
                    else
                    {
                        badgeSize = iconRect.width * 0.45f;
                        badgeRect = new Rect(
                            iconRect.xMax - badgeSize,
                            iconRect.yMax - badgeSize,
                            badgeSize, badgeSize);
                    }

                    GUI.color = new Color(0f, 0f, 0f, 0.4f);
                    GUI.DrawTexture(new Rect(badgeRect.x + 1, badgeRect.y + 1, badgeRect.width, badgeRect.height),
                        tex, ScaleMode.ScaleToFit);

                    GUI.color = Color.white;
                    GUI.DrawTexture(badgeRect, tex, ScaleMode.ScaleToFit);
                }
            }

            GUI.color = prev;

            Event e = Event.current;
            if (e.alt && e.type == EventType.MouseDown && selectionRect.Contains(e.mousePosition))
            {
                OpenWindow(guid);
                e.Use();
            }
        }

        static void OpenWindow(string guid)
        {
            if (database == null) LoadDatabase();
            FolderCustomizerWindow.Open(guid, database);
        }

        [MenuItem("Assets/Folder/Customize %&c", false, 1000)]
        static void CustomizeFolder()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(path)) return;
            OpenWindow(AssetDatabase.AssetPathToGUID(path));
        }

        [MenuItem("Assets/Folder/Customize %&c", true)]
        static bool ValidateCustomize() =>
            AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

        [MenuItem("Assets/Folder/Clear All", false, 1001)]
        static void ClearAll()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(path)) return;
            if (database == null) LoadDatabase();

            string guid = AssetDatabase.AssetPathToGUID(path);
            database.SetColor(guid, Color.clear);
            database.SetIcon(guid, "");
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            EditorApplication.RepaintProjectWindow();
        }

        [MenuItem("Assets/Folder/Clear All", true)]
        static bool ValidateClearAll() =>
            AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }
}
