# recognize.exe

## 動作環境
Windows 10/11
python 3.10/3.11/3.12

## ビルド
build.batをダブルクリックで自動的に必要コンポーネントをwiinget,pip3経由でインストールしexeを作成します。python 3.9以前がインストールされている場合事前にアンインストールしてください。ビルドに失敗します。3.13は現時点で依存ライブラリが対応していないため未保障です。

## 実行
環境に応じて*-template.batを実行してください。必要に応じてオプションを編集してください。

## コマンドオプション
### --test  string
テストモードで起動します
| オプション     |-|
|:--------------|:------------|
| mic           |マイクテストモードで起動します|
| mic_ambient   |環境音測定モードで起動します|


### --method string
認識方法を指定(~~whisper|~~faster_whisper|google)します。指定がない場合faster_whisperが設定されます。
| オプション     |認識モデル|
|:--------------|:------------|
| ~~whisper~~   |~~wisperモデルを使用してローカルでAI音声認識を行います~~|
| faster_whisper|wisperを軽量化したfaster_whisperを使用してローカルでAI音声認識を行います|
| kotoba_whisper|日本語に特化したkotoba_whisperを使用してローカルでAI音声認識を行います|
| google        |googleの音声認識API(v2)を使用してインターネット経由で音声認識を行います|
| google_duplex |googleの音声認識API(全二重)を使用してインターネット経由で音声認識を行います|
| google_mix    |googleの音声認識API(v2)とgoogleの音声認識API(全二重)を併用してインターネット経由で音声認識を行います|

whisperはopenai-whisperを含めてビルドした場合有効になります。標準では含まれていません。


### --whisper_model string
whisper系で有効(kotoba_whisper除く)  
使用する推論モデル(tiny|base|small|medium|large|large-v2|large-v3)などを指定します。指定がない場合mediumが設定されます。推論モデルは初回の使用時にダウンロードされます。

### --whisper_device string
whisper系で有効  
推論を行う演算装置(cpu|cuda)を指定します。指定がない場合cudaが設定されますがcudaが使用できない環境の場合cpuにバックフォールされます。

### --whisper_device_index int
faster_whisperのみで有効  
推論を行うGPU indexを指定します。指定がない場合0が設定されます。

### --whisper_language string
whisper系で有効(kotoba_whisper除く)  
推論言語を限定したい場合(en|ja|...)を指定します。指定がない場合全言語を対象に推移されます。指定しておくことを推奨します。

### --google_language string
google系で有効  
推論言語を限定したい場合(en-US|ja-JP|...)を指定します。指定がない場合ja-JPが設定されます。

### --google_timeout float
google系で有効  
googleサーバからのタイムアウト時間(秒)を指定します。標準で5.0が設定されています。0を指定するとタイムアウト時間が無限と解釈されます。

### --google_error_retry int
google系で有効  
指定回数500エラー時に再試行します。指定がない場合1が設定されます。1回のみ実行、つまり再試行されません。

### --google_duplex_parallel
google_duplexのみで有効  
このオプションが指定された場合3並列でリクエストを投げ500エラーの抑制を図ります。  
google_mixでは標準で有効化されます。

### --google_duplex_parallel_max int
google_duplex,google_mixで有効  
エラー時に増やしていく並列数の最大増加数。指定がない場合6が設定されます。

### --google_duplex_parallel_reduce_count int
google_duplex,google_mixで有効  
増加した並列数を減らすために必要な成功数。指定がない場合3が設定されます。

### --mic int
使用するマイクの番号を指定します。指定がない場合標準のマイクが使用されます。実行環境マイクデバイスの一覧を取得するには一度--print_micsオプションを指定してexeを実行してください。

### --mic_energy_threshold float
指定した音量より小さい値を無音として扱います。標準ではNoneが指定されています。この引数が指定された場合--mic_db_thresholdは無視されます。

### --mic_db_threshold float
指定した音量より小さい値を無音として扱います。標準では0が指定されています。この適切な値は使用されているマイクにより異なります。環境音テストを実行して適切な値を見極めてください。

### --mic_pause_duration float
VADをかけていく塊の秒数を指定します。標準では0.5秒が指定されています。この値を小さくとるとレスポンスはよくなりますが、文節区切りがタイトになります。0.5より大きくすることは想定されていません。未検証です。

### --mic_head_insert_duration float
認識音声の前側に挿入する秒数を指定します。認識モデルによって初期値は異なりgoogleでは2秒挿入されます。

### --mic_tail_insert_duration float
認識音声の後ろ側に挿入する秒数を指定します。認識モデルによって初期値は異なりgoogleでは2.5秒挿入されます。

### --filter_hpf int
ハイパスフィルタのカットオフ周波数を設定します。標準はNoneでハイパスフィルタは無効化されています。

### --vad string
VADエンジンの選択の選択をします

| オプション     |-|
|:--------------|:------------|
| google        |webrtcを使用します|
| silero        |stable-tsで使われているsileroを使用します|

※sileroはNVIDIAのGPUが必要です

### --vad_google_mode 0|1|2|3
webrtcのVAD強度を指定します。強度が高くなるほど積極的にノイズ判定します。

### --vad_silero_threshold float
sileroのオプション。初期値は0.5

### --vad_silero_min_speech_duration float
sileroのオプション。初期値は0.25

### --out string
認識結果出力方法を指定(print|yukarinette|yukacone)します。指定がない場合printが設定されます。
| オプション  |出力先|
|:-----------|:------------|
| print      |標準出力に認識結果を出力します|
| yukarinette|ゆかりねっとにWebsocketで送信します|
| yukacone   |ゆかコネNEOにWebsocketで送信します|
|illuminate|-|

### --out_yukarinette int
ゆかりねっとに送信する外部連携ポートを指定します。指定がない場合49513が設定されます。

### --out_yukacone int
ゆかコネNEOに送信する外部連携ポートを指定します。指定がない場合レジストリから取得されます。

###  --print_mics
このオプションを指定するとマイクデバイスの一覧を出力して終了します。一部デバイスが文字化けするのは仕様です。

### --verbose int
ログ出力レベル(0|1|2|3)を指定します。指定がない場合1が設定されます。ログ出力が有効になっている場合標準出力に追加情報を出力します。

### --log_file
ログファイル名を指定します。標準値はrecognize.logです。  
このログは--verboseの設定とは別のサポート用のログです。

### --log_directory
ログファイル格納先を指定します。指定されない場合exeファイルと同じディレクトリに出力します。  
このログは--verboseの設定とは別のサポート用のログです。

### --record
このオプションを指定された場合と録音した音声を出力します。

### --record_file string
録音するファイル名を指定します。標準値はrecordです。  
実際に出力されるファイル名は{ファイル名}-{連番}.wavになります。

### --record_directory string
録音ファイルの出力先ディレクトリを指定します。指定されない場合exeファイルと同じディレクトリに出力します。  
※ソースから実行した場合デバッグ向けディレクトリ.debugが指定されます。
