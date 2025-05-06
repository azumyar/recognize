using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace VoiceLink.Clients;

public class AiVoice2 : IVoiceClient {
	private readonly string AiVoice2WindowsClass = "FLUTTER_RUNNER_WIN32_WINDOW";
	private string exe = "";
	private int pId;
	private nint hAiVoice = 0;
	private int targetWidth;
	//private readonly string DefaultAiVoicePath = @"C:\Program Files\AI\AIVoice2\AIVoice2Editor\aivoice.exe";


	public int ProcessId { get => this.pId; }

	public bool StartClient(string targetExe, bool isLaunch) {
		this.exe = targetExe;
		return this.Load(this.exe, isLaunch);
	}

	private bool Load(string targetExe, bool isLaunch) {
		this.pId = 0;
		this.hAiVoice = 0;

		var p = Util.GetProcess(targetExe);
		var h = p?.MainWindowHandle ?? 0;
		if (p == null) {
			var pp = isLaunch switch {
				true => Util.LaunchProcess(targetExe, (x) => x.ToLower() == AiVoice2WindowsClass.ToLower()),
				false => null,
			};
			if (pp == null) {
				return false;
			}
			p = pp.Value.Proc;
			h = pp.Value.WindowHandle;
		}
		this.pId = p.Id;
		this.hAiVoice = h;

		Interop.GetWindowRect(this.hAiVoice, out var rc);
		Interop.SetWindowPos(this.hAiVoice, 0, rc.left, rc.top, 1152, 720, 0);
		Interop.GetClientRect(this.hAiVoice, out rc);
		this.targetWidth = rc.right - rc.left;
		return true;
	}

	public void EndClient() { }

	public void BeginSpeech(string text) {
		if (!Interop.IsWindow(this.hAiVoice)) {
			this.Load(this.exe, false);
		}
	}

	public bool Speech(string text) {
		// キーボードフォーカス握るウインドウに差し替え
		var aivoiceTarget = Interop.FindWindowEx(this.hAiVoice, 0, "FLUTTERVIEW", "FLUTTERVIEW");
		if(aivoiceTarget == 0) {
			return false;
		}

		// 文字入力
		Util.PlatformClick(aivoiceTarget, 380, 185);
		Thread.Sleep(100);
		foreach (var c in text) {
			Interop.SendMessage(aivoiceTarget, Interop.WM_CHAR, c, 0);
		}
		// 逐次変換されるぽいので固定で1秒待つ
		Thread.Sleep(1000);

		// 再生
		Util.PlatformClick(aivoiceTarget, 475, 45);
		return true;
	}

	public void EndSpeech(string text) {
		var aivoiceTarget = Interop.FindWindowEx(this.hAiVoice, 0, "FLUTTERVIEW", "FLUTTERVIEW");

		// 後片付け
		// 再生終了直後はフォーカスが奪えないので少し待つ
		Thread.Sleep(100);
		Util.PlatformClick(aivoiceTarget, 380, 185);
		Thread.Sleep(100);
		if (!string.IsNullOrEmpty(text)) {
			// 残ることがあるらしいので3週Deleteを打つ
			for (var i = 0; i < 3; i++) {
				Util.PlatformKeyboard(aivoiceTarget, Interop.VK_HOME);
				foreach (var _ in text) {
					Util.PlatformKeyboard(aivoiceTarget, Interop.VK_DELETE);
				}
			}
		}
	}
}

