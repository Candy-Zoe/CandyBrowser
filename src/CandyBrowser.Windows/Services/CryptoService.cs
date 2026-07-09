using System.IO;
using System.Security.Cryptography;

namespace CandyBrowser.Windows.Services;

public static class CryptoService
{
    private static readonly string KeyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CandyBrowser",
        "master.key");

    public static byte[] GetOrCreateMasterKey()
    {
        var dir = Path.GetDirectoryName(KeyFilePath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(KeyFilePath))
        {
            return File.ReadAllBytes(KeyFilePath);
        }

        var key = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }

        File.WriteAllBytes(KeyFilePath, key);
        return key;
    }
}
