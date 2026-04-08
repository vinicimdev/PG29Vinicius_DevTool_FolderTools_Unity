using System.Collections.Generic;
using UnityEngine;

namespace FolderTools
{
    [CreateAssetMenu(menuName = "FolderTools/Folder Color Database")]
    public class FolderColorDatabase : ScriptableObject
    {
        // Folder where the .asset files are saved on the users project
        // This is saved OUTSIDE of `Package/`, so it can be altered if necessary.
        public const string DataPath = "Assets/FolderToolsData";
        public const string AssetPath = DataPath + "/FolderColorDatabase.asset";

        [System.Serializable]
        public class Entry
        {
            public string guid;
            public Color  color;
            public string iconName;
        }

        public List<Entry> entries = new List<Entry>();

        // == Color ===============================================================

        public Color GetColor(string guid)
        {
            var e = Find(guid);
            return e != null ? e.color : Color.clear;
        }

        public void SetColor(string guid, Color color)
        {
            FindOrCreate(guid).color = color;
            Cleanup();
        }

        // == Icon =============================================================

        public string GetIcon(string guid)
        {
            var e = Find(guid);
            return e != null ? e.iconName : "";
        }

        public void SetIcon(string guid, string iconName)
        {
            FindOrCreate(guid).iconName = iconName;
            Cleanup();
        }

        // == Helper Methods ===========================================================

        Entry Find(string guid)
        {
            foreach (var e in entries)
                if (e.guid == guid) return e;
            return null;
        }

        Entry FindOrCreate(string guid)
        {
            var e = Find(guid);
            if (e == null) { e = new Entry { guid = guid }; entries.Add(e); }
            return e;
        }

        void Cleanup()
        {
            entries.RemoveAll(e => e.color.a <= 0f && string.IsNullOrEmpty(e.iconName));
        }
    }
}
