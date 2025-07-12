using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceLink;
public interface IVoiceClient {
	/// <summary>オーディオキャプチャが接続するためのプロセスID</summary>
	int ProcessId { get; }

	/// <summary>クライアントの初期化</summary>
	/// <param name="targetExe">クライアントexeファイルのフルパス</param>
	/// <param name="isLaunch">クライアントを自動起動する場合はtrue(現在使用されていません)</param>
	/// <returns>初期化に成功した場合はtrue</returns>
	bool StartClient(string targetExe, bool isLaunch);
	/// <summary>クライアントの開放(現在使用されていません)</summary>
	void EndClient();

	/// <summary>読み上げ開始前の準備を行います。失敗した場合は<see cref="VoiceLinkException"/>を投げてください。</summary>
	/// <param name="text">読み上げテキスト</param>
	public void BeginSpeech(string text);
	/// <summary>読み上げ処理を行います。失敗した場合は<see cref="VoiceLinkException"/>を投げてください。</summary>
	/// <param name="text">読み上げテキスト</param>
	public void Speech(string text);
	/// <summary>読み上げで使用したリソースの解放処理を行います。失敗した場合は<see cref="VoiceLinkException"/>を投げてください。</summary>
	/// <param name="text">読み上げテキスト</param>
	public void EndSpeech(string text);
}