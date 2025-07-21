using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haru.Kei.Models;
internal interface IClient {
	public void StartClient(bool isLaunch);
	public void EndClinet();
	public void BeginSpeech(string text);
	public void Speech(string text);
	public void EndSpeech(string text);

	public static IClient Get(CommandOptions opt) {
		return opt.Voice switch {
			CommandOptions.VoiceClientType.voiceroid => new VoiroClinet(opt),
			CommandOptions.VoiceClientType.voiceroid2 => new VoiroClinet(opt),
			CommandOptions.VoiceClientType.voicepeak => new VoiroClinet(opt),
			CommandOptions.VoiceClientType.aivoice => new VoiroClinet(opt),
			CommandOptions.VoiceClientType.aivoice2 => new VoiroClinet(opt),
			CommandOptions.VoiceClientType.ceviocs => new CeVioClinet(opt),
			CommandOptions.VoiceClientType.cevioai => new CeVioClinet(opt),
			_ => throw new NotImplementedException($"不正な合成音声{opt.Voice}"),
		};
	}
}

// VoiceRoid, VoiceRoid2, VoicePeak, A.I.Voice, A.I.Voice2
class VoiroClinet : IClient {
	private readonly VoiceLink.NopVoiceObject nop = new();
	private readonly VoiceLink.IVoiceClient<VoiceLink.AudioCaptreStart, VoiceLink.NopVoiceObject, VoiceLink.IAudioCaptireClient> client;
	private readonly string exe;

	public VoiroClinet(CommandOptions opt) {
		this.client = opt.Voice switch {
			CommandOptions.VoiceClientType.voiceroid => new VoiceLink.Clients.VoiceRoid(),
			CommandOptions.VoiceClientType.voiceroid2 => new VoiceLink.Clients.VoiceRoid2(),
			CommandOptions.VoiceClientType.voicepeak => new VoiceLink.Clients.VoicePeak(),
			CommandOptions.VoiceClientType.aivoice => new VoiceLink.Clients.AiVoice(),
			CommandOptions.VoiceClientType.aivoice2 => new VoiceLink.Clients.AiVoice2(),
			_ => throw new NotImplementedException($"不正な合成音声{opt.Voice}"),
		};
		this.exe = opt.Client;
	}

	public VoiceLink.IVoiceClient<VoiceLink.AudioCaptreStart, VoiceLink.NopVoiceObject, VoiceLink.IAudioCaptireClient> VoiceClient => client;

	public void StartClient(bool isLaunch) => client.StartClient(isLaunch, new(exe));
	public void EndClinet() => client.EndClient();
	public void BeginSpeech(string text) => client.BeginSpeech(text, nop);
	public void Speech(string text) => client.Speech(text, nop);
	public void EndSpeech(string text) => client.EndSpeech(text, nop);
}

// CeVIO CS7, CeVIO AI
class CeVioClinet : IClient {
	private readonly VoiceLink.NopVoiceObject nop = new();
	private readonly VoiceLink.IVoiceClient<VoiceLink.NopVoiceObject, VoiceLink.CeVioSpeechClient, VoiceLink.NopVoiceObject> client;
	private readonly VoiceLink.CeVioSpeechClient cevio;

	public CeVioClinet(Models.CommandOptions opt) {
		this.client = opt.Voice switch {
			CommandOptions.VoiceClientType.ceviocs => new VoiceLink.Clients.CeVioCs(),
			CommandOptions.VoiceClientType.cevioai => new VoiceLink.Clients.CeVioAi(),
			_ => throw new NotImplementedException($"不正な合成音声{opt.Voice}"),
		};
		this.cevio = new(
			Cast: "さとうささら",
			Volume: 100u,
			Speed: 100u,
			Tone: 100u,
			ToneScale: 100u,
			Alpha: 100u,
			Components: Array.Empty<(string, uint)>()
		);
	}

	public VoiceLink.IVoiceClient<VoiceLink.NopVoiceObject, VoiceLink.CeVioSpeechClient, VoiceLink.NopVoiceObject> VoiceClient => client;
	public void StartClient(bool isLaunch) => client.StartClient(isLaunch, nop);
	public void EndClinet() => client.EndClient();
	public void BeginSpeech(string text) => client.BeginSpeech(text, cevio);
	public void Speech(string text) => client.Speech(text, cevio);
	public void EndSpeech(string text) => client.EndSpeech(text, cevio);
}



