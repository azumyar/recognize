using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows.Automation;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Reactive.Linq;

using CommandLine;
using System.Threading;
using Fleck;
using System.Windows.Threading;

namespace Haru.Kei;
class Program {
	class MessageForm : Form {
		private System.Reactive.Concurrency.EventLoopScheduler SpeechScheduler { get; } = new();
		private Models.CommandOptions opt;
		private int targetProcess;
		private VoiceLink.IVoiceClient client;
		private ApplicationCapture? capture;
		private CancellationTokenSource cancellationSource;
		private AutoResetEvent autoResetEvent = new(false);
		private NotifyIcon notifyIcon = new();
		private IDisposable wsSubscriber;
		private IDisposable? masterMoniter = null;

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern uint ExtractIconEx(string pszFile, uint nIconIndex, out nint phIconLarge, out nint phIconSmall, uint nIcons);

		public MessageForm(Models.CommandOptions opt) {
			this.FormBorderStyle = FormBorderStyle.None;
			this.Opacity = 0;
			this.ControlBox = false;
			this.ShowInTaskbar = false;

			this.opt = opt;
			this.client = this.CreateVoiceClient(opt.Voice);
			this.client.StartClient(this.opt.Client, this.opt.Launch);
			this.targetProcess = this.client.ProcessId;
			cancellationSource = new CancellationTokenSource();

			// ウェブソケット
			wsSubscriber = Observable.Create<Models.RecognitionObject>(oo => {
				try {
					using var server = new WebSocketServer($"ws://127.0.0.1:{this.opt.Port}");

					IWebSocketConnection? soc = null;
					server.Start(socket => {
						soc = socket;
						Logger.Current.Log("WebSocketサーバが起動しました");
						socket.OnMessage = message => {
							try {
								Logger.Current.Log($"メッセージ受信=>{message}");
								if (message == "ping") {
									var _ = socket.Send("pong");
								} else {
									var json = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.RecognitionObject?>(message);
									if (json != null) {
										oo.OnNext(json);
									}
								}
							}
							catch (Exception e) {
								Logger.Current.Log("WebSocketで予期しない例外");
								Logger.Current.Log(e);
							}
						};
					});
					cancellationSource.Token.WaitHandle.WaitOne();
					Logger.Current.Log("WebSocketサーバをシャットダウンします。");
					try {
						soc?.Send("exit").Wait();
					}
					catch (AggregateException) { }
				}
				finally { }
				return System.Reactive.Disposables.Disposable.Empty;
			}) .ObserveOn(this.SpeechScheduler)
				.SubscribeOn(System.Reactive.Concurrency.TaskPoolScheduler.Default)
				.Subscribe(async x => {
					if (SynchronizationContext.Current == null) {
						SynchronizationContext.SetSynchronizationContext(
							new DispatcherSynchronizationContext());
					}

					await RunVoiceRoid(x);
				});

			// 親プロセスの死活監視
			if (opt.Master != 0) {
				masterMoniter = RunLifeMoniter(opt.Master);
			}
		}

		private VoiceLink.IVoiceClient CreateVoiceClient(Models.CommandOptions.VoiceClientType voice) {
			return voice switch {
				Models.CommandOptions.VoiceClientType.voiceroid => new VoiceLink.Clients.VoiceRoid(),
				Models.CommandOptions.VoiceClientType.voiceroid2 => new VoiceLink.Clients.VoiceRoid2(),
				Models.CommandOptions.VoiceClientType.voicepeak => new VoiceLink.Clients.VoicePeak(),
				Models.CommandOptions.VoiceClientType.aivoice => new VoiceLink.Clients.AiVoice(),
				Models.CommandOptions.VoiceClientType.aivoice2 => new VoiceLink.Clients.AiVoice2(),
				_ => throw new NotImplementedException($"不正な合成音声{opt.Voice}"),
			};
		}

