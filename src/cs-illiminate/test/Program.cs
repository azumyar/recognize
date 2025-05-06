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
using static Haru.Kei.CommandOptions;
using Newtonsoft.Json;

namespace Haru.Kei;

class Logger {

	public static void Log(object s) {
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
				_ => throw new NotImplementedException($"不正な合成音声{opt.Voice}"),
			};
			this.client.StartClient(this.opt.Client, this.opt.Launch);
			this.targetProcess = this.client.ProcessId;
			cancellationSource = new CancellationTokenSource();

			wsSubscriber = Observable.Create<RecognitionObject>(oo => {
				try {
					using var server = new WebSocketServer($"ws://127.0.0.1:{this.opt.Port}");

					IWebSocketConnection? soc = null;
					server.Start(socket => {
						soc = socket;
						socket.OnMessage = message => {
							try {
								Logger.Log($"メッセージ受信=>{message}");
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
								Logger.Log("WebSocketで予期しない例外");
								Logger.Log(e);
							}
						};
					});
					cancellationSource.Token.WaitHandle.WaitOne();
					Logger.Log("WebSocketサーバをシャットダウンします。");
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
						Logger.Log($"合成音声呼び出し開始:{x.Transcript}");
						this.autoResetEvent.Reset();
						this.client.BeginSpeech(x.Transcript);
						if (this.targetProcess != this.client.ProcessId) {
							Logger.Log($"！！合成音声クライアントの再起動が確認されました");
							this.targetProcess = this.client.ProcessId;
							this.capture = await ApplicationCapture.Get(this.targetProcess);
						}
						if (this.capture != null) {
							this.capture.Start();
							_ = Task.Run(() => {
								this.capture.Wait();
								this.capture.Stop();
								//PostMessage(reciveWnd, YACM_CAPTURE, index, 0);
								this.autoResetEvent.Set();
							});
							if (this.client.Speech(x.Transcript)) {
								this.autoResetEvent.WaitOne();
								this.client.EndSpeech(x.Transcript);
							} else {
								Logger.Log($"！！合成音声クライアントの呼び出しに失敗");
							}
						} else {
							Logger.Log($"！！音声キャプチャの準備ができていません。スキップします。");
						}
					}
					finally {
						Logger.Log($"合成音声呼び出し終了:{x.Transcript}");
					}
				});
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
			Application.Exit();
		}
	}
	private const string MapNameCapture = "yarukizero-net-yukarinette.audio-capture";
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateFileMapping(nint hFile, nint lpFileMappingAttributes, int flProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);
	[DllImport("kernel32.dll")]
	private static extern nint MapViewOfFile(nint hFileMappingObject, int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, nint dwNumberOfBytesToMap);
	[DllImport("kernel32.dll")]
	private static extern nint UnmapViewOfFile(nint hFileMappingObject);

	private const int PAGE_READWRITE = 0x04;
	private const int FILE_MAP_READ = 0x00000004;
	private const int ERROR_ALREADY_EXISTS = 183;


	[STAThread]
	static int Main(string[] args) {
		/*
		var s = Console.ReadLine();
		if (string.IsNullOrEmpty(s)) {
			return;
		}
		*/

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
			Port = 49514,
			Voice = VoiceClientType.voiceroid2,
			Client = @"C:\Program Files (x86)\AHS\VOICEROID2\VoiceroidEditor.exe",
			Launch = true
		}));
