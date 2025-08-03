---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "ゆーかねすぴれこ"
  text: "Yukarinette Speech Recognition"
  tagline: Chromeブラウザを介さずゆかりねっとの音声認識を行います。
  image:
    src: /images/logo.svg
    alt: logo
  actions:
      - theme: brand
        text: ダウンロード
        link: https://github.com/HARUKei66494739/recognize/releases
      - theme: alt
        text: 使い方
        link: /usage/

features:
  - title: 🌏Google音声認識
    details: ゆかりねっと同様Google音声認識を行います。エラー処理の強化及び音声認識の細かい設定が行えますので認識詰まりを抑えることができます。
  - title: 🤖AI音声認識
    details: PCの処理能力を使用しローカル音声認識を行います。ローカルの処理になりますのでGoogleに依存しません。NVIDIAのGPUが必要でそこそこパワーを使います。
  - title: ✨簡単導入
    details: 導入用の実行ファイルでビルド済みパッケージを展開します！PCのレジストリは弄らないのでいらなくなったらファイル削除でOK。
---
<script setup lang="ts">
  // いい感じの方法ある気がするけどYAMLにv-bindする方法が見当たらないので強引に書き換える
  import { onMounted } from 'vue';
  onMounted(() => {
    const account = "HARUKei66494739";
    const repository = "recognize";
    fetch(`https://api.github.com/repos/${account}/${repository}/releases`)
      .then(function (res) {
        return res.json();
      }).then(function (json) {
        for(const release of json) {
          if(!release.draft && !release.prerelease) {
            for(const asset of release.assets) {
                console.log(asset.name);
              if(asset.name.match(/^setup-v.+\.exe$/)) {
                const a = document.querySelector(".actions .action a");
                if(a != null) {
                  a.innerText = `ダウンロード(${release.tag_name})`;
                  a.href = asset.browser_download_url;
                }
                return;
              }
            }
          }
        }
     });
  });
</script>