using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Haru.Kei.Models;
using Haru.Kei.Views;
using Livet.Messaging;
using Livet.Messaging.IO;
using Prism.Dialogs;
using Prism.Mvvm;
using Reactive.Bindings;

namespace Haru.Kei.ViewModels;

public class MainWindowViewModel : BindableBase {
	public static string ConfirmationKey = "Confirmation";

	private readonly string CONFIG_FILE = "frontend.conf";
	private readonly string CONFIG2_FILE = "frontend.dev.v250705.conf";
	private readonly string FILTER_FILE = "frontend-filter.conf";
	private readonly string BAT_FILE = "custom-recognize.bat";
	private readonly string TEMP_BAT = global::System.IO.Path.Combine(
		global::System.IO.Path.GetTempPath(),
		string.Format("recognize-gui-{0}.bat", Guid.NewGuid()));

	public ReactiveProperty<Models.Filter> Filter { get; } = new(initialValue: new());

	public ReactiveCommand CreateBatchommand { get; } = new();
	public ReactiveCommand MicTestCommand { get; } = new();
	public ReactiveCommand AmbientTestCommand { get; } = new();
	public ReactiveCommand CloseCommand { get; } = new();

	public ReactiveCommand ConnectWhisperCommand { get; } = new();
	public ReactiveCommand ConnectGoogleCommand { get; } = new();
	public ReactiveCommand ConnectYukarinetteCommand { get; } = new();
	public ReactiveCommand ConnectYukaConeCommand { get; } = new();

	public ReactiveCommand ExecCommand { get; } = new();


	public ReactiveCommand<RoutedEventArgs> LoadedCommand { get; } = new();
	public ReactiveCommand ClosingCommand { get; } = new();


	public ReactiveCommand<OpeningFileSelectionMessage> IlluminateClientClickCommand { get; } = new();	

	public ReactiveCommand<RoutedEventArgs> FilterAddClickCommand { get; } = new();
	public ReactiveCommand<RoutedEventArgs> FilterRemoveClickCommand { get; } = new();
	public ReactiveCommand<RoutedEventArgs> RuleAddClickCommand { get; } = new();
	public ReactiveCommand<RoutedEventArgs> RuleEditClickCommand { get; } = new();
	public ReactiveCommand<RoutedEventArgs> RuleRemoveClickCommand { get; } = new();

	public ReactiveProperty<Models.FilterItem?> SelectedFilterItem { get; } = new();


	public InteractionMessenger Messenger { get; } = new();

	private global::System.Windows.Forms.PropertyGrid? propertyGrid;
	private readonly IDialogService dialogService;

	public Models.Config Config { get; private set; }
	public Models.ConfigBinder ConfigBinder { get; private set; }

