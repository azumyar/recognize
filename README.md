# ゆーかねすぴれこ(Yukarinette Speech Recognition)

## 概要
ゆかりねっと-Yukarinette-の音声認識エンジンを別処理で認識させるためのツールです。  
実行ファイルは使用者の環境でビルドして作成する形になります。

認識エンジンはwhisperとgoogleを選択できます。  
追加された認識モデルkotoba_whisperについては[こちら](docs/KOTOBA_WHISPER.md)。

何が違うのか[概要図](docs/Overview.md)をご確認ください。

## 動作環境
Windows 10/11  
諸々ダウンロードするので解凍先ドライブに10Gほどの空きスペースが必要です。  
実行するドライブの容量にお気を付けください。

## ビルドと実行
右上のコードボタンから[zip](https://gitlab.com/HARUKei66494739/recognize/-/archive/main/recognize-main.zip)をダウンロードできます。  
build-all.batを実行してexeを作成できます。  
詳しくは画像付きの[画像たくさん簡単スタート](docs/KANTAN.md)を用意しました。まずはこちらも見てみてね。

## アップデート
srcとbinを削除してアーカイブから上書き更新した後build-all.batを再度実行してください。  

![_](docs/assets/update.png)

## ドキュメント
[作成中](docs/index.md)

## 規約
本ツールを使用しての直接または間接的に発生したいかなる損失や損害についても責任を負いません。

## その他
[FAQ](docs/FAQ.md)あります。  
バックエンドの実行オプションなどは[py-recognition配下のREADME.md](src/py-recognition/README.md)を参照してください。  

ゆかりねっと-Yukarinette-はおかゆぅさん氏のソフトウェアです。→[リンク](http://www.okayulu.moe/)
