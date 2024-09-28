using Newtonsoft.Json;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Haru.Kei {
	internal class ViewModel {
		public ReactivePropertySlim<string> StatusText { get; } = new(initialValue: "");

		public ReactivePropertySlim<int> Phase { get; } = new(initialValue: 0);
		public ReadOnlyReactivePropertySlim<Visibility> Phase0 { get; }
		public ReadOnlyReactivePropertySlim<Visibility> Phase1 { get; }
		public ReadOnlyReactivePropertySlim<Visibility> Phase2 { get; }
		public ReadOnlyReactivePropertySlim<Visibility> Phase3 { get; }

		public ReactivePropertySlim<double> DownloadIndicator { get; } = new(initialValue: 0d);

		public ViewModel() {
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
			this.Phase3 = Phase.Select(x => x switch {
				2 => Visibility.Visible,
				_ => Visibility.Collapsed,
			}).ToReadOnlyReactivePropertySlim();
			this.StatusText.Value = "ゆーかねすぴれこをダウンロードします。次へを押してください。処理中に終了する場合はウインドウを閉じてください。";
		}

		public IObservable<string> DoPhase1() {
			// テンポラリディレクトリを構成する
			this.Phase.Value = 1;
			return Observable.Create<string>(async o => {
				var target = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Const.InstallDirectory);
				this.StatusText.Value = "ダウンローダは作業領域を構成中…";
				if(Directory.Exists(target)) {
					try {
						Directory.Delete(target, true);

					}
					catch(Exception e) {
						o.OnNext("古い作業フォルダの作成に失敗しました。不要なアプリケーションを閉じてもう一度実施してみてください。");
					}
				}

				try {
					Directory.CreateDirectory(target);
				}
				catch(Exception e) {
					o.OnNext("作業フォルダの作成に失敗しました。不要なアプリケーションを閉じてもう一度実施してみてください。");
				}
				o.OnNext("");
				o.OnCompleted();
				return System.Reactive.Disposables.Disposable.Empty;
			});
		}
		public IObservable<string> DoPhase2() {
			this.Phase.Value = 2;
			return Observable.Create<string>(async o => {
				using System.Net.Http.HttpClient clinet = new();
				this.StatusText.Value = "フィル情報をダウンロード中…";

				//https://api.github.com/repos/VOICEVOX/voicevox/releases/tags/0.20.0)
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
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory,	Const.FileName),
					FileMode.OpenOrCreate,
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
				o.OnNext("");
				o.OnCompleted();
				return System.Reactive.Disposables.Disposable.Empty;
			});
		}

		public IObservable<string> DoPhase3() {
			// TODO: 解凍処理を入れる
			this.Phase.Value = 3;
			return Observable.Create<string>(async o => {
				this.StatusText.Value = $"ここから先は未実装";
				return System.Reactive.Disposables.Disposable.Empty;
			});

		}
	}
}
