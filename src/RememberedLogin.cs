using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace SRLogbook
{
    /// <summary>
    /// 可选的“记住登录”：把令牌存进 Windows 凭据管理器，再用 6 位 PIN 在应用内解锁。
    /// PIN 只是应用层锁，真正的保护来自系统凭据库与 Windows 账户；连续输错 5 次会清除保存的登录。
    /// </summary>
    public static class RememberedLogin
    {
        public const string Target = "SRLogbook/remembered_login";
        private const int Iterations = 120000;
        private const int MaxFailures = 5;

        public class Saved
        {
            public string Login;
            public string Token;
        }

        public static bool Exists()
        {
            return !string.IsNullOrEmpty(SecretStore.Load(Target));
        }

        public static string SavedLogin()
        {
            Saved s = Read();
            return s == null ? null : s.Login;
        }

        public static bool Save(string login, string token, string pin)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(token) || !ValidPin(pin)) return false;
            byte[] salt = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] hash = Hash(pin, salt, Iterations);
            Dictionary<string, object> d = new Dictionary<string, object>();
            d["v"] = 1;
            d["login"] = login;
            d["token"] = token;
            d["salt"] = Convert.ToBase64String(salt);
            d["hash"] = Convert.ToBase64String(hash);
            d["iter"] = Iterations;
            string json = new JavaScriptSerializer().Serialize(d);
            if (json.Length > 2000) return false;
            return SecretStore.Save(Target, json);
        }

        /// <summary>校验 PIN；成功返回令牌与账号，失败返回 null。</summary>
        public static Saved Unlock(string pin)
        {
            Saved s = Read();
            if (s == null || !ValidPin(pin)) return null;
            Dictionary<string, object> d = Parse();
            if (d == null) return null;
            try
            {
                byte[] salt = Convert.FromBase64String(Convert.ToString(d["salt"]));
                byte[] expected = Convert.FromBase64String(Convert.ToString(d["hash"]));
                int iter = d.ContainsKey("iter") ? Convert.ToInt32(d["iter"]) : Iterations;
                byte[] actual = Hash(pin, salt, iter);
                return FixedEquals(expected, actual) ? s : null;
            }
            catch { return null; }
        }

        public static void Delete(string appDir)
        {
            SecretStore.Delete(Target);
            DeleteFailureFile(appDir);
        }

        public static bool ValidPin(string pin)
        {
            if (pin == null || pin.Length != 6) return false;
            foreach (char c in pin) if (c < '0' || c > '9') return false;
            return true;
        }

        public static int Failures(string appDir)
        {
            try
            {
                string path = FailurePath(appDir);
                if (!File.Exists(path)) return 0;
                int n;
                return int.TryParse(File.ReadAllText(path, Encoding.UTF8).Trim(), out n) ? n : 0;
            }
            catch { return 0; }
        }

        public static int RecordFailure(string appDir)
        {
            int n = Failures(appDir) + 1;
            try { File.WriteAllText(FailurePath(appDir), n.ToString(), new UTF8Encoding(false)); } catch { }
            return n;
        }

        public static void ResetFailures(string appDir)
        {
            DeleteFailureFile(appDir);
        }

        public static int MaxAttempts { get { return MaxFailures; } }

        private static void DeleteFailureFile(string appDir)
        {
            try
            {
                if (string.IsNullOrEmpty(appDir)) return;
                string path = FailurePath(appDir);
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        private static string FailurePath(string appDir)
        {
            return Path.Combine(appDir, "pin_fails.txt");
        }

        private static Saved Read()
        {
            try
            {
                Dictionary<string, object> d = Parse();
                if (d == null) return null;
                Saved s = new Saved();
                s.Login = Convert.ToString(d["login"]);
                s.Token = Convert.ToString(d["token"]);
                if (string.IsNullOrEmpty(s.Login) || string.IsNullOrEmpty(s.Token)) return null;
                return s;
            }
            catch { return null; }
        }

        private static Dictionary<string, object> Parse()
        {
            try
            {
                string json = SecretStore.Load(Target);
                if (string.IsNullOrEmpty(json)) return null;
                return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            }
            catch { return null; }
        }

        private static byte[] Hash(string pin, byte[] salt, int iterations)
        {
            using (Rfc2898DeriveBytes k = new Rfc2898DeriveBytes(pin, salt, iterations, HashAlgorithmName.SHA256))
                return k.GetBytes(32);
        }

        private static bool FixedEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
