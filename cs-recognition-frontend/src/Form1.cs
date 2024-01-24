using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using System.Xml.Linq;

namespace Haru.Kei {
	public partial class Form1 : Form {
		private readonly string CONFIG_FILE = "frontend.conf";
		private readonly string BAT_FILE = "custom-recognize.bat";
		private RecognizeExeArgument arg;
	
		public Form1() {
			InitializeComponent();
			this.batToolStripMenuItem.Click += (_, __) => {
				try {
					var properties = this.arg.GetType().GetProperties();

					var bat = new StringBuilder()
						.AppendLine("@echo off")
						.AppendLine("pushd \"%~dp0\"")
						.AppendLine()
						.AppendFormat("\"{0}\"", this.arg.RecognizeExePath).Append(" ").AppendLine(this.GenExeArguments(properties))
						.AppendLine("pause");
					System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.BAT_FILE), bat.ToString());
				}
				catch(System.IO.IOException) { }
			};
			this.testmicToolStripMenuItem.Click += (_, __) => {
				var properties = this.arg.GetType().GetProperties();
				try {
					using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = this.arg.RecognizeExePath,
						Arguments = string.Format("--test mic {0}", this.GenExeArguments(properties)),
						UseShellExecute = true,
					})) { }
				}
				catch(Exception) { }
			};
			this.exitToolStripMenuItem.Click += (_, __) => this.Close();

			this.whisperToolStripMenuItem.Click+= (_, __) => {
				this.arg.ArgMethod = "faster_whisper";
				this.arg.ArgWhisperModel = "medium";
				this.arg.ArgWhisperLanguage = "ja";
				this.arg.ArgDisableLpf = null;
				this.arg.ArgDisableHpf = null;
				this.propertyGrid.Refresh();
			};
			this.googleToolStripMenuItem.Click += (_, __) => {
				this.arg.ArgMethod = "google_duplex";
				this.arg.ArgGoogleConvertSamplingRate = true;
				this.arg.ArgDisableLpf = true;
				this.arg.ArgDisableHpf = true;
				this.propertyGrid.Refresh();
			};
			this.yukarinetteToolStripMenuItem.Click += (_, __) => {
				this.arg.ArgOut = "yukarinette";
				if(!this.arg.ArgOutYukarinette.HasValue) {
					this.arg.ArgOutYukarinette = 49513;
				}
				this.propertyGrid.Refresh();
			};
			this.yukaconeToolStripMenuItem.Click += (_, __) => {
				this.arg.ArgOut = "yukacone";
				this.propertyGrid.Refresh();
			};

			this.button.Click += (_, __) => {
				var properties = this.arg.GetType().GetProperties();
				this.SaveConfig(properties);

				try {
					using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = this.arg.RecognizeExePath,
						Arguments = this.GenExeArguments(properties),
						UseShellExecute = true,
					})) { }
				}
				catch(Exception) { }
			};
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			var convDic = new Dictionary<Type, Func<string, object>>();
			convDic.Add(typeof(string), (x) => x);
			convDic.Add(typeof(bool?), (x) => {
				bool v;
				return bool.TryParse(x, out v) ? (object)v : null;
			});
			convDic.Add(typeof(int?), (x) => {
				int v;
				return int.TryParse(x, out v) ? (object)v : null;
			});
			convDic.Add(typeof(float?), (x) => {
				float v;
				return float.TryParse(x, out v) ? (object)v : null;
			});

			var list = new List<Tuple<string, string>>();
			try {
				var save = System.IO.File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.CONFIG_FILE));
				foreach(var line in save.Replace("\r\n", "\n").Split('\n')) {
					var c = line.IndexOf(':');
					if(0 < c) {
						list.Add(new Tuple<string, string>(line.Substring(0, c), line.Substring(c + 1)));
					}
				}
			}
			catch(System.IO.IOException) {}

			var prop = typeof(RecognizeExeArgument).GetProperties();
			var pr = typeof(RecognizeExeArgument).GetProperty("RecognizeExePath");
			var exe = list.Where(x => x.Item1 == pr.Name).FirstOrDefault();
			this.arg = RecognizeExeArgumentEx.Init((exe != null) ? exe.Item2 : (string)pr.GetCustomAttribute<DefaultValueAttribute>().Value);
			foreach(var tp in list) {
				var p = prop.Where(x => x.Name == tp.Item1).FirstOrDefault();
				if(p != null) {
					Func<string, object> f;
					if(convDic.TryGetValue(p.PropertyType, out f)) {
						var v = f(tp.Item2);
						if(v != null) {
							p.SetValue(this.arg, v);
						}
					}
				}
			}
			this.propertyGrid.SelectedObject = this.arg;
		}

		protected override void OnFormClosed(FormClosedEventArgs e) {
			this.SaveConfig(this.arg.GetType().GetProperties());

			base.OnFormClosed(e);
		}

		private string GenExeArguments(System.Reflection.PropertyInfo[] properties) {
			var araguments = new StringBuilder();
			foreach(var p in properties) {
				var att = p.GetCustomAttribute<ArgAttribute>();
				if(att != null) {
					var opt = att.Generate(p.GetValue(this.arg, null), this.arg);
					if(!string.IsNullOrEmpty(opt)) {
						araguments.Append(" ").Append(opt);
					}
				}
			}
			return araguments.ToString();
		}

		private void SaveConfig(System.Reflection.PropertyInfo[] properties) {
			try {
				var save = new StringBuilder();
				foreach(var p in properties) {
					var dfattr = p.GetCustomAttribute<DefaultValueAttribute>();
					if(dfattr != null) {
						var pv = p.GetValue(this.arg, null);
						var dv = dfattr.Value;
						if((pv != null) && !pv.Equals(dv)) {
							save.Append(p.Name).Append(":").AppendLine(pv.ToString());
							continue;
						}
						//if((dv != null) && !dv.Equals(pv)) {
						//	save.Append(p.Name).Append(":").AppendLine(pv.ToString());
						//	continue;
						//}
					}
				}
				System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.CONFIG_FILE), save.ToString());
			}
			catch(System.IO.IOException) { }
		}
	}
}