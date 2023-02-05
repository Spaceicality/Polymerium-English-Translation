using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceMetadataConfigurationView : Page
{
    public InstanceMetadataConfigurationView()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<InstanceMetadataConfigurationViewModel>();
    }

    public InstanceMetadataConfigurationViewModel ViewModel { get; }
}