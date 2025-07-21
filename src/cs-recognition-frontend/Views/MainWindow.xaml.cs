using Haru.Kei.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security;
using System.Windows.Interop;
using System.Diagnostics;
using Reactive.Bindings;

namespace Haru.Kei.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint SendMessage(nint hwnd, int msg, nint wParam, nint lParam);
	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern uint ExtractIconEx(string pszFile, uint nIconIndex, out nint phIconLarge, out nint phIconSmall, uint nIcons);
	private const int WM_SETICON = 0x0080;
	private const int ICON_BIG = 1;
	private const int ICON_SMALL = 0;

	public MainWindow() {
		InitializeComponent();

		this.Loaded += (_, _) => {;
			var hwnd = new WindowInteropHelper(this).Handle;
			ExtractIconEx(
				Process.GetCurrentProcess().MainModule?.FileName ?? "",
				0,
				out var hIcon,
				out var hIconSmall,
				1);
			SendMessage(hwnd, WM_SETICON, ICON_BIG, hIcon);
			SendMessage(hwnd, WM_SETICON, ICON_SMALL, hIconSmall);
		};
		//this.Closing += (_, _) => vm.OnClosing();
	}
}