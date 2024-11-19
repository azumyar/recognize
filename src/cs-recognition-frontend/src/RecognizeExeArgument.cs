using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Windows.Forms.VisualStyles;

namespace Haru.Kei {
	/// <summary>
	/// バッチ引数
	/// 引数の変更はPropertyGrid経由で行うことを想定
	/// </summary>
	[TypeConverter(typeof(DefinitionOrderTypeConverter))]
	internal class RecognizeExeArgument {
		public static readonly int FormatVersion = 2024080400;

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
					// "whisper",
					"faster_whisper",
					"kotoba_whisper",
					"google",
					"google_duplex",
					"google_mix",
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
			// 自由に編集して
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
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
		/// <summary>--translateの選択一覧</summary>
		class ArgTranslateConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"kotoba_whisper",
				};
			}
		}

		/// <summary>--outの選択一覧</summary>
		class ArgHpfConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					HpfArgGenerater.HpfParamater.無効.ToString(),
					HpfArgGenerater.HpfParamater.弱い.ToString(),
					HpfArgGenerater.HpfParamater.普通.ToString(),
					HpfArgGenerater.HpfParamater.強め.ToString(),
				};
			}
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

		/// <summary>--filter_vadの選択一覧</summary>
		class ArgVadConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"0",
					"1",
					"2",
					"3",
				};
			}
		}

		/// <summary>--mehodの選択一覧</summary>
		class ArgSubtitleConverter : SelectableConverter<string> {
			protected override string[] GetItems() {
				return new[] {
					"",
					"obs",
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
					"3",
				};
			}
		}

		protected const string categoryOutput = "00.環境";
		protected const string categoryModel = "01.認識モデル";
		protected const string categoryTranslate = "02.翻訳モデル";
		protected const string categoryMic = "03.マイク";
		protected const string categoryOut = "04.出力";
		protected const string categorySubtitle = "05.字幕";

		[Browsable(false)]
		[Save(IsRestore = false)]
		public int Version {
			get { return FormatVersion; }
		}

		[Category(categoryOutput)]
		[DisplayName("recognize.exeパス")]
		[Description("recognize.exeのパスをフルパスまたは相対パスで指定")]
		[DefaultValue(".\\src\\py-recognition\\dist\\recognize\\recognize.exe")]
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
		[Description("ウィスパーの音声認識モデル(kotoba_whisperは無視します)")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgWhisperModelConverter))]
		[ArgAttribute("--whisper_model", TargetProperty = "ArgMethod", TargetValue = "whisper;faster_whisper")]
		public string ArgWhisperModel { get; set; }

		[Category(categoryModel)]
		[DisplayName("音声認識言語(whisper)")]
		[Description("whisperの音声認識言語(kotoba_whisperは無視します)")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgWhisperLangConverter))]
		[ArgAttribute("--whisper_language", TargetProperty = "ArgMethod", TargetValue = "whisper;faster_whisper")]
		public string ArgWhisperLanguage { get; set; }


		[Category(categoryModel)]
		[DisplayName("音声認識言語(google)")]
		[Description("グーグルの音声認識言語")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgGoogleLangConverter))]
		[ArgAttribute("--google_language", TargetProperty = "ArgMethod", TargetValue = "google;google_duplex;google_mix")]
		public string ArgGoogleLanguage { get; set; }

		[Category(categoryModel)]
		[DisplayName("タイムアウト時間[秒](google)")]
		[Description("グーグルサーバからのタイムアウト時間")]
		[DefaultValue(null)]
		[ArgAttribute("--google_timeout", TargetProperty = "ArgMethod", TargetValue = "google;google_duplex;google_mix")]
		public float? ArgGoogleTimeout { get; set; }

		[Category(categoryModel)]
		[DisplayName("500エラーリトライ(google)")]
		[Description("500エラーでエラーを返さず認識処理を指定した回数実行します")]
		[DefaultValue(null)]
		[ArgAttribute("--google_error_retry", TargetProperty = "ArgMethod", TargetValue = "google;google_duplex;google_mix")]
		public int? ArgGoogleErrorRetry { get; set; }

		[Category(categoryModel)]
		[DisplayName("冒とくフィルタ(google)")]
		[Description("trueにするとgoogleで冒とく的な単語を伏字にします")]
		[DefaultValue(null)]
		[ArgAttribute("--google_profanity_filter", IsFlag = true, TargetProperty = "ArgMethod", TargetValue = "google;google_duplex;google_mix")]
		public bool? ArgGoogleProfanityFilter { get; set; }

		[Category(categoryModel)]
		[DisplayName("並列認識呼び出し(google_duplex)")]
		[Description("認識リクエスト並列で呼び出し500エラーを抑制します")]
		[DefaultValue(null)]
		[ArgAttribute("--google_duplex_parallel", IsFlag = true, TargetProperty = "ArgMethod", TargetValue = "google_duplex")]
		public bool? ArgGoogleDuplexParallelRun { get; set; }

		[Category(categoryTranslate)]
		[DisplayName("翻訳モデル")]
		[Description("β機能")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgTranslateConverter))]
		[ArgAttribute("--translate")]
		public string ArgTranslate { get; set; }


		[Category(categoryMic)]
		[DisplayName("マイクデバイス")]
		[Description("マイクのデバイスIndex\r\nマイクのデバイスリストを見るには--print_micsで実行してください")]
		[DefaultValue(null)]
		[ArgAttribute("--mic")]
		public virtual int? ArgMicV2 { get; set; }


		[Category(categoryMic)]
		[DisplayName("環境音境界値[dB]")]
		[Description("環境音ではないと判断する音圧の境界値。デフォルトでは0が設定されています。お使いのマイクによって感度は異なります。")]
		[DefaultValue(null)]
		[ArgAttribute("--mic_db_threshold")]
		public float? ArgMicDbThresholdV2 { get; set; }


		[Category(categoryMic)]
		[DisplayName("雑音判定の積極性")]
		[Description("VADフィルタの強度を設定します。数値が大きくなるほど積極的に雑音判定します")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgVadConverter))]
		[ArgAttribute("--vad_google_mode")]
		public string ArgVadParamaterV2 { get; set; }

		[Category(categoryMic)]
		[DefaultValue("")]
		[DisplayName("HPFの強さ")]
		[Description("HPFフィルタの強度を設定します。google音声認識を使用する場合無効を推奨します")]
		[ArgAttribute("", IsFlag = true, Generater = typeof(HpfArgGenerater))]
		[TypeConverter(typeof(ArgHpfConverter))]
		public string ArgHpfParamaterV2 { get; set; }


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

		[DisplayName("字幕連携")]
		[DefaultValue(null)]
		[Category(categorySubtitle)]
		[Description("β機能")]
		[TypeConverter(typeof(ArgSubtitleConverter))]
		[ArgAttribute("--subtitle")] 
		public string ArgSubtitle { get; set; }

		[DisplayName("字幕時間[秒]")]
		[DefaultValue(null)]
		[Category(categorySubtitle)]
		[Description("β機能")]
		[ArgAttribute("--subtitle_truncate")]
		public float? ArgSubtitleTruncate { get; set; }

		[DisplayName("Web Socket ポート(OBS)")]
		[DefaultValue(null)]
		[Category(categorySubtitle)]
		[Description("β機能")]
		[ArgAttribute("--subtitle_obs_port", TargetProperty = "ArgSubtitle", TargetValue = "obs")]
		public int? ArgSubtitlePort { get; set; }

		[DisplayName("Web Socket パスワード(OBS)")]
		[DefaultValue(null)]
		[Category(categorySubtitle)]
		[Description("β機能")]
		[ArgAttribute("--subtitle_obs_password", TargetProperty = "ArgSubtitle", TargetValue = "obs")]
		public string ArgSubtitlePassword { get; set; }

		[DisplayName("日本語字幕テキストソース(OBS)")]
		[DefaultValue(null)]
		[Category(categorySubtitle)]
		[Description("β機能")]
		[ArgAttribute("--subtitle_obs_text_ja", TargetProperty = "ArgSubtitle", TargetValue = "obs")]
		public string ArgSubtitleObsTextJa { get; set; }

		[DisplayName("英語字幕テキストソース(OBS)")]
		[DefaultValue(null)]
		[Category(categorySubtitle)]
		[Description("β機能")]
		[ArgAttribute("--subtitle_obs_text_en", TargetProperty = "ArgSubtitle", TargetValue = "obs")]
		public string ArgSubtitleObsTextEn { get; set; }


		[DisplayName("ログレベル")]
		[Description("コンソールに出すログ出力レベルを設定します")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgVerboseConverter))]
		[ArgAttribute("--verbose")]
		public string ArgVerbose { get; set; }

		/* 設定できないほうがいい気がするので保留
		[DisplayName("ログファイル名")]
		[Description("ログファイル名を指定します")]
		[DefaultValue("")]
		[ArgAttribute("--log_file")]
		public string ArgLogFile { get; set; }
		*/

		[DisplayName("ログファイル出力先")]
		[Description("ログファイル出力先フォルダパスを指定します")]
		[DefaultValue("")]
		[ArgAttribute("--log_directory")]
		public string ArgLogDirectory { get; set; }

		[DisplayName("録音")]
		[DefaultValue(null)]
		[Description("録音データを保存する場合trueにします")]
		[ArgAttribute("--record", IsFlag = true)]
		public bool? ArgRecord { get; set; }

		[DisplayName("録音ファイル名")]
		[Description("録音ファイル名を指定します。最終的なファイル名は{指定ファイル名}-{連番}.wavになります。")]
		[DefaultValue("record")]
		[ArgAttribute("--record_file", TargetProperty = "ArgRecord", TargetValue = "true", IgnoreCase = true)]
		public string ArgRecordFile { get; set; }

		[DisplayName("録音格納先")]
		[Description("録音ファイル出力先フォルダパスを指定します")]
		[ArgAttribute("--record_directory", TargetProperty = "ArgRecord", TargetValue = "true", IgnoreCase = true)]
		public string ArgRecordDirectory { get; set; }

		[DisplayName("AIファイル格納先")]
		[Description("AIファイル格納ルートフォルダパスを指定します。このパスの配下に.cacheディレクトリが作られます。")]
		[ArgAttribute("--torch_cache")]
		public string ArgTorchCache { get; set; }

		[DisplayName("自由記入欄")]
		[Description("入力した文字列はコマンド引数末尾に追加されます")]
		[DefaultValue("")]
		public string ExtraArgument { get; set; }

		public RecognizeExeArgument() {
			foreach(var p in this.GetType().GetProperties()) {
				var dva = p.GetCustomAttribute(typeof(DefaultValueAttribute)) as DefaultValueAttribute;
				if(dva != null) {
					p.SetValue(this, dva.Value);
				}
			}
			this.ArgLogDirectory
				= this.ArgTorchCache
				= AppDomain.CurrentDomain.BaseDirectory;
			this.ArgRecordDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Record");
		}

		private static float? Rms2dB(float? rms, float p0 = 1f) {
			if(!rms.HasValue) {
				return null;
			}
			return (float)(20d * Math.Log10(rms.Value / p0));
		}


		private static float? DB2Rms(float? db, float p0 = 1f) {
			if(!db.HasValue) {
				return null;
			}
			return (float)(Math.Pow(10, db.Value / 20f) * p0);
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
		public override int? ArgMicV2 {
			get { return base.ArgMicV2; }
			set { base.ArgMicV2 = value; }
		}

		/// <summary>デバイス名から選べるプロパティ</summary>
		[Category(categoryMic)]
		[DisplayName("マイクデバイス")]
		[Description("")]
		[TypeConverter(typeof(MicDeviceConverter))]
		[DefaultValue("")]
		public string MicDeviceV2 {
			get { return micDevice; }
			set {
				// ArgMicに設定する
				micDevice = value;
				if(!string.IsNullOrEmpty(micDevice)) {
					int r;
					if(int.TryParse(micDevice.Split(' ')[0], out r)) {
						this.ArgMicV2 = r;
					}
				} else {
					this.ArgMicV2 = null;
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
						string s;
						var list = new List<string>() {
							""
						};
						while((s = p.StandardOutput.ReadLine()) != null) {
							list.Add(s);
						}
						p.WaitForExit();
						if(p.ExitCode == 0) {
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

	interface IArgGeneratable {
		string Generate(object o, RecognizeExeArgument arg);
	}

	/// <summary>プロパティを保存するかコントロールする</summary>
	[AttributeUsage(AttributeTargets.Property)]
	class SaveAttribute : Attribute {
		public bool IsSave = true;
		public bool IsRestore = true;
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
		/// <summary>TargetValueの大文字小文字を無視</summary>
		public bool IgnoreCase = false;
		/// <summary>カスタム引数ジェネレータ</summary>
		public Type Generater = null;

		public ArgAttribute(string arg) {
			this.arg = arg;
		}

		public string Generate(object v, RecognizeExeArgument arg) {
			if((v != null) && !"".Equals(v)) {
				if(!string.IsNullOrEmpty(TargetProperty)) {
					Func<object, string> toLower = (x) => x == null ? null : x.ToString().ToLower();
					var pv = arg.GetType().GetProperty(TargetProperty).GetValue(arg, null);
					if(!TargetValue.Split(TargetValueSplit).Any(x => IgnoreCase ? x.ToLower().Equals(toLower(pv)) : x.Equals(pv))) {
						goto end;
					}
				}
				if(Generater != null) {
					var c = Generater.GetConstructor(new Type[0]);
					if(c != null) {
						var gen = c.Invoke(null) as IArgGeneratable;
						if(gen != null) {
							return gen.Generate(v, arg);
						}
					}
					throw new ArgumentException();
				} else if(IsFlag) {
					if(v is bool && (bool)v) {
						return this.arg;
					}
				} else {
					return string.Format("{0} \"{1}{2}\"", this.arg, v, v.ToString().Last() == '\\' ? "\\" : "");
				}
			}
		end:
			return "";
		}
	}

	class HpfArgGenerater : IArgGeneratable {
		public enum HpfParamater {
			無効,
			弱い,
			普通,
			強め
		}

		public string Generate(object o, RecognizeExeArgument arg) {
			var v = o as string;
			if(v != null) {
				if(v == HpfParamater.無効.ToString()) {
					return "";
				} else if(v == HpfParamater.弱い.ToString()) {
					return "--filter_hpf \"80\"";
				} else if(v == HpfParamater.普通.ToString()) {
					return "--filter_hpf \"120\"";
				} else if(v == HpfParamater.強め.ToString()) {
					return "--filter_hpf \"200\"";
				}
				return "";
			}

			throw new ArgumentException();
		}
	}
}