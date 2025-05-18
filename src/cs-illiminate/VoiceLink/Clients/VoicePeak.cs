using System;
using System.Threading;

namespace VoiceLink.Clients;

public class VoicePeak : IVoiceClient {
	private readonly string AiVoice2WindowsClass = "FLUTTER_RUNNER_WIN32_WINDOW";
	private string exe = "";
	private int pId;
	private nint hVoicePeak = 0;
	private int targetWidth;
	public int ProcessId { get => this.pId; }

	public bool StartClient(string targetExe, bool isLaunch) {
		this.exe = targetExe;
		return this.Load(this.exe, isLaunch);
	}

	private bool Load(string targetExe, bool isLaunch) {
		this.pId = 0;
		this.hVoicePeak = 0;

		var p = Util.GetProcess(targetExe);
		var h = p?.MainWindowHandle ?? 0;
		if (p == null) {
			/*
			var pp = isLaunch switch {
				true => Util.LaunchProcess(targetExe, (x) => x.ToLower() == AiVoice2WindowsClass.ToLower()),
				false => null,
			};
			if (pp == null) {
				return false;
			}
			p = pp.Value.Proc;
			h = pp.Value.WindowHandle;
			*/
			return false;
		}

		this.pId = p.Id;
		this.hVoicePeak = h;

		Interop.GetWindowRect(this.hVoicePeak, out var rc);
		Interop.SetWindowPos(this.hVoicePeak, IntPtr.Zero, rc.left, rc.top, 1100, 877, 0);
		Interop.GetClientRect(this.hVoicePeak, out rc);
		this.targetWidth = rc.right - rc.left;
		return true;
	}

	public void EndClient() {
	}

	public void BeginSpeech(string text) {
		if (!Interop.IsWindow(this.hVoicePeak)) {
			this.Load(this.exe, false);
		}
	}

	public void Speech(string text) {
		var len = text.Length;
		Util.PlatformClick(this.hVoicePeak, 400, 140);
		foreach (var c in text) {
			Interop.SendMessage(this.hVoicePeak, Interop.WM_IME_CHAR, c, 0);
		}
		Thread.Sleep(50 * text.Length);

		Interop.SendMessage(this.hVoicePeak, Interop.WM_KEYDOWN, Interop.VK_HOME, 0x000000001);
		Interop.SendMessage(this.hVoicePeak, Interop.WM_KEYUP, Interop.VK_HOME, unchecked((int)0xC00000001));
		Thread.Sleep(50);
		// フォーカスを削除してカーソルのWM_PAINTを抑制する
		Util.PlatformClick(this.hVoicePeak, this.targetWidth / 2 + 160, 20);
		Util.PlatformClick(this.hVoicePeak, this.targetWidth / 2 + 200, 20);
	}


	public void EndSpeech(string text) {
		Util.PlatformClick(this.hVoicePeak, 400, 140);
		if (!string.IsNullOrEmpty(text)) {
			// 残ることがあるらしいので3週Deleteを打つ
			for (var i = 0; i < 3; i++) {
				Util.PlatformKeyboard(this.hVoicePeak, Interop.VK_HOME);
				foreach (var _ in text) {
					Util.PlatformKeyboard(this.hVoicePeak, Interop.VK_DELETE);
				}
			}
		}
	}
}
