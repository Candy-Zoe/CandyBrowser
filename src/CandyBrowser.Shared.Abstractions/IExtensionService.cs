using System.Collections.Generic;
using System.Threading.Tasks;
using CandyBrowser.Core.Models;

namespace CandyBrowser.Shared.Abstractions
{
    public interface IExtensionService
    {
        Task<IReadOnlyList<ExtensionInfo>> GetAllAsync();
        Task InstallExtensionAsync(string manifestPath);
        Task UninstallExtensionAsync(string extensionId);
        Task EnableExtensionAsync(string extensionId);
        Task DisableExtensionAsync(string extensionId);
        Task<ExtensionInfo?> GetByIdAsync(string extensionId);
        Task<ExtensionInfo?> GetByManifestAsync(string manifestJson);
    }
}
