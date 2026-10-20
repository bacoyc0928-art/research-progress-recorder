using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SRLogbook
{
    /// <summary>把团队同步用的 GitHub 令牌保存到 Windows 凭据管理器，不写入项目文件。</summary>
    public static class SecretStore
    {
        private const uint CRED_TYPE_GENERIC = 1;
        private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CredDelete(string target, uint type, uint flags);
        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr buffer);

        public static bool Save(string target, string secret)
        {
            if (string.IsNullOrEmpty(secret)) return false;
            byte[] blob = Encoding.UTF8.GetBytes(secret);
            IntPtr p = Marshal.AllocHGlobal(blob.Length);
            try
            {
                Marshal.Copy(blob, 0, p, blob.Length);
                CREDENTIAL c = new CREDENTIAL();
                c.Type = CRED_TYPE_GENERIC;
                c.TargetName = target;
                c.Comment = "SRLogbook sync";
                c.CredentialBlobSize = (uint)blob.Length;
                c.CredentialBlob = p;
                c.Persist = CRED_PERSIST_LOCAL_MACHINE;
                c.UserName = Environment.UserName;
                return CredWrite(ref c, 0);
            }
            finally { Marshal.FreeHGlobal(p); }
        }

        public static string Load(string target)
        {
            IntPtr ptr;
            if (!CredRead(target, CRED_TYPE_GENERIC, 0, out ptr)) return null;
            try
            {
                CREDENTIAL c = (CREDENTIAL)Marshal.PtrToStructure(ptr, typeof(CREDENTIAL));
                if (c.CredentialBlobSize == 0) return null;
                byte[] blob = new byte[c.CredentialBlobSize];
                Marshal.Copy(c.CredentialBlob, blob, 0, blob.Length);
                return Encoding.UTF8.GetString(blob);
            }
            finally { CredFree(ptr); }
        }

        public static void Delete(string target)
        {
            try { CredDelete(target, CRED_TYPE_GENERIC, 0); } catch { }
        }
    }
}
