using System.Security.Cryptography;
using System.Text;

namespace NetBlox;

public static class HashingUtils
{
    public static string Sha256(string data)
    {
        byte[] originalData = Encoding.UTF8.GetBytes(data);
        byte[] hashData = SHA256.HashData(originalData);
        return string.Concat(hashData.Select(x => x.ToString("X2")));
    }   
}