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
public class VoiceRoid2 : IVoiceClient {
	private readonly string VoiceRoid2EditorClass = "HwndWrapper[VoiceroidEditor.exe;;4cc5cceb-49d9-4fbf-8374-11d461e38c4c]";
	private string exe = "";
	private int pId;
	private nint hVoiceRoid2;
	private Accessibility.IAccessible? TextBox;
	private Accessibility.IAccessible? PlayButton;

	public int ProcessId { get => this.pId; }


	public bool StartClient(string targetExe, bool isLaunch) {
		this.exe = targetExe;
		return this.Load(this.exe, isLaunch);
	}

	private bool Load(string targetExe, bool isLaunch) {
		this.pId = 0;
		this.hVoiceRoid2 = 0;
		if(this.TextBox != null) {
			Marshal.ReleaseComObject(this.TextBox);
			this.TextBox = null;
		}
		if (this.PlayButton != null) {
			Marshal.ReleaseComObject(this.PlayButton);
			this.PlayButton = null;
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
		Interop.AccessibleObjectFromWindow(
			h,
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
				if (obj1.Length != c) {
					return false;
				}
				if(c < 3) {
					return false;
				}

				var acc2 = (Accessibility.IAccessible)obj1[3];
				obj2 = new object[acc2.accChildCount];
				Interop.AccessibleChildren(acc2, 0, obj2.Length, obj2, out c);
				if (obj2.Length != c) {
					return false;
				}
				if (c < 3) {
					return false;
				}
				var acc3 = (Accessibility.IAccessible)obj2[3];
				obj3 = new object[acc3.accChildCount];
				Interop.AccessibleChildren(acc3, 0, obj3.Length, obj3, out c);
				if (obj3.Length != c) {
					return false;
				}
				if (c < 2) {
					return false;
				}

				this.pId = p.Id;
				this.hVoiceRoid2 = h;
				this.TextBox = (Accessibility.IAccessible)obj3[0];
				this.PlayButton = (Accessibility.IAccessible)obj3[1];
			}
			finally {
				foreach (var it in obj3?.Skip(2) ?? Array.Empty<object>()) {
					if (it != null) {
						Marshal.ReleaseComObject(o);
					}
				}
				foreach (var it in obj2 ?? Array.Empty<object>()) {
					if (it != null) {
						Marshal.ReleaseComObject(o);
					}
				}
				foreach (var it in obj1 ?? Array.Empty<object>()) {
					if (it != null) {
						Marshal.ReleaseComObject(o);
					}
				}
			}
			return true;
		} else {
			return false;
		}
	}

	public void EndClient() {}


	public void BeginSpeech(string text) {
		if(!Interop.IsWindow(this.hVoiceRoid2)) {
			this.Load(this.exe, false);
		}
	}

	public bool Speech(string text) {
		if (this.TextBox == null) {
			return false;
		}
		if (this.PlayButton == null) {
			return false;
		}

		var sucessed = false;
		for (int i = 0; i < 3; i++) {
			try {
				this.TextBox.accValue[0] = text;
				sucessed = true;
				break;
			}
			catch (COMException e) {
				// 0x80040200が確認されている
				Console.WriteLine("％％例外％％");
				Console.WriteLine(e);
				Thread.Sleep(100);
			}
		}
		if (sucessed) {
			this.PlayButton.accDoDefaultAction(0);
			return true;
		}else {
			return false;
		}
	}

	public void EndSpeech(string text) {
		if (this.TextBox == null) {
			return;
		}

		try {
			this.TextBox.accValue[0] = "";
		}
		catch {}
	}
}

