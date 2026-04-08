using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FolderTools
{
    public class QuickAccessWindow : EditorWindow
    {
        [MenuItem("Window/Folder Tools/Quick Access %#q")]
        public static void Open()
        {
            var w = GetWindow<QuickAccessWindow>();
            w.titleContent = new GUIContent("Quick Access", EditorGUIUtility.FindTexture("Favorite Icon"));
            w.minSize = new Vector2(160f, 80f);
            w.Show();
        }

        QuickAccessDatabase database;

        bool isDragOver;
        int  dragReorderIndex = -1;
        int  dropTargetIndex  = -1;
        bool isReordering;

        Vector2 scroll;

        const float ROW_H     = 28f;
        const float ICON_SIZE = 18f;
        const float PAD_X     = 8f;

        void OnEnable()
        {
            LoadDatabase();
            wantsMouseMove = true;
        }

        void LoadDatabase()
        {
            EnsureDataFolder();
            database = AssetDatabase.LoadAssetAtPath<QuickAccessDatabase>(QuickAccessDatabase.AssetPath);

            if (database == null)
            {
                database = CreateInstance<QuickAccessDatabase>();
                AssetDatabase.CreateAsset(database, QuickAccessDatabase.AssetPath);
                AssetDatabase.SaveAssets();
            }
        }

        static void EnsureDataFolder()
        {
            if (!AssetDatabase.IsValidFolder(FolderColorDatabase.DataPath))
                AssetDatabase.CreateFolder("Assets", "FolderToolsData");
        }

        void OnGUI()
        {
            if (database == null) LoadDatabase();

            HandleDragAndDrop();
            DrawHeader();

            scroll = GUILayout.BeginScrollView(scroll);
            DrawEntries();
            GUILayout.EndScrollView();

            if (isDragOver)
            {
                var r = new Rect(0, 0, position.width, position.height);
                EditorGUI.DrawRect(r, new Color(0.25f, 0.55f, 1f, 0.12f));
                GUI.Label(r, "Drop to add folder",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    {
                        fontSize = 11,
                        normal   = { textColor = new Color(0.5f, 0.75f, 1f) }
                    });
            }
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Quick Access", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            var trashIcon = EditorGUIUtility.FindTexture("TreeEditor.Trash")
                         ?? EditorGUIUtility.FindTexture("d_TreeEditor.Trash");

            if (GUILayout.Button(
                new GUIContent(trashIcon, "Clear all"),
                EditorStyles.toolbarButton, GUILayout.Width(26)))
            {
                if (EditorUtility.DisplayDialog("Clear Quick Access",
                    "Remove all folders from Quick Access?", "Clear", "Cancel"))
                {
                    database.entries.Clear();
                    Save();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawEntries()
        {
            if (database.entries.Count == 0)
            {
                GUILayout.Space(16f);
                GUILayout.Label("Drag folders here", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Event e = Event.current;

            for (int i = 0; i < database.entries.Count; i++)
            {
                if (isReordering && dropTargetIndex == i) DrawDropLine();
                DrawRow(i, database.entries[i], e);
            }

            if (isReordering && dropTargetIndex == database.entries.Count) DrawDropLine();
        }

        void DrawRow(int index, QuickAccessDatabase.Entry entry, Event e)
        {
            string path  = AssetDatabase.GUIDToAssetPath(entry.guid);
            bool   valid = AssetDatabase.IsValidFolder(path);

            Rect rowRect = GUILayoutUtility.GetRect(0, ROW_H, GUILayout.ExpandWidth(true));
            bool isHover = rowRect.Contains(e.mousePosition);

            if (!valid)       EditorGUI.DrawRect(rowRect, new Color(1f, 0.3f, 0.3f, 0.08f));
            else if (isHover) EditorGUI.DrawRect(rowRect, new Color(1f, 1f,   1f,   0.05f));

            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.yMax - 1, rowRect.width, 1),
                new Color(1f, 1f, 1f, 0.04f));

            if (!valid) { DrawInvalidRow(rowRect, entry); return; }

            // Folder icon
            Texture2D folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
            if (folderIcon != null)
            {
                GUI.DrawTexture(
                    new Rect(rowRect.x + PAD_X, rowRect.y + (ROW_H - ICON_SIZE) * 0.5f, ICON_SIZE, ICON_SIZE),
                    folderIcon, ScaleMode.ScaleToFit);
            }

            DrawCustomIconBadge(rowRect, entry.guid);

            // Label
            string label = !string.IsNullOrEmpty(entry.customLabel)
                ? entry.customLabel
                : System.IO.Path.GetFileName(path);

            float labelX = rowRect.x + PAD_X + ICON_SIZE + 6f;
            GUI.Label(new Rect(labelX, rowRect.y, rowRect.width - labelX - 36f, ROW_H),
                label, new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, fontSize = 12 });

            // Remove button
            if (isHover)
            {
                var removeIcon = EditorGUIUtility.FindTexture("Toolbar Minus")
                              ?? EditorGUIUtility.FindTexture("d_Toolbar Minus")
                              ?? EditorGUIUtility.FindTexture("ol minus");

                Rect removeRect = new Rect(rowRect.xMax - 22f, rowRect.y + (ROW_H - 16f) * 0.5f, 16f, 16f);

                if (GUI.Button(removeRect,
                    removeIcon != null ? new GUIContent(removeIcon, "Remove") : new GUIContent("X"),
                    EditorStyles.iconButton))
                {
                    database.Remove(entry.guid);
                    Save();
                    GUIUtility.ExitGUI();
                    return;
                }
            }

            HandleRowEvents(rowRect, index, entry, path, e);
        }

        void DrawInvalidRow(Rect rowRect, QuickAccessDatabase.Entry entry)
        {
            string label = !string.IsNullOrEmpty(entry.cachedPath)
                ? System.IO.Path.GetFileName(entry.cachedPath)
                : entry.guid.Substring(0, 8) + "…";

            GUI.Label(
                new Rect(rowRect.x + PAD_X, rowRect.y, rowRect.width - 40f, ROW_H),
                label + " (missing)",
                new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = new Color(1f, 0.4f, 0.4f) }
                });

            Rect removeRect = new Rect(rowRect.xMax - 22f, rowRect.y + (ROW_H - 16f) * 0.5f, 16f, 16f);
            if (GUI.Button(removeRect, new GUIContent("✕", "Remove"), EditorStyles.iconButton))
            {
                database.Remove(entry.guid);
                Save();
                GUIUtility.ExitGUI();
            }
        }

        void DrawCustomIconBadge(Rect rowRect, string guid)
        {
            var colorDb = AssetDatabase.LoadAssetAtPath<FolderColorDatabase>(FolderColorDatabase.AssetPath);
            if (colorDb == null) return;

            string iconName = colorDb.GetIcon(guid);
            if (string.IsNullOrEmpty(iconName)) return;

            Texture2D tex = EditorGUIUtility.FindTexture(iconName);
            if (tex == null) return;

            float badgeSize = 10f;
            GUI.DrawTexture(
                new Rect(
                    rowRect.x + PAD_X + ICON_SIZE - badgeSize + 2f,
                    rowRect.y + (ROW_H - ICON_SIZE) * 0.5f + ICON_SIZE - badgeSize + 2f,
                    badgeSize, badgeSize),
                tex, ScaleMode.ScaleToFit);
        }

        void HandleRowEvents(Rect rowRect, int index, QuickAccessDatabase.Entry entry, string path, Event e)
        {
            if (!rowRect.Contains(e.mousePosition)) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);

                    // Navigates to the folder inside the Project Window with reflection
                    var browserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
                    if (browserType != null)
                    {
                        var browser    = GetWindow(browserType);
                        var showMethod = browserType.GetMethod("ShowFolderContents",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        showMethod?.Invoke(browser, new object[] { obj.GetInstanceID(), true });
                    }
                }
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0)
            {
                dragReorderIndex = index;
                isReordering     = true;
                e.Use();
            }

            if (isReordering && (e.type == EventType.MouseMove || e.type == EventType.MouseDrag))
            {
                dropTargetIndex = index;
                Repaint();
            }

            if (isReordering && e.type == EventType.MouseUp)
            {
                if (dragReorderIndex >= 0 && dropTargetIndex >= 0 && dragReorderIndex != dropTargetIndex)
                {
                    database.Move(dragReorderIndex, dropTargetIndex);
                    Save();
                }
                dragReorderIndex = -1;
                dropTargetIndex  = -1;
                isReordering     = false;
                e.Use();
            }
        }

        void DrawDropLine()
        {
            EditorGUI.DrawRect(
                GUILayoutUtility.GetRect(0, 2f, GUILayout.ExpandWidth(true)),
                new Color(0.25f, 0.55f, 1f, 0.9f));
        }

        void HandleDragAndDrop()
        {
            Event e = Event.current;

            if (e.type != EventType.DragUpdated &&
                e.type != EventType.DragPerform  &&
                e.type != EventType.DragExited) return;

            if (e.type == EventType.DragExited) { isDragOver = false; Repaint(); return; }

            bool hasFolder = false;
            foreach (var p in DragAndDrop.paths)
                if (AssetDatabase.IsValidFolder(p)) { hasFolder = true; break; }

            if (!hasFolder) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            isDragOver = true;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var p in DragAndDrop.paths)
                {
                    if (!AssetDatabase.IsValidFolder(p)) continue;
                    database.Add(AssetDatabase.AssetPathToGUID(p), p);
                }
                Save();
                isDragOver = false;
            }

            e.Use();
            Repaint();
        }

        void Save()
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Repaint();
        }
    }
}
