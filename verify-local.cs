using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using SRLogbook;
public class Verify002
{
    static int count;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); count++; }
    static Entry E(string id, string date, string title) { return new Entry { Id=id, Updated=date, Title=title, Date="2026-10-20", Category="", Status="进行中", Progress=0, Content="" }; }
    [STAThread] public static void Main()
    {
        string root = AppDomain.CurrentDomain.BaseDirectory; LocalTransferChecks.Run(root);
        string older="2026-10-20T10:00:00.0000000Z", newer="2026-10-20T10:00:00.0000001Z";
        var a=new Store(); var b=new Store(); a.Entries.Add(E("a",older,"local")); b.Entries.Add(E("a",newer,"remote")); b.Entries.Add(E("b",older,"other"));
        SyncData.Merge(a,b,"entries"); Check(a.Entries.Count==2 && a.Entries[0].Title=="remote","merge newer records and preserve independent entries");
        a.DeletedEntries.Add(new DeletedCategory {Id="a",At=newer}); SyncData.Merge(a,b,"entries"); Check(!a.Entries.Exists(e=>e.Id=="a"),"deleted records do not resurrect");
        a.Categories.Add(new Category{Id="cat",Name="old",Updated=older}); a.Entries[0].Category="old";
        b.Categories.Add(new Category{Id="cat",Name="new",Updated=newer}); SyncData.Merge(a,b,"categories");
        Check(a.Categories[0].Name=="new" && a.Entries.Find(e=>e.Id=="b").Category=="new","category rename updates references");
        b.DeletedCategories.Add(new DeletedCategory{Id="cat",At=newer}); SyncData.Merge(a,b,"categories"); Check(a.Categories.Count==0,"category deletion wins equal timestamps");
        a.Refs.Add(new RefItem { Path="PRIVATE-PATH" }); a.ProjectRoot="PRIVATE-ROOT"; a.Documents.Add(new LocalDoc{Path="PRIVATE-DOC"});
        var remote=new Store(); remote.Entries.Add(E("untouched",older,"remote"));
        string payload=SyncData.Payload(a,remote,"categories"); var shared=SyncData.Read(payload);
        Check(shared.Entries[0].Id=="untouched","unselected channel preserved on upload");
        Check(!payload.Contains("PRIVATE-"),"shared payload excludes local private metadata");
        Check(SyncData.Compare("2026-10-20T11:00:00+01:00",older)==0,"timezone-aware comparison");
        Check(SyncData.Compare(newer,older)>0,"subsecond precision retained");
        bool rejected=false; try { SyncData.Read("{\"unexpected\":true}"); } catch { rejected=true; } Check(rejected,"invalid remote schema stops upload");
        Check(!SyncData.IsWithin(Path.Combine(root,"sibling"),Path.Combine(root,"sib")),"path protection uses directory boundary");
        string file=Path.Combine(root,"atomic.json"); SyncData.WriteAtomic(file,"{\"v\":1}"); SyncData.WriteAtomic(file,"{\"v\":2}");
        Check(File.ReadAllText(file+".bak").Contains(":1"),"atomic writes retain previous data");
        string doc=LocalDocs.CreateDocx(root,"a&b","","CON<> .");
        using(var z=ZipFile.OpenRead(doc)) { foreach(var e in z.Entries) using(var r=e.Open()) new XmlDocument().Load(r); } Check(true,"Word document is valid XML/OOXML");
        SyncData.WriteAtomic(Path.Combine(root,"workspace.json"),"{\"folder\":"+SyncData.Serializer().Serialize(root)+"}");
        SyncData.WriteAtomic(Path.Combine(root,"sync_config.json"),"{\"owner\":\"\",\"repo\":\"\",\"scope\":\"entries,categories\"}");
        File.WriteAllText(Path.Combine(root,"lang.txt"),"zh");
        Application.EnableVisualStyles();
        using(var f=new MainForm())
        {
            f.Show(); Application.DoEvents();
            for(int i=0;i<5;i++) { f.SetPagePublic(i); Application.DoEvents(); }
            Lang.En=true; Lang.Apply(f); f.SetPagePublic(3); Application.DoEvents(); f.CaptureTo(Path.Combine(root,"windows-en.png"));
            Lang.En=false; Lang.Apply(f); f.SetPagePublic(0); Application.DoEvents(); f.CaptureTo(Path.Combine(root,"windows-zh.png"));
            var sf=typeof(MainForm).GetField("store",BindingFlags.NonPublic|BindingFlags.Instance); var data=(Store)sf.GetValue(f);
            Check(data.Entries.Count==0 && data.Categories.Count==0,"fresh install has no personal seed data");
            f.Close();
        }
        Check(true,"all five Windows pages and language switch render");
        Console.WriteLine("TOTAL " + count + " checks passed");
    }
}
