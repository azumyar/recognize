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

	public ReactiveProperty<string> TranscribeModel { get; set; } = new(initialValue: "");

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
		public string IlliminateExePath { get; set; }

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
}


public class ConfigBinder : INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;


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

	// マイク
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


	public ConfigBinder(Config config) {
		// マイクタブ
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
			int v => v + 1,
			_ => 0
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
		/*
	// ゆかこね連携
	public ReactiveProperty<bool> IsUsedYukaConeBinding { get; }
	public ReactiveProperty<int?> YukaConePortBinding { get; }
	public ReactiveProperty<Visibility> YukaConePortError { get; }
		*/
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



