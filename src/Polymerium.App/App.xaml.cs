// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.App.Configurations;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.App.ViewModels.AddAccountWizard;
using Polymerium.App.ViewModels.Instances;
using Polymerium.App.Views;
using Polymerium.Core;
using Polymerium.Core.Engines;
using Polymerium.Core.Importers;
using Polymerium.Core.Managers;
using Polymerium.Core.ResourceResolving;
using Polymerium.Core.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Provider = ConfigureServices();
    }

    public new static App Current => (App)Application.Current;

    public MainWindow Window { get; private set; } = null!;

    public IServiceProvider Provider { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        Window.Closed += Window_Closed;
        Window.Activate();
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        ((IDisposable)Provider).Dispose();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        // application services registration
        services
            .AddLogging(
                logging =>
                    logging
                        .AddSimpleConsole()
                        .AddDebug()
#if DEBUG
                        .SetMinimumLevel(LogLevel.Debug)
#endif
            )
            .AddMemoryCache()
            .AddHttpClient();
        // view models registration
        services
            .AddSingleton<ViewModelContext>()
            .AddTransient<MainViewModel>()
            .AddTransient<NewInstanceViewModel>()
            .AddTransient<CreateInstanceWizardViewModel>()
            .AddTransient<InstanceViewModel>()
            .AddTransient<HomeViewModel>()
            .AddTransient<SettingViewModel>()
            .AddTransient<PrepareGameViewModel>()
            .AddTransient<AddAccountWizardViewModel>()
            .AddTransient<SelectionViewModel>()
            .AddTransient<OfflineAccountViewModel>()
            .AddTransient<MicrosoftAccountIntroViewModel>()
            .AddTransient<MicrosoftAccountAuthViewModel>()
            .AddTransient<MicrosoftAccountFinishViewModel>()
            .AddTransient<InstanceConfigurationViewModel>()
            .AddTransient<InstanceMetadataConfigurationViewModel>()
            .AddTransient<InstanceLaunchConfigurationViewModel>()
            .AddTransient<InstanceAdvancedConfigurationViewModel>()
            .AddTransient<InstanceAssetViewModel>()
            .AddTransient<AddMetaComponentWizardViewModel>()
            .AddTransient<ImportModpackWizardViewModel>()
            .AddTransient<SearchCenterViewModel>()
            .AddTransient<SearchDetailViewModel>()
            .AddTransient<InstanceUpdateViewModel>();
        // local service registration
        services
            .AddSingleton<IOverlayService, WindowOverlayService>()
            .AddSingleton<INotificationService, InAppNotificationService>()
            .AddSingleton<NavigationService>()
            .AddSingleton<AccountManager>()
            .AddSingleton<InstanceManager>()
            .AddSingleton<ConfigurationManager>()
            .AddSingleton<DataStorage>()
            .AddSingleton<FilePoolService>()
            .AddSingleton<MemoryStorage>()
            .AddSingleton<ComponentManager>()
            .AddSingleton<JavaManager>()
            .AddSingleton<ImportService>()
            .Configure<ImportServiceOptions>(
                configure =>
                    configure
                        .Register<CurseForgeImporter>("manifest.json")
                        .Register<ModrinthImporter>("modrinth.index.json")
            )
            .AddSingleton<LocalizationService>();
        // global services
        services
            .AddSingleton<AssetManager>()
            .AddSingleton<GameManager>()
            .AddSingleton<ResourceManager>()
            .AddSingleton<IFileBaseService, MainFileBaseService>()
            .Configure<MainFileBaseOptions>(
                configure =>
                    configure.BaseFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".polymerium/"
                    )
            )
            .AddSingleton<AppSettings>();
        // engines
        services.AddScoped<DownloadEngine>().AddScoped<ResolveEngine>().AddScoped<RestoreEngine>();
        // resolvers
        services
            .AddTransient<ResourceResolverBase, LocalFileResolver>()
            .AddTransient<ResourceResolverBase, RemoteFileResolver>()
            .AddTransient<ResourceResolverBase, CurseForgeResolver>()
            .AddTransient<ResourceResolverBase, ModrinthResolver>();
        // repository
        services
            .AddTransient<IResourceRepository, CurseForgeRepository>()
            .AddTransient<IResourceRepository, ModrinthRepository>();
        return services.BuildServiceProvider();
    }
}
