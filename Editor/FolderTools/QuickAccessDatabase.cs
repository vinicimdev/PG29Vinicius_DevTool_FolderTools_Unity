using System.Collections.Generic;
using UnityEngine;

namespace FolderTools
{
    [CreateAssetMenu(menuName = "FolderTools/Quick Access Database")]
    public class QuickAccessDatabase : ScriptableObject
    {
        public const string AssetPath = FolderColorDatabase.DataPath + "/QuickAccessDatabase.asset";

        [System.Serializable]
        public class Entry
        {
            public string guid;
            public string cachedPath;
            public string customLabel;
        }

        public List<Entry> entries = new List<Entry>();

        public bool Contains(string guid)
        {
            foreach (var e in entries)
                if (e.guid == guid) return true;
            return false;
        }

        public void Add(string guid, string path)
        {
            if (Contains(guid)) return;
            entries.Add(new Entry { guid = guid, cachedPath = path });
        }

        public void Remove(string guid)
        {
            entries.RemoveAll(e => e.guid == guid);
        }

        public void Move(int from, int to)
        {
            if (from < 0 || from >= entries.Count) return;
            if (to   < 0 || to   >= entries.Count) return;
            var item = entries[from];
            entries.RemoveAt(from);
            entries.Insert(to, item);
        }
    }
}
