using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Reactive.Bindings;

namespace Haru.Kei.Models;

[JsonObject]
public class Config {
	public const int CurrentVersion = 2025071400;

	[JsonProperty("version")]
	public int Version { get; private set; } = CurrentVersion;
	[JsonProperty("method")]
	public string TranscribeModel { get; set; } = "";
	[JsonProperty("google_language")]
	public string GoogleLanguage { get; set; } = "";
	[JsonProperty("google_timeout")]
	public float? GoogleTimeout { get; set; }
	[JsonProperty("google_profanity_filter")]
	public bool GoogleProfanityFilter { get; set; }
	[JsonProperty("translate")]
	public string TranslateModel { get; set; } = "";

	// マイク
	[JsonProperty("mic")]
	public int? Microphone { get; set; } = null;
	[JsonProperty("mic_db_threshold")]
	public float? MicrophoneThresholdDb { get; set; } = null;
	[JsonProperty("mic_record_min_duration")]
	public float? MicrophoneRecordMinDuration { get; set; } = null;
	[JsonProperty("vad_google_mode")]
	public int? VadGoogleParamater { get; set; } = null;
	[JsonProperty("filter_hpf")]
	public int? HpfParamater { get; set; } = null;

	// ゆかりねっと連携
	[JsonProperty("out:yukarinette")]
	public bool IsUsedYukarinette { get; set; } = false;
	[JsonProperty("out_yukarinette")]
	public int? YukatinettePort { get; set; } = null;

	// ゆかこね連携
	[JsonProperty("out:yukacone")]
	public bool IsUsedYukaCone { get; set; } = false;
	[JsonProperty("out_yukacone")]
	public int? YukaConePort { get; set; } = null;

	// 字幕
	[JsonProperty("out:obs")]
	public bool IsUsedObsSubtitle { get; set; } = false;
	[JsonProperty("out_obs_truncate")]
	public float? ObsSubtitleTruncate { get; set; } = null;
	[JsonProperty("out_obs_text_ja")]
	public string ObsSubtitleTextJp { get; set; } = "";
	[JsonProperty("out_obs_text_en")]
	public string ObsSubtitleTextEn { get; set; } = "";
	[JsonProperty("out_obs_port")]
	public int? ObsSubtitlePort { get; set; } = null;
	[JsonProperty("out_obs_password")]
	public string ObsSubtitlePassword { get; set; } = "";
	[JsonProperty("out:vrc")]
	public bool IsUsedVrcSubtitle { get; set; } = false;

	// illuminate
	[JsonProperty("out:illuminate")]
	public bool IsUsedIlluminate { get; set; } = false;
	[JsonProperty("out_illuminate_voice")]
	public string IlluminateVoice { get; set; } = "voiceroid";

	[JsonProperty("out_illuminate_client:voiceroid")]
	public string IlluminateClientVoiceRoid { get; set; } = "";
	[JsonProperty("out_illuminate_client:voiceroid2")]
	public string IlluminateClientVoiceRoid2 { get; set; } = "";
	[JsonProperty("out_illuminate_client:voicepeak")]
	public string IlluminateClientVoicePeak { get; set; } = "";
	[JsonProperty("out_illuminate_client:aivoice")]
	public string IlluminateClientAiVoice { get; set; } = "";
	[JsonProperty("out_illuminate_client:aivoice2")]
	public string IlluminateClientAiVoice2 { get; set; } = "";
	[JsonProperty("out_illuminate_kana:voiceroid")]
	public bool IlluminateKanaVoiceRoid { get; set; } = false;
	[JsonProperty("out_illuminate_kana:voiceroid2")]
	public bool IlluminateKanaVoiceRoid2 { get; set; } = false;
	[JsonProperty("out_illuminate_kana:voicepeak")]
	public bool IlluminateKanaVoicePeak { get; set; } = true;
	[JsonProperty("out_illuminate_kana:aivoice")]
	public bool IlluminateKanaAiVoice { get; set; } = false;
	[JsonProperty("out_illuminate_kana:aivoice2")]
	public bool IlluminateKanaAiVoice2 { get; set; } = false;


