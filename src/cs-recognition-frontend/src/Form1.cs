using System;
using System.IO;
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
		private readonly string TEMP_BAT = Path.Combine(Path.GetTempPath(), string.Format("recognize-gui-{0}.bat", Guid.NewGuid()));

		private RecognizeExeArgument arg;

	
		public Form1() {
			InitializeComponent();
			this.batToolStripMenuItem.Click += (_, __) => {
				try {
					var bat = new StringBuilder()
						.AppendLine("@echo off")
						.AppendLine("pushd \"%~dp0\"")
						.AppendLine()
						.AppendFormat("\"{0}\"", this.arg.RecognizeExePath).Append(" ").AppendLine(this.GenExeArguments(this.arg))
						.AppendLine("pause");
					File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.BAT_FILE), bat.ToString(), Encoding.GetEncoding("Shift_JIS"));
					MessageBox.Show(this, string.Format("{0}を作成しました！", this.BAT_FILE), "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch(System.IO.IOException) { }
			};
			this.testmicToolStripMenuItem.Click += (_, __) => {
				var properties = this.arg.GetType().GetProperties();
				try {
					using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = this.arg.RecognizeExePath,
						Arguments = string.Format("--test mic {0}", this.GenExeArguments(this.arg)),
						UseShellExecute = true,
					})) { }
				}
				catch(Exception) { }
			};
			testambientToolStripMenuItem.Click += (_, __) => {
				using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
					FileName = this.arg.RecognizeExePath,
					Arguments = string.Format("--test mic_ambient {0}", this.GenExeArguments(this.arg)),
					UseShellExecute = true,
				})) { }
			};
			this.exitToolStripMenuItem.Click += (_, __) => this.Close();

			this.whisperToolStripMenuItem.Click+= (_, __) => {
				this.arg.ArgMethod = "kotoba_whisper";
				this.arg.ArgDisableLpf = null;
				this.arg.ArgDisableHpf = null;
				this.propertyGrid.Refresh();
			};
			this.googleToolStripMenuItem.Click += (_, __) => {
				this.arg.ArgMethod = "google_mix";
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
			this.micToolStripMenuItem.Click += (_, __) => {
				this.arg.ArgMicDbThreshold = 49.54f;
				this.arg.ArgMicAmbientNoiseToDB = true;
				this.arg.ArgMicDynamicDB = true;
				this.arg.ArgMicDynamicDBMin = 40.0f;
				this.arg.ArgMicPharse = 0.3f;
				this.arg.ArgMicPause = 0.4f;
				this.propertyGrid.Refresh();
			};

			this.button.Click += (_, __) => {
				this.SaveConfig(this.arg);


				var bat = new StringBuilder()
					.AppendLine("@echo off")
					.AppendLine()
					.AppendFormat("\"{0}\"", this.arg.RecognizeExePath).Append(" ").AppendLine(this.GenExeArguments(this.arg))
					.AppendLine("if %ERRORLEVEL% neq 0 (")
					.AppendLine("  pause")
					.AppendLine(")");
				File.WriteAllText(this.TEMP_BAT, bat.ToString(), Encoding.GetEncoding("Shift_JIS"));


				try {
					/*
					using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = this.arg.RecognizeExePath,
						Arguments = this.GenExeArguments(this.arg),
						UseShellExecute = true,
					})) { }
					*/
					using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = this.TEMP_BAT,
						WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
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
				var save = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.CONFIG_FILE));
				foreach(var line in save.Replace("\r\n", "\n").Split('\n')) {
					var c = line.IndexOf(':');
					if(0 < c) {
						list.Add(new Tuple<string, string>(line.Substring(0, c), line.Substring(c + 1)));
					}
				}
			}
			catch(IOException) {}

			var prop = typeof(RecognizeExeArgument).GetProperties().Where(x => x.CanWrite);
			var pr = typeof(RecognizeExeArgument).GetProperty("RecognizeExePath");
			var exe = list.Where(x => x.Item1 == pr.Name).FirstOrDefault();
			this.arg = RecognizeExeArgumentEx.Init((exe != null) ? exe.Item2 : (string)pr.GetCustomAttribute<DefaultValueAttribute>().Value);
			foreach(var tp in list) {
				var p = prop.Where(x => x.Name == tp.Item1).FirstOrDefault();
				if(p != null) {
					var svattr = p.GetCustomAttribute<SaveAttribute>();
					if((svattr != null) && !svattr.IsRestore) {
						continue;
					}

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
			this.SaveConfig(this.arg);
			try {
				if(File.Exists(this.TEMP_BAT)) {
					File.Delete(this.TEMP_BAT);
				}
			}
			catch(IOException) {}

			base.OnFormClosed(e);
		}

		private string GenExeArguments(RecognizeExeArgument argument) {
			var araguments = new StringBuilder();
			foreach(var p in argument.GetType().GetProperties()) {
				var att = p.GetCustomAttribute<ArgAttribute>();
				if(att != null) {
					var opt = att.Generate(p.GetValue(this.arg, null), this.arg);
					if(!string.IsNullOrEmpty(opt)) {
						araguments.Append(" ").Append(opt);
					}
				}
			}
			if(!string.IsNullOrEmpty(argument.ExtraArgument)) {
				araguments.Append(" ")
					.Append(argument.ExtraArgument);
			}
			return araguments.ToString();
		}

		private void SaveConfig(RecognizeExeArgument argument) {
			try {
				var save = new StringBuilder();
				var dict = new  Dictionary<string, string>();
				foreach(var p in argument.GetType().GetProperties()) {
					var dfattr = p.GetCustomAttribute<DefaultValueAttribute>();
					if(dfattr != null) {
						var pv = p.GetValue(this.arg, null);
						var dv = dfattr.Value;
						if((pv != null) && !pv.Equals(dv)) {
							var svattr = p.GetCustomAttribute<SaveAttribute>();
							if((svattr != null) && !svattr.IsSave) {
								continue;
							}
							dict.Add(p.Name, pv.ToString());
							continue;
						}
						//if((dv != null) && !dv.Equals(pv)) {
						//	save.Append(p.Name).Append(":").AppendLine(pv.ToString());
						//	continue;
						//}
					}
				}

				foreach(var p in argument.GetType().GetProperties()) {
					var svattr = p.GetCustomAttribute<SaveAttribute>();
					var pv = p.GetValue(this.arg, null);
					if((pv != null) && (svattr != null) && svattr.IsSave) {
						if(!dict.ContainsKey(p.Name)) {
							dict.Add(p.Name, pv.ToString());
							continue;
						}
					}
				}

				foreach(var key in  dict.Keys) {
					save.Append(key).Append(":").AppendLine(dict[key]);
				}
				File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.CONFIG_FILE), save.ToString());
			}
			catch(IOException) { }
		}
	}
}
