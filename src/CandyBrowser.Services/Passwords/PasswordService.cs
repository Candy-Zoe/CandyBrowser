using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using CandyBrowser.Data.Contexts;
using CandyBrowser.Data.Entities;
using CandyBrowser.Shared.Abstractions;
using Models = CandyBrowser.Core.Models;

namespace CandyBrowser.Services.Passwords;

public class PasswordService : IPasswordService
{
    private readonly BrowserDbContext _db;
    private readonly byte[] _masterKey;
    private readonly string _keyStoragePath;

    public PasswordService(BrowserDbContext db, byte[]? masterKey = null)
    {
        _db = db;
        
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CandyBrowser");
        Directory.CreateDirectory(dataDir);
        _keyStoragePath = Path.Combine(dataDir, ".master_key");

        if (masterKey != null && masterKey.Length == 32)
        {
            _masterKey = masterKey;
        }
        else
        {
            _masterKey = LoadOrCreateMasterKey();
        }
    }

    public async Task<IReadOnlyList<Models.PasswordEntry>> GetAllAsync()
    {
        return await _db.Passwords
            .OrderBy(p => p.Domain)
            .ThenBy(p => p.Username)
            .Select(p => MapToModel(p))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Models.PasswordEntry>> SearchAsync(string query, int limit = 10)
    {
        return await _db.Passwords
            .Where(p => p.Domain.Contains(query) || p.Username.Contains(query))
            .OrderBy(p => p.Domain)
            .Take(limit)
            .Select(p => MapToModel(p))
            .ToListAsync();
    }

    public async Task<Models.PasswordEntry?> GetByIdAsync(long id)
    {
        var entity = await _db.Passwords.FindAsync(id);
        return entity == null ? null : MapToModel(entity);
    }

    public async Task<Models.PasswordEntry?> GetByDomainAsync(string domain)
    {
        var entity = await _db.Passwords.FirstOrDefaultAsync(p => p.Domain == domain);
        return entity == null ? null : MapToModel(entity);
    }

    public async Task<Models.PasswordEntry> SaveAsync(Models.PasswordEntry entry)
    {
        var existing = await _db.Passwords
            .FirstOrDefaultAsync(p => p.Domain == entry.Domain && p.Username == entry.Username);

        if (existing != null)
        {
            existing.Url = entry.Url;
            existing.Password = await EncryptAsync(entry.Password);
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return MapToModel(existing);
        }

        var entity = new PasswordEntity
        {
            Domain = entry.Domain,
            Username = entry.Username,
            Password = await EncryptAsync(entry.Password),
            Url = entry.Url,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Passwords.Add(entity);
        await _db.SaveChangesAsync();

        entry.Id = entity.Id;
        entry.CreatedAt = entity.CreatedAt;
        entry.UpdatedAt = entity.UpdatedAt;
        return entry;
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _db.Passwords.FindAsync(id);
        if (entity == null) return;

        _db.Passwords.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public Task<string> EncryptAsync(string plainText)
    {
        return Task.Run(() =>
        {
            using var aes = Aes.Create();
            aes.Key = _masterKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
            }

            var cipherBytes = ms.ToArray();
            return Convert.ToBase64String(cipherBytes);
        });
    }

    public Task<string> DecryptAsync(string cipherText)
    {
        return Task.Run(() =>
        {
            var cipherBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _masterKey;

            var iv = new byte[16];
            Array.Copy(cipherBytes, 0, iv, 0, 16);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs, Encoding.UTF8);

            return reader.ReadToEnd();
        });
    }

    public Task<string> GeneratePasswordAsync(int length = 16, bool includeSymbols = true)
    {
        return Task.Run(() =>
        {
            const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var chars = letters + digits;
            if (includeSymbols) chars += symbols;

            var result = new char[length];
            using var rng = RandomNumberGenerator.Create();

            for (int i = 0; i < length; i++)
            {
                var randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                var randomInt = BitConverter.ToUInt32(randomBytes, 0);
                result[i] = chars[(int)(randomInt % (uint)chars.Length)];
            }

            return new string(result);
        });
    }

    private byte[] LoadOrCreateMasterKey()
    {
        try
        {
            if (File.Exists(_keyStoragePath))
            {
                var keyBytes = File.ReadAllBytes(_keyStoragePath);
                if (keyBytes.Length == 32)
                    return keyBytes;
            }
        }
        catch { }

        // Generate new key and persist it
        var newKey = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(newKey);

        try
        {
            File.WriteAllBytes(_keyStoragePath, newKey);
            // Restrict file permissions on Unix
#if UNIX
            var info = new System.IO.FileInfo(_keyStoragePath);
            info.Attributes |= System.IO.FileAttributes.Hidden;
#endif
        }
        catch { /* best effort persistence */ }

        return newKey;
    }

    private static Models.PasswordEntry MapToModel(PasswordEntity entity)
    {
        return new Models.PasswordEntry
        {
            Id = entity.Id,
            Domain = entity.Domain,
            Username = entity.Username,
            Password = entity.Password, // encrypted
            Url = entity.Url,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            SyncId = entity.SyncId
        };
    }
}
