using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceLink.Clients;
public abstract class CeVio : VoiceClient<NopVoiceObject, CeVioSpeechClient, NopVoiceObject> {
	protected record struct CeVioComInterface(string Service, string Talker);
	private readonly dynamic cevio;
	private readonly dynamic talker;

	public override NopVoiceObject ClientParameter { get; } = new();

	public CeVio() {
		static dynamic create(string progId) {
			var type = Type.GetTypeFromProgID(progId);
			if (type == null) {
				throw new VoiceLinkException($"CeVio[{progId}]が見つかりません");
			}
			dynamic? _cevio = Activator.CreateInstance(type);
			if (_cevio == null) {
				throw new VoiceLinkException($"CeVio[{progId}]のインスタンス化に失敗しました");
			}
			return _cevio;
		}

		// .NET Remotingが使われているので.NETで使用できないCOMを経由する
		var com = ProgId;
		this.cevio = create(com.Service);
		this.talker = create(com.Talker);
	}

	protected abstract CeVioComInterface ProgId { get; } 

	public override bool StartClient(bool isLaunch, NopVoiceObject extra) {
		if (isLaunch) {
			this.cevio.StartHost(true);
		}
		return true;
	}

	public override void EndClient() {
		//this.cevio.CloseHost(0);
	}

	public override void BeginSpeech(string text, CeVioSpeechClient extra) {
		if(!((bool)cevio.IsHostStarted)) {
			throw new VoiceLinkException("CeVioが起動していません");
		}

		LogDebug("キャスト設定");
		this.talker.Cast = extra.Cast;
		this.talker.Volume = extra.Volume;
		this.talker.Speed = extra.Speed;
		this.talker.Tone = extra.Tone;
		this.talker.ToneScale = extra.ToneScale;
		this.talker.Alpha = extra.Alpha;
		LogDebug("キャスト感情情報設定");
		foreach (var it in extra.Components) {
			this.talker.Components.ByName(it.Name).Value = it.Value;
		}
	}

	public override void Speech(string text, CeVioSpeechClient extra) {
		var state = this.talker.Speak(text);
		state.Wait();
	}

	public override void EndSpeech(string text, CeVioSpeechClient extra) {}
}


public class CeVioCs() : CeVio {
	protected override CeVioComInterface ProgId => new(
			"CeVIO.Talk.RemoteService.ServiceControlV40",
			"CeVIO.Talk.RemoteService.TalkerV40"
		);
}

public class CeVioAi() : CeVio {
	protected override CeVioComInterface ProgId => new(
			"CeVIO.Talk.RemoteService2.ServiceControl2",
			"CeVIO.Talk.RemoteService2.Talker2V40"
		);
}