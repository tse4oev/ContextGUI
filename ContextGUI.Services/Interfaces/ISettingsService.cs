using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;

namespace ContextGUI.Services.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
