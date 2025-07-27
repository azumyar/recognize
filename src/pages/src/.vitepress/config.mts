import { defineConfig } from "vitepress"

// https://vitepress.dev/reference/site-config
export default defineConfig({
  base: "/recognize/",
  title: "ゆーかねすぴれこ",
  description: "",
  head: [
    ["link", {rel: "icon", href: "/recognize/images/favicon.ico"}],
    ["meta", {property: "twitter:card", content: "summary"}],
    ["meta", {property: "twitter:site", content: "@HARUKei66494739"}],
    ["meta", {property: "twitter:description", content: "ゆかりねっと&amp;ゆかコネNEOの音声認識代替ツール"}],
    ["meta", {property: "twitter:image", content: "https://harukei66494739.github.io/recognize/og.png"}],
    ["meta", {property: "og:url", content: "https://harukei66494739.github.io/recognize/"}],
    ["meta", {property: "og:type", content: "product"}],
    ["meta", {property: "og:title", content: "ゆーかねすぴれこ"}],
    ["meta", {property: "og:description", content: "ゆかりねっと&amp;ゆかコネNEOの音声認識代替ツール"}],
    ["meta", {property: "og:image", content: "https://harukei66494739.github.io/recognize/og.png"}],
    ["meta", {property: "og:image:width", content: "64"}],
    ["meta", {property: "og:image:height", content: "64"}],
  ],
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    logo: "/images/logo-mini.svg",
    nav: [
      { text: "ホーム", link: "/" },
      { text: "使い方", link: "/usage/" },
      { text: "開発", link: "/dev/" }
    ],

    sidebar: {
      "/usage/": [
        {
          text: "使い方",
          items: [
            { text: "はじめに", link: "/usage/" },
            { text: "簡単スタート", link: "/usage/kantan" },
            { text: "合成音声連携", link: "/usage/illuminate" },
            { text: "FAQ", link: "/usage/faq"},
          ]
        },
        {
          text: "Tips",
          items: [
            { text: "自由記入欄", link: "/usage/tips-free_input"},
          ]
        },
        {
          text: "高度なお話",
          items: [
            { text: "-", },
            //{ text: "VAD機能", link: "/usage/advanced-vad" },
          ]
        },
      ],
      "/dev/": [
        {
          text: "開発",
          items: [
            { text: "はじめに", link: "/dev/" },
            { text: "ドキュメントの編集", link: "/dev/build-doc" }
          ]
        }
      ]
    },

    socialLinks: [
      { icon: "github", link: "https://github.com/HARUKei66494739/recognize/" }
    ]
  }
})