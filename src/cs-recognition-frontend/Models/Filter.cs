using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



