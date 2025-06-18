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
using Prism.Dialogs;
using Prism.Mvvm;
using Reactive.Bindings;

namespace Haru.Kei.ViewModels;

public class MainWindowViewModel : BindableBase {
	public static string ConfirmationKey = "Confirmation";

	private readonly string CONFIG_FILE = "frontend.conf";
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

	public ReactiveCommand<RoutedEventArgs> FilterAddClickCommand { get; } = new();
	public ReactiveCommand<RoutedEventArgs> FilterRemoveClickCommand { get; } = new();
	public ReactiveCommand<RoutedEventArgs> RuleAddClickCommand { get; } = new();
	public ReactiveCommand<RoutedEventArgs> RuleEditClickCommand { get; } = new();
	public ReactiveCommand<RoutedEventArgs> RuleRemoveClickCommand { get; } = new();

	public ReactiveProperty<Models.FilterItem?> SelectedFilterItem { get; } = new();


	// 一時的実装
	public ReactiveCommand ___SaveCommand { get; } = new();


	public InteractionMessenger Messenger { get; } = new();

	private global::System.Windows.Forms.PropertyGrid? propertyGrid;
	private readonly IDialogService dialogService;

	public Models.Config Config { get; private set; }
	public Models.ConfigBinder ConfigBinder { get; private set; }

	public MainWindowViewModel(IDialogService dialogService) {
		this.dialogService = dialogService;
		this.LoadedCommand.Subscribe(async x => await this.OnLoaded(x));
		this.ClosingCommand.Subscribe(() => this.OnClosing());
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

		this.___SaveCommand.Subscribe(() => {
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(this.Filter.Value);

			try {
				System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, this.FILTER_FILE), json);
			}
			catch { }
		});


		this.ExecCommand.Subscribe(() => {
			/*
			System.Diagnostics.Debug.Assert(this.arg is not null);
			this.SaveConfig(this.arg);

			var bat = new StringBuilder()
				.AppendLine("@echo off")
				.AppendLine()
				.AppendFormat("\"{0}\"", this.arg.RecognizeExePath).Append(" ").AppendLine(this.GenExeArguments(this.arg))
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
			*/
		});

		this.CreateBatchommand.Subscribe(async () => {
			/*
			System.Diagnostics.Debug.Assert(this.arg is not null);
			try {
				var bat = new StringBuilder()
					.AppendLine("@echo off")
					.AppendLine("pushd \"%~dp0\"")
					.AppendLine()
					.AppendFormat("\"{0}\"", this.arg.RecognizeExePath).Append(" ").AppendLine(this.GenExeArguments(this.arg))
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
			*/
		});
		this.MicTestCommand.Subscribe(() => {
			/*
			System.Diagnostics.Debug.Assert(this.arg is not null);
			var properties = this.arg.GetType().GetProperties();
			try {
				using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
					FileName = this.arg.RecognizeExePath,
					Arguments = string.Format("--test mic {0}", this.GenExeArguments(this.arg)),
					UseShellExecute = true,
				})) { }
			}
			catch(Exception) { }
			*/
		});
		this.AmbientTestCommand.Subscribe((_) => {
			/*
			System.Diagnostics.Debug.Assert(this.arg is not null);
			using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
				FileName = this.arg.RecognizeExePath,
				Arguments = string.Format("--test mic_ambient {0}", this.GenExeArguments(this.arg)),
				UseShellExecute = true,
			})) { }
			*/
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

		this.Config = new();
		this.ConfigBinder = new(this.Config);
	}

	public async Task OnLoaded(RoutedEventArgs e) {
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

	public void OnClosing() {
		//this.SaveConfig(this.arg);
		try {
			if(File.Exists(this.TEMP_BAT)) {
				File.Delete(this.TEMP_BAT);
			}
		}
		catch(IOException) { }
	}

	public void OnFilterAdd() {
		var f = new Models.FilterItem();
		f.Name.Value = "新規フィルタ";
		this.Filter.Value.Filters?.Add(f);
		this.SelectedFilterItem.Value = f;
	}

	public void OnFilterRemove(RoutedEventArgs e) {
		if(e.Source is FrameworkElement el && el.DataContext is Models.FilterItem item) {
			this.Filter.Value.Filters?.Remove(item);
		}
	}

	public void OnRuleAdd(RoutedEventArgs e) {
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

	public void OnRuleEdit(RoutedEventArgs e) {
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


	public void OnRuleRemove(RoutedEventArgs e) {
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
			var path = global::System.IO.Path.GetFullPath(argument.Extra.RecognizeExePath);
			if(path.ToLower() != Config.Extra.RecognizeExePath.ToLower()) {
				// exeは相対パス
				// 作業ディレクトリがexeのディレクトリとは限らないので作り直す
				path = global::System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.Extra.RecognizeExePath);
			} else {
				// exeはフルパス
			}

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

	private string GenExeArguments(RecognizeExeArgument argument) {
		var araguments = new StringBuilder();
		/*
		foreach(var p in argument.GetType().GetProperties()) {
			var att = p.GetCustomAttribute<ArgAttribute>();
			if(att != null) {
				var opt = att.Generate(p.GetValue(this.arg, null), this.arg);
				if(!string.IsNullOrEmpty(opt)) {
					araguments.Append(" ").Append(opt);
				}
			}
		}
		if(!string.IsNullOrEmpty(argument.ExtraArgument)) {
			araguments.Append(" ")
				.Append(argument.ExtraArgument);
		}
		*/
		return araguments.ToString();
	}

	private void SaveConfig(RecognizeExeArgument argument) {
	}

}