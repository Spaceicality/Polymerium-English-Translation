using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceAdvancedConfigurationView : Page
{
    public InstanceAdvancedConfigurationView()
    {
        ViewModel =
            App.Current.Provider.GetRequiredService<InstanceAdvancedConfigurationViewModel>();
        InitializeComponent();
    }

    public InstanceAdvancedConfigurationViewModel ViewModel { get; }

    private async void DeleteInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmationDialog
        {
            XamlRoot = XamlRoot,
            Title = ViewModel.Localization.GetString("InstanceAdvancedConfigurationView_Confirm_Title"),
            Text = ViewModel.Localization.GetString("InstanceAdvancedConfigurationView_Confirm_Text")
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            if (ViewModel.DeleteInstance())
                ViewModel.PopNotification(
                    ViewModel.Localization.GetString("InstanceAdvancedConfigurtationView_Delete_Caption"),
                    ViewModel.Localization.GetString("InstanceAdvancedConfigurtationView_Delete_Success_Message"));
            else
                ViewModel.PopNotification(
                    ViewModel.Localization.GetString("InstanceAdvancedConfigurtationView_Delete_Caption"),
                    ViewModel.Localization.GetString("InstanceAdvancedConfigurtationView_Delete_Failure_Message"),
                    InfoBarSeverity.Error);
    }

    private async void ResetInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmationDialog
        {
            XamlRoot = XamlRoot,
            Title = ViewModel.Localization.GetString("InstanceAdvancedConfigurationView_Confirm_Title"),
            Text = ViewModel.Localization.GetString("InstanceAdvancedConfigurationView_Confirm_Text")
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            if (ViewModel.ResetInstance())
                ViewModel.PopNotification(
                    ViewModel.Localization.GetString("InstanceAdvancedConfigurtationView_Reset_Caption"),
                    ViewModel.Localization.GetString("InstanceAdvancedConfigurtationView_Reset_Success_Message"));
            else
                ViewModel.PopNotification(
                    ViewModel.Localization.GetString("InstanceAdvancedConfigurtationView_Reset_Caption"),
                    ViewModel.Localization.GetString("InstanceAdvancedConfigurtationView_Reset_Failure_Message"),
                    InfoBarSeverity.Error);
    }

    private async void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        var instance = ViewModel.Context.AssociatedInstance;
        var dialog = new TextInputDialog();
        dialog.Title = ViewModel.Localization.GetString("InstanceAdvancedConfigurationView_Rename_Title");
        dialog.InputTextPlaceholder = instance!.Name;
        dialog.Description = ViewModel.Localization.GetString("InstanceAdvancedConfigurationView_Rename_Description");
        dialog.XamlRoot = App.Current.Window.Content.XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var result = ViewModel.RenameInstance(dialog.InputText);
            if (result.HasValue)
            {
                var errorDialog = new MessageDialog
                {
                    XamlRoot = App.Current.Window.Content.XamlRoot,
                    Title = ViewModel.Localization.GetString("InstanceAdvancedConfigurationView_Rename_Title"),
                    Message = result.Value.ToString()
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}