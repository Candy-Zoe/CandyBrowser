using System.Collections.Generic;
using System.Threading.Tasks;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Shared.Abstractions
{
    public interface IPasswordService
    {
        Task<IReadOnlyList<PasswordEntry>> GetAllAsync();
        Task<IReadOnlyList<PasswordEntry>> SearchAsync(string query, int limit = 10);
        Task<PasswordEntry?> GetByIdAsync(long id);
        Task<PasswordEntry?> GetByDomainAsync(string domain);
        Task<PasswordEntry> SaveAsync(PasswordEntry entry);
        Task DeleteAsync(long id);
        Task<string> EncryptAsync(string plainText);
        Task<string> DecryptAsync(string cipherText);
        Task<string> GeneratePasswordAsync(int length = 16, bool includeSymbols = true);
    }
}
