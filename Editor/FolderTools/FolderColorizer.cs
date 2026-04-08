using UnityEditor;
using UnityEngine;

namespace FolderTools
{
    // Attribute that ensures the static constructor of this class is called
    // when the Unity Editor loads.
    // Without this, the class would only initialize when something else references it
    // which never happens :p
    [InitializeOnLoad]
    public static class FolderColorizer
    {
        // Keeps the reference to the Scriptable Object that stores the folder colors, icons and GUIDs
        static FolderColorDatabase database;
        
        // Keeps the reference to the Icon texture to avoid searching for it every time
        static Texture2D folderTex;

        // Static constructor that runs automatically because of the [InitializeOnLoad] attribute
        static FolderColorizer()
        {
            // Delegate (list of functions) that Unity calls in a sequence
            // Here we are attributing the OnGUI method to be called for every item in the Project Window
            // So we can draw the colors in the folders and add the icons
            EditorApplication.projectWindowItemOnGUI += OnGUI;

            // Loads the Scriptable Object with the colors , icons and GUIDs
            // If it doesn't exist, creates a new one
            LoadDatabase();
        }

        // Static method to load the Scriptable Object that contains the folder colors, icons and GUIDs
        static void LoadDatabase()
        {
            // Check if the "Assets/Editor" folder exists, if not, create it
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");

            // Try to load the Scriptable Object at the specified path
            string path = "Assets/Editor/FolderColorDatabase.asset";

            // This method returns null if the asset doesn't exist
            database = AssetDatabase.LoadAssetAtPath<FolderColorDatabase>(path);

            // If the asset (Scriptable Object/Database) doesn't exist,
            // create a new instance as a '.asset' file in the specified path
            // and save the asset to make sure it shows up in the Project Window
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<FolderColorDatabase>();
                AssetDatabase.CreateAsset(database, path);
                AssetDatabase.SaveAssets();
            }
        }

        // The signature of this method HAS to be EXACTLY LIKE THIS!!!!!!
        // This is the signature of the delegate expected by the 'projectWindowItemOnGUI' event
        // The parameters here are the GUID: a unique identifier for each asset in the project
        // and the Rect: the area where the asset is drawn in the Project Window
        static void OnGUI(string guid, Rect selectionRect)
        {
            // Checks if the database is loaded, if not, tries to load it
            // Sometimes Unity is dumb and recompiles, nullifying the static fields before 
            // running the static constructor
            if (database == null) LoadDatabase();

            // Gets the path of every visible asset inside the Project Window
            // Then checks if the path is a folder. If not, return
            // This allows us to only customize folders, not other files :D
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path)) return;

            // Here we search for the color and icon assigned to the folder with the GUID of the current folder
            // GetColor() returns 'Color.clear' (with alpha = 0 or fully transparent) if the folder
            // doesn't have a color assigned
            // GetIcon() returns an empty string if the folder doesn't have an icon assigned
            Color color = database.GetColor(guid);
            string iconName = database.GetIcon(guid);

            // Then we kinda cheat a lil bit
            // We check if color has some value in the alpha, if not, we know
            // that the folder has the default color (no color assigned)
            bool hasColor = color.a > 0f;
            bool hasIcon = !string.IsNullOrEmpty(iconName);

            // This is really sad and dumb and pisses me off
            // But it has to be like this:
            // Here, we check if the folder doesn't have a color and an icon
            // If it doesn't have both, we return withouth doing anything.
            // This is mainly for performance, because OnGUI will run for every visible item,
            // multiple times per second. So we don't wanna do that for folders that weren't customized yet :(
            if (!hasColor && !hasIcon) return;

            // HEre we check if the folder texture is still null (basically for the first time)
            // If it is null, returns the default, built-in folder icon
            if (folderTex == null)
                folderTex = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;

            // In case for some weird Unity reason the texture doesn't load
            // (most likely because Unity keeps changing the names of the icons),
            // this if avoids NullReferenceExceptions
            if (folderTex == null) return;

            // Here we check if the current view in the Project Window is in List mode or Grid mode
            // because Unity doesn't explicitly tell us
            // SO WE CHEAT AGAIN :D
            // The trick here is that in List view, the height of each line is around 16 to 20 pixels
            // And in Grid view it is way bigger, depending on the zoom level.
            bool isListView = selectionRect.height <= 20f;

            Rect iconRect;

            // In list view, the icon is a square with the same height as the line
            if (isListView)
            {
                // So we get the height of the selected Rect and pass it twice
                // This will give the square shape for the icon
                float size = selectionRect.height;
                iconRect = new Rect(selectionRect.x, selectionRect.y, size, size);
            }
            // In grid view, the icon takes the whole width of the Rect, but we need to leave some space at the bottom for the name of the folder
            else
            {
                float iconHeight = selectionRect.height - 16f;
                iconRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, iconHeight);
            }

            // GUI color is a global state that affects all the GUI drawing functions
            // So we have to save the previous GUI color to restore it later, otherwise we would mess up the colors of the rest of the Project Window
            Color prev = GUI.color;

            // When Unity calls the OnGUI method, it has already drawn the default icons for the folders
            // And this WEIRD icon has a gray border.
            // So if you only try to paint the folder icon, you will get the color with a gray border
            // Which is ugly and dumb
            if (hasColor)
            {
                // To fix this, we nuke the original icon first by drawing a solid rectangle with the
                // same color as the Project Window background
                // Then, 'EditorGUIUtility.isProSkin' returns true if the user is in Dark mode
                // So we check SPECIFICALLY for these values (0.2196f and 0.7843f)
                // These are the exact background colors for the Project Window in Dark and Light mode, respectively
                Color bgColor = EditorGUIUtility.isProSkin
                    ? new Color(0.2196f, 0.2196f, 0.2196f, 1f)
                    : new Color(0.7843f, 0.7843f, 0.7843f, 1f);
                EditorGUI.DrawRect(iconRect, bgColor);

                // Then we use the color we got from the database to draw the folder icon again, but this time with the custom color
                // This is a global multiplier for colors.
                // Since the folders are grayish/white, if we multiply it by red, we will get a red folder
                GUI.color = color;
                GUI.DrawTexture(iconRect, folderTex, ScaleMode.ScaleToFit);

                // Here we just draw the texture again in white with a low alpha, 
                // to keep the shadow of the icon, giving it some depth and making it look nicer
                GUI.color = new Color(1f, 1f, 1f, 0.25f);
                GUI.DrawTexture(iconRect, folderTex, ScaleMode.ScaleToFit);
            }

            // TODO: finish commenting 
            if (hasIcon)
            {
                Texture2D tex = EditorGUIUtility.IconContent(iconName).image as Texture2D;
                if (tex != null)
                {
                    float badgeSize;
                    Rect badgeRect;

                    if (isListView)
                    {
                        badgeSize = iconRect.height * 0.55f;
                        badgeRect = new Rect(
                            iconRect.xMax - badgeSize + 2f,
                            iconRect.yMax - badgeSize + 2f,
                            badgeSize,
                            badgeSize);
                    }
                    else
                    {
                        badgeSize = iconRect.width * 0.45f;
                        badgeRect = new Rect(
                            iconRect.xMax - badgeSize,
                            iconRect.yMax - badgeSize,
                            badgeSize,
                            badgeSize);
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

        [MenuItem("Assets/Folder/Customize", false, 1000)]
        static void CustomizeFolder()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(path)) return;
            OpenWindow(AssetDatabase.AssetPathToGUID(path));
        }

        [MenuItem("Assets/Folder/Customize", true)]
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
