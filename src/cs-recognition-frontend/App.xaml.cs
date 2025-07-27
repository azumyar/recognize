using Prism.Unity;
using Prism.Mvvm;
using Prism.Ioc;
using System.ComponentModel;
using System;
using System.Configuration;
using System.Data;
using System.Windows;
using static Haru.Kei.Views.MainWindow;
namespace Haru.Kei;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication {

	protected override void OnStartup(StartupEventArgs e) {
		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		Reactive.Bindings.UIDispatcherScheduler.Initialize();

		base.OnStartup(e);
	}

	protected override void OnExit(ExitEventArgs e) {
		base.OnExit(e);
	}

	protected override Window CreateShell() {
		return Container.Resolve<Views.MainWindow>();
	}

	protected override void RegisterTypes(IContainerRegistry containerRegistry) {
		base.ConfigureViewModelLocator();

		ViewModelLocationProvider.Register<Views.MainWindow, ViewModels.MainWindowViewModel>();
		containerRegistry.RegisterDialog<Views.FilterRuleEditDialog>();
		containerRegistry.RegisterInstance(this.Container);
	}
}

