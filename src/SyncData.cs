using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace SRLogbook
{
    public static class SyncData
    {
        public static string Now() { return DateTime.UtcNow.ToString("o"); }
        public static JavaScriptSerializer Serializer() { return new JavaScriptSerializer { MaxJsonLength = 20000000 }; }
        public static int Compare(string a, string b)
        {
            DateTimeOffset x, y;
            bool ax = DateTimeOffset.TryParse(a, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out x);
            bool by = DateTimeOffset.TryParse(b, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out y);
            return ax && by ? x.CompareTo(y) : ax ? 1 : by ? -1 : string.CompareOrdinal(a ?? "", b ?? "");
        }
        public static bool Includes(string scope, string part)
        {
            return string.IsNullOrEmpty(scope) || scope == "all" || ("," + scope + ",").Contains("," + part + ",");
        }
        public static Store Read(string json)
        {
            var s = Serializer();
            var shape = s.Deserialize<Dictionary<string, object>>(json);
            if (shape == null || (!shape.ContainsKey("Entries") && !shape.ContainsKey("Categories")))
                throw new InvalidDataException("Invalid research JSON: Entries or Categories is required.");
            Store value = s.Deserialize<Store>(json);
            Normalize(value);
            return value;
        }
        public static void Normalize(Store s)
        {
            if (s == null) throw new InvalidDataException("Research data is empty.");
            if (s.Entries == null) s.Entries = new List<Entry>();
            if (s.Categories == null) s.Categories = new List<Category>();
            if (s.Refs == null) s.Refs = new List<RefItem>();
            if (s.Documents == null) s.Documents = new List<LocalDoc>();
            if (s.DeletedCategories == null) s.DeletedCategories = new List<DeletedCategory>();
            if (s.DeletedEntries == null) s.DeletedEntries = new List<DeletedCategory>();
            s.Entries.RemoveAll(x => x == null); s.Categories.RemoveAll(x => x == null);
            s.Refs.RemoveAll(x => x == null); s.Documents.RemoveAll(x => x == null);
            s.DeletedCategories.RemoveAll(x => x == null); s.DeletedEntries.RemoveAll(x => x == null);
            foreach (Entry e in s.Entries) e.Progress = Math.Max(0, Math.Min(100, e.Progress));
            foreach (Category c in s.Categories)
            {
                if (c.Links == null) c.Links = new List<CatLink>();
                c.Links.RemoveAll(x => x == null);
                c.TaskTotal = Math.Max(0, Math.Min(10000, c.TaskTotal));
                c.TaskDone = Math.Max(0, Math.Min(c.TaskTotal, c.TaskDone));
                if (double.IsNaN(c.Hours) || double.IsInfinity(c.Hours)) c.Hours = 0;
                c.Hours = Math.Max(0, Math.Min(100000, c.Hours));
            }
        }
        public static void WriteAtomic(string path, string text)
        {
            string tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(tmp, text, new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(tmp, path, path + ".bak", true);
                else File.Move(tmp, path);
            }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }
        private static void MergeDeleted(List<DeletedCategory> local, List<DeletedCategory> remote)
        {
            foreach (var d in remote)
            {
                if (string.IsNullOrEmpty(d.Id)) continue;
                var old = local.Find(x => x.Id == d.Id);
                if (old == null) local.Add(d);
                else if (Compare(d.At, old.At) > 0) old.At = d.At;
            }
        }
        public static int Merge(Store local, Store remote, string scope)
        {
            Normalize(local); Normalize(remote);
            string before = Serializer().Serialize(local);
            if (Includes(scope, "entries"))
            {
                MergeDeleted(local.DeletedEntries, remote.DeletedEntries);
                foreach (Entry r in remote.Entries)
                {
                    if (string.IsNullOrEmpty(r.Id)) continue;
                    int i = local.Entries.FindIndex(x => x.Id == r.Id);
                    if (i < 0) local.Entries.Add(r);
                    else if (Compare(r.Updated, local.Entries[i].Updated) > 0) local.Entries[i] = r;
                }
                local.Entries.RemoveAll(e => local.DeletedEntries.Exists(d => d.Id == e.Id));
            }
            if (Includes(scope, "categories"))
            {
                MergeDeleted(local.DeletedCategories, remote.DeletedCategories);
                foreach (Category r in remote.Categories)
                {
                    if (string.IsNullOrEmpty(r.Id)) continue;
                    int i = local.Categories.FindIndex(x => x.Id == r.Id);
                    if (i < 0) local.Categories.Add(r);
                    else if (Compare(r.Updated, local.Categories[i].Updated) > 0)
                    {
                        string oldName = local.Categories[i].Name;
                        local.Categories[i] = r;
                        if (oldName != r.Name)
                        {
                            foreach (Entry e in local.Entries) if (e.Category == oldName) e.Category = r.Name;
                            foreach (LocalDoc d in local.Documents) if (d.Category == oldName) d.Category = r.Name;
                        }
                    }
                }
                foreach (Category c in new List<Category>(local.Categories))
                {
                    if (!local.DeletedCategories.Exists(d => d.Id == c.Id)) continue;
                    foreach (Entry e in local.Entries) if (e.Category == c.Name) e.Category = "";
                    foreach (LocalDoc d in local.Documents) if (d.Category == c.Name) d.Category = "";
                    local.Categories.Remove(c);
                }
            }
            return before == Serializer().Serialize(local) ? 0 : 1;
        }
        public static string Payload(Store local, Store remote, string scope)
        {
            remote = remote ?? new Store(); Normalize(remote); Normalize(local);
            Store s = new Store { CategoriesInitialized = true };
            s.Entries = Includes(scope, "entries") ? local.Entries : remote.Entries;
            s.DeletedEntries = Includes(scope, "entries") ? local.DeletedEntries : remote.DeletedEntries;
            s.Categories = Includes(scope, "categories") ? local.Categories : remote.Categories;
            s.DeletedCategories = Includes(scope, "categories") ? local.DeletedCategories : remote.DeletedCategories;
            // Keep legacy remote metadata when its channel is not selected; never upload local paths here.
            s.Refs = remote.Refs; s.Documents = remote.Documents;
            return Serializer().Serialize(s);
        }
        public static bool IsWithin(string path, string parent)
        {
            string p = System.IO.Path.GetFullPath(path).TrimEnd('\\', '/');
            string root = System.IO.Path.GetFullPath(parent).TrimEnd('\\', '/');
            return p.Equals(root, StringComparison.OrdinalIgnoreCase) || p.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
