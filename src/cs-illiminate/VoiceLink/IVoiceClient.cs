using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceLink;
public interface IVoiceClient {
	int ProcessId { get; }

	bool StartClient(string targetExe, bool isLaunch);
	void EndClient();

	public void BeginSpeech(string text);

	public bool Speech(string text);
	public void EndSpeech(string text);
}