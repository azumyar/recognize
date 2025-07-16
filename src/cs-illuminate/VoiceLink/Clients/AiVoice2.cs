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
using System.Windows.Automation;

namespace VoiceLink.Clients;

public class AiVoice2 : VoiceRoid<AudioCaptreStart, NopVoiceObject> {
	private readonly string AiVoice2WindowsClass = "FLUTTER_RUNNER_WIN32_WINDOW";
	private string exe = "";
	private nint hTargetWindow;
	private Accessibility.IAccessible? playButton;
	private Accessibility.IAccessible? text1;
	private Accessibility.IAccessible? text2;
	private Accessibility.IAccessible? textBox;

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
		this.ProcessId = p.Id;
		this.hTargetWindow = h;
		return true;
	}

	public override void EndClient() { }

	public override void BeginSpeech(string text, NopVoiceObject extra) {
		if (!Interop.IsWindow(this.hTargetWindow)) {
			this.Load(this.exe, false);
		}

		var aivoiceTarget = Interop.FindWindowEx(this.hTargetWindow, 0, "FLUTTERVIEW", "FLUTTERVIEW");
		if (aivoiceTarget == 0) {
			throw new VoiceLinkException();
		}
		Interop.AccessibleObjectFromWindow(
			aivoiceTarget,
			0,
			in Interop.IID_IAccessible,
			out var o
			);
		if (o is Accessibility.IAccessible acc) {
			object[]? obj1 = default;
			object[]? obj2 = default;
			object[]? obj3 = default;
			object[]? obj4 = default;
			object[]? obj5 = default;
			object[]? obj6 = default;
			object?[]? obj7 = default;

			object[]? o6_1 = default;
			object?[]? o6_1_1 = default;
			
			object[]? o6_2 = default;
			object[]? o6_2_1 = default;
			object[]? o6_2_2 = default;
			object?[]? o6_2_3 = default;
			try {
				static object[] get(Accessibility.IAccessible a, Action<Accessibility.IAccessible, object[], int> judge) {
					var obj = new object[a.accChildCount];
					Interop.AccessibleChildren(a, 0, obj.Length, obj, out int c);
					judge(a, obj, c);
					return obj;
				}

				obj1 = get(acc,
					(x, y, z) => {
						if ((y.Length != z) || (z < 3)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(1/13)");
						}
					});
				obj2 = get((Accessibility.IAccessible)obj1[3],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(2/13)");
						}
					});
				obj3 = get((Accessibility.IAccessible)obj2[0],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(3/13)");
						}
					});
				obj4 = get((Accessibility.IAccessible)obj3[0],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(4/13)");
						}
					});
				obj5 = get((Accessibility.IAccessible)obj4[0],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(5/13)");
						}
					});
				obj6 = get((Accessibility.IAccessible)obj5[0],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(6/13)");
						}
					});
				obj7 = get((Accessibility.IAccessible)obj6[7],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(7/13)");
						}
					});
				playButton = (Accessibility.IAccessible?)obj7[0];
				obj7[0] = null;

				o6_1 = get((Accessibility.IAccessible)obj6[1],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(8/13)");
						}
					});
				o6_1_1 = get((Accessibility.IAccessible)o6_1[0],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("A.I.Voive2オブジェクトの取得に失敗(9/13)");
						}
					});
				this.text1 = (Accessibility.IAccessible?)o6_1_1[4];
				this.text2 = (Accessibility.IAccessible?)o6_1_1[5];
				o6_1_1[4] = o6_1_1[5] = null;

				o6_2 = get((Accessibility.IAccessible)obj6[4],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("VoiceRoid2オブジェクトの取得に失敗(10/13)");
						}
					});
				o6_2_1 = get((Accessibility.IAccessible)o6_2[1],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("VoiceRoid2オブジェクトの取得に失敗(11/13)");
						}
					});

				o6_2_2 = get((Accessibility.IAccessible)o6_2_1[0],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("VoiceRoid2オブジェクトの取得に失敗(12/13)");
						}
					});
				o6_2_3 = get((Accessibility.IAccessible)o6_2_2[0],
					(x, y, z) => {
						if ((y.Length != z) || (z < 1)) {
							throw new VoiceLinkException("VoiceRoid2オブジェクトの取得に失敗(13/13)");
						}
					});
				this.textBox = (Accessibility.IAccessible?)o6_2_3[0];
				o6_2_3[0] = null;

				return;
			}
			finally {
				var rls = (obj1 ?? Array.Empty<object>()).ToList<object?>();
				rls.AddRange(obj2 ?? Array.Empty<object>());
				rls.AddRange(obj3 ?? Array.Empty<object>());
				rls.AddRange(obj4 ?? Array.Empty<object>());
				rls.AddRange(obj5 ?? Array.Empty<object>());
				rls.AddRange(obj6 ?? Array.Empty<object>());
				rls.AddRange(obj7 ?? Array.Empty<object>());
				rls.AddRange(o6_1 ?? Array.Empty<object>());
				rls.AddRange(o6_1_1 ?? Array.Empty<object>());
				rls.AddRange(o6_2 ?? Array.Empty<object>());
				rls.AddRange(o6_2_1 ?? Array.Empty<object>());
				rls.AddRange(o6_2_2 ?? Array.Empty<object>());
				rls.AddRange(o6_2_3 ?? Array.Empty<object>());
				rls.Add(o);

				foreach (var it in rls) {
					if ((it != null) && Marshal.IsComObject(it)) {
						Marshal.ReleaseComObject(it);
					}
				}
			}
		}
	}

	public override void Speech(string text, NopVoiceObject extra) {
		if((this.text1 == null) || (this.text2 == null) || (this.playButton == null) || (this.textBox == null)) {
			return;
		}

		// 書き込めないぽい
		//textBox.accValue[0] = text;

		// キーボードフォーカス握るウインドウに差し替え
		var aivoiceTarget = Interop.FindWindowEx(this.hTargetWindow, 0, "FLUTTERVIEW", "FLUTTERVIEW");
		if (aivoiceTarget == 0) {
			throw new VoiceLinkException();
		}

		// 文字入力
		Util.PlatformClick(aivoiceTarget, 380, 185);
		if (textBox.accValue[0] == "") {
			Thread.Sleep(100);
		} else {
			// テキストボックスになにか残っている場合以前のテキストが再生されることがあるので多く待つ
			Thread.Sleep(1000);
		}
		foreach (var c in text.Replace('。', '　')) { //。で読み上げが区切られるので置き換える
			Interop.SendMessage(aivoiceTarget, Interop.WM_CHAR, c, 0);
		}

		// 再生
		//void play() => Util.PlatformClick(aivoiceTarget, 475, 45);
		var t1 = this.text1.accValue[0];
		var t2 = this.text2.accValue[0];
		// すぐに再生しないのでループする
		do {
			this.playButton.accDoDefaultAction(0);
			Thread.Sleep(1000);

			// 再生インジケータの表示が変わったら再生開始とみなす
			if ((this.text1.accValue[0] != t1) || (this.text2.accValue[0] != t2)) {
				break;
			}
		} while (true);
	}

	public override void EndSpeech(string text, NopVoiceObject extra) {
		var aivoiceTarget = Interop.FindWindowEx(this.hTargetWindow, 0, "FLUTTERVIEW", "FLUTTERVIEW");

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

		if (this.text1 != null) {
			Marshal.ReleaseComObject(this.text1);
			this.text1 = null;
		}
		if (this.text2 != null) {
			Marshal.ReleaseComObject(this.text2);
			this.text2 = null;
		}
		if (this.playButton != null) {
			Marshal.ReleaseComObject(this.playButton);
			this.text2 = null;
		}
		if (this.textBox != null) {
			Marshal.ReleaseComObject(this.textBox);
			this.textBox = null;
		}
	}
}

