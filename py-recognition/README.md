# recognize.exe

## 動作環境
Windows 10/11
python 3.10/3.11

## ビルド
build.batをダブルクリックで自動的に必要コンポーネントをwiinget,pip3経由でインストールしexeを作成します。python 3.9以前がインストールされている場合事前にアンインストールしてください。ビルドに失敗します。3.12は現時点で依存ライブラリが対応していないため未保障です。

## 実行
環境に応じて*-template.batを実行してください。必要に応じてオプションを編集してください。

## コマンドオプション
### --test  string
テストモードで起動します
| オプション     |認識モデル|
|:--------------|:------------|
| recognition   |一度だけ音声認識して終了します|
| mic           |マイクテストモードで起動します|


### --method string
認識方法を指定(whisper|faster_whisper|google)します。指定がない場合faster_whisperが設定されます。
| オプション     |認識モデル|
|:--------------|:------------|
| whisper       |wisperモデルを使用してローカルでAI音声認識を行います|
| faster_whisper|wisperを軽量化したfaster_whisperを使用してローカルでAI音声認識を行います|
| google        |googleの音声認識API(v2)を使用してインターネット経由で音声認識を行います|
| google_duplex |googleの音声認識API(全二重)を使用してインターネット経由で音声認識を行います|

### --whisper_model string
whisper系で有効  
使用する推論モデル(tiny|base|small|medium|large|large-v2|large-v3)を指定します。指定がない場合mediumが設定されます。推論モデルは初回の使用時にダウンロードされます。

###  --whisper_device string
whisper系で有効  
推論を行う演算装置(cpu|cuda)を指定します。指定がない場合cudaが設定されますがcudaが使用できない環境の場合cpuにバックフォールされます。

### --whisper_language string
whisper系で有効  
推論言語を限定したい場合(en|ja|...)を指定します。指定がない場合全言語を対象に推移されます。指定しておくことを推奨します。

### --google_language string
google系で有効  
推論言語を限定したい場合(en-US|ja-JP|...)を指定します。指定がない場合ja-JPが設定されます。

### --google_timeout float
google系で有効  
googleサーバからのタイムアウト時間(秒)を指定します。標準で5.0が設定されています。0を指定するとタイムアウト時間が無限と解釈されます。

### --google_convert_sampling_rate
google系で有効  
マイク入力を16kに変換して送信します。WASAPIデバイスはデバイスの周波数で録音するためデータの肥大化を抑えます。

### --google_error_retry int
google系で有効  
指定回数500エラー時に再試行します。指定がない場合1が設定されます。1回のみ実行、つまり再試行されません。

### --google_duplex_parallel
google_duplexのみで有効  
このオプションが指定された場合3並列でリクエストを投げ500エラーの抑制を図ります。

### --google_duplex_parallel_max int
google_duplexのみで有効  
エラー時に増やしていく並列数の最大増加数。指定がない場合6が設定されます。

### --google_duplex_parallel_reduce_count int
google_duplexのみで有効  
増加した並列数を減らすために必要な成功数。指定がない場合3が設定されます。

### --mic int
使用するマイクの番号を指定します。指定がない場合標準のマイクが使用されます。実行環境マイクデバイスの一覧を取得するには一度--print_micsオプションを指定してexeを実行してください。

### --mic_energy float
指定した音量より小さい値を無音として扱います。標準では300.0が指定されていますがこの適切な値は使用されているマイクにより異なります。なにもしゃべってなくても音声認識処理が走る場合はこの値を大きくしてください。

### --mic_ambient_noise_to_energy
このオプションを指定すると起動時に環境音を収拾して--mic_energyの値を設定します。このオプションが有効な場合--mic_energyは無視されます。また--mic_dynamic_energy_minよりさがることはありません。

### --mic_dynamic_energy
このオプションを指定すると周りの騒音レベルに応じてマイクの収音レベルを動的に変更します。使用しているライブラリは一般的な環境では有効にすることを推奨しています。

### --mic_dynamic_energy_ratio float
指定がない場合1.5が設定されます。

### --mic_dynamic_energy_adjustment_damping float
指定がない場合1.5が設定されます。

### --mic_dynamic_energy_min float
しゃべっていないと補正により--mic_energyが減衰していくためこの値よりは下がらない値を指定します。指定がない場合100が設定されなます。

### --mic_pause float
無音として認識される秒数を指定します。指定がない場合0.8秒が設定されます。

### --mic_phrase float
この時間発声すると有効な音声によにして認識される秒数を指定します。指定がない場合0.3秒が設定されます。

### --mic_listen_interval float
マイク監視ループにおいて1回あたりの監視秒数を指定します。指定がいない場合0.25秒数が設定されます。  
監視秒数はこの間に発声されるかの監視であり、この時間内に発声が確認された場合--mic_listen_intervalを超えていてもマイクからの録音は継続します。

### --out string
認識結果出力方法を指定(print|yukarinette|yukacone)します。指定がない場合printが設定されます。
| オプション  |出力先|
|:-----------|:------------|
| print      |標準出力に認識結果を出力します|
| yukarinette|ゆかりねっとにWebsocketで送信します|
| yukacone   |ゆかコネNEOにWebsocketで送信します|

### --out_yukarinette int
ゆかりねっとに送信する外部連携ポートを指定します。指定がない場合49513が設定されます。

### --out_yukacone int
ゆかコネNEOに送信する外部連携ポートを指定します。指定がない場合レジストリから取得されます。

### --filter_lpf_cutoff int
ローパスフィルタのカットオフ周波数を指定します。指定がない場合200が設定されます。

### --filter_lpf_cutoff_upper int
ローパスフィルタのカットオフ周波数(アッパー)を指定します。指定がない場合200が設定されます。

### --filter_hpf_cutoff int
ハイパスフィルタのカットオフ周波数を指定します。指定がない場合200が設定されます。

### --filter_hpf_cutoff_upper int
ハイパスフィルタのカットオフ周波数(アッパー)を指定します。指定がない場合200が設定されます。

### --disable_lpf
このオプションが指定された場合ローパスフィルタを使用しません。

### --disable_hpf 
このオプションが指定された場合ハイパスフィルタを使用しません。

###  --print_mics
このオプションを指定するとマイクデバイスの一覧を出力して終了します。一部デバイスが文字化けするのは仕様です。

### --verbose int
ログ出力レベル(0|1|2)を指定します。指定がない場合1が設定されます。ログ出力が有効になっている場合標準出力に追加情報を出力します。

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
