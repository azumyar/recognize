using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceLink.Clients;
public class AiVoice : IVoiceClient {
	private string exe = "";
	private int pId;
	private dynamic ttsClient;

	public int ProcessId { get => this.pId; }

	public AiVoice() {
		// WCFが使われているのでCOMを経由する
		var type = Type.GetTypeFromCLSID(new Guid("B628D293-341C-41BE-B2E7-9E7822B2B7AC"));
		if (type == null) {
			throw new VoiceLinkException("A.I.Voiceが見つかりません");
		}
		dynamic? tts = Activator.CreateInstance(type);
		if (tts == null) {
			throw new VoiceLinkException("A.I.Voiceのインスタンス化に失敗しました");
		}
		this.ttsClient = tts;
		var hosts = this.ttsClient.GetAvailableHostNames();
		if (hosts.Length == 0) {
			throw new VoiceLinkException("A.I.Voiceのホストが見つかりません");
		}
		this.ttsClient.Initialize(hosts[0]);
	}

	public bool StartClient(string targetExe, bool isLaunch) {
		this.exe = targetExe;
		return this.Load(this.exe, isLaunch);
	}

	private bool Load(string targetExe, bool isLaunch) {
		this.pId = 0;
		var p = Util.GetProcess(targetExe);
		if (p == null) {
			var pp = isLaunch switch {
				true => Util.LaunchProcess(targetExe, null),
				false => null,
			};
			if (pp == null) {
				return false;
			}
			p = pp.Value.Proc;
		}
		this.pId = p.Id;
		return true;
	}

	public void EndClient() { }

	public void BeginSpeech(string text) {
		if ((int)this.ttsClient.Status == 0) {
			throw new VoiceLinkException("A.I.Voiceが起動していません");
		}
		this.ttsClient.Connect();
	}

	public void Speech(string text) {
		this.ttsClient.Text = text;
		this.ttsClient.Play();
	}

	public void EndSpeech(string text) {
		this.ttsClient.Text = "";
		this.ttsClient.Disconnect();
	}
}
