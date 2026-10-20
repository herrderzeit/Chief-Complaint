using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ChiefComplaint.Sim.Data
{
    /// <summary>A reference to another Unity object: <c>{fileID: X, guid: Y, type: 2}</c>.</summary>
    public readonly struct UnityRef
    {
        public readonly long FileId;
        /// <summary>Empty for a reference to an object in the same file.</summary>
        public readonly string Guid;

        public UnityRef(long fileId, string guid) { FileId = fileId; Guid = guid ?? ""; }

        public bool IsNull => FileId == 0;
        public bool IsLocal => !IsNull && Guid.Length == 0;

        private static readonly Regex Pattern = new Regex(@"\{fileID:\s*(-?\d+)(?:,\s*guid:\s*([0-9a-f]+))?", RegexOptions.Compiled);

        public static bool TryParse(string text, out UnityRef value)
        {
            value = default;
            if (text == null) return false;
            Match m = Pattern.Match(text);
            if (!m.Success) return false;
            value = new UnityRef(long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), m.Groups[2].Success ? m.Groups[2].Value : "");
            return true;
        }
    }

    /// <summary>
    /// One <c>--- !u!114 &amp;id</c> document of a Unity YAML file: the serialized fields of a single object.
    /// Only the shapes Unity writes for ScriptableObjects and MonoBehaviours are supported: top-level scalars,
    /// lists of scalars, lists of flat maps, and hex-packed enum lists. Multi-line strings are skipped.
    /// </summary>
    public sealed class UnityDoc
    {
        public long FileId;
        public int ClassId;
        public readonly Dictionary<string, string> Scalars = new Dictionary<string, string>();
        public readonly Dictionary<string, List<string>> Lists = new Dictionary<string, List<string>>();
        public readonly Dictionary<string, List<Dictionary<string, string>>> MapLists = new Dictionary<string, List<Dictionary<string, string>>>();

        public string ScriptGuid => UnityRef.TryParse(Get("m_Script"), out var r) ? r.Guid : "";
        public string Name => Get("m_Name");
        public string ClassIdentifier => Get("m_EditorClassIdentifier");

        public string Get(string key, string fallback = "") => Scalars.TryGetValue(key, out var v) ? v : fallback;

        public int GetInt(string key, int fallback = 0) =>
            int.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

        public float GetFloat(string key, float fallback = 0f) =>
            float.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;

        public bool GetBool(string key, bool fallback = false) => Scalars.ContainsKey(key) ? GetInt(key) != 0 : fallback;

        public UnityRef GetRef(string key) => UnityRef.TryParse(Get(key), out var r) ? r : default;

        public List<string> GetList(string key) => Lists.TryGetValue(key, out var v) ? v : new List<string>();

        public List<UnityRef> GetRefList(string key)
        {
            var result = new List<UnityRef>();
            foreach (string item in GetList(key))
                if (UnityRef.TryParse(item, out var r)) result.Add(r);
            return result;
        }

        public List<Dictionary<string, string>> GetMapList(string key) =>
            MapLists.TryGetValue(key, out var v) ? v : new List<Dictionary<string, string>>();

        /// <summary>
        /// Unity packs a <c>List&lt;enum&gt;</c> as one hex string of little-endian int32s, e.g. <c>0100000003000000</c> = [1, 3].
        /// </summary>
        public List<int> GetPackedIntList(string key)
        {
            var result = new List<int>();
            string hex = Get(key).Trim();
            for (int i = 0; i + 8 <= hex.Length; i += 8)
            {
                int value = 0;
                for (int b = 0; b < 4; b++)
                    value |= Convert.ToInt32(hex.Substring(i + b * 2, 2), 16) << (8 * b);
                result.Add(value);
            }
            return result;
        }
    }

    public static class UnityYaml
    {
        private static readonly Regex DocHeader = new Regex(@"^--- !u!(\d+) &(-?\d+)", RegexOptions.Compiled);

        public static List<UnityDoc> ParseFile(string path) => Parse(File.ReadAllLines(path));

        public static List<UnityDoc> Parse(IList<string> lines)
        {
            var docs = new List<UnityDoc>();
            UnityDoc doc = null;
            string listKey = null;
            Dictionary<string, string> currentMap = null;

            foreach (string raw in lines)
            {
                Match header = DocHeader.Match(raw);
                if (header.Success)
                {
                    doc = new UnityDoc
                    {
                        ClassId = int.Parse(header.Groups[1].Value, CultureInfo.InvariantCulture),
                        FileId = long.Parse(header.Groups[2].Value, CultureInfo.InvariantCulture)
                    };
                    docs.Add(doc);
                    listKey = null;
                    currentMap = null;
                    continue;
                }
                if (doc == null || raw.Length == 0) continue;

                int indent = 0;
                while (indent < raw.Length && raw[indent] == ' ') indent++;
                string line = raw.Substring(indent);

                if (indent == 2 && line.StartsWith("- ", StringComparison.Ordinal) && listKey != null)
                {
                    string item = line.Substring(2);
                    if (!item.StartsWith("{", StringComparison.Ordinal) && SplitKeyValue(item, out string k, out string v))
                    {
                        currentMap = new Dictionary<string, string> { [k] = v };
                        GetOrAdd(doc.MapLists, listKey).Add(currentMap);
                    }
                    else
                    {
                        currentMap = null;
                        GetOrAdd(doc.Lists, listKey).Add(Unquote(item));
                    }
                    continue;
                }

                if (indent == 4 && currentMap != null && SplitKeyValue(line, out string mk, out string mv))
                {
                    currentMap[mk] = mv;
                    continue;
                }

                if (indent == 2 && SplitKeyValue(line, out string key, out string value))
                {
                    doc.Scalars[key] = Unquote(value);
                    currentMap = null;
                    // A key with an empty value may be followed by "  - item" lines.
                    listKey = value.Length == 0 ? key : null;
                    if (value == "[]") listKey = null;
                    continue;
                }
                // Anything else is a multi-line string continuation or a nested block we do not need.
            }
            return docs;
        }

        private static bool SplitKeyValue(string line, out string key, out string value)
        {
            int colon = line.IndexOf(':');
            if (colon <= 0 || line[0] == '{' || line[0] == '\'' || line[0] == '"')
            {
                key = value = null;
                return false;
            }
            key = line.Substring(0, colon);
            if (key.Contains(" ")) { value = null; return false; }
            value = line.Substring(colon + 1).Trim();
            return true;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            return value;
        }

        private static List<T> GetOrAdd<T>(Dictionary<string, List<T>> dict, string key)
        {
            if (!dict.TryGetValue(key, out var list)) dict[key] = list = new List<T>();
            return list;
        }
    }
}
