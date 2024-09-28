using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Haru.Kei;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
	private readonly ViewModel _viewModel;

	public MainWindow() {
		InitializeComponent();

		this.DataContext = _viewModel = new();
	}

	private void OnClickNext1(object sender, RoutedEventArgs e) {
		this._viewModel.DoPhase1()
			.Subscribe(
				x => {
					if(!string.IsNullOrEmpty(x)) {
						MessageBox.Show(this, x, "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						App.Current.Shutdown();
					}
				},
				() => {
					this.DoPhase2();
				});
	}

	private void DoPhase2() {
		this._viewModel.DoPhase2()
			.Subscribe(
				x => {
					if(!string.IsNullOrEmpty(x)) {
						MessageBox.Show(this, x, "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						App.Current.Shutdown();
					}
				},
				() => {
					this.DoPhase3();
				});
	}

	private void DoPhase3() {
		this._viewModel.DoPhase3()
			.Subscribe(
				x => {
					// TODO: リトライ実装
					if(!string.IsNullOrEmpty(x)) {
						MessageBox.Show(this, x, "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						App.Current.Shutdown();
					}
				},
				() => {
					this.DoPhase4();
				});
	}

	private void DoPhase4() {
		this._viewModel.DoPhase4()
			.Subscribe(
				x => {
					// TODO: リトライ実装
					if(!string.IsNullOrEmpty(x)) {
						MessageBox.Show(this, x, "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						App.Current.Shutdown();
					}
				},
				() => {
					this.DoPhase5();
				});
	}
	private void DoPhase5() {
		this._viewModel.DoPhase5()
			.Subscribe(
				x => {
					if(!string.IsNullOrEmpty(x)) {
						MessageBox.Show(this, x, "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						App.Current?.Shutdown();
					}
				},
				() => {
					this.DoPhase6();
				});
	}

	private void DoPhase6() {
		this._viewModel.DoPhase6()
			.Subscribe(
				x => {
					if(!string.IsNullOrEmpty(x)) {
						MessageBox.Show(this, x, "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						App.Current?.Shutdown();
					}
				},
				() => {
					this._viewModel.DoPhase7();
				});

	}
}