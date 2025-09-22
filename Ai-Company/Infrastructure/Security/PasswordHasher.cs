using System;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}


