using System;
using System.Threading;

namespace VoiceLink.Clients;

public class VoicePeak : VoiceRoid<AudioCaptreStart, NopVoiceObject> {
	private readonly string AiVoice2WindowsClass = "FLUTTER_RUNNER_WIN32_WINDOW";
	private string exe = "";
	private nint hTargetWindow;
	private int targetWidth;

	public override bool StartClient(bool isLaunch, AudioCaptreStart extra) {
		this.exe = extra.TargetExe;
		return this.Load(this.exe, isLaunch);
	}

	private bool Load(string targetExe, bool isLaunch) {
		this.ProcessId = 0;
		this.hTargetWindow = 0;

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

		this.ProcessId = p.Id;
		this.hTargetWindow = h;

		Interop.GetWindowRect(this.hTargetWindow, out var rc);
		Interop.SetWindowPos(this.hTargetWindow, IntPtr.Zero, rc.left, rc.top, 1100, 877, 0);
		Interop.GetClientRect(this.hTargetWindow, out rc);
		this.targetWidth = rc.right - rc.left;
		return true;
	}

	public override void EndClient() {
	}

	public override void BeginSpeech(string text, NopVoiceObject extra) {
		if (!Interop.IsWindow(this.hTargetWindow)) {
			this.Load(this.exe, false);
		}
	}

	public override void Speech(string text, NopVoiceObject extra) {
		var len = text.Length;
		Util.PlatformClick(this.hTargetWindow, 400, 140);
		foreach (var c in text) {
			Interop.SendMessage(this.hTargetWindow, Interop.WM_IME_CHAR, c, 0);
		}
		Thread.Sleep(50 * text.Length);

		Interop.SendMessage(this.hTargetWindow, Interop.WM_KEYDOWN, Interop.VK_HOME, 0x000000001);
		Interop.SendMessage(this.hTargetWindow, Interop.WM_KEYUP, Interop.VK_HOME, unchecked((int)0xC00000001));
		Thread.Sleep(50);
		// フォーカスを削除してカーソルのWM_PAINTを抑制する
		Util.PlatformClick(this.hTargetWindow, this.targetWidth / 2 + 160, 20);
		Util.PlatformClick(this.hTargetWindow, this.targetWidth / 2 + 200, 20);
	}


	public override void EndSpeech(string text, NopVoiceObject extra) {
		Util.PlatformClick(this.hTargetWindow, 400, 140);
		if (!string.IsNullOrEmpty(text)) {
			// 残ることがあるらしいので3週Deleteを打つ
			for (var i = 0; i < 3; i++) {
				Util.PlatformKeyboard(this.hTargetWindow, Interop.VK_HOME);
				foreach (var _ in text) {
					Util.PlatformKeyboard(this.hTargetWindow, Interop.VK_DELETE);
				}
			}
		}
	}
}