	public MainWindowViewModel(IDialogService dialogService) {
		this.dialogService = dialogService;
		this.LoadedCommand.Subscribe(async x => await this.OnLoaded(x));
		this.ClosingCommand.Subscribe(() => this.OnClosing());
		this.IlluminateClientClickCommand.Subscribe(x => this.OnIlluminateClientClick(x));
		this.FilterAddClickCommand.Subscribe(_ => this.OnFilterAdd());
		this.FilterRemoveClickCommand.Subscribe(x => this.OnFilterRemove(x));
		this.RuleAddClickCommand.Subscribe(x => this.OnRuleAdd(x));
		this.RuleEditClickCommand.Subscribe(x => this.OnRuleEdit(x));
		this.RuleRemoveClickCommand.Subscribe(x => this.OnRuleRemove(x));

		this.SelectedFilterItem.Subscribe(x => {
			if((x == null) && (this.Filter.Value.Filters?.FirstOrDefault() is Models.FilterItem it)) {
				this.SelectedFilterItem.Value = it;
			}
		});

		this.ExecCommand.Subscribe(() => {
			System.Diagnostics.Debug.Assert(this.Config is not null);
			this.SaveConfig();

			var bat = new StringBuilder()
				.AppendLine("@echo off")
				.AppendLine()
				.AppendFormat("\"{0}\"", this.Config.Extra.RecognizeExePath).Append(" ").AppendLine(this.Config.ToCommandOption())
				.AppendLine("if %ERRORLEVEL% neq 0 (")
				.AppendLine("  pause")
				.AppendLine(")");
			File.WriteAllText(this.TEMP_BAT, bat.ToString(), Encoding.GetEncoding("Shift_JIS"));

			try {
				using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
					FileName = this.TEMP_BAT,
					WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
					UseShellExecute = true,
				})) { }
			}
			catch(Exception) { }
		});

		this.CreateBatchommand.Subscribe(async () => {
			System.Diagnostics.Debug.Assert(this.Config is not null);
			try {
				var bat = new StringBuilder()
					.AppendLine("@echo off")
					.AppendLine("pushd \"%~dp0\"")
					.AppendLine()
					.AppendFormat("\"{0}\"", this.Config.Extra.RecognizeExePath).Append(" ").AppendLine(this.Config.ToCommandOption())
					.AppendLine("pause");
				File.WriteAllText(
					global::System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.BAT_FILE),
					bat.ToString(),
					Encoding.GetEncoding("Shift_JIS"));

				await Messenger.RaiseAsync(new ConfirmationMessage(
					$"{this.BAT_FILE}を作成しました！",
					"ゆーかねすぴれこ",
					ConfirmationKey) {

					Button = MessageBoxButton.OK,
					Image = MessageBoxImage.Information,
				});
			}
			catch(System.IO.IOException) { }
		});
		this.MicTestCommand.Subscribe(() => {
			System.Diagnostics.Debug.Assert(this.Config is not null);
			try {
				using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
					FileName = this.Config.Extra.RecognizeExePath,
					Arguments = string.Format("--test mic {0}", this.Config.ToCommandOption()),
					UseShellExecute = true,
				})) { }
			}
			catch(Exception) { }
		});
		this.AmbientTestCommand.Subscribe((_) => {
			System.Diagnostics.Debug.Assert(this.Config is not null);
			try {
				using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
					FileName = this.Config.Extra.RecognizeExePath,
					Arguments = string.Format("--test mic_ambient {0}", this.Config.ToCommandOption()),
					UseShellExecute = true,
				})) { }
			}
			catch(Exception) { }
		});
		this.CloseCommand.Subscribe((_) => {
			global::System.Windows.Application.Current?.Shutdown();
		});

		this.ConnectWhisperCommand.Subscribe(() => {
		});
		this.ConnectGoogleCommand.Subscribe((_) => {
		});
		this.ConnectYukarinetteCommand.Subscribe((_) => {
		});
		this.ConnectYukaConeCommand.Subscribe(() => {
		});

		try {
			if(File.Exists(this.CONFIG2_FILE)) {
				var json = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.CONFIG2_FILE));
				this.Config = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Config>(json) switch {
					Models.Config v => v,
					_ => new Config(),
				};
			} else {
				this.Config = new();
			}
		}
		catch(Exception) {
			this.Config = new();
		}
		this.ConfigBinder = new(this.Config);

		try {
			if(File.Exists(this.Config.Extra.RecognizeExePath)) {
				this.ConfigBinder.MicDevicesBinder.Clear();
				this.ConfigBinder.MicDevicesBinder.Add("設定しない");
				using(var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
					FileName = this.Config.Extra.RecognizeExePath,
					Arguments = "--print_mics",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
					CreateNoWindow = true,
				})) {
					string? s;
					while((s = p?.StandardOutput?.ReadLine()) != null) {
						this.ConfigBinder.MicDevicesBinder.Add(s);
					}
					p.WaitForExit();
				}
			}
		}
		catch(Exception) { }
	}

	private async Task OnLoaded(RoutedEventArgs e) {
		this.propertyGrid = ((MainWindow)e.Source).propertyGrid;
		this.propertyGrid.SelectedObject = this.Config.Extra;
		if(!IsValidExePath(this.Config)) {
			await Messenger.RaiseAsync(new ConfirmationMessage(
				"パスに不正な文字が含まれます。ゆーかねすぴれこは英数字だけのパスに配置してください。",
				"ゆーかねすぴれこ",
				ConfirmationKey) {

				Button = MessageBoxButton.OK,
				Image = MessageBoxImage.Warning,
			}); 
			global::System.Windows.Application.Current?.Shutdown();
		}

		Models.Filter? filter = default;
		try {
			var json = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.FILTER_FILE));
			filter = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Filter>(json);
		}
		catch(IOException) { }
		if(filter != null) {
			this.Filter.Value = filter;
			this.SelectedFilterItem.Value = filter.Filters?.FirstOrDefault();
		}
	}

	private void OnClosing() {
		this.SaveConfig();
		try {
			if(File.Exists(this.TEMP_BAT)) {
				File.Delete(this.TEMP_BAT);
			}
		}
		catch(IOException) { }
	}

	private void OnIlluminateClientClick(OpeningFileSelectionMessage e) { 
		if(e.Response?.FirstOrDefault() is string file) {
			this.ConfigBinder.IlluminateClientBinding.Value = file;
		}
	}

	private void OnFilterAdd() {
		var f = new Models.FilterItem();
		f.Name.Value = "新規フィルタ";
		this.Filter.Value.Filters?.Add(f);
		this.SelectedFilterItem.Value = f;
	}

	private void OnFilterRemove(RoutedEventArgs e) {
		if(e.Source is FrameworkElement el && el.DataContext is Models.FilterItem item) {
			this.Filter.Value.Filters?.Remove(item);
		}
	}

	private void OnRuleAdd(RoutedEventArgs e) {
		if(this.SelectedFilterItem.Value != null) {
			var parameters = new DialogParameters();
			this.dialogService.ShowDialog(nameof(Views.FilterRuleEditDialog), parameters, dialogResult => {
				if(dialogResult.Result == ButtonResult.OK) {
					if(dialogResult.Parameters["result"] is Models.FilterRule r) {
						this.SelectedFilterItem.Value.Rules?.Add(r);
					}
				}
			});
		}
	}

	private void OnRuleEdit(RoutedEventArgs e) {
		if(e.Source is FrameworkElement el && el.DataContext is Models.FilterRule rule) {
			if(this.SelectedFilterItem.Value != null) {
				var index = this.SelectedFilterItem.Value.Rules?.Select((x, i) => (V: x, I: i))
					.Where(x => object.ReferenceEquals(x.V, rule))
					.Select(x => x.I)
					.FirstOrDefault();
				System.Diagnostics.Debug.WriteLine(index);


				var parameters = new DialogParameters
				{
					{ "input", rule}
				};

				this.dialogService.ShowDialog(nameof(Views.FilterRuleEditDialog), parameters, dialogResult => {
					if(dialogResult.Result == ButtonResult.OK) {
					}
				});
			}
		}
	}


	private void OnRuleRemove(RoutedEventArgs e) {
		if(e.Source is FrameworkElement el && el.DataContext is Models.FilterRule rule) {
			if(this.SelectedFilterItem.Value != null) {
				this.SelectedFilterItem.Value.Rules?.Remove(rule);
			}
		}
	}


	private bool IsValidExePath(Models.Config argument) {
		if(string.IsNullOrEmpty( argument.Extra.RecognizeExePath)) {
			return true;
		}

		try {
			var path = Helpers.Util.IsFullPath(argument.Extra.RecognizeExePath) switch {
				true => argument.Extra.RecognizeExePath,
				false => global::System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.Extra.RecognizeExePath) // 作業ディレクトリがexeのディレクトリとは限らないので作り直す
			};

			// ASCIIを超えた場合はfalse
			foreach(var c in path) {
				if(255 < c) {
					return false;
				}
			}
			return true;
		}
		catch(ArgumentException) { return false; }
		catch(SecurityException) { return false; }
		catch(NotSupportedException) { return false; }
		catch(PathTooLongException) { return false; }
	}

	private void SaveConfig() {
		static bool json(string fileName, object o) {
			try {
				System.IO.File.WriteAllText(
					System.IO.Path.Combine(AppContext.BaseDirectory, fileName),
					Newtonsoft.Json.JsonConvert.SerializeObject(o));
				return true;
			}
			catch {
				return false;
			}
		}

		json(this.CONFIG2_FILE, this.Config);
		json(this.FILTER_FILE, this.Filter.Value);
	}
}