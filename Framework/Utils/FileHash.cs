using System.Security.Cryptography;
using System.Text;

namespace Utils
{
    public static class FileHash
    {
        public static string HashContents(string fileContents)
        {
            var fileBytes = Encoding.ASCII.GetBytes(fileContents
                .Replace(Environment.NewLine, "")
                .Replace(" ", "")); // Ignore whitespace deviations in openapi.yaml
      
            var hash = SHA256.HashData(fileBytes);
            return BitConverter.ToString(hash);
        }
    
        public static string Hash(string filePath)
        {
            var file = File.ReadAllText(filePath);
            return HashContents(file);
        }
    }
}
