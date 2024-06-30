# 認識モデル

ゆーかねすぴれこはwhisper系のAI音声認識とgoogle音声認識の2系統の音声認識を扱えます。

## whisper系
ローカルで音声認識を行います。CPUでも動作しますが、実用にはCUDAが使えるGPUが必要です。  
whisper系の注意点としてノイズもなんらかの言葉に変換するため事前にノイズカットしておかないと文脈のない誤認識が発生します。

### whisper [LINK](https://github.com/openai/whisper)
openai/whisperです。(ビルド構成ファイルを手動で変更しないと使えません)

### faster_whisper [LINK](https://github.com/SYSTRAN/faster-whisper)
openai/whisperの高速な実装faster-whisperです。large-v3は高い認識精度を誇りますがその分高性能なGPUは必要です。
使用のおすすめしません

### kotoba_whisper [LINK](https://huggingface.co/kotoba-tech/kotoba-whisper-v1.0/)
distil-whisperの日本語限定実装kotoba-whisperです。日本語限定のためfaster_whisperより省メモリで動作します。おすすめです。  
kotoba-whisperでは認識言語、モデルは選択できません。


## google系
google音声認識APIを介して音声認識を行います。すべてはgoogleの機嫌次第…

WASAPIのマイクを使用している場合認識周波数が48000Hzなどマイクの周波数になっています。これでもgoogle音声認識APIは認識を行えますが、通信量が肥大化するため、16k変換オプションを導入することをお勧めします。

エラーリトライはサーバが不安定なとき有効ですが、配信で使用する場合レスポンスが悪くなるのでおすすめしません。

### google
pythonライブラリ[SpeechRecognition](https://pypi.org/project/SpeechRecognition/)の実装のクローン

### google_duplex
chromiumのWeb APIのほうのSpeechRecognitionで使われている実装のクローン。並列オプションを指定することで認識精度をあげます。

### google_mix
googleとgoogle_duplexの併用で認識精度を高めます。おすすめです。

