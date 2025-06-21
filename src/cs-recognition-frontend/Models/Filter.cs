using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Accessibility;
using Newtonsoft.Json;
using Reactive.Bindings;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Haru.Kei.Models;

public class ReactivePropertyConverter<T> : JsonConverter {
	public override bool CanConvert(Type objectType) => objectType == typeof(ReactiveProperty<T>);

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
		return new ReactiveProperty<T>(new JsonSerializer().Deserialize<T>(reader));
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
		if(value is ReactiveProperty<T> c) {
			writer.WriteValue(c.Value);
		}
	}
}


public class ReactiveCollectionConverter<T> : JsonConverter {
	public override bool CanConvert(Type objectType) => objectType == typeof(ReactiveCollection<T>);

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
		var ret = new ReactiveCollection<T>();
		foreach(var it in new JsonSerializer().Deserialize<T[]>(reader) ?? Array.Empty<T>()) {
			ret.Add(it);
		}
		return ret;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
		if(value is ReactiveCollection<T> c) {
			serializer.Serialize(writer, c.ToArray());
		}
	}
}

public class Config {

	public string TranscribeModel { get; set; } = "";

	public string GoogleLanguage { get; set; } = "";

	public float? GoogleTimeout { get; set; }

	public bool GoogleProfanityFilter { get; set; }

	public string TranslateModel { get; set; } = "";

	// マイク
	public int? Microphone { get; set; } = null;
	public float? MicrophoneThresholdDb { get; set; } = null;

	public float? MicrophoneRecordMinDuration { get; set; } = null;

	public int? VadGoogleParamater { get; set; } = null;

	public int? HpfParamater { get; set; } = null;

	// ゆかりねっと連携
	public bool IsUsedYukarinette { get; set; } = false;
	public int? YukatinettePort { get; set; } = null;

	// ゆかこね連携
	public bool IsUsedYukaCone { get; set; } = false;
	public int? YukaConePort { get; set; } = null;


	public bool IsUsedIlluminate { get; set; } = false;
	public string IlluminateVoice { get; set; } = "voiceroid2";
	public string IlluminateClient { get; set; } = "";


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

		protected const string categoryOutput = "00.環境";

		[Category(categoryOutput)]
		[DisplayName("recognize.exeパス")]
		[Description("recognize.exeのパスをフルパスまたは相対パスで指定")]
		[DefaultValue(@".\src\\py-recognition\dist\recognize\recognize.exe")]
		public string RecognizeExePath { get; set; }

