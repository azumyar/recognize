# ゆーかねすぴこれ(Yukarinette Speech Recognition)

## 概要
ゆかりねっと-Yukarinette-の音声認識エンジンを別処理で認識させるためのツールです。  
実行ファイルは使用者の環境でビルドして作成する形になります。  
build-all.batを実行してexeを作成してください。  
バックエンドの実行オプションなどは[py-recognition配下のREADME.md](src/py-recognition/README.md)を参照してください。  
※ビルド時exeを作成する都合上、<span style="color: red; ">4～5Gほど容量を使用します。</span>  
※kotoba_whisperを使用する場合は<span style="color: red; ">容量合計10Gほど使用します。</span>  
実行するドライブの容量にお気を付けください。

## ビルドするためのソースコードの取得
右上のコードボタンから[zip](https://gitlab.com/HARUKei66494739/recognize/-/archive/main/recognize-main.zip)をダウンロードできます。

## クイックスタート(recognize.exeを作成後からの手順)
1. ゆかりねっとを起動。  
2. ゆかりねっとの[設定]から[音声認識エンジン]の[サードパーティー製の音声認識エンジンを使用する]にチェックを入れる。  
3. ゆかりねっとの音声認識を開始。  
4. recognize-gui.exeを起動し必要な設定をして起動ボタン(設定は次回以降に保存されます)

[画像たくさん簡単スタート](docs/KANTAN.md)を用意しました。  
[FAQ](docs/FAQ.md)もあります。

追加された認識モデルkotoba_whisperについてはこちら。  
[認識モデル kotoba_whisper](docs/KOTOBA_WHISPER.md)

## ドキュメント
[作成中](docs/index.md)

## その他
google認識を指定した場合別処理を経由しているとは言え最終的に認識させているエンジンはgoogleになります。  
オリジナルに比べて認識しやすい場合もあればしにくい場合もあります。  

## 規約
本ツールを使用しての直接または間接的に発生したいかなる損失や損害についても責任を負いません。

