using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VoiceLink.Clients;

/// <summary>ボイスロイド系の基底クラス</summary>
/// <typeparam name="TStartObj"></typeparam>
/// <typeparam name="TSpeechObj"></typeparam>
public abstract class VoiceRoid<TStartObj, TSpeechObj> : VoiceClient<TStartObj, TSpeechObj, IAudioCaptireClient>
	where TStartObj : IStartObject
	where TSpeechObj : ISpeechObject {
	class AudioCaptireClient : IAudioCaptireClient {
		private readonly VoiceRoid<TStartObj, TSpeechObj> owner;
		public AudioCaptireClient(VoiceRoid<TStartObj, TSpeechObj> owner) {
			this.owner = owner;
		}

		public int ProcessId {
			get {
				return this.owner.ProcessId;
			}
		}
	}

	public override IAudioCaptireClient ClientParameter { get; }


	public VoiceRoid() {
		this.ClientParameter = new AudioCaptireClient(this);
	}

	protected int ProcessId { get; set; }
}

public class VoiceRoid : VoiceRoid<AudioCaptreStart, NopVoiceObject> {
	private readonly string VoiceRoidEditorClass = "WindowsForms10.Window.8.app.0.378734a";
	private string exe = "";
	private nint hTargetWindow;
	private nint hTextBox;
	private nint hPlayButton;

	public override bool StartClient(bool isLaunch, AudioCaptreStart extra) {
		this.exe = extra.TargetExe;
		return this.Load(this.exe, isLaunch);
	}

	private bool Load(string targetExe, bool isLaunch) {
		this.ProcessId = 0;
		this.hTargetWindow = 0;
		this.hTextBox = 0;
		this.hPlayButton = 0;

		var p = Util.GetProcess(targetExe);
		var h = p?.MainWindowHandle ?? 0;
		if (p == null) {
			/* スプラッシュスクリーンが出るので考えないといけない */
			return false;
		}
		this.ProcessId = p.Id;
		this.hTargetWindow = h;
		return true;
	}

	public override void EndClient() { }


	public override void BeginSpeech(string text, NopVoiceObject extra) {
		if (!Interop.IsWindow(this.hTargetWindow)) {
			this.Load(this.exe, false);
			if (this.hTargetWindow == 0) {
				throw new VoiceLinkException("VoiceRoidが見つかりません");
			}
		}

		static nint getChildFromIndex(nint parent, int index) {
			var r = default(nint);
			var i = 0;
			Interop.EnumChildWindows(parent, (h, l) => {
				if (Interop.GetParent(h) == parent) {
					if (i++ == index) {
						r = h;
						return false;
					}
				}
				return true;
			}, 0);
			return r;
		}

		// ウインドウ階層をたどる
		// まとめて書くとよくわからないので分解する
		var _0 = getChildFromIndex(this.hTargetWindow, 0);
		var _1 = getChildFromIndex(_0, 1);
		var _2 = getChildFromIndex(_1, 0);
		var _3 = getChildFromIndex(_2, 0);
		var _4 = getChildFromIndex(_3, 0);
		var _5 = getChildFromIndex(_4, 0);

		this.hTextBox = getChildFromIndex(getChildFromIndex(_5, 0), 0);
		this.hPlayButton = getChildFromIndex(getChildFromIndex(_5, 1), 0);
		if ((this.hTextBox == 0) || (this.hPlayButton == 0)) {
			throw new VoiceLinkException("読み上げ開始準備に失敗");
		}
	}

	public override void Speech(string text, NopVoiceObject extra) {
		if ((this.hTextBox == 0) || (this.hPlayButton == 0)) {
			throw new VoiceLinkException("");
		}
		Interop.SendMessage(this.hTextBox, Interop.WM_SETTEXT, 0, text);
		Interop.SendMessage(this.hPlayButton, Interop.BM_CLICK, 0, 0);
	}

	public override void EndSpeech(string text, NopVoiceObject extra) {
		if (this.hTextBox == 0) {
			return;
		}

		Interop.SendMessage(this.hTextBox, Interop.WM_SETTEXT, 0, "");

		this.hTextBox = 0;
		this.hPlayButton = 0;
	}
}