		[Category(categoryOutput)]
		[DisplayName("illiminate.exeパス")]
		[Description("illiminate.exeのパスをフルパスまたは相対パスで指定")]
		[DefaultValue(@".\src\\cs-illiminate\dist\illiminate.exe")]
		public string IlluminateExePath { get; set; }

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
	}

	public RecognizeExeArgument Extra { get; private set; } = new();



	public readonly string TranscribeModelWhisper = "kotoba_whisper";
	public readonly string TranscribeModelGoogle = "google_mix";
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
					return opt.Append($"{name} \"{val}\" ");
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

		if(this.IsUsedYukarinette) {
			opt.Append($"--out \"yukarinette\" ");
			arg(opt, "--out_yukarinette", this.YukatinettePort);

		}
		if(this.IsUsedYukaCone) {
			opt.Append($"--out \"yukacone\" ");
			arg(opt, "--out_yukacone", this.YukaConePort);
		}

		// 絶対パスに変換してillminateパス
		if(this.IsUsedIlluminate) {
			opt.Append($"--out \"illuminate\" ");
			opt.Append($"--out_illuminate_voice \"voiceroid2\" ");
			opt.Append($"--out_illuminate_client \"{this.IlluminateClient}\" ");
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
		
		if(!string.IsNullOrEmpty(this.Extra.ExtraArgument)) {
			opt.Append(this.Extra.ExtraArgument);
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
	private readonly string[] TranslateModels = {
		"設定しない",
		"AI翻訳",
	};
	private readonly string[] VadGoogleParamaters = {
		"設定しない",
		"0",
		"1",
		"2",
		"3",
	};
	private readonly string[] HpfParamaters = {
		"設定しない",
		"無効",
		"弱い",
		"普通",
		"強め",
	};

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

	// ボイロ連携
	public ReactiveProperty<bool> IsUsedIlluminateBinding { get; }
	//public ReactiveProperty<string> IlluminateVoiceBinding { get; }
	public ReactiveProperty<string> IlluminateClientBinding { get; }


	public ConfigBinder(Config config) {
		// モデル
		this.TranscribeModelsBinder = new();
		this.TranscribeModelsBinder.AddRangeOnScheduler(TranscribeModels);
		this.TranscribeModeIndex = new(initialValue: config.TranscribeModel switch {
			"kotoba_whisper" => 1,
			"google_mix" => 2,
			_ => 0
		});
		this.TranscribeModeIndex.Subscribe(x => config.TranscribeModel = x switch {
			1 => "kotoba_whisper",
			2 => "google_mix",
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
			"kotoba_whisper" => 1,
			_ => 0
		});
		this.TranslateModelIndex.Subscribe(x => config.TranslateModel = x switch {
			1 => "kotoba_whisper",
			_ => "",
		});

		this.GoogleItemVisibility = this.TranscribeModeIndex
			.Select(x => x switch {
				2 => Visibility.Visible,
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
			int v when (0 <= v) && (v < 80) => 1,
			int v when (80 <= v) && (v < 120) => 2,
			int v when (120 <= v) && (v < 200) => 3,
			int v when (200 <= v) => 4,
			_ => 0
		});
		this.HpfParamaterIndex.Subscribe(x => config.HpfParamater = x switch {
			1 => 0,
			2 => 80,
			3 => 120,
			4 => 200,
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

		// ボイロ連携
		this.IsUsedIlluminateBinding = new(initialValue: config.IsUsedIlluminate);
		this.IsUsedIlluminateBinding.Subscribe(x => config.IsUsedIlluminate = x);
		//this.IlluminateVoiceBinding = new(initialValue: config.IsUsedIlluminate);
		//this.IlluminateVoiceBinding.Subscribe(x => config.IsUsedIlluminate = x);
		this.IlluminateClientBinding = new(initialValue: config.IlluminateClient);
		this.IlluminateClientBinding.Subscribe(x => config.IlluminateClient = x);
	}

	private string ToString<T>(T v) {
		if (v == null) {
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

		if(float.TryParse(s, out var v)) {
			return Visibility.Collapsed;
		} else {
			return Visibility.Visible;
		}
	}
}

public class Filter : INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;


	[JsonProperty("filters")]
	[JsonConverter(typeof(ReactiveCollectionConverter<FilterItem>))]
	public ReactiveCollection<FilterItem>? Filters { get; private set; } = new();
}

public class FilterItem : INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	[JsonProperty("name")]
	[JsonConverter(typeof(ReactivePropertyConverter<string?>))]
	public ReactiveProperty<string?> Name { get; private set; } = new(initialValue: "");

	[JsonProperty("Lang")]
	[JsonConverter(typeof(ReactivePropertyConverter<string?>))]
	public ReactiveProperty<string?> Lang { get; private set; } = new(initialValue: "ja");

	[JsonProperty("enable")]
	[JsonConverter(typeof(ReactivePropertyConverter<bool?>))]
	public ReactiveProperty<bool?> Enable { get; private set; } = new(initialValue: true);


	[JsonProperty("rules")]
	[JsonConverter(typeof(ReactiveCollectionConverter<FilterRule>))]
	public ReactiveCollection<FilterRule>? Rules { get; private set; } = new();
}

public class FilterRule : INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	[JsonProperty("action")]
	[JsonConverter(typeof(ReactivePropertyConverter<string?>))]
	public ReactiveProperty<string?> Action { get; private set; } = new(initialValue: "mask");

	[JsonProperty("rule")]
	[JsonConverter(typeof(ReactivePropertyConverter<string?>))]
	public ReactiveProperty<string?> Rule { get; private set; } = new(initialValue: "match");

	[JsonProperty("src")]
	[JsonConverter(typeof(ReactivePropertyConverter<string?>))]
	public ReactiveProperty<string?> Src { get; private set; } = new(initialValue: "");

	[JsonProperty("dst")]
	[JsonConverter(typeof(ReactivePropertyConverter<string?>))]
	public ReactiveProperty<string?> Dst { get; private set; } = new(initialValue: "");
}



