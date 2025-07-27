---
layout: doc
prev: false
next: false
---

# 合成音声連携機能

## サポートされている合成音声
- VOICEROID Exとその互換クライアント
- VOICEROID2
- VOICEPAEK
- A.I.VOICE
- A.I.VOICE2
<!--
- CeVIO CS7
- CeVIO AI
-->

## はじめに
ゆーかねすぴれこの合成音声連携機能はAPIが提供されていない場合独自に画面を操作して実装しています。力づくでしゃべらせています。ゆーかねすぴれこ使用中は合成音声のウインドウを触らないようにしてください。

各合成音声クライアントは自動起動しません。自分で起動、終了してください。
断りのない限り感情などのパラメータはゆーかねすぴれこから設定できません。あらかじめ調声しておいてください。

## 対応表

| 合成音声 | 連携方法 | 備考 |
| ---- | ---- | ---- |
| VOICEROID Ex | 独自 | ボイロEx互換クライアントも動作します
| VOICEROID2 | 独自 | - |
| VOICEPAEK | 独自 | 特別な制限があります。後述 |
| A.I.VOICE | COM API | - |
| A.I.VOICE2 | 独自 | - |
<!--
| CeVIO CS7 | COM API | 感情パラメータの設定対応。CS6以前は連携できません |
| CeVIO AI | COM API | 感情パラメータの設定対応 |
-->

## VOICEPAEKの制限
VOICEPEAK ver 1.2.14以降で動作します。
赤枠の部分は見えている状態にしてください。
![ボイスピ制限事項](/images/usage/illuminate-voicepeak.png)

