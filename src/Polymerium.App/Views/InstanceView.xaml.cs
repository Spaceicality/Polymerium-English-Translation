using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class InstanceView : Page
{
    public bool IsPending
    {
        get => (bool)GetValue(IsPendingProperty);
        set => SetValue(IsPendingProperty, value);
    }

    // Using a DependencyProperty as the backing store for IsPending.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsPendingProperty =
        DependencyProperty.Register(nameof(IsPending), typeof(bool), typeof(InstanceView), new PropertyMetadata(false));



    public InstanceView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<InstanceViewModel>();
        InitializeComponent();
    }

    public InstanceViewModel ViewModel { get; }

    private void Header_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        EditButton.Opacity = 1.0;
    }

    private void Header_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        EditButton.Opacity = 0.0;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadAssets();
        IsPending = true;
        Task.Run(() => ViewModel.LoadInstanceInformationAsync(LoadInformationHandler));
    }

    private void LoadInformationHandler(Uri? url, bool isNeeded)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.IsRestorationNeeded = isNeeded;
            ViewModel.ReferenceUrl = url;
            IsPending = false;
        });
    }
}