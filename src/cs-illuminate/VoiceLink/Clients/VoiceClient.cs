using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceLink.Clients;
public abstract class VoiceClient<TStartObj, TSpeechObj, TClientObj> : IVoiceClient<TStartObj, TSpeechObj, TClientObj>
	where TStartObj : IStartObject
	where TSpeechObj : ISpeechObject
	where TClientObj : IClientObject {

	Action<string>? IVoiceLogger.LogInfo { get; set; }
	Action<string>? IVoiceLogger.LogDebug { get; set; }

	public abstract TClientObj ClientParameter { get; }

	protected void LogInfo(string s) => ((IVoiceClient<TStartObj, TSpeechObj, TClientObj>)this).LogInfo?.Invoke($"[{GetType().Name}]{s}");
	protected void LogDebug(string s) => ((IVoiceClient<TStartObj, TSpeechObj, TClientObj>)this).LogDebug?.Invoke($"[{GetType().Name}]{s}");

	public abstract bool StartClient(bool isLaunch, TStartObj extra);
	public abstract void EndClient();

	public abstract void BeginSpeech(string text, TSpeechObj extra);
	public abstract void Speech(string text, TSpeechObj extra);
	public abstract void EndSpeech(string text, TSpeechObj extra);
}
