using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;
using ContextGUI.Services.Interfaces;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ContextGUI.Dialogs;

public sealed class ItemEditDialogService : IItemEditDialogService
{
    private readonly IContentDialogService _dialogService;

    public ItemEditDialogService(IContentDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<ContextMenuItemEditModel?> ShowEditDialogAsync(ContextMenuItem item, CancellationToken cancellationToken = default)
    {
        var viewModel = new EditItemDialogViewModel(item);
        var view = new EditItemDialogView { DataContext = viewModel };

        var dialog = new ContentDialog
        {
            Title = "Редактировать элемент",
            Content = view,
            PrimaryButtonText = "Сохранить",
            SecondaryButtonText = "Отмена",
            CloseButtonText = "Закрыть",
            DialogWidth = 520,
            DialogHeight = 420
        };

        dialog.Closing += (_, args) =>
        {
            if (args.Result == ContentDialogResult.Primary && !viewModel.Validate())
            {
                args.Cancel = true;
            }
        };

        var result = await _dialogService.ShowAsync(dialog, cancellationToken);
        if (result == ContentDialogResult.Primary)
        {
            return viewModel.ToEditModel();
        }

        return null;
    }
}
