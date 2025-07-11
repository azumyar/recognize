using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

namespace VoiceLink;


internal static class Util {

	public static Process? GetProcess(string targetExe) {
		return Process.GetProcesses().Where(x => {
			try {
				return x.MainModule?.FileName?.ToLower() == targetExe.ToLower();
			}
			catch (Exception e) when (e is Win32Exception || e is InvalidOperationException) {
				return false;
			}
		}).FirstOrDefault();
	}

	public static (Process Proc, nint WindowHandle)? LaunchProcess(string targetExe, Func<string, bool>? classProc) {
		if (File.Exists(targetExe)) {
			try {
				var p = Process.Start(targetExe);
				p.WaitForInputIdle();
				// 自分で起動したプロセスはMainWindowHandleが必ず(？)空なので自分でとりに行く
				var l = new List<ProcessThread>();
				foreach (ProcessThread thread in p.Threads) {
					l.Add(thread);
				}
				if (classProc == null) {
					return (p, 0);
				} else {
					if (l.OrderBy(x => x.StartTime).FirstOrDefault() is ProcessThread pt) {
						nint h = 0;
						Interop.EnumThreadWindows(pt.Id, (hwnd, lP) => {
							var s = new StringBuilder(128);
							if ((Interop.GetClassName(hwnd, s, s.Capacity) != 0) && classProc(s.ToString())) {
								h = hwnd;
								return false;
							}
							return true;
						}, 0);
						if (h != 0) {
							return (p, h);
						}
					}
				}
			}
			catch (Exception e) when (e is Win32Exception || e is InvalidOperationException) {}
		}
		return null;
	}

	public static void PlatformKeyboard(nint hwnd, int keycode) {
		unchecked {
			Interop.PostMessage(hwnd, Interop.WM_KEYDOWN, keycode, 0x000000001);
			Interop.PostMessage(hwnd, Interop.WM_KEYUP, keycode, (int)0xC00000001);
		}
	}
	public static void PlatformClick(nint hwnd, int x, int y) {
		var pos = x | y << 16;
		Interop.PostMessage(hwnd, Interop.WM_LBUTTONDOWN, Interop.MK_LBUTTON, pos);
		Interop.PostMessage(hwnd, Interop.WM_LBUTTONUP, 0, pos);
	}

}

