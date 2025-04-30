using System.Configuration;
using System.Data;
using System.Windows;

namespace Haru.Kei;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : global::System.Windows.Application {

	protected override void OnStartup(StartupEventArgs e) {
		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

		base.OnStartup(e);
	}
}

