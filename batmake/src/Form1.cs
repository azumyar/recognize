using System;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

namespace Haru.Kei {
	public partial class Form1 : Form {
		private BatArgument arg;

		public Form1() {
			InitializeComponent();
			this.arg = BatArgumentEx.Init(@"..\py-recognition\dist\recognize\recognize.exe");
			this.propertyGrid.SelectedObject = arg;
			this.button.Click += (_, __) => {
				var sb = new StringBuilder();
				sb.AppendLine("@echo off")
					.AppendLine("pushd \"%~dp0\"")
					.Append(this.arg.RecognizeExePath);
				foreach(var p in this.arg.GetType().GetProperties()) {
					var att = p.GetCustomAttribute(typeof(ArgAttribute)) as ArgAttribute;
					if(att != null) {
						var opt = att.Gen(p.GetValue(this.arg, null), this.arg);
						if(!string.IsNullOrEmpty(opt)) {
							sb.Append(" ").Append(opt);
						}
					}
				}
				sb.AppendLine();
				System.IO.File.WriteAllText(System.IO.Path.Combine(this.arg.OutputPath, this.arg.BatFile), sb.ToString(), Encoding.GetEncoding("Shift_JIS"));

				System.Diagnostics.Debug.WriteLine(sb.ToString());
				MessageBox.Show(this, "作成しました！");
			};
		}
	}
}
