using System;
using System.Collections.Generic;
using System.IO;
using SRLogbook;
using SRUninstall;
public static class LocalTransferChecks
{
    static int count;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); count++; }
    static bool Fails(Action action) { try { action(); return false; } catch { return true; } }
    static string Info(byte[] bytes) { return "{\"sha\":\"test-sha\",\"encoding\":\"base64\",\"content\":\"" + Convert.ToBase64String(bytes) + "\"}"; }
    public static void Run(string root)
    {
        string t = Path.Combine(root, "transfer-tests"); Directory.CreateDirectory(t);
        Check(AppPaths.Root(t) == Path.GetFullPath(t), "SRCRec root has no extra Sync/SRCRec nesting");
        AppPaths.Ensure(t); Check(Directory.Exists(Path.Combine(t,"PersonalWorkspaceSRRec")), "portable progress folder created");
        Check(AppPaths.MemberReferences(t,"Default") != AppPaths.DefaultReferences(t), "member Default cannot overwrite default library");
        foreach (string name in new[] { "..", "../escape", "a/b", "a\\b", "CON", "x:stream", "trailing.", "trailing " })
            Check(Fails(delegate { FileTransfers.Child(t,name); }), "download rejects " + name);
        Check(Path.GetFileName(FileTransfers.Child(t,"实验.pdf")) == "实验.pdf", "Unicode reference filename accepted");
        string dest=Path.Combine(t,"library"); Directory.CreateDirectory(dest); File.WriteAllText(Path.Combine(dest,"original.txt"),"original");
        Check(Fails(delegate { FileTransfers.ReplaceFromDownload(dest,stage=> { File.WriteAllText(Path.Combine(stage,"partial.txt"),"partial"); throw new IOException("offline"); }); }) && File.ReadAllText(Path.Combine(dest,"original.txt"))=="original", "interrupted download preserves old library");
        Check(Fails(delegate { FileTransfers.ReplaceFromDownload(dest,stage=>{}); }) && File.Exists(Path.Combine(dest,"original.txt")), "empty/missing library cannot erase old data");
        string backup=FileTransfers.ReplaceFromDownload(dest,stage=>File.WriteAllText(Path.Combine(stage,"new.txt"),"new"));
        Check(File.Exists(Path.Combine(dest,"new.txt")) && File.Exists(Path.Combine(backup,"original.txt")), "successful replacement retains old backup");
        string big=Path.Combine(t,"too-large.bin"); using(var stream=File.Create(big)) stream.SetLength(FileTransfers.MaxUploadBytes+1);
        Check(Fails(delegate {FileTransfers.UploadFiles(t);}), "oversize upload fails during preflight"); File.Delete(big);
        byte[] expected=new byte[] {0,128,255,10}; string lastUrl=null;
        GitHubSync.RequestForTests=(method,url,token,body,accept)=> {lastUrl=url; return "{\"sha\":\"big-sha\",\"encoding\":\"none\",\"content\":\"\"}";};
        GitHubSync.BytesForTests=(url,token)=>expected;
        string sha; var raw=GitHubSync.GetFileBytes("owner","repo","folder/大.pdf","feature/test","fake",out sha);
        Check(raw.Length==4 && raw[2]==255 && sha=="big-sha" && lastUrl.Contains("ref=feature%2Ftest"), "large binary raw download preserves bytes and branch");
        int puts=0,gets=0;
        GitHubSync.RequestForTests=(method,url,token,body,accept)=> {
            if(method=="GET") {gets++; return "{\"sha\":\"version-"+gets+"\"}";}
            puts++; if(puts==1) throw new GitHubApiException(409,"test conflict");
            Check(body.Contains("version-2") && body.Contains(Convert.ToBase64String(expected)), "binary retry uses refreshed SHA and bytes"); return "{}";
        };
        GitHubSync.UploadFileBytes("owner","repo","team/me/file.bin","main","fake",expected);
        Check(puts==2 && gets==2,"binary conflict retries without downloading old file body");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=> {
            if(url.Contains("/contents/team/me?")) return "[{\"name\":\"nested\",\"type\":\"dir\"}]";
            if(url.Contains("/contents/team/me/nested?")) return "[{\"name\":\"binary.pdf\",\"type\":\"file\"}]";
            return Info(expected);
        };
        string downloaded=Path.Combine(t,"recursive"); GitHubSync.DownloadDir("owner","repo","team/me","main","fake",downloaded);
        Check(File.ReadAllBytes(Path.Combine(downloaded,"nested","binary.pdf"))[2]==255,"recursive team download retains binary bytes");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=> {
            if(url.Contains("/branches/")) return "{}";
            if(url.Contains("/contents/team/me?")) return "[{\"name\":\"profile.json\",\"type\":\"file\"},{\"name\":\"progress\",\"type\":\"dir\"}]";
            if(url.Contains("/contents/team/me/progress?")) return "[{\"name\":\"nested\",\"type\":\"dir\"}]";
            if(url.Contains("/contents/team/me/progress/nested?")) return "[{\"name\":\"report.bin\",\"type\":\"file\"}]";
            return Info(expected);
        };
        string memberTarget=Path.Combine(t,"member-download");
        Check(GitHubSync.DownloadMemberFiles("owner","repo","me","main","fake",memberTarget) && File.ReadAllBytes(Path.Combine(memberTarget,"progress","nested","report.bin"))[2]==255 && !File.Exists(Path.Combine(memberTarget,"profile.json")), "member download includes nested progress and excludes profile metadata");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=>url.Contains("/branches/") ? "{}" : "[{\"name\":\"profile.json\",\"type\":\"file\"}]";
        Check(!GitHubSync.DownloadMemberFiles("owner","repo","me","main","fake",memberTarget) && File.Exists(Path.Combine(memberTarget,"progress","nested","report.bin")), "profile-only member preserves previously downloaded files");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=>"[{\"name\":\"../escape\",\"type\":\"file\"}]";
        Check(Fails(delegate {GitHubSync.DownloadDir("owner","repo","team/me","main","fake",downloaded);}),"remote traversal name rejected before write");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=>{throw new GitHubApiException(404,"missing");};
        Check(Fails(delegate {GitHubSync.DownloadDir("owner","repo","default","main","fake",downloaded);}),"missing remote library is an error, not empty success");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=> {throw new Exception("Unexpected network call in offline test: "+method);};
        GitHubSync.BytesForTests=(url,token)=>{throw new Exception("Unexpected binary network call");};
        foreach(bool purge in new[] {false,true})
        {
            string install=Path.Combine(t,purge?"purge-install":"keep-install"); Directory.CreateDirectory(install);
            File.WriteAllText(Path.Combine(install,UninstallCore.Marker),"SRPRecorder2027 portable v1");
            File.WriteAllText(Path.Combine(install,"SRPRecorder2027.exe"),"dummy");
            File.WriteAllText(Path.Combine(install,"research_log.json"),"research");
            File.WriteAllText(Path.Combine(install,"user-notes.txt"),"unknown user file");
            Directory.CreateDirectory(Path.Combine(install,"PersonalWorkspaceSRRec"));
            File.WriteAllText(Path.Combine(install,"PersonalWorkspaceSRRec","doc.txt"),"personal");
            string external=Path.Combine(t,"external-personal.txt"); File.WriteAllText(external,"external");
            string saved=UninstallCore.Backup(install,Path.Combine(t,"backups"));
            UninstallCore.Remove(install,purge);
            Check(!File.Exists(Path.Combine(install,"SRPRecorder2027.exe")) && File.Exists(Path.Combine(install,"user-notes.txt")) && File.Exists(external),"uninstall deletes only known program files, preserves unknown/external files");
            Check(File.Exists(Path.Combine(saved,"research_log.json")) && File.Exists(Path.Combine(install,"research_log.json"))!=purge && File.Exists(Path.Combine(install,"PersonalWorkspaceSRRec","doc.txt"))!=purge,"uninstall data selection and timestamped backup "+purge);
        }
        string invalid=Path.Combine(t,"not-an-install"); Directory.CreateDirectory(invalid); File.WriteAllText(Path.Combine(invalid,"SRPRecorder2027.exe"),"keep");
        Check(Fails(delegate {UninstallCore.Remove(invalid,true);}) && File.Exists(Path.Combine(invalid,"SRPRecorder2027.exe")),"uninstaller refuses unmarked directories");
        Check(Fails(delegate {UninstallCore.Child(t,"../escape");}),"uninstaller rejects paths outside installation");
        Console.WriteLine("LOCAL TRANSFER/UNINSTALL CHECKS: "+count);
    }
}
