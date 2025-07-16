using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Automation;

namespace VoiceLink.Clients;
public class VoiceRoid2 : VoiceRoid<AudioCaptreStart, NopVoiceObject> {
	private readonly string VoiceRoid2EditorClass = "HwndWrapper[VoiceroidEditor.exe;;4cc5cceb-49d9-4fbf-8374-11d461e38c4c]";
	private string exe = "";
	private nint hTargetWindow;
	private Accessibility.IAccessible? textBox;
	private Accessibility.IAccessible? playButton;
	private Accessibility.IAccessible? caretStartButton;

	public override bool StartClient(bool isLaunch, AudioCaptreStart extra) {
		this.exe = extra.TargetExe;
		return this.Load(this.exe, isLaunch);
	}

	private bool Load(string targetExe, bool isLaunch) {
		this.ProcessId = 0;
		this.hTargetWindow = 0;
		if(this.textBox != null) {
			Marshal.ReleaseComObject(this.textBox);
			this.textBox = null;
		}
		if (this.playButton != null) {
			Marshal.ReleaseComObject(this.playButton);
			this.playButton = null;
		}

		var p = Util.GetProcess(targetExe);
		var h = p?.MainWindowHandle ?? 0;
		if (p == null) {
			/* スプラッシュスクリーンが出るのでこれじゃダメ
			var pp = isLaunch switch {
				true => Util.LaunchProcess(targetExe, (x) => x.ToLower().StartsWith(VoiceRoid2EditorClass.ToLower())),
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
		return true;
	}

	public override void EndClient() {}


	public override void BeginSpeech(string text, NopVoiceObject extra) {
		if(!Interop.IsWindow(this.hTargetWindow)) {
			this.Load(this.exe, false);
			if (this.hTargetWindow == 0) {
				throw new VoiceLinkException("VoiceRoid2が見つかりません");
			}
		}
		Interop.AccessibleObjectFromWindow(
			this.hTargetWindow,
			0,
			in Interop.IID_IAccessible,
			out var o
			);
		if (o is Accessibility.IAccessible acc) {
			object[]? obj1 = default;
			object[]? obj2 = default;
			object[]? obj3 = default;
			try {
				obj1 = new object[acc.accChildCount];
				Interop.AccessibleChildren(acc, 0, obj1.Length, obj1, out var c);
				if ((obj1.Length != c) || (c < 3)) {
					throw new VoiceLinkException("VoiceRoid2オブジェクトの取得に失敗(1/3)");
				}

				var acc2 = (Accessibility.IAccessible)obj1[3];
				obj2 = new object[acc2.accChildCount];
				Interop.AccessibleChildren(acc2, 0, obj2.Length, obj2, out c);
				if ((obj2.Length != c) || (c < 3)) {
					throw new VoiceLinkException("VoiceRoid2オブジェクトの取得に失敗(2/3)");
				}

				var acc3 = (Accessibility.IAccessible)obj2[3];
				obj3 = new object[acc3.accChildCount];
				Interop.AccessibleChildren(acc3, 0, obj3.Length, obj3, out c);
				if ((obj3.Length != c) || (c < 2)) {
					throw new VoiceLinkException("VoiceRoid2オブジェクトの取得に失敗(3/3)");
				}

				this.textBox = (Accessibility.IAccessible)obj3[0];
				this.playButton = (Accessibility.IAccessible)obj3[1];
				this.caretStartButton = (Accessibility.IAccessible)obj3[3];
				return;
			}
			finally {
				var rls = (obj3?.Skip(2) ?? Array.Empty<object>()).ToList();
				rls.AddRange(obj2 ?? Array.Empty<object>());
				rls.AddRange(obj1 ?? Array.Empty<object>());
				rls.Add(o);

				foreach (var it in rls) {
					if ((it != null) && Marshal.IsComObject(it)) {
						Marshal.ReleaseComObject(it);
					}
				}
			}
		}
		throw new VoiceLinkException("読み上げ開始準備に失敗");
	}

	public override void Speech(string text, NopVoiceObject extra) {
		if ((this.textBox == null) || (this.playButton == null) || (this.caretStartButton == null)) {
			throw new VoiceLinkException("");
		}

		foreach(var it in new (int Index, Action Action)[] {
			(1, () => { this.textBox.accValue[0] = text; }),
			(2, () => { this.playButton.accDoDefaultAction(0); }),
		}) {
			try {
				it.Action();
			}
			catch (COMException e) {
				// 0x80040200が確認されている
				throw new VoiceLinkException($"VoiceRoid2読み上げ連携に失敗({it.Index}/2)", e);
			}
		}
	}

	public override void EndSpeech(string text, NopVoiceObject extra) {
		try {
			if (this.textBox == null) {
				return;
			}

			// 本当に読み上げが終わっているかのダブルチェック
			if (this.caretStartButton != null) {
				var s = new StringBuilder(256);
				try {
					while (true) {
						var state = (int)this.caretStartButton.accState[0];
						if (state == 0x1) {
							Thread.Sleep(100);
							continue;
						}
						break;
					}
				}
				catch { }
			}

			try {
				this.textBox.accValue[0] = "";
			}
			catch { }
		}
		finally {
			if (this.textBox != null) {
				Marshal.ReleaseComObject(this.textBox);
				this.textBox = null;
			}
			if (this.playButton != null) {
				Marshal.ReleaseComObject(this.playButton);
				this.playButton = null;
			}
			if (this.caretStartButton != null) {
				Marshal.ReleaseComObject(this.caretStartButton);
				this.caretStartButton = null;
			}
		}
	}
}

