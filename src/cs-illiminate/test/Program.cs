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
using Newtonsoft.Json;

namespace Haru.Kei;

class Logger {
	public static Logger Current { get; } = new();

	public void Log(object s) {
		var pid = Process.GetCurrentProcess().Id;
		var tid = Thread.CurrentThread.ManagedThreadId;
		var time = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
		Console.WriteLine($"{time}[{pid}][{tid}]{s}");
	}
}

class CommandOptions {
	public enum VoiceClientType { 
		voiceroid2,
		voicepeak,
		aivoice2,
	}

	[Option("master", Required = false, HelpText = "-")]
	public int Master { get; set; }
	[Option("port", Required = true, HelpText = "-")]
	public int Port { get; set; }
	[Option("voice", Required = true, HelpText = " - ")]
	public VoiceClientType Voice { get; set; }
	[Option("client", Required = true, HelpText = "-")]
	public string Client { get; set; }
	[Option("launch", Required = false, HelpText = "-")]
	public bool Launch { get; set; }
}

public record class RecognitionObject {
#pragma warning disable CS8618
	[JsonProperty("transcript", Required = Required.Always)]
	public string Transcript { get; init; }
#pragma warning restore

	[JsonProperty("finish", Required = Required.Always)]
	public bool IsFinish { get; init; }
}

class Program {
	class MessageForm : Form {
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern nint PostMessage(nint hwnd, int msg, nint wParam, nint lParam);
		private System.Reactive.Concurrency.EventLoopScheduler SpeechScheduler { get; } = new System.Reactive.Concurrency.EventLoopScheduler();

		private const int WM_APP = 0x8000;
		private const int YACM_STARTUP = WM_APP + 1;
		private const int YACM_SHUTDOWN = WM_APP + 2;
		private const int YACM_GETSTATE = WM_APP + 4;
		private const int YACM_CAPTURE = WM_APP + 5;

		private const int YACSTATE_NONE = 0;
		private const int YACSTATE_FAIL = 1;
		private const int YACSTATE_INITILIZED = 3;

		private CommandOptions opt;
		private int targetProcess;
		private bool isInit = false;
		private VoiceLink.IVoiceClient client;
		private ApplicationCapture? capture;
		private CancellationTokenSource cancellationSource;
		private IDisposable wsSubscriber;
		private IDisposable? masterMoniter = null;

		private AutoResetEvent autoResetEvent = new(false);

		public MessageForm(CommandOptions opt) {
			this.FormBorderStyle = FormBorderStyle.None;
			this.Opacity = 0;
			this.ControlBox = false;
			this.ShowInTaskbar = false;

			this.opt = opt;
			this.client = opt.Voice switch {
				CommandOptions.VoiceClientType.voiceroid2 => new VoiceLink.Clients.VoiceRoid2(),
				CommandOptions.VoiceClientType.aivoice2 => new VoiceLink.Clients.AiVoice2(),
				CommandOptions.VoiceClientType.voicepeak => new VoiceLink.Clients.VoicePeak(),
				_ => throw new NotImplementedException($"不正な合成音声{opt.Voice}"),
			};
			this.client.StartClient(this.opt.Client, this.opt.Launch);
			this.targetProcess = this.client.ProcessId;
			cancellationSource = new CancellationTokenSource();

			// ウェブソケット
			wsSubscriber = Observable.Create<RecognitionObject>(oo => {
				try {
					using var server = new WebSocketServer($"ws://127.0.0.1:{this.opt.Port}");

					IWebSocketConnection? soc = null;
					server.Start(socket => {
						soc = socket;
						socket.OnMessage = message => {
							try {
								Logger.Current.Log($"メッセージ受信=>{message}");
								if (message == "ping") {
									var _ = socket.Send("pong");
								} else {
									var json = Newtonsoft.Json.JsonConvert.DeserializeObject<RecognitionObject?>(message);
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
					try {
						Logger.Current.Log($"合成音声呼び出し開始:{x.Transcript}");
						this.autoResetEvent.Reset();
						this.client.BeginSpeech(x.Transcript);
						if ((this.capture == null && this.targetProcess != 0)
							|| (this.targetProcess != this.client.ProcessId)) {
							Logger.Current.Log($"！！合成音声クライアントの再起動が確認されました");
							this.targetProcess = this.client.ProcessId;
							this.capture = await ApplicationCapture.Get(this.targetProcess);
						}
						if (this.capture != null) {
							this.capture.Start();
							_ = Task.Run(() => {
								this.capture.Wait();
								this.capture.Stop();
								this.autoResetEvent.Set();
							});
							if (this.client.Speech(x.Transcript)) {
								this.autoResetEvent.WaitOne();
								this.client.EndSpeech(x.Transcript);
							} else {
								Logger.Current.Log($"！！合成音声クライアントの呼び出しに失敗");
							}
						} else {
							Logger.Current.Log($"！！音声キャプチャの準備ができていません。スキップします。");
						}
					}
					finally {
						Logger.Current.Log($"合成音声呼び出し終了:{x.Transcript}");
					}
				});

			// 親プロセスの死活監視
			if (opt.Master != 0) {
				try {
					var p = Process.GetProcessById(opt.Master);
					masterMoniter = Observable.Interval(TimeSpan.FromMilliseconds(100))
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
			}
		}

		protected override async void OnLoad(EventArgs e) {
			base.OnLoad(e);
			ApplicationCapture.UiInitilize();
			//PostMessage(reciveWnd, YACM_STARTUP, 0, this.Handle);
			var _ = Task.Run(async () => {
				this.capture = await ApplicationCapture.Get(this.targetProcess);
				this.isInit = true;
			});
			await Task.Delay(500);
			this.Hide();
		}

		protected override void OnFormClosed(FormClosedEventArgs e) {
			base.OnFormClosed(e);
			cancellationSource.Cancel();
			Application.Exit();
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateFileMapping(nint hFile, nint lpFileMappingAttributes, int flProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);
	[DllImport("kernel32.dll")]
	private static extern nint MapViewOfFile(nint hFileMappingObject, int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, nint dwNumberOfBytesToMap);
	[DllImport("kernel32.dll")]
	private static extern nint UnmapViewOfFile(nint hFileMappingObject);



	[STAThread]
	static int Main(string[] args) {
		var result = Parser.Default.ParseArguments<CommandOptions>(args);
		if (result.Tag == ParserResultType.Parsed) {
			var parsed = (Parsed<CommandOptions>)result;
			if (parsed != null) {
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
			Console.WriteLine("コマンドが不正");
		}

#if DEBUG
		ApplicationConfiguration.Initialize();
		Application.Run(new MessageForm(new CommandOptions() {
			Master = 129036,
			Port = 49514,
			Voice = CommandOptions.VoiceClientType.voiceroid2,
			Client = @"C:\Program Files (x86)\AHS\VOICEROID2\VoiceroidEditor.exe",
			Launch = true
		}));
#endif
		return 1;
	}
}