	[JsonProperty("user_args")]
	public string UserArguments { get; set; } = "";


	[JsonObject]
	[TypeConverter(typeof(DefinitionOrderTypeConverter))]
	public class RecognizeExeArgument {
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

		protected const string category00 = "00.環境";
		protected const string category01 = "01.illuminate";

		[Category(category00)]
		[DisplayName("recognize.exeパス")]
		[Description("recognize.exeのパスをフルパスまたは相対パスで指定")]
		[DefaultValue(@".\src\\py-recognition\dist\recognize\recognize.exe")]
		[JsonProperty("recognize_exe")]
		public string RecognizeExePath { get; set; } = "";

		[Category(category00)]
		[DisplayName("illuminate.exeパス")]
		[Description("illuminate.exeのパスをフルパスまたは相対パスで指定")]
		[DefaultValue(@".\src\\cs-illuminate\dist\illuminate.exe")]
		[JsonProperty("out_illuminate_exe")]
		public string IlluminateExePath { get; set; } = "";

		[Category(category01)]
		[DisplayName("ポート")]
		[Description("illuminateのTCP待受ポートを指定します")]
		[DefaultValue(null)]
		[JsonProperty("out_illuminate_port")]
		public int? IlluminatePort { get; set; } = default;

		[Category(category01)]
		[DisplayName("通知領域")]
		[Description("illuminateは通知領域に常駐します")]
		[DefaultValue(null)]
		[JsonProperty("out_illuminate_notify_icon")]
		public bool? IlluminateNotifyIcon { get; set; } = default;

		[DisplayName("ログレベル")]
		[Description("コンソールに出すログ出力レベルを設定します")]
		[DefaultValue("")]
		[TypeConverter(typeof(ArgVerboseConverter))]
		[JsonProperty("verbose")]
		public string ArgVerbose { get; set; } = "";

		/* 設定できないほうがいい気がするので保留
		[DisplayName("ログファイル名")]
		[Description("ログファイル名を指定します")]
		[DefaultValue("")]
		[ArgAttribute("--log_file")]
		public string ArgLogFile { get; set; } = "";
		*/

		[DisplayName("ログファイル出力先")]
		[Description("ログファイル出力先フォルダパスを指定します")]
		[DefaultValue("")]
		[JsonProperty("log_directory")]
		public string ArgLogDirectory { get; set; } = "";

		[DisplayName("録音")]
		[DefaultValue(null)]
		[Description("録音データを保存する場合trueにします")]
		[JsonProperty("record")]
		public bool? ArgRecord { get; set; } = null;

		[DisplayName("録音ファイル名")]
		[Description("録音ファイル名を指定します。最終的なファイル名は{指定ファイル名}-{連番}.wavになります。")]
		[DefaultValue("record")]
		[JsonProperty("record_file")]
		public string ArgRecordFile { get; set; } = "";

		[DisplayName("録音格納先")]
		[Description("録音ファイル出力先フォルダパスを指定します")]
		[JsonProperty("record_directory")]
		public string ArgRecordDirectory { get; set; } = "";

		[DisplayName("AIファイル格納先")]
		[Description("AIファイル格納ルートフォルダパスを指定します。このパスの配下に.cacheディレクトリが作られます。")]
		[JsonProperty("torch_cache")]
		public string ArgTorchCache { get; set; } = "";

		/*
		[DisplayName("自由記入欄")]
		[Description("入力した文字列はコマンド引数末尾に追加されます")]
		[DefaultValue("")]
		[JsonProperty("user_args")]
		public string ExtraArgument { get; set; } = "";
		*/
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
	}

	public RecognizeExeArgument Extra { get; private set; } = new();

	public const string TranscribeModelWhisper = "kotoba_whisper";
	public const string TranscribeModelGoogle = "google_mix";

