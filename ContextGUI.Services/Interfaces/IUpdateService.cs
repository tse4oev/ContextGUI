using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;

namespace ContextGUI.Services.Interfaces;

public interface IUpdateService
{
    Task<RegistryResult<UpdateInfo>> CheckForUpdateAsync(UpdateSettings settings, CancellationToken cancellationToken = default);
    Version GetCurrentVersion();
}
