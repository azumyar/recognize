![_](docs/assets/icon48.png)  
# ゆーかねすぴれこ(Yukarinette Speech Recognition)

## 概要
ゆかりねっと-Yukarinette-の音声認識エンジンを別処理で認識させるためのツールです。  
実行ファイルは使用者の環境でビルドして作成する形になります。

認識エンジンはwhisperとgoogleを選択できます。  
ゆかりねっと単体と何が違うのか比べてみたので[これ見て！](docs/Overview.md)

最近の更新された機能の概要については[こちら](docs/NEW.md)。

## 動作環境
Windows 10/11(64bitのみ)  
各々でビルドするので解凍先ドライブに10Gほどの空きスペースが必要です。  
実行するドライブの容量にお気を付けください。

python 3.10/3.11/3.12  
いずれかのバージョンが必要です。存在しない場合ビルド時に自動的にインストールされます。

Visual C++コンパイラ  
ビルドに必要です。存在しない場合ビルド時に自動的にインストールします。  
インストールする場合Cドライブに7Gほどの空きスペースが必要です。


## ビルドと実行
右上のコードボタンから[zip](https://gitlab.com/HARUKei66494739/recognize/-/archive/main/recognize-main.zip)をダウンロードできます。  
build-all.batを実行してexeを作成できます。  
詳しくは画像付きの[画像たくさん簡単スタート](docs/KANTAN.md)を用意しました。まずはこちらも見てみてね。

## アップデート
srcとbinを削除してアーカイブから上書き更新した後build-all.batを再度実行してください。  

## アンインストール方法
ダウンロードしたrecognizeフォルダごと削除してください。  
PCのレジストリは変更していません。

![_](docs/assets/update.png)

## ドキュメント
[作成中](docs/index.md)  
[FAQ](docs/FAQ.md) 

## 規約
本ツールを使用しての直接または間接的に発生したいかなる損失や損害についても責任を負いません。

## その他 
バックエンドの実行オプションなどは[py-recognition配下のREADME.md](src/py-recognition/README.md)を参照してください。  

ゆかりねっと-Yukarinette-はおかゆぅさん氏のソフトウェアです。→[リンク](http://www.okayulu.moe/)