	public const string VoiroVoiceRoid = "voiceroid";
	public const string VoiroVoiceRoid2 = "voiceroid2";
	public const string VoiroAiVoice = "aivoice";
	public const string VoiroAiVoice2 = "aivoice2";
	public const string VoiroVoicePeak = "voicepeak";

	public string ToCommandOption() {
		static StringBuilder arg<T>(StringBuilder opt, string name, T? val) {
			if(val switch {
				string v => !string.IsNullOrEmpty(v),
				var v => v != null,
			}) {
				if(val is bool v1) {
					if(v1) {
						return opt.Append($"{name} ");
					}
				} else {
					if($"{val}".ToString().Last() == '\\') {
						return opt.Append($"{name} \"{val}\\\" ");
					} else {
						return opt.Append($"{name} \"{val}\" ");
					}
				}
			}
			return opt;
		}

		var opt = new StringBuilder();
		if(!string.IsNullOrEmpty(this.TranscribeModel)) {
			opt.Append($"--method \"{this.TranscribeModel}\" ");
			if(this.TranscribeModel == TranscribeModelGoogle) {
				arg(opt, "--google_language", this.GoogleLanguage);
				arg(opt, "--google_timeout", this.GoogleTimeout);
				arg(opt, "--google_profanity_filter", this.GoogleProfanityFilter);
			}
		}
		arg(opt, "--translate", this.TranslateModel);

		arg(opt, "--mic", this.Microphone);
		arg(opt, "--mic_db_threshold", this.MicrophoneThresholdDb);
		arg(opt, "--mic_record_min_duration", this.MicrophoneRecordMinDuration);
		arg(opt, "--vad_google_mode", this.VadGoogleParamater);
		arg(opt, "--filter_hpf", this.HpfParamater);

		opt.Append($"--out \"print\" "); // ないと本体が起動しないのでいったん付与する
		if(this.IsUsedYukarinette) {
			opt.Append($"--out \"yukarinette\" ");
			arg(opt, "--out_yukarinette", this.YukatinettePort);
		}
		if(this.IsUsedYukaCone) {
			opt.Append($"--out \"yukacone\" ");
			arg(opt, "--out_yukacone", this.YukaConePort);
		}

		if(this.IsUsedObsSubtitle) {
			opt.Append($"--out \"obs\" ");
			arg(opt, "--out_obs_truncate", this.ObsSubtitleTruncate);
			arg(opt, "--out_obs_port", this.ObsSubtitlePort);
			arg(opt, "--out_obs_password", this.ObsSubtitlePassword);
			arg(opt, "--out_obs_text_ja", this.ObsSubtitleTextJp);
			arg(opt, "--out_obs_text_en", this.ObsSubtitleTextEn);
		}

		if(this.IsUsedVrcSubtitle) {
			opt.Append($"--out \"vrc\" ");
		}

		if(this.IsUsedIlluminate) {
			opt.Append($"--out \"illuminate\" ");
			if(Helpers.Util.IsFullPath(this.Extra.IlluminateExePath)) {
				var p = Path.Combine(AppContext.BaseDirectory, this.Extra.IlluminateExePath);
				arg(opt, "--out_illuminate_exe", p);
			} else {
				arg(opt, "--out_illuminate_exe", this.Extra.IlluminateExePath);
			}
			arg(opt, "--out_illuminate_voice", this.IlluminateVoice);
			arg(opt, "--out_illuminate_client", this.IlluminateVoice switch {
				VoiroVoiceRoid => this.IlluminateClientVoiceRoid,
				VoiroVoiceRoid2 => this.IlluminateClientVoiceRoid2,
				VoiroVoicePeak => this.IlluminateClientVoicePeak,
				VoiroAiVoice => this.IlluminateClientAiVoice,
				VoiroAiVoice2 => this.IlluminateClientAiVoice2,
				_ => throw new NotImplementedException($"--out_illuminate_voice {this.IlluminateVoice}")
			});
			arg(opt, "--out_illuminate_kana", this.IlluminateVoice switch {
				VoiroVoiceRoid => this.IlluminateKanaVoiceRoid,
				VoiroVoiceRoid2 => this.IlluminateKanaVoiceRoid2,
				VoiroVoicePeak => this.IlluminateKanaVoicePeak,
				VoiroAiVoice => this.IlluminateKanaAiVoice,
				VoiroAiVoice2 => this.IlluminateKanaAiVoice2,
				_ => throw new NotImplementedException($"--out_illuminate_voice {this.IlluminateVoice}")
			});

			arg(opt, "--out_illuminate_port", this.Extra.IlluminatePort);
			arg(opt, "--out_illuminate_notify_icon", this.Extra.IlluminateNotifyIcon);
		}

		arg(opt, "--verbose", this.Extra.ArgVerbose);
		arg(opt, "--log_directory", this.Extra.ArgLogDirectory);
		if(this.Extra.ArgRecord.HasValue) {
			if(this.Extra.ArgRecord.Value) {
				opt.Append($"--record ");
				arg(opt, "--record_file", this.Extra.ArgRecordFile);
				arg(opt, "--record_directory", this.Extra.ArgRecordDirectory);
			}
		}
		arg(opt, "--torch_cache", this.Extra.ArgTorchCache);

		if(!string.IsNullOrEmpty(this.UserArguments)) {
			opt.Append(string.Join(
				" ",
				this.UserArguments
					.Replace("\r\n", "\n")
					.Split('\n')));
		}

		return opt.ToString();
	}
}


