using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VoiceLink.Clients;
public class VoiceRoid : IVoiceClient {
	private readonly string VoiceRoidEditorClass = "WindowsForms10.Window.8.app.0.378734a";
	private string exe = "";
	private int pId;
	private nint hVoiceRoid2;
	private nint textBox;
	private nint playButton;

	public int ProcessId { get => this.pId; }


	public bool StartClient(string targetExe, bool isLaunch) {
		this.exe = targetExe;
		return this.Load(this.exe, isLaunch);
	}

	private bool Load(string targetExe, bool isLaunch) {
		this.pId = 0;
		this.hVoiceRoid2 = 0;
		this.textBox = 0;
		this.playButton = 0;

		var p = Util.GetProcess(targetExe);
		var h = p?.MainWindowHandle ?? 0;
		if (p == null) {
			/* スプラッシュスクリーンが出るので考えないといけない */
			return false;
		}
		this.pId = p.Id;
		this.hVoiceRoid2 = h;
		return true;
	}

	public void EndClient() { }


	public void BeginSpeech(string text) {
		if (!Interop.IsWindow(this.hVoiceRoid2)) {
			this.Load(this.exe, false);
			if (this.hVoiceRoid2 == 0) {
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
		var _0 = getChildFromIndex(this.hVoiceRoid2, 0);
		var _1 = getChildFromIndex(_0, 1);
		var _2 = getChildFromIndex(_1, 0);
		var _3 = getChildFromIndex(_2, 0);
		var _4 = getChildFromIndex(_3, 0);
		var _5 = getChildFromIndex(_4, 0);

		this.textBox = getChildFromIndex(getChildFromIndex(_5, 0), 0);
		this.playButton = getChildFromIndex(getChildFromIndex(_5, 1), 0);
		if ((this.textBox == 0) || (this.playButton == 0)) {
			throw new VoiceLinkException("読み上げ開始準備に失敗");
		}
	}

	public void Speech(string text) {
		if ((this.textBox == 0) || (this.playButton == 0)) {
			throw new VoiceLinkException("");
		}
		Interop.SendMessage(this.textBox, Interop.WM_SETTEXT, 0, text);
		Interop.SendMessage(this.playButton, Interop.BM_CLICK, 0, 0);
	}

	public void EndSpeech(string text) {
		if (this.textBox == 0) {
			return;
		}

		Interop.SendMessage(this.textBox, Interop.WM_SETTEXT, 0, "");

		this.textBox = 0;
		this.playButton = 0;
	}
}