		private async Task RunVoiceRoid(Models.RecognitionObject x) {
			try {
				Logger.Current.Log($"合成音声呼び出し開始:{x.Transcript}");
				this.autoResetEvent.Reset();
				try {
					this.client.BeginSpeech(x.Transcript);
				}
				catch (VoiceLink.VoiceLinkException e) {
					Logger.Current.Log($"！！合成音声クライアントの読み上げ準備に失敗しました");
					Logger.Current.Log($"{e}");
					return;
				}

				try {
					if ((this.capture == null && this.targetProcess != 0)
						|| (this.targetProcess != this.client.ProcessId)) {
						Logger.Current.Log($"！！合成音声クライアントの再起動が確認されました");
						this.targetProcess = this.client.ProcessId;
						this.capture = await ApplicationCapture.Get(this.targetProcess, this.opt.CapturePauseSec);
					}
					if (this.capture != null) {
						this.capture.Start();
						_ = Task.Run(() => {
							this.capture.Wait();
							this.capture.Stop();
							this.autoResetEvent.Set();
						});
						this.client.Speech(x.Transcript);
						this.autoResetEvent.WaitOne();
					} else {
						Logger.Current.Log($"！！音声キャプチャの準備ができていません。スキップします。");
					}
				}
				catch (VoiceLink.VoiceLinkException e) {
					Logger.Current.Log($"！！合成音声クライアントの呼び出しに失敗");
					Logger.Current.Log($"{e}");
				}
				finally {
					try {
						this.capture?.Stop();
						this.client.EndSpeech(x.Transcript);
					}
					catch (VoiceLink.VoiceLinkException) { }
				}

			}
			finally {
				Logger.Current.Log($"合成音声呼び出し終了:{x.Transcript}");
			}
		}

		private IDisposable? RunLifeMoniter(int targetPid) {
			try {
				var p = Process.GetProcessById(opt.Master);
				return Observable.Interval(TimeSpan.FromMilliseconds(100))
					.Subscribe(_ => {
						try {
							if (p.HasExited) {
								Logger.Current.Log("親プロセスの終了を確認しました");
								Application.Exit();
								masterMoniter?.Dispose();
							}
						}
						catch { }
					});
			}
			catch { }
			return default;
		}

		protected override async void OnLoad(EventArgs e) {
			base.OnLoad(e);

			var menu = new ContextMenuStrip();
			// 表示メニュー項目の追加
			var mItem = new ToolStripMenuItem("終了");
			mItem.Click += (_, _) => { Application.Exit(); };
			menu.Items.Add(mItem);

			ExtractIconEx(
				Process.GetCurrentProcess().MainModule?.FileName ?? "",
				0,
				out var hIcon,
				out var hIconSmall,
				1);
			this.notifyIcon.Text = "ゆーかねすぴれこ/illuminate";
			this.notifyIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);
			this.notifyIcon.ContextMenuStrip = menu;
			this.notifyIcon.Visible = true;

			ApplicationCapture.UiInitilize();
			var _ = Task.Run(async () => {
				this.capture = await ApplicationCapture.Get(this.targetProcess);
			});
			await Task.Delay(500);
			this.Hide();
		}

		protected override void OnFormClosed(FormClosedEventArgs e) {
			base.OnFormClosed(e);
			this.cancellationSource.Cancel();
			this.notifyIcon.Dispose();
			this.wsSubscriber.Dispose();
			this.masterMoniter?.Dispose();
			Application.Exit();
		}
	}

	[STAThread]
	static int Main(string[] args) {
		/* ボイロ各種パスメモ
		const string VoiceRoidExKiritan = @"C:\Program Files (x86)\AHS\VOICEROID+\KiritanEX\VOICEROID.exe";
		const string VoiceRoid2 = @"C:\Program Files (x86)\AHS\VOICEROID2\VoiceroidEditor.exe";
		const string VoicePeak = @"C:\Program Files\VOICEPEAK\voicepeak.exe";
		const string AiVoice = @"C:\Program Files\AI\AIVoice\AIVoiceEditor\AIVoiceEditor.exe";
		const string AiVoice2 = @"C:\Program Files\AI\AIVoice2\AIVoice2Editor\aivoice.exe";
		*/

		var result = Parser.Default.ParseArguments<Models.CommandOptions>(args);
		if (result.Tag == ParserResultType.Parsed) {
			var parsed = (Parsed<Models.CommandOptions>)result;
			if (parsed != null) {
				AppDomain.CurrentDomain.UnhandledException += (_, e) => {
					Logger.Current.Log("致命的なエラー");
					Logger.Current.Log(e.ExceptionObject);
				};

				ApplicationConfiguration.Initialize();
				Application.Run(new MessageForm(parsed.Value));
				return 0;
			}
		} else {
			/*
			// パース失敗時
			var notParsed = (NotParsed<Options>)result;
			// 処理
			*/
		}
		return 1;
	}
}
