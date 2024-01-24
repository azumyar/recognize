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
	/// <summary>
	/// バッチ引数
	/// 引数の変更はPropertyGrid経由で行うことを想定
	/// </summary>
	[TypeConverter(typeof(DefinitionOrderTypeConverter))]
	internal class RecognizeExeArgument {
		/// <summary>プロパティグリッドのソート順番を宣言順に行う</summary>
		class DefinitionOrderTypeConverter : TypeConverter {
			public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
				var pdc = TypeDescriptor.GetProperties(value, attributes);
				return pdc.Sort(value.GetType().GetProperties().Select(x => x.Name).ToArray());
			}

			public override bool GetPropertiesSupported(ITypeDescriptorContext context) { return true; }
		}

		/// <summary>文字列選択ボックスを出す用の基底</summary>
		/// <typeparam name="T"></typeparam>
		protected abstract class SelectableConverter<T> : StringConverter {
			protected abstract T[] GetItems();
			public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
			public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
				return new StandardValuesCollection(this.GetItems());
			}
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }
		}
		/// <summary>--mehodの選択一覧</summary>
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
		/// <summary>--whisper_modelの選択一覧</summary>
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
		/// <summary>--whisper_languageの選択一覧</summary>
		class ArgWhisperLangConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				// jaだけでいいでしょ
				return new[] {
					"",
					"ja",
				};
			}
			// 自由に編集して
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
		}
		/// <summary>--google_languageの選択一覧</summary>
		class ArgGoogleLangConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				// jaだけでいいでしょ
				return new[] {
					"",
					"ja-JP",
				};
			}
			// 自由に編集して
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
		}
		/// <summary>--outの選択一覧</summary>
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
		/// <summary>--verboseの選択一覧</summary>
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

		protected const string categoryOutput = "00.環境";
		protected const string categoryModel = "01.認識モデル";
		protected const string categoryMic = "02.マイク";
		protected const string categoryOut = "03.出力";
		protected const string categoryFilter = "04.フィルタ";


		[Category(categoryOutput)]
		[DisplayName("recognize.exeパス")]
		[Description("recognize.exeのパスをフルパスまたは相対パスで指定")]
		[DefaultValue(".\\py-recognition\\dist\\recognize\\recognize.exe")]
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
		[ArgAttribute("--google_convert_sampling_rate", IsFlag = true, TargetProperty = "ArgMethod", TargetValue = "google;google_duplex")]
		public bool? ArgGoogleConvertSamplingRate { get; set; }

		[Category(categoryModel)]
		[DisplayName("500エラーリトライ(google)")]
		[Description("500エラーでエラーを返さず認識処理を指定した回数実行します")]
		[DefaultValue(null)]
		[ArgAttribute("--google_error_retry", TargetProperty = "ArgMethod", TargetValue = "google;google_duplex")]
		public int? ArgGoogleErrorRetry { get; set; }

		[Category(categoryModel)]
		[DisplayName("並列認識呼び出し(google_duplex)")]
		[Description("認識リクエスト並列で呼び出し500エラーを抑制します")]
		[DefaultValue(null)]
		[ArgAttribute("--google_duplex_parallel", IsFlag = true, TargetProperty = "ArgMethod", TargetValue = "google_duplex")]
		public bool? ArgGoogleDuplexParallelRun { get; set; }

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
		[ArgAttribute("--mic_dynamic_energy", IsFlag = true)]
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
		[ArgAttribute("--disable_lpf", IsFlag = true)]
		public bool? ArgDisableLpf { get; set; }
		[Category(categoryFilter)]
		[DefaultValue(null)]
		[DisplayName("HPFを無効化")]
		[Description("HPFフィルタを無効にする場合trueにします。google音声認識を使用する場合trueを推奨します")]
		[ArgAttribute("--disable_hpf", IsFlag = true)]
		public bool? ArgDisableHpf { get; set; }

		[DisplayName("ログレベル")]
		[Description("ログ出力レベルを設定します")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgVerboseConverter))]
		[ArgAttribute("--verbose")]
		public string ArgVerbose { get; set; }

		public RecognizeExeArgument() { 
			foreach(var p in this.GetType().GetProperties()) {
				var dva = p.GetCustomAttribute(typeof(DefaultValueAttribute)) as DefaultValueAttribute;
				if(dva != null) {
					p.SetValue(this, dva.Value);
				}
			}
		}
	}

	/// <summary>UIから使うための拡張引数クラス</summary>
	class RecognizeExeArgumentEx : RecognizeExeArgument {
		class MicDeviceConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return s_mic_devices.ToArray();
			}
		}
		private static IEnumerable<string> s_mic_devices;

		private string micDevice = "";

		private RecognizeExeArgumentEx() : base() { }

		// MicDeviceで置き換えるので非表示する
		[Browsable(false)]
		public override int? ArgMic {
			get { return base.ArgMic; }
			set { base.ArgMic = value; }
		}

		/// <summary>デバイス名から選べるプロパティ</summary>
		[Category(categoryMic)]
		[DisplayName("マイクデバイス")]
		[Description("")]
		[TypeConverter(typeof(MicDeviceConverter))]
		[DefaultValue("")]
		public string MicDevice {
			get { return micDevice; }
			set {
				// ArgMicに設定する
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


		public static RecognizeExeArgument Init(string recognizeExe) {
			try {
				if(File.Exists(recognizeExe)) {
					using(var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = recognizeExe,
						Arguments = "--print_mics",
						RedirectStandardOutput = true,
						UseShellExecute = false,
						WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
						CreateNoWindow = true,
					})) {
						p.WaitForExit();
						if(p.ExitCode == 0) {
							string s;
							var list = new List<string>();
							list.Add("");
							while((s = p.StandardOutput.ReadLine()) != null) {
								list.Add(s);
							}
							s_mic_devices = list;
							return new RecognizeExeArgumentEx();
						}
					}
				}
			}
			catch(Exception) {}
			return new RecognizeExeArgument(); // 取得できない場合基底クラスのインスタンスを返す
		}

	}

	/// <summary>プロパティをオプション文字列に変換する</summary>
	[AttributeUsage(AttributeTargets.Property)]
	class ArgAttribute : Attribute {
		private string arg;
		public bool IsFlag = false;

		/// <summary>有効条件のターゲットプロパティ</summary>
		public string TargetProperty;
		/// <summary>有効な値(TargetValueSplit区切り)</summary>
		public string TargetValue;
		/// <summary>配列が使えないのでこの値で区切ることで複数設定</summary>
		public char TargetValueSplit = ';';

		public ArgAttribute(string arg) {
			this.arg = arg;
		}

		public string Generate(object v, RecognizeExeArgument arg) {
			if((v != null) && !"".Equals(v)) {
				if(!string.IsNullOrEmpty(TargetProperty)) {
					var pv = arg.GetType().GetProperty(TargetProperty).GetValue(arg, null);
					if(!TargetValue.Split(TargetValueSplit).Any(x => x.Equals(pv))) {
						goto end;
					}
				}
				if(IsFlag) {
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