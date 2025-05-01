using Haru.Kei.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
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
using System.Security;
using System.Windows.Interop;
using System.Diagnostics;

namespace Haru.Kei.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
	public class ViewModel : INotifyPropertyChanged {
		public class Command : ICommand {
			public Action<object?>? Invoker { get; set; } = default;

			public event EventHandler? CanExecuteChanged;
			public bool CanExecute(object? parameter) => true;
			public void Execute(object? parameter) => this.Invoker?.Invoke(parameter);
		}

		private RecognizeExeArgument? arg = default;
		private readonly Window owner;
		private readonly global::System.Windows.Forms.PropertyGrid propertyGrid;

		private readonly string CONFIG_FILE = "frontend.conf";
		private readonly string BAT_FILE = "custom-recognize.bat";
		private readonly string TEMP_BAT = global::System.IO.Path.Combine(
			global::System.IO.Path.GetTempPath(),
			string.Format("recognize-gui-{0}.bat", Guid.NewGuid()));

		public event PropertyChangedEventHandler? PropertyChanged;

		public Command CreateBatchommand { get; } = new();
		public Command MicTestCommand { get; } = new();
		public Command AmbientTestCommand { get; } = new();
		public Command CloseCommand { get; } = new();

		public Command ConnectWhisperCommand { get; } = new();
		public Command ConnectGoogleCommand { get; } = new();
		public Command ConnectYukarinetteCommand { get; } = new();
		public Command ConnectYukaConeCommand { get; } = new(); 
		
		public Command ExecCommand { get; } = new();
		
		public ViewModel(MainWindow @this) {
			this.owner = @this;
			propertyGrid = @this.propertyGrid;

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
					/*
					using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = this.arg.RecognizeExePath,
						Arguments = this.GenExeArguments(this.arg),
						UseShellExecute = true,
					})) { }
					*/
					using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = this.TEMP_BAT,
						WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
						UseShellExecute = true,
					})) { }

				}
				catch(Exception) { }
			}; 
			
			this.CreateBatchommand.Invoker = (_) => {
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
					MessageBox.Show(
						this.owner,
						string.Format("{0}を作成しました！", this.BAT_FILE),
						"成功",
						MessageBoxButton.OK,
						MessageBoxImage.Information);
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
				this.arg.ArgOut = "yukarinette";
				if(!this.arg.ArgOutYukarinette.HasValue) {
					this.arg.ArgOutYukarinette = 49513;
				}
				this.propertyGrid.Refresh();
			};
			this.ConnectYukaConeCommand.Invoker = (_) => {
				System.Diagnostics.Debug.Assert(this.arg is not null);
				this.arg.ArgOut = "yukacone";
				this.propertyGrid.Refresh();
			};
		}

		public void OnLoaded() {
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

				MessageBox.Show(
					this.owner,
					"設定が更新されています。内容を確認してね",
					"ゆーかねすぴれこ",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			if(!IsValidExePath(this.arg)) {
				MessageBox.Show(
					this.owner,
					"パスに不正な文字が含まれます。ゆーかねすぴれこは英数字だけのパスに配置してください。",
					"ゆーかねすぴれこ",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				global::System.Windows.Application.Current?.Shutdown();
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

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint SendMessage(nint hwnd, int msg, nint wParam, nint lParam);
	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern uint ExtractIconEx(string pszFile, uint nIconIndex, out nint phIconLarge, out nint phIconSmall, uint nIcons);
	private const int WM_SETICON = 0x0080;
	private const int ICON_BIG = 1;
	private const int ICON_SMALL = 0;
	private readonly ViewModel vm;

	public MainWindow() {
		InitializeComponent();

		this.vm = new(this);
		this.DataContext = this.vm;
		this.Loaded += (_, _) => {;
			var hwnd = new WindowInteropHelper(this).Handle;
			ExtractIconEx(
				Process.GetCurrentProcess().MainModule?.FileName ?? "",
				0,
				out var hIcon,
				out var hIconSmall,
				1);
			SendMessage(hwnd, WM_SETICON, ICON_BIG, hIcon);
			SendMessage(hwnd, WM_SETICON, ICON_SMALL, hIconSmall);
			vm.OnLoaded();
		};
		this.Closing += (_, _) => vm.OnClosing();
	}
}