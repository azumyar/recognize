using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
using System.Net.NetworkInformation;

namespace Haru.Kei {
	[TypeConverter(typeof(DefinitionOrderTypeConverter))]
	internal class BatArgument {
		class DefinitionOrderTypeConverter : TypeConverter {
			public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
				var pdc = TypeDescriptor.GetProperties(value, attributes);
				return pdc.Sort(value.GetType().GetProperties().Select(x => x.Name).ToArray());
			}

			public override bool GetPropertiesSupported(ITypeDescriptorContext context) { return true; }
		}

		protected abstract class SelectableConverter<T> : StringConverter {
			protected abstract T[] GetItems();
			public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
			public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
				return new StandardValuesCollection(this.GetItems());
			}
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }
		}
		class ArgMethodConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"whisper",
					"faster_whisper",
					"google",
					"google_duplex",
				};
			}
		}
		class ArgWhisperModelConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"tiny",
					"base",
					"small",
					"medium",
					"large",
					"large-v2",
					"large-v3",
				};
			}
		}
		class ArgWhisperLangConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"ja",
				};
			}
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
		}
		class ArgGoogleLangConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"ja-JP",
				};
			}
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
		}
		class ArgOutConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"print",
					"yukarinette",
					"yukacone",
				};
			}
		}
		class ArgVerboseConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"0",
					"1",
					"2",
				};
			}
		}

		protected const string categoryOutput = "00.出力設定";
		protected const string categoryModel = "01.認識モデル";
		protected const string categoryMic = "02.マイク";
		protected const string categoryOut = "03.出力";
		protected const string categoryFilter = "04.フィルタ";

		[Category(categoryOutput)]
		[DisplayName("バッチファイル名")]
		[Description("作成するバッチファイル名")]
		[DefaultValue("execute-recognize.bat")]
		public string BatFile { get; set; }
		[Category(categoryOutput)]
		[DisplayName("出力先")]
		[Description("作成先フォルダパス\r\n(通常変更しません)")]
		[DefaultValue("..\\py-recognition")]
		public string OutputPath { get; set; }
		[Category(categoryOutput)]
		[DisplayName("recognize.exeパス")]
		[Description("バッチファイルからみたrecognize.exeのパス\r\n(通常変更しません)")]
		[DefaultValue(".\\dist\\recognize\\recognize.exe")]
		public string RecognizeExePath { get; set; }


		[Category(categoryModel)]
		[DisplayName("認識モデル")]
		[Description("認識モデル")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgMethodConverter))]
		[ArgAttribute("--method")]
		public string ArgMethod { get; set; }

		[Category(categoryModel)]
		[DisplayName("音声認識モデル(whisper)")]
		[Description("ウィスパーの音声認識モデル")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgWhisperModelConverter))]
		[ArgAttribute("--whisper_model", TargetProperty = "ArgMethod", TargetValue = "whisper;faster_whisper")]
		public string ArgWhisperModel { get; set; }

		[Category(categoryModel)]
		[DisplayName("音声認識言語(whisper)")]
		[Description("whisperの音声認識言語")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgWhisperLangConverter))]
		[ArgAttribute("--whisper_language", TargetProperty = "ArgMethod", TargetValue = "whisper;faster_whisper")]
		public string ArgWhisperLanguage { get; set; }


		[Category(categoryModel)]
		[DisplayName("音声認識言語(google)")]
		[Description("グーグルの音声認識言語")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgGoogleLangConverter))]
		[ArgAttribute("--google_language", TargetProperty = "ArgMethod", TargetValue = "google;google_duplex")]
		public string ArgGoogleLanguage { get; set; }

		[Category(categoryModel)]
		[DisplayName("タイムアウト時間(google)")]
		[Description("グーグルサーバからのタイムアウト時間(秒)")]
		[DefaultValue(null)]
		[ArgAttribute("--google_timeout", TargetProperty = "ArgMethod", TargetValue = "google;google_duplex")]
		public float? ArgGoogleTimeout { get; set; }

		[Category(categoryModel)]
		[DisplayName("サンプル周波数16k変換(google)")]
		[Description("trueにすると16kに変換してgoogleサーバに送信します。データサイズの減量を狙います。")]
		[DefaultValue(null)]
		[ArgAttribute("--google_convert_sampling_rate", isFlag:true, TargetProperty = "ArgMethod", TargetValue = "google;google_duplex")]
		public bool? ArgGoogleConvertSamplingRate { get; set; }


		[Category(categoryMic)]
		[DisplayName("マイクデバイス")]
		[Description("マイクのデバイスIndex\r\nマイクのデバイスリストを見るには--print_micsで実行してください")]
		[DefaultValue(null)]
		[ArgAttribute("--mic")]
		public virtual int? ArgMic { get; set; }

		[Category(categoryMic)]
		[DisplayName("無音レベルの閾値")]
		[Description("無音ではないと判断するマイクの閾値。デフォルトでは300が設定されています。お使いのマイクによって感度は異なります。")]
		[DefaultValue(null)]
		[ArgAttribute("--mic_energy")]
		public float? ArgMicEnergy { get; set; }

		[Category(categoryMic)]
		[DisplayName("動的マイク感度の変更")]
		[Description("trueの場合周りの騒音に応じて動的にマイクの感度を変更します")]
		[DefaultValue(null)]
		[ArgAttribute("--mic_dynamic_energy", isFlag:true)]
		public bool? ArgMicDynamicEnergy { get; set; }

		[Category(categoryMic)]
		[DisplayName("無音時間閾値")]
		[Description("この時間無音であるばあいしゃべり終わったと判断します(秒)")]
		[DefaultValue(null)]
		[ArgAttribute("--mic_pause")]
		public float? ArgMicPause { get; set; }

		[Category(categoryOut)]
		[DisplayName("認識結果出力先")]
		[Description("認識結果出力先")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgOutConverter))]
		[ArgAttribute("--out")]
		public string ArgOut { get; set; }
		[Category(categoryOut)]
		[DisplayName("ゆかりねっと外部連携ポート")]
		[Description("ゆかりねっとの外部連携ポートを指定します")]
		[DefaultValue(null)]
		[ArgAttribute("--out_yukarinette", TargetProperty = "ArgOut", TargetValue = "yukarinette")]
		public int? ArgOutYukarinette { get; set; }
		[DisplayName("ゆかコネNEO外部連携ポート")]
		[DefaultValue(null)]
		[Category(categoryOut)]
		[Description("ゆかコネNEOのウェブソケットポートを指定します。\r\n通常自動的に取得するため必要ありません")]
		[ArgAttribute("--out_yukacone", TargetProperty = "ArgOut", TargetValue = "yukacone")]
		public int? ArgOutYukacone { get; set; }

		[Category(categoryFilter)]
		[DisplayName("LPFを無効化")]
		[DefaultValue(null)]
		[Description("LPFフィルタを無効にする場合trueにします。google音声認識を使用する場合trueを推奨します")]
		[ArgAttribute("--disable_lpf", isFlag:true)]
		public bool? ArgDisableLpf { get; set; }
		[Category(categoryFilter)]
		[DefaultValue(null)]
		[DisplayName("HPFを無効化")]
		[Description("HPFフィルタを無効にする場合trueにします。google音声認識を使用する場合trueを推奨します")]
		[ArgAttribute("--disable_hpf", isFlag:true)]
		public bool? ArgDisableHpf { get; set; }

		[DisplayName("ログレベル")]
		[Description("ログ出力レベルを設定します")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgVerboseConverter))]
		[ArgAttribute("--verbose")]
		public string ArgVerbose { get; set; }

		public BatArgument() { 
			foreach(var p in this.GetType().GetProperties()) {
				var dva = p.GetCustomAttribute(typeof(DefaultValueAttribute)) as DefaultValueAttribute;
				if(dva != null) {
					p.SetValue(this, dva.Value);
				}
			}
		}
	}

	class BatArgumentEx : BatArgument {
		class MicDeviceConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return s_mic_devices.ToArray();
			}
		}
		private static IEnumerable<string> s_mic_devices;

		private string micDevice = "";

		private BatArgumentEx() : base() { }

		[Browsable(false)]
		public override int? ArgMic {
			get { return base.ArgMic; }
			set { base.ArgMic = value; }
		}

		[Category(categoryMic)]
		[DisplayName("マイクデバイス")]
		[Description("")]
		[TypeConverter(typeof(MicDeviceConverter))]
		[DefaultValue("")]
		public string MicDevice {
			get { return micDevice; }
			set {
				micDevice = value;
				if(!string.IsNullOrEmpty(micDevice)) {
					int r;
					if(int.TryParse(micDevice.Split(' ')[0], out r)) {
						this.ArgMic = r;
					}
				} else {
					this.ArgMic = null;
				}
			}
		}


		public static BatArgument Init(string recognizeExe) {
			try {
				if(File.Exists(recognizeExe)) {
					var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = recognizeExe,
						Arguments = "--print_mics",
						RedirectStandardOutput = true,
						UseShellExecute = false,
						WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
						CreateNoWindow = true,
					});
					p.WaitForExit();
					if(p.ExitCode == 0) {
						string s;
						var list = new List<string>();
						list.Add("");
						while((s = p.StandardOutput.ReadLine()) != null) {
							list.Add(s);
						}
						s_mic_devices = list;
						return new BatArgumentEx();
					}
				}
			}
			catch(Exception) {}
			return new BatArgument();
		}

	}

	[AttributeUsage(AttributeTargets.Property)]
	class ArgAttribute : Attribute {
		private string arg;
		private bool isFlag;

		public string TargetProperty;
		public string TargetValue;
		public char TargetValueSplit = ';';

		public ArgAttribute(string arg, bool isFlag = false) {
			this.arg = arg;
			this.isFlag = isFlag;
		}

		public string Gen(object v, BatArgument arg) {
			if((v != null) && !"".Equals(v)) {
				if(!string.IsNullOrEmpty(TargetProperty)) {
					var pv = arg.GetType().GetProperty(TargetProperty).GetValue(arg, null);
					if(!TargetValue.Split(TargetValueSplit).Any(x => x.Equals(pv))) {
						goto end;
					}
				}
				if(isFlag) {
					if(v is bool && (bool)v) {
						return this.arg;
					}
				} else {
					return string.Format("{0} {1}", this.arg, v);
				}
			}
		end:
			return "";
		}
	}

}