#endif
		return 1;

		/*

		VoiceLink.IVoiceClient client = new VoiceLink.Clients.VoiceRoid2();
		/*
		ApplicationConfiguration.Initialize();
		Application.Run(new MessageForm());
		
		client.StartClient(
			//targetExe: @"C:\Program Files\AI\AIVoice2\AIVoice2Editor\aivoice.exe",
			targetExe: @"C:\Program Files (x86)\AHS\VOICEROID2\VoiceroidEditor.exe",
			isLaunch: true
			);
		client.BeginSpeech(s);
		client.Speech(s);




		var path = @"C:\Program Files\VOICEPEAK\voicepeak.exe".ToLower();
		*/
	}


    static
	AutomationElement? Get(string targetProcessPath) {
		foreach (var p in Process.GetProcesses()) {
			try {
				if (p.MainModule?.FileName?.ToLower() == targetProcessPath) {
                    /*
					var guid = new Guid("{618736e0-3c3d-11cf-810c-00aa00389b71}");
					OleApi.AccessibleObjectFromWindow(
						p.MainWindowHandle,
						0,
						ref guid,
						out var obj);
					if (obj is Accessibility.IAccessible acc) {
						return acc;
					}
                    */
                    return AutomationElement.FromHandle(p.MainWindowHandle);
				}
			}
			catch (Exception e) when (
				(e is Win32Exception)
				|| (e is InvalidOperationException)) { }
		}
        return null;
	}
}

//namespace Haru.Kei.Interop;

public static class OleApi {
	[DllImport("oleacc.dll")]
	public static extern uint AccessibleObjectFromWindow(
        nint hwnd,
        int dwObjectID,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)][Out] out object? ppvObject);

	// Token: 0x0600005C RID: 92
	[DllImport("oleacc.dll")]
	public static extern uint AccessibleChildren(
        Accessibility.IAccessible paccContainer,
        int iChildStart,
        int cChildren,
        [In][Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren,
        out int pcObtained);
}

[StructLayout(LayoutKind.Sequential)]
public struct VARIANT {
	public ushort vt;
	public ushort r0;
	public ushort r1;
	public ushort r2;
	public long ptr0;
	public long ptr1;
}

[Guid("618736E0-3C3D-11CF-810C-00AA00389B71")]
[ComImport]
public interface IAccessible {
    uint get_accParent(
        [Out] out nint ppdispParent
        );
    uint get_accChildCount(
		[Out] out int pcountChildren
        );
	uint get_accChild(
        [In] VARIANT varChild,
        [Out] out nint ppdispChild
		);

    //[return: MarshalAs(UnmanagedType.BStr)]
	uint get_accName(
		[In] VARIANT varChild,
		[Out]/*[MarshalAs(UnmanagedType.BStr)]*/out nint pszName
		);

