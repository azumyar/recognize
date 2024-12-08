using Newtonsoft.Json;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Haru.Kei;
internal class ViewModel {
	private readonly string logPath = Path.Combine(AppContext.BaseDirectory, "setup.log");

	public ReactivePropertySlim<string> StatusText { get; } = new(initialValue: "");

	public ReactivePropertySlim<int> Phase { get; } = new(initialValue: 0);
	public ReadOnlyReactivePropertySlim<Visibility> Phase0 { get; }
	public ReadOnlyReactivePropertySlim<Visibility> Phase1 { get; }
	public ReadOnlyReactivePropertySlim<Visibility> Phase2 { get; }
	public ReadOnlyReactivePropertySlim<Visibility> Phase3_4_5_6 { get; }
	public ReadOnlyReactivePropertySlim<Visibility> Phase7 { get; }

	public ReactivePropertySlim<double> DownloadIndicator { get; } = new(initialValue: 0d);

	public ViewModel() {
		UIDispatcherScheduler.Initialize();
		this.Phase0 = Phase.Select(x => x switch {
			0 => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.Phase1 = Phase.Select(x => x switch {
			1 => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.Phase2 = Phase.Select(x => x switch {
			2 => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.Phase3_4_5_6 = Phase.Select(x => x switch {
			3 => Visibility.Visible,
			4 => Visibility.Visible,
			5 => Visibility.Visible,
			6 => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.Phase7 = Phase.Select(x => x switch {
			7 => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.StatusText.Value = "ゆーかねすぴれこをダウンロードします。次へを押してください。処理中に終了する場合はウインドウを閉じてください。";
		File.WriteAllText(logPath, $"start setup tag:{Const.Tag}\r\n");
	}

	public IObservable<string> DoPhase1() {
		// テンポラリディレクトリを構成する
		this.Phase.Value = 1;
		return Observable.Create<string>(o => {
			try {
				File.AppendAllText(logPath, "start phase1\r\n");

				var target = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Const.InstallDirectory);
				this.StatusText.Value = "ダウンローダは作業領域を構成中…";
				if(Directory.Exists(target)) {
					try {
						Directory.Delete(target, true);

					}
					catch(Exception e) {
						o.OnNext("古い作業フォルダの作成に失敗しました。不要なアプリケーションを閉じてもう一度実施してみてください。");
						goto end;
					}
				}

				try {
					Directory.CreateDirectory(target);
				}
				catch(Exception e) {
					o.OnNext("作業フォルダの作成に失敗しました。不要なアプリケーションを閉じてもう一度実施してみてください。");
					goto end;
				}
				File.AppendAllText(logPath, "done.\r\n");
				o.OnNext("");
				o.OnCompleted();
			}
			catch(Exception ex) {
				o.OnNext($"不明なエラー:{ex.GetType()}");
			}
		end:
			return System.Reactive.Disposables.Disposable.Empty;
		});
	}
	public IObservable<string> DoPhase2() {
		this.Phase.Value = 2;
		return Observable.Create<string>(async o => {
			try {
				File.AppendAllText(logPath, "start phase2\r\n");

				using System.Net.Http.HttpClient clinet = new();
				this.StatusText.Value = "ファイル情報をダウンロード中…";
				string json;
				{
					var m = new System.Net.Http.HttpRequestMessage(
						System.Net.Http.HttpMethod.Get,
						$"https://api.github.com/repos/{Const.Account}/{Const.Repository}/releases/tags/{Const.Tag}");
					m.Headers.Add("Accept", "application/vnd.github.v3+json");
					m.Headers.Add("User-Agent", "request");
					var res = await clinet.SendAsync(m);
					json = await res.Content.ReadAsStringAsync();
				}
				var response = JsonConvert.DeserializeObject<GitHubApiResponse>(json);
				var fileNames = Enumerable
					.Range(0, Const.SplitCount)
					.Select(x => $"{Const.FileName}.{x}")
					.ToArray();

				var total = response.Assets
					.Where(x => fileNames.Contains(x.Name))
					.Select(x => x.Size)
					.Sum();
				var indicator = 0d;
				using var ouputStream = new FileStream(
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Const.InstallDirectory, Const.FileName),
					FileMode.Create,
					FileAccess.Write);
				foreach(var download in response.Assets
					.Where(x => fileNames.Contains(x.Name))
					.Select(x => x.BrowserDownloadUrl)) {

					this.StatusText.Value = $"{download}をダウンロード中…";
					var m = new System.Net.Http.HttpRequestMessage(
						System.Net.Http.HttpMethod.Get,
						download);
					m.Headers.Add("User-Agent", "request");
					var res = await clinet.SendAsync(m, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
					using var inputStream = await res.Content.ReadAsStreamAsync();
					var bytes = new byte[1024 * 10];
					while(true) {
						var len = await inputStream.ReadAsync(bytes, 0, bytes.Length);
						if(len == 0) {
							break;
						}
						await ouputStream.WriteAsync(bytes, 0, len);
						indicator += len;
						this.DownloadIndicator.Value = indicator / total;
					}
				}
				await ouputStream.FlushAsync();
				File.AppendAllText(logPath, "done.\r\n");
				o.OnNext("");
				o.OnCompleted();
			}
			catch(Exception e) when((e is HttpRequestException) || (e is TaskCanceledException)) {
				o.OnNext("ダウンロードに失敗しました");
			}
			catch(Exception ex) {
				o.OnNext($"不明なエラー:{ex.GetType()}");
			}
			return System.Reactive.Disposables.Disposable.Empty;
		});
	}

	public IObservable<string> DoPhase3() {
		this.Phase.Value = 3;
		return Observable.Create<string>(async o => {
			try {
				File.AppendAllText(logPath, "start phase3\r\n");

				this.StatusText.Value = $"アーカイブの展開中…";
				var r = await Task.Run(async () => {
					var count = 0;
				start:
					try {
						ZipFile.ExtractToDirectory(
							Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Const.InstallDirectory, Const.FileName),
							Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Const.InstallDirectory));
						return true;
					}
					catch(Exception ex) when(ex is IOException) {
						// 別のプロセス(多分アンチウィルス)が使用していることがあるのでリトライする
						if(count < 5) {
							await Task.Delay(1000);
							count++;
							goto start;
						}
						return false;
					}
				});
				if(r) {
					File.AppendAllText(logPath, "done.\r\n");

					o.OnNext("");
					o.OnCompleted();
				} else {
					o.OnNext("アーカイブの展開に失敗しました");
				}
			}
			catch(Exception ex) {
				o.OnNext($"不明なエラー:{ex.GetType()}");
			}
			return System.Reactive.Disposables.Disposable.Empty;
		}).ObserveOn(UIDispatcherScheduler.Default);
	}

	public IObservable<string> DoPhase4() {
		this.Phase.Value = 4;
		return Observable.Create<string>(o => {
			try {
				File.AppendAllText(logPath, "start phase4\r\n");

				this.StatusText.Value = "古いフォルダを削除中…";
				foreach(var d in Const.RemoveOldDirectories) {
					try {
						var dd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, d);
						if(Directory.Exists(dd)) {
							Directory.Delete(dd, true);
						}
						if(File.Exists(dd)) {
							File.Delete(dd);
						}
						File.AppendAllText(logPath, "done.\r\n");

						o.OnNext("");
						o.OnCompleted();
					}
					catch(Exception e) when(e is IOException) {
						o.OnNext("古いフォルダの削除に失敗しました");
					}
				}
			}
			catch(Exception ex) {
				o.OnNext($"不明なエラー:{ex.GetType()}");
			}
			return System.Reactive.Disposables.Disposable.Empty;
		});
	}

	public IObservable<string> DoPhase5() {
		this.Phase.Value = 5;
		return Observable.Create<string>(async o => {
			try {
				File.AppendAllText(logPath, "start phase5\r\n");

				this.StatusText.Value = "新しいファイルを移動中…";
				try {
					foreach(var d in Directory.EnumerateDirectories(
						Path.Combine(
							AppDomain.CurrentDomain.BaseDirectory,
							Const.InstallDirectory,
							Const.ExtractRootDirectory))) {
						Directory.Move(
							d,
							Path.Combine(
								AppDomain.CurrentDomain.BaseDirectory,
								Path.GetFileName(d)));
					}
					foreach(var f in Directory.EnumerateFiles(
						Path.Combine(
							AppDomain.CurrentDomain.BaseDirectory,
							Const.InstallDirectory,
							Const.ExtractRootDirectory))) {
						File.Move(
							f,
							Path.Combine(
								AppDomain.CurrentDomain.BaseDirectory,
								Path.GetFileName(f)),
							true);
					}
					File.AppendAllText(logPath, "done.\r\n");

					o.OnNext("");
					o.OnCompleted();
				}
				catch(Exception e) when(e is IOException) {
					o.OnNext("新しいファイルの移動に失敗しました");
				}
			}
			catch(Exception ex) {
				o.OnNext($"不明なエラー:{ex.GetType()}");
			}
			return System.Reactive.Disposables.Disposable.Empty;
		});
	}

	public IObservable<string> DoPhase6() {
		this.Phase.Value = 6;
		return Observable.Create<string>(o => {
			try {
				File.AppendAllText(logPath, "start phase6\r\n");

				this.StatusText.Value = "後片付け中…";
				try {
					Directory.Delete(
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Const.InstallDirectory),
						true);
				}
				catch(Exception e) {
					o.OnNext("作業フォルダの削除に失敗しました。セットアップは完了しています。");
					goto end;
				}
				File.AppendAllText(logPath, "done.\r\n");

				o.OnNext("");
				o.OnCompleted();
			}
			catch(Exception ex) {
				o.OnNext($"不明なエラー:{ex.GetType()}");
			}
		end:
			return System.Reactive.Disposables.Disposable.Empty;
		});
	}

	public void DoPhase7() {
		this.Phase.Value = 7;
		this.StatusText.Value = "ゆーかねすぴれこのセットアップを終了しました。ウインドウを閉じてください。";
	}
}