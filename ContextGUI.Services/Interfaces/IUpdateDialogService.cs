using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;

namespace ContextGUI.Services.Interfaces;

public interface IUpdateDialogService
{
    Task<UpdateDialogResult> ShowUpdateDialogAsync(UpdateInfo updateInfo, Version currentVersion, CancellationToken cancellationToken = default);
}
