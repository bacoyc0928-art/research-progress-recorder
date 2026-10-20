using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SRLogbook
{
    /// <summary>
    /// 在项目目录内创建本地 Word 文档（.docx），不依赖 Google 或任何网络服务。
    /// </summary>
    public static class LocalDocs
    {
        public static string Folder(string projectRoot)
        {
            string materials = Path.Combine(projectRoot, "同步辐射器件资料");
            string dir = Directory.Exists(materials)
                ? Path.Combine(materials, "本地文档")
                : Path.Combine(projectRoot, "本地文档");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string CreateDocx(string projectRoot, string project, string category, string title)
        {
            string dir = Folder(projectRoot);
            string safe = Sanitize(title);
            string path = Path.Combine(dir, safe + ".docx");
            int n = 1;
            while (File.Exists(path))
            {
                n++;
                path = Path.Combine(dir, safe + " (" + n + ").docx");
            }


            using (FileStream fs = new FileStream(path, FileMode.CreateNew))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                Write(zip, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
                    "</Types>");
                Write(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
                    "</Relationships>");
                Write(zip, "word/document.xml", DocumentXml(title, project, category));
            }
            return path;
        }

        public static string CreateDocxIn(string dir, string project, string title)
        {
            Directory.CreateDirectory(dir);
            string safe = Sanitize(title);
            string path = Path.Combine(dir, safe + ".docx");
            int n = 1;
            while (File.Exists(path)) { n++; path = Path.Combine(dir, safe + " (" + n + ").docx"); }
            using (FileStream fs = new FileStream(path, FileMode.CreateNew))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                Write(zip, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
                    "</Types>");
                Write(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
                    "</Relationships>");
                Write(zip, "word/document.xml", DocumentXml(title, project, ""));
            }
            return path;
        }

        private static string DocumentXml(string title, string project, string category)
        {
            string meta = "项目：" + project + "    类别：" + (string.IsNullOrEmpty(category) ? "未分类" : category) +
                          "    日期：" + DateTime.Now.ToString("yyyy-MM-dd");
            StringBuilder b = new StringBuilder();
            b.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            b.Append("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
            b.Append(Para(title, true, 32));
            b.Append(Para(meta, false, 20));
            b.Append(Para("", false, 20));
            b.Append(Para("研究进展", true, 24));
            b.Append(Para("", false, 22));
            b.Append(Para("", false, 22));
            b.Append(Para("下一步", true, 24));
            b.Append(Para("", false, 22));
            b.Append(Para("", false, 22));
            b.Append("<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"1440\" w:right=\"1440\" w:bottom=\"1440\" w:left=\"1440\"/></w:sectPr>");
            b.Append("</w:body></w:document>");
            return b.ToString();
        }

        private static string Para(string text, bool bold, int halfPoints)
        {
            StringBuilder b = new StringBuilder();
            b.Append("<w:p><w:pPr><w:spacing w:after=\"120\"/></w:pPr><w:r><w:rPr>");
            b.Append("<w:rFonts w:ascii=\"Times New Roman\" w:hAnsi=\"Times New Roman\" w:eastAsia=\"楷体\"/>");
            if (bold) b.Append("<w:b/>");
            b.Append("<w:sz w:val=\"" + halfPoints + "\"/><w:szCs w:val=\"" + halfPoints + "\"/></w:rPr>");
            b.Append("<w:t xml:space=\"preserve\">" + Escape(text) + "</w:t></w:r></w:p>");
            return b.ToString();
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Trim().TrimEnd('.');
            if (name.Length == 0) name = "Untitled";
            if (name.Length > 100) name = name.Substring(0, 100);
            return "Document_" + name;
        }

        private static void Write(ZipArchive zip, string name, string content)
        {
            ZipArchiveEntry e = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (Stream s = e.Open())
            {
                byte[] data = new UTF8Encoding(false).GetBytes(content);
                s.Write(data, 0, data.Length);
            }
        }
    }
}
