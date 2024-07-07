# 構成比較
とりあえず何が違うのか構成の比較を図で簡単に説明します。  

## ゆかりねっと通常使用時
ゆかりねっと通常使用時は以下のようにchromeを通してgoogleから音声認識結果を取得します。（③の箇所）  
この音声認識結果を取得する際にgoogleが応答を返さないケースがあり、これが俗に言う認識詰まりです。  

![_](assets/overview_01_yukarinet_.png)

## ゆーかねすぴれこ（google）使用時
基本の流れは通常使用と同じですがchromeを経由しません。  
chromeには無いゆーかねすぴれこの様々なパラメータを使うことにより詰まりを減らすことが出来ます。

![_](assets/overview_02_yukanesupireco_google_.png)

## ゆーかねすぴれこ（kotoba_whisper）使用時
googleに任せていた音声認識結果をkotoba_whisperに任せます。(②と③の箇所)  
内部処理なので詰まることはありませんがgoogleとまた違った認識結果になったり、誤認識が発生する場合があります。  
またGPUパワーも使います。  

![_](assets/overview_03_yukanesupireco_kotoba_.png)