public class ConfigBinder : INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	private readonly string[] TranscribeModels = {
		"設定しない",
		"AI音声認識",
		"google音声認識",
	};
	public const int TranscribeIndexNull = 0;
	public const int TranscribeIndexAi = 1;
	public const int TranscribeIndexGoogle = 2;
	private readonly string[] TranslateModels = {
		"設定しない",
		"AI翻訳",
	};
	public const int TranslateIndexNull = 0;
	public const int TranslateIndexAi = 1; 
	private readonly string[] VadGoogleParamaters = {
		"設定しない",
		"0",
		"1",
		"2",
		"3",
	};
	public const int VadGoogleLevel0 = 1;
	public const int VadGoogleLevel1 = 2;
	public const int VadGoogleLevel2 = 3;
	public const int VadGoogleLevel3 = 4;
	private readonly string[] HpfParamaters = {
		"設定しない",
		"無効",
		"弱い",
		"普通",
		"強め",
	};
	public const int HpfIndexNull = 0;
	public const int HpfIndexDisable = 1;
	public const int HpfIndexLow = 2;
	public const int HpfIndexNormal = 3;
	public const int HpfIndexHi = 4;
	public const int HpfParamDisable = 0;
	public const int HpfParamLow = 80;
	public const int HpfParamNormal = 120;
	public const int HpfParamHi = 200;
	private readonly string[] IlluminateVoices = {
		"VOICEROID1/PLUS/EX",
		"VOICEROID2",
		"VOICEPEAK",
		"A.I.VOICE",
		"A.I.VOICE2",
	};
	public const int VoiceIndexVoiceRoid = 0;
	public const int VoiceIndexVoiceRoid2 = 1;
	public const int VoiceIndexVoicePeak = 2;
	public const int VoiceIndexAiVoice = 3;
	public const int VoiceIndexAiVoice2 = 4;

	// モデル
	public ReactiveCollection<string> TranscribeModelsBinder { get; }
	public ReactiveProperty<int> TranscribeModeIndex { get; }
	public ReactiveProperty<string> GoogleLanguageBinding { get; }
	public ReactiveProperty<string> GoogleTimeoutBinding { get; set; }
	public ReactiveProperty<bool> GoogleProfanityFilterBinder { get; set; }
	public ReactiveCollection<string> TranslateModelsBinder { get; set; }
	public ReactiveProperty<int> TranslateModelIndex { get; }
	public ReactiveProperty<Visibility> GoogleItemVisibility { get; }
	public ReactiveProperty<Visibility> GoogleTimeoutError { get; }

	// マイク
	public ReactiveCollection<string> MicDevicesBinder { get; }
	public ReactiveProperty<int> MicDeviceIndex { get; }
	public ReactiveProperty<string> MicrophoneThresholdDbBinder { get; }
	public ReactiveProperty<string> MicrophoneRecordMinDurationBinder { get; }
	public ReactiveCollection<string> VadGoogleParamatersBinder { get; }
	public ReactiveProperty<int> VadGoogleParamaterIndex { get; }
	public ReactiveCollection<string> HpfParamatersBinder { get; }
	public ReactiveProperty<int> HpfParamaterIndex { get; }
	public ReactiveProperty<Visibility> MicrophoneThresholdDbError { get; }
	public ReactiveProperty<Visibility> MicrophoneRecordMinDurationError { get; }

	// ゆかりねっと連携
	public ReactiveProperty<bool> IsUsedYukarinetteBinding { get; }
	public ReactiveProperty<string> YukarinettePortBinding { get; }
	public ReactiveProperty<Visibility> YukarinettePortError { get; }

	// ゆかこね連携
	public ReactiveProperty<bool> IsUsedYukaConeBinding { get; }
	public ReactiveProperty<string> YukaConePortBinding { get; }
	public ReactiveProperty<Visibility> YukaConePortError { get; }

	// 字幕
	public ReactiveProperty<bool> IsUsedObsSubtitleBinder { get; }
	public ReactiveProperty<string> ObsSubtitleTruncateBinder { get; }
	public ReactiveProperty<string> ObsSubtitleTextJpBinder { get; }
	public ReactiveProperty<string> ObsSubtitleTextEnBinder { get; }
	public ReactiveProperty<string> ObsSubtitlePortBinder { get; }
	public ReactiveProperty<string> ObsSubtitlePasswordBinder { get; }
	public ReactiveProperty<bool> IsUsedVrcSubtitleBinder { get; }
	public ReactiveProperty<Visibility> ObsSubtitleTruncateError { get; }
	public ReactiveProperty<Visibility> ObsSubtitlePortError { get; }


	// ボイロ連携
	public ReactiveProperty<bool> IsUsedIlluminateBinding { get; }
	public ReactiveCollection<string> IlluminateVoiceBinding { get; }
	public ReactiveProperty<int> IlluminateVoiceIndex { get; }
	public ReactiveProperty<string> IlluminateClientBinding { get; }

	// 自由記入欄
	public ReactiveProperty<string> UserArgumentsBinding { get; }

	public ConfigBinder(Config config) {
		// モデル
		this.TranscribeModelsBinder = new();
		this.TranscribeModelsBinder.AddRangeOnScheduler(TranscribeModels);
		this.TranscribeModeIndex = new(initialValue: config.TranscribeModel switch {
			"kotoba_whisper" => TranscribeIndexAi,
			"google_mix" => TranscribeIndexGoogle,
			_ => TranscribeIndexNull
		});
		this.TranscribeModeIndex.Subscribe(x => config.TranscribeModel = x switch {
			TranscribeIndexAi => "kotoba_whisper",
			TranscribeIndexGoogle => "google_mix",
			_ => ""
		});
		this.GoogleLanguageBinding = new(initialValue: config.GoogleLanguage);
		this.GoogleLanguageBinding.Subscribe(x => config.GoogleLanguage = x);
		this.GoogleTimeoutBinding = new(initialValue: this.ToString(config.GoogleTimeout));
		this.GoogleTimeoutBinding.Subscribe(x => config.GoogleTimeout = this.ToFloat(x));
		this.GoogleProfanityFilterBinder = new(initialValue: config.GoogleProfanityFilter);

		this.TranslateModelsBinder = new();
		this.TranslateModelsBinder.AddRangeOnScheduler(TranslateModels);
		this.TranslateModelIndex = new(initialValue: config.TranslateModel switch {
			"kotoba_whisper" => TranslateIndexAi,
			_ => TranslateIndexNull
		});
		this.TranslateModelIndex.Subscribe(x => config.TranslateModel = x switch {
			TranslateIndexAi => "kotoba_whisper",
			_ => "",
		});

		this.GoogleItemVisibility = this.TranscribeModeIndex
			.Select(x => x switch {
				TranscribeIndexGoogle => Visibility.Visible,
				_ => Visibility.Hidden,
			}).ToReactiveProperty();
		this.GoogleTimeoutError = this.GoogleTimeoutBinding
			.Select(x => this.ToFloatError(x))
			.ToReactiveProperty();

		// マイク
		this.MicDevicesBinder = new();
		this.MicDeviceIndex = new(initialValue: config.Microphone switch {
			int v => v + 1,
			_ => 0
		});
		this.MicDeviceIndex.Subscribe(x => config.Microphone = (x - 1) switch {
			int v when(0 <= v) => v,
			_ => null
		});
		this.MicrophoneThresholdDbBinder = new(initialValue: this.ToString(config.MicrophoneThresholdDb));
		this.MicrophoneThresholdDbBinder.Subscribe(x => {
			config.MicrophoneThresholdDb = this.ToFloat(x);
		});
		this.MicrophoneRecordMinDurationBinder = new(initialValue: this.ToString(config.MicrophoneRecordMinDuration));
		this.MicrophoneRecordMinDurationBinder.Subscribe(x => {
			config.MicrophoneRecordMinDuration = this.ToFloat(x);
		});
		this.VadGoogleParamatersBinder = new();
		this.VadGoogleParamatersBinder.AddRangeOnScheduler(this.VadGoogleParamaters);
		this.VadGoogleParamaterIndex = new(initialValue: config.VadGoogleParamater switch {
			int v => v + 1,
			_ => 0
		});
		this.HpfParamatersBinder = new();
		this.HpfParamatersBinder.AddRangeOnScheduler(this.HpfParamaters);
		this.HpfParamaterIndex = new(initialValue: config.HpfParamater switch {
			int v when(HpfParamDisable <= v) && (v < HpfParamLow) => HpfIndexDisable,
			int v when(HpfParamLow <= v) && (v < HpfParamNormal) => HpfIndexLow,
			int v when(HpfParamNormal <= v) && (v < HpfParamHi) => HpfIndexNormal,
			int v when(HpfParamHi <= v) => HpfIndexHi,
			_ => HpfIndexNull
		});
		this.HpfParamaterIndex.Subscribe(x => config.HpfParamater = x switch {
			HpfIndexDisable => HpfParamDisable,
			HpfIndexLow => HpfParamLow,
			HpfIndexNormal => HpfParamNormal,
			HpfIndexHi => HpfParamHi,
			_ => null
		});
		this.MicrophoneThresholdDbError = this.MicrophoneThresholdDbBinder
			.Select(x => this.ToFloatError(x))
			.ToReactiveProperty();
		this.MicrophoneRecordMinDurationError = this.MicrophoneRecordMinDurationBinder
			.Select(x => this.ToFloatError(x))
			.ToReactiveProperty();

		// ゆかりねっと連携
		this.IsUsedYukarinetteBinding = new(initialValue: config.IsUsedYukarinette);
		this.IsUsedYukarinetteBinding.Subscribe(x => config.IsUsedYukarinette = x);
		this.YukarinettePortBinding = new(initialValue: ToString(config.YukatinettePort));
		this.YukarinettePortBinding.Subscribe(x => config.YukatinettePort = ToInt(x));
		this.YukarinettePortError = this.YukarinettePortBinding
			.Select(x => ToIntError(x))
			.ToReactiveProperty();

		this.IsUsedYukaConeBinding = new(initialValue: config.IsUsedYukaCone);
		this.IsUsedYukaConeBinding.Subscribe(x => config.IsUsedYukaCone = x);
		this.YukaConePortBinding = new(initialValue: ToString(config.YukaConePort));
		this.YukaConePortBinding.Subscribe(x => config.YukaConePort = ToInt(x));
		this.YukaConePortError = this.YukaConePortBinding
			.Select(x => ToIntError(x))
			.ToReactiveProperty();

		// 字幕
		this.IsUsedObsSubtitleBinder = new(initialValue: config.IsUsedObsSubtitle);
		this.IsUsedObsSubtitleBinder.Subscribe(x => config.IsUsedObsSubtitle = x);
		this.ObsSubtitleTruncateBinder = new(initialValue: ToString(config.ObsSubtitleTruncate));
		this.ObsSubtitleTruncateBinder.Subscribe(x => config.ObsSubtitleTruncate = ToFloat(x));
		this.ObsSubtitleTruncateError = this.ObsSubtitleTruncateBinder
			.Select(x => ToFloatError(x))
			.ToReactiveProperty();
		this.ObsSubtitleTextJpBinder = new(initialValue: config.ObsSubtitleTextJp);
		this.ObsSubtitleTextJpBinder.Subscribe(x => config.ObsSubtitleTextJp = x);
		this.ObsSubtitleTextEnBinder = new(initialValue: config.ObsSubtitleTextEn);
		this.ObsSubtitleTextEnBinder.Subscribe(x => config.ObsSubtitleTextEn = x);
		this.ObsSubtitlePortBinder = new(initialValue: ToString(config.ObsSubtitlePort));
		this.ObsSubtitlePortBinder.Subscribe(x => config.ObsSubtitlePort = ToInt(x));
		this.ObsSubtitlePortError = this.ObsSubtitlePortBinder
			.Select(x => ToIntError(x))
			.ToReactiveProperty();
		this.ObsSubtitlePasswordBinder = new(initialValue: config.ObsSubtitlePassword);
		this.ObsSubtitlePasswordBinder.Subscribe(x => config.ObsSubtitlePassword = x);
		this.IsUsedVrcSubtitleBinder = new(initialValue: config.IsUsedVrcSubtitle);
		this.IsUsedVrcSubtitleBinder.Subscribe(x => config.IsUsedVrcSubtitle = x);

		// ボイロ連携
		this.IsUsedIlluminateBinding = new(initialValue: config.IsUsedIlluminate);
		this.IsUsedIlluminateBinding.Subscribe(x => config.IsUsedIlluminate = x);
		this.IlluminateVoiceBinding = new();
		this.IlluminateVoiceBinding.AddRangeOnScheduler(IlluminateVoices);
		this.IlluminateVoiceIndex = new(initialValue: config.IlluminateVoice switch {
			Config.VoiroVoiceRoid => VoiceIndexVoiceRoid,
			Config.VoiroVoiceRoid2 => VoiceIndexVoiceRoid2,
			Config.VoiroVoicePeak => VoiceIndexVoicePeak,
			Config.VoiroAiVoice => VoiceIndexAiVoice,
			Config.VoiroAiVoice2 => VoiceIndexAiVoice2,
			_ => VoiceIndexVoiceRoid
		});
		this.IlluminateVoiceIndex.Subscribe(x => {
			(string Voice, string Client) v = x switch {
				VoiceIndexVoiceRoid2 => (Config.VoiroVoiceRoid2, config.IlluminateClientVoiceRoid2),
				VoiceIndexVoicePeak => (Config.VoiroVoicePeak, config.IlluminateClientVoicePeak),
				VoiceIndexAiVoice => (Config.VoiroAiVoice, config.IlluminateClientAiVoice),
				VoiceIndexAiVoice2 => (Config.VoiroAiVoice2, config.IlluminateClientAiVoice2),
				_ => (Config.VoiroVoiceRoid, config.IlluminateClientVoiceRoid),
			};
			config.IlluminateVoice = v.Voice;
		});
		this.IlluminateClientBinding = this.IlluminateVoiceIndex.Select(x => {
			(string Voice, string Client) v = x switch {
				VoiceIndexVoiceRoid2 => (Config.VoiroVoiceRoid2, config.IlluminateClientVoiceRoid2),
				VoiceIndexVoicePeak => (Config.VoiroVoicePeak, config.IlluminateClientVoicePeak),
				VoiceIndexAiVoice => (Config.VoiroAiVoice, config.IlluminateClientAiVoice),
				VoiceIndexAiVoice2 => (Config.VoiroAiVoice2, config.IlluminateClientAiVoice2),
				_ => (Config.VoiroVoiceRoid, config.IlluminateClientVoiceRoid),
			};
			return v.Client;
		}).ToReactiveProperty<string>();
		this.IlluminateClientBinding.Subscribe(x => {
			Action<string> aply = this.IlluminateVoiceIndex.Value switch {
				VoiceIndexVoiceRoid2 => (y) => config.IlluminateClientVoiceRoid2 = y,
				VoiceIndexVoicePeak => (y) => config.IlluminateClientVoicePeak = y,
				VoiceIndexAiVoice => (y) => config.IlluminateClientAiVoice = y,
				VoiceIndexAiVoice2 => (y) => config.IlluminateClientAiVoice2 = y,
				_ => (y) => config.IlluminateClientVoiceRoid = y,
			};
			aply(x);
		});

		/*
		this.IlluminateClientBinding = new(initialValue: config.IlluminateClient);
		this.IlluminateClientBinding.Subscribe(x => config.IlluminateClient = x);
		*/

		this.IlluminateClientDialogFilter = this.IlluminateVoiceIndex.Select(x => x switch {
			VoiceIndexVoiceRoid => "VOICEROID|VOICEROID.exe",
			VoiceIndexVoiceRoid2 => "VOICEROID2|VoiceroidEditor.exe",
			VoiceIndexVoicePeak => "VOICEPEAK|voicepeak.exe",
			VoiceIndexAiVoice => "A.I.VOICE|AIVoiceEditor.exe",
			VoiceIndexAiVoice2 => "A.I.VOICE2|aivoice.exe",
			_ => "",
		}).Select(x => $"{x}|すべてのファイル(*.*)|*.*").ToReactiveProperty<string>();
		this.IlluminateClientDialogDirectory = this.IlluminateVoiceIndex.Select(x => x switch {
			VoiceIndexVoiceRoid => @"C:\Program Files (x86)\AHS\",
			VoiceIndexVoiceRoid2 => @"C:\Program Files (x86)\AHS\VOICEROID2",
			VoiceIndexVoicePeak => @"C:\Program Files\VOICEPEAK",
			VoiceIndexAiVoice => @"C:\Program Files\AI\AIVoice\AIVoiceEditor",
			VoiceIndexAiVoice2 => @"C:\Program Files\AI\AIVoice2\AIVoice2Editor",
			_ => null,
		}).Select(x => Directory.Exists(x) switch {
			true => x,
			_ => null,
		}).ToReactiveProperty();

		this.UserArgumentsBinding = new(initialValue: config.UserArguments);
		this.UserArgumentsBinding.Subscribe(x => config.UserArguments = x);
	}
	public ReactiveProperty<string> IlluminateClientDialogFilter { get; }
	public ReactiveProperty<string?> IlluminateClientDialogDirectory { get; }

	private string ToString<T>(T v) {
		if(v == null) {
			return "";
		} else {
			return $"{v}";
		}
	}

	private int? ToInt(string s) {
		if(int.TryParse(s, out var v)) {
			return v;
		} else {
			return null;
		}
	}

	private Visibility ToIntError(string s) {
		if(string.IsNullOrEmpty(s)) {
			return Visibility.Collapsed;
		}

		if(int.TryParse(s, out var v)) {
			return Visibility.Collapsed;
		} else {
			return Visibility.Visible;
		}
	}

	private float? ToFloat(string s) {
		if(float.TryParse(s, out var v)) {
			return v;
		} else {
			return null;
		}
	}

	private Visibility ToFloatError(string s) {
		if(string.IsNullOrEmpty(s)) {
			return Visibility.Collapsed;
		}

		if(float.TryParse(s, out var _)) {
			return Visibility.Collapsed;
		} else {
			return Visibility.Visible;
		}
	}
}
