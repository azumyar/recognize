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
	public class Command : ICommand {
		public Action<object?>? Invoker { get; set; } = default;

		public event EventHandler? CanExecuteChanged;
		public bool CanExecute(object? parameter) => true;
		public void Execute(object? parameter) => this.Invoker?.Invoke(parameter);
	}

	public static string ConfirmationKey = "Confirmation";



	private readonly string CONFIG_FILE = "frontend.conf";
	private readonly string FILTER_FILE = "frontend-filter.conf";
	private readonly string BAT_FILE = "custom-recognize.bat";
	private readonly string TEMP_BAT = global::System.IO.Path.Combine(
		global::System.IO.Path.GetTempPath(),
		string.Format("recognize-gui-{0}.bat", Guid.NewGuid()));

	public ReactiveProperty<Models.Filter> Filter { get; } = new(initialValue: new());

	public Command CreateBatchommand { get; } = new();
	public Command MicTestCommand { get; } = new();
	public Command AmbientTestCommand { get; } = new();
	public Command CloseCommand { get; } = new();

	public Command ConnectWhisperCommand { get; } = new();
	public Command ConnectGoogleCommand { get; } = new();
	public Command ConnectYukarinetteCommand { get; } = new();
	public Command ConnectYukaConeCommand { get; } = new();

	public Command ExecCommand { get; } = new();


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

	private RecognizeExeArgument? arg = default;
	private global::System.Windows.Forms.PropertyGrid? propertyGrid;
	private readonly IDialogService dialogService;

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


		this.ExecCommand.Invoker = (_) => {
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
		};

		this.CreateBatchommand.Invoker = async (_) => {
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
		};
		this.MicTestCommand.Invoker = (_) => {
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
		};
		this.AmbientTestCommand.Invoker = (_) => {
			System.Diagnostics.Debug.Assert(this.arg is not null);
			using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
				FileName = this.arg.RecognizeExePath,
				Arguments = string.Format("--test mic_ambient {0}", this.GenExeArguments(this.arg)),
				UseShellExecute = true,
			})) { }
		};
		this.CloseCommand.Invoker = (_) => {
			global::System.Windows.Application.Current?.Shutdown();
		};

		this.ConnectWhisperCommand.Invoker = (_) => {
			System.Diagnostics.Debug.Assert(this.arg is not null);
			this.arg.ArgMethod = "kotoba_whisper";
			this.arg.ArgHpfParamaterV2 = HpfArgGenerater.HpfParamater.強め.ToString();
			this.arg.ArgVadParamaterV2 = "0";
			this.arg.ArgMicRecordMinDuration = 0.8f;
			this.propertyGrid.Refresh();
		};
		this.ConnectGoogleCommand.Invoker = (_) => {
			System.Diagnostics.Debug.Assert(this.arg is not null);
			this.arg.ArgMethod = "google_mix";
			this.arg.ArgGoogleProfanityFilter = true;
			this.arg.ArgHpfParamaterV2 = HpfArgGenerater.HpfParamater.無効.ToString();
			this.arg.ArgVadParamaterV2 = "0";
			this.arg.ArgMicRecordMinDuration = null;
			this.propertyGrid.Refresh();
		};
		this.ConnectYukarinetteCommand.Invoker = (_) => {
			System.Diagnostics.Debug.Assert(this.arg is not null);
			this.arg.ArgOutWithYukarinette = true;
			if(!this.arg.ArgOutYukarinette.HasValue) {
				this.arg.ArgOutYukarinette = 49513;
			}
			this.propertyGrid.Refresh();
		};
		this.ConnectYukaConeCommand.Invoker = (_) => {
			System.Diagnostics.Debug.Assert(this.arg is not null);
			this.arg.ArgOutWithYukacone = true;
			this.propertyGrid.Refresh();
		};
	}

	public async Task OnLoaded(RoutedEventArgs e) {
		this.propertyGrid = ((MainWindow)e.Source).propertyGrid;

		var convDic = new Dictionary<Type, Func<string, object?>>();
		convDic.Add(typeof(string), (x) => x);
		convDic.Add(typeof(bool?), (x) => {
			bool v;
			return bool.TryParse(x, out v) ? (object)v : null;
		});
		convDic.Add(typeof(int?), (x) => {
			int v;
			return int.TryParse(x, out v) ? (object)v : null;
		});
		convDic.Add(typeof(float?), (x) => {
			float v;
			return float.TryParse(x, out v) ? (object)v : null;
		});

		var isVesionUp = false;
		var list = new List<Tuple<string, string>>();
		var ver = default(int);
		try {
			var save = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.CONFIG_FILE));
			foreach(var line in save.Replace("\r\n", "\n").Split('\n')) {
				var c = line.IndexOf(':');
				if(0 < c) {
					var tp = new Tuple<string, string>(line.Substring(0, c), line.Substring(c + 1));
					list.Add(tp);


					if(tp.Item1.ToLower() == "version") {
						if(int.TryParse(tp.Item2, out ver) && ver < RecognizeExeArgument.FormatVersion) {
							isVesionUp = true;
						}
					}
				}
			}
		}
		catch(IOException) { }

		var prop = typeof(RecognizeExeArgument).GetProperties().Where(x => x.CanWrite);
		var pr = typeof(RecognizeExeArgument).GetProperty("RecognizeExePath");
		var exe = list.Where(x => x.Item1 == pr.Name).FirstOrDefault();
		this.arg = RecognizeExeArgumentEx.Init((exe != null) ? exe.Item2 : (string)pr.GetCustomAttribute<DefaultValueAttribute>().Value);
		foreach(var tp in list) {
			var p = prop.Where(x => x.Name == tp.Item1).FirstOrDefault();
			if(p != null) {
				var svattr = p.GetCustomAttribute<SaveAttribute>();
				if((svattr != null) && !svattr.IsRestore) {
					continue;
				}

				Func<string, object> f;
				if(convDic.TryGetValue(p.PropertyType, out f)) {
					var v = f(tp.Item2);
					if(v != null) {
						p.SetValue(this.arg, v);
					}
				}
			}
		}
		this.propertyGrid.SelectedObject = this.arg;
		if(isVesionUp) {
			// マイグレ処理
			if(ver < 2025042700) {
				// 2025/04/27対応
				// --mic_record_min_duration追加処理
				this.arg.ArgMicRecordMinDuration = this.arg.ArgMethod switch {
					"whisper" => 0.8f,
					"faster_whisper" => 0.8f,
					"kotoba_whisper" => 0.8f,
					_ => null
				};
			}
			await Messenger.RaiseAsync(new ConfirmationMessage(
				"設定が更新されています。内容を確認してね",
				"ゆーかねすぴれこ",
				ConfirmationKey) {

				Button = MessageBoxButton.OK,
				Image = MessageBoxImage.Information,
			});
		}
		if(!IsValidExePath(this.arg)) {
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
		this.SaveConfig(this.arg);
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


	private bool IsValidExePath(RecognizeExeArgument argument) {
		try {
			var path = global::System.IO.Path.GetFullPath(argument.RecognizeExePath);
			if(path.ToLower() != arg.RecognizeExePath.ToLower()) {
				// exeは相対パス
				// 作業ディレクトリがexeのディレクトリとは限らないので作り直す
				path = global::System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arg.RecognizeExePath);
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
		return araguments.ToString();
	}

	private void SaveConfig(RecognizeExeArgument argument) {
		try {
			var save = new StringBuilder();
			var dict = new Dictionary<string, string>();
			foreach(var p in argument.GetType().GetProperties()) {
				var dfattr = p.GetCustomAttribute<DefaultValueAttribute>();
				if(dfattr != null) {
					var pv = p.GetValue(this.arg, null);
					var dv = dfattr.Value;
					if((pv != null) && !pv.Equals(dv)) {
						var svattr = p.GetCustomAttribute<SaveAttribute>();
						if((svattr != null) && !svattr.IsSave) {
							continue;
						}
						dict.Add(p.Name, pv.ToString());
						continue;
					}
					//if((dv != null) && !dv.Equals(pv)) {
					//	save.Append(p.Name).Append(":").AppendLine(pv.ToString());
					//	continue;
					//}
				}
			}

			foreach(var p in argument.GetType().GetProperties()) {
				var svattr = p.GetCustomAttribute<SaveAttribute>();
				var pv = p.GetValue(this.arg, null);
				if((pv != null) && (svattr != null) && svattr.IsSave) {
					if(!dict.ContainsKey(p.Name)) {
						dict.Add(p.Name, pv.ToString());
						continue;
					}
				}
			}

			foreach(var key in dict.Keys) {
				save.Append(key).Append(":").AppendLine(dict[key]);
			}
			File.WriteAllText(global::System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.CONFIG_FILE), save.ToString());
		}
		catch(IOException) { }
	}

}