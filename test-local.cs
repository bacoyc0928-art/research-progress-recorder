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
        // 使用说明：Mac/脚本上传的 LF 文本在 Windows 文本框中必须能换行，且已有 CRLF 不被重复转换。
        Check(MainForm.HelpEditorText("a\nb\r\nc\rd") == "a\r\nb\r\nc\r\nd" && MainForm.HelpEditorText("x\r\ny") == "x\r\ny", "help text from any platform shows line breaks in the Windows editor");
        // 更新检查的发布页回退：只接受本仓库 github.com 的 vX.Y.Z 标签，只取符合命名规则的安装包。
        Check(Updater.TagFromReleaseUrl(new Uri("https://github.com/bacoyc0928-art/research-progress-recorder/releases/tag/v0.2.13")) == "v0.2.13", "release-page fallback reads the version tag");
        Check(Updater.TagFromReleaseUrl(new Uri("https://evil.example/bacoyc0928-art/research-progress-recorder/releases/tag/v0.2.13")) == null
            && Updater.TagFromReleaseUrl(new Uri("http://github.com/bacoyc0928-art/research-progress-recorder/releases/tag/v0.2.13")) == null
            && Updater.TagFromReleaseUrl(new Uri("https://github.com/bacoyc0928-art/research-progress-recorder/releases")) == null
            && Updater.TagFromReleaseUrl(new Uri("https://github.com/other/repo/releases/tag/v9.9.9")) == null
            && Updater.TagFromReleaseUrl(new Uri("https://github.com/bacoyc0928-art/research-progress-recorder/releases/tag/v0.2.13-rc")) == null,
            "release-page fallback rejects other hosts, plain HTTP, other repositories and malformed tags");
        string sumsText = new string('a', 64) + "  research-progress-recorder-0.2.13-Windows.zip\n" + new string('b', 64) + " *research-progress-recorder-0.2.13-macOS-arm64.dmg\n"
            + new string('c', 64) + "  ../evil.exe\nnot-a-hash  research-progress-recorder-0.2.13-macOS-x64.dmg\n";
        List<string> sumNames = Updater.AssetNamesFromSums(sumsText);
        Check(sumNames.Count == 2 && sumNames[0] == "research-progress-recorder-0.2.13-Windows.zip" && sumNames[1] == "research-progress-recorder-0.2.13-macOS-arm64.dmg",
            "release-page fallback takes only well-formed package names from SHA256SUMS");
        Check(AppPaths.Root(t) == Path.GetFullPath(t), "SRCRec root has no extra Sync/SRCRec nesting");
        AppPaths.Ensure(t); Check(Directory.Exists(Path.Combine(t,"PersonalWorkspaceSRRec")), "portable progress folder created");
        Check(AppPaths.MemberReferences(t,"Default") != AppPaths.DefaultReferences(t), "member Default cannot overwrite default library");
        foreach (string name in new[] { "..", "../escape", "a/b", "a\\b", "CON", "x:stream", "trailing.", "trailing " })
            Check(Fails(delegate { FileTransfers.Child(t,name); }), "download rejects " + name);
        Check(Path.GetFileName(FileTransfers.Child(t,"实验.pdf")) == "实验.pdf", "Unicode reference filename accepted");
        string progressRoot=Path.Combine(t,"Mat_Local"); Directory.CreateDirectory(Path.Combine(progressRoot,"nested"));
        string progressFile=Path.Combine(progressRoot,"nested","report.txt"); File.WriteAllText(progressFile,"progress");
        string extraDir=Path.Combine(t,"external"); Directory.CreateDirectory(extraDir);
        string extraFile=Path.Combine(extraDir,"extra.txt"); File.WriteAllText(extraFile,"extra");
        var progressUploads=FileTransfers.ProgressUploads(progressRoot,new[] {progressFile,extraFile});
        Check(progressUploads[0].Value=="nested/report.txt" && progressUploads[1].Value=="extra.txt", "progress upload preserves source hierarchy and added external filenames");
        string collision=Path.Combine(progressRoot,"extra.txt"); File.WriteAllText(collision,"different");
        Check(Fails(delegate {FileTransfers.ProgressUploads(progressRoot,new[] {collision,extraFile});}), "progress destination collisions fail before any upload");
        Check(Fails(delegate {FileTransfers.ProgressRelativePath(Path.Combine(t,"missing","CON"),progressRoot);}), "progress path validation rejects cross-platform reserved names");
        string referenceRoot=Path.Combine(t,"Reference"); Directory.CreateDirectory(Path.Combine(referenceRoot,"references"));
        string directRef=Path.Combine(referenceRoot,"current.pdf"); File.WriteAllText(directRef,"current");
        string legacyRef=Path.Combine(referenceRoot,"references","old.pdf"); File.WriteAllText(legacyRef,"old");
        Check(FileTransfers.LocalReferenceCopy(referenceRoot,"current.pdf")==directRef && FileTransfers.LocalReferenceCopy(referenceRoot,"old.pdf")==legacyRef, "reference removal resolves current direct copies and legacy nested copies");
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
        int rosterPage=0;
        GitHubSync.RequestForTests=(method,url,token,body,accept)=> {
            rosterPage++;
            if (rosterPage==2) return "[{\"login\":\"PERSON0\"},{\"login\":\"Person100\"}]";
            var rows=new List<object>(); for(int i=0;i<100;i++) rows.Add(new {login="Person"+i});
            return SyncData.Serializer().Serialize(rows);
        };
        Check(GitHubSync.ListCollaborators("owner","repo","main","fake").Count==101 && rosterPage==2,"roster paginates and deduplicates accounts case-insensitively");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=> { throw new GitHubApiException(403,"owner only"); };
        Check(GitHubSync.ListPendingInvitations("owner","repo","main","fake").Count==0,"regular members do not require permission to list pending invitations");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=>Info(System.Text.Encoding.UTF8.GetBytes("[{\"time\":\"2026-11-03T10:00:00Z\",\"user\":\"MacUser\",\"action\":\"登录 / Sign in\",\"detail\":\"test\"}]"));
        var macRows=ActivityLog.Read("owner","repo","main","fake",ActivityLog.PublicPath);
        Check(macRows.Count==1 && macRows[0].User=="MacUser" && macRows[0].Action.Contains("Sign in"),"Windows reads lowercase activity fields from macOS");
        bool missingSums=false;
        try { Updater.DownloadAndStage(new Updater.Release {tag_name="v0.2.10",assets=new List<Updater.Asset>{new Updater.Asset{name="app-Windows.zip",browser_download_url="https://example.invalid"}}},null); }
        catch(InvalidDataException) { missingSums=true; }
        Check(missingSums,"updater requires checksums before any download");
        GitHubSync.RequestForTests=(method,url,token,body,accept)=> {throw new Exception("Unexpected network call in offline test: "+method);};
        Console.WriteLine("LOCAL TRANSFER/UNINSTALL CHECKS: "+count);
    }
}