	uint get_accValue(
		[In] VARIANT varChild,
		[Out]/* [MarshalAs(UnmanagedType.BStr)] */ out nint pszValue
        );
	uint get_accDescription(
		[In] VARIANT varChild,
		[Out]/* [MarshalAs(UnmanagedType.BStr)] */ out nint pszDescription
		);
	uint get_accRole(
		[In] VARIANT varChild,
		[Out] out VARIANT pvarRole
		);
	uint get_accState(
		[In] VARIANT varChild,
		[Out] out VARIANT pvarState
		);
	uint get_accHelp(
		[In] VARIANT varChild,
		[Out]/* [MarshalAs(UnmanagedType.BStr)] */ out nint pszHelp
		);
	uint get_accHelpTopic(
		[Out]/* [MarshalAs(UnmanagedType.BStr)] */ out nint pszHelpFile,
		[Optional][In] VARIANT varChild,
		[Out] out int pidTopic
		);
	uint get_accKeyboardShortcut(
		[In] VARIANT varChild,
		[Out]/* [MarshalAs(UnmanagedType.BStr)] */ out nint pszKeyboardShortcut
		);
	uint get_accFocus(
		[Out] out VARIANT pvarChild
		);
	uint get_accSelection(
		[Out] out VARIANT pvarChildren
		);
	uint get_accDefaultAction(
		[Optional][In] VARIANT varChild,
		[Out]/* [MarshalAs(UnmanagedType.BStr)] */ out nint pszDefaultAction);
	uint accSelect(
		[In] int flagsSelect,
		[Optional][In] VARIANT varChild
		);
	uint accLocation(
        [Out] out int pxLeft,
        [Out] out int pyTop,
        [Out] out int pcxWidth,
        [Out] out int pcyHeight,
        [Optional][In] VARIANT varChild
		);
	uint accNavigate(
        [In] int navDir,
        [Optional][In] VARIANT varStart,
        [Out] out VARIANT pvarEndUpAt
		);
	uint accHitTest(
        [In] int xLeft,
        [In] int yTop,
        [Out] out VARIANT pvarChild
		);
	uint accDoDefaultAction(
        [Optional][In] VARIANT varChild
		);
	uint put_accName(
        [Optional][In] VARIANT varChild,
        [In][MarshalAs(UnmanagedType.BStr)] string szName
		);
	uint put_accValue(
		[Optional][In] VARIANT varChild,
		[In][MarshalAs(UnmanagedType.BStr)] string szValue
	);
}
#if false
    MIDL_INTERFACE("618736e0-3c3d-11cf-810c-00aa00389b71")
    IAccessible : public IDispatch
    {
    public:
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accParent( 
            /* [retval][out] */ __RPC__deref_out_opt IDispatch **ppdispParent) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accChildCount( 
            /* [retval][out] */ __RPC__out long *pcountChildren) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accChild( 
            /* [in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__deref_out_opt IDispatch **ppdispChild) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accName( 
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__deref_out_opt BSTR *pszName) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accValue( 
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__deref_out_opt BSTR *pszValue) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accDescription( 
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__deref_out_opt BSTR *pszDescription) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accRole( 
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__out VARIANT *pvarRole) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accState( 
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__out VARIANT *pvarState) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accHelp( 
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__deref_out_opt BSTR *pszHelp) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accHelpTopic( 
            /* [out] */ __RPC__deref_out_opt BSTR *pszHelpFile,
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__out long *pidTopic) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accKeyboardShortcut( 
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__deref_out_opt BSTR *pszKeyboardShortcut) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accFocus( 
            /* [retval][out] */ __RPC__out VARIANT *pvarChild) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accSelection( 
            /* [retval][out] */ __RPC__out VARIANT *pvarChildren) = 0;
        
        virtual /* [id][propget][hidden] */ HRESULT STDMETHODCALLTYPE get_accDefaultAction( 
            /* [optional][in] */ VARIANT varChild,
            /* [retval][out] */ __RPC__deref_out_opt BSTR *pszDefaultAction) = 0;
        
        virtual /* [id][hidden] */ HRESULT STDMETHODCALLTYPE accSelect( 
            /* [in] */ long flagsSelect,
            /* [optional][in] */ VARIANT varChild) = 0;
        
        virtual /* [id][hidden] */ HRESULT STDMETHODCALLTYPE accLocation( 
            /* [out] */ __RPC__out long *pxLeft,
            /* [out] */ __RPC__out long *pyTop,
            /* [out] */ __RPC__out long *pcxWidth,
            /* [out] */ __RPC__out long *pcyHeight,
            /* [optional][in] */ VARIANT varChild) = 0;
        
        virtual /* [id][hidden] */ HRESULT STDMETHODCALLTYPE accNavigate( 
            /* [in] */ long navDir,
            /* [optional][in] */ VARIANT varStart,
            /* [retval][out] */ __RPC__out VARIANT *pvarEndUpAt) = 0;
        
        virtual /* [id][hidden] */ HRESULT STDMETHODCALLTYPE accHitTest( 
            /* [in] */ long xLeft,
            /* [in] */ long yTop,
            /* [retval][out] */ __RPC__out VARIANT *pvarChild) = 0;
        
        virtual /* [id][hidden] */ HRESULT STDMETHODCALLTYPE accDoDefaultAction( 
            /* [optional][in] */ VARIANT varChild) = 0;
        
        virtual /* [id][propput][hidden] */ HRESULT STDMETHODCALLTYPE put_accName( 
            /* [optional][in] */ VARIANT varChild,
            /* [in] */ __RPC__in BSTR szName) = 0;
        
        virtual /* [id][propput][hidden] */ HRESULT STDMETHODCALLTYPE put_accValue( 
            /* [optional][in] */ VARIANT varChild,
            /* [in] */ __RPC__in BSTR szValue) = 0;
        
    };
#endif
