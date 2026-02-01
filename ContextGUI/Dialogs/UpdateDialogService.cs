using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;
using ContextGUI.Services.Interfaces;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ContextGUI.Dialogs;

public sealed class UpdateDialogService : IUpdateDialogService
{
    private readonly IContentDialogService _dialogService;

    public UpdateDialogService(IContentDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<UpdateDialogResult> ShowUpdateDialogAsync(UpdateInfo updateInfo, System.Version currentVersion, CancellationToken cancellationToken = default)
    {
        var content = BuildContent(updateInfo, currentVersion);

        var dialog = new ContentDialog
        {
            Title = "Доступно обновление",
            Content = content,
            PrimaryButtonText = "Открыть релиз",
            SecondaryButtonText = "Не показывать эту версию",
            CloseButtonText = "Позже",
            DialogWidth = 560,
            DialogHeight = 420
        };

        var result = await _dialogService.ShowAsync(dialog, cancellationToken);
        return result switch
        {
            ContentDialogResult.Primary => UpdateDialogResult.OpenRelease,
            ContentDialogResult.Secondary => UpdateDialogResult.SkipVersion,
            _ => UpdateDialogResult.None
        };
    }

    private static string BuildContent(UpdateInfo updateInfo, System.Version currentVersion)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Текущая версия: {currentVersion}");
        builder.AppendLine($"Новая версия: {updateInfo.Version}");
        if (updateInfo.PublishedAt.HasValue)
        {
            builder.AppendLine($"Дата релиза: {updateInfo.PublishedAt.Value:dd.MM.yyyy}");
        }

        if (!string.IsNullOrWhiteSpace(updateInfo.Notes))
        {
            builder.AppendLine();
            builder.AppendLine("Что нового:");
            builder.AppendLine(updateInfo.Notes);
        }

        return builder.ToString();
    }
}
