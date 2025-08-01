﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haru.Kei.Models;
class CommandOptions {
	public enum VoiceClientType {
		voiceroid,
		voiceroid2,
		voicepeak,
		aivoice,
		aivoice2,
		ceviocs,
		cevioai
	}

	[Option("master", Required = false, HelpText = "-")]
	public int Master { get; set; }
	[Option("port", Required = true, HelpText = "-")]
	public int Port { get; set; }
	[Option("voice", Required = true, HelpText = " - ")]
	public VoiceClientType Voice { get; set; }
	[Option("client", Required = true, HelpText = "-")]
	public string Client { get; set; } = "";
	[Option("launch", Required = false, HelpText = "-")]
	public bool Launch { get; set; }

	[Option("log_dir", Required = false, HelpText = "-")]
	public string LogDir { get; set; } = "";
	/*
	[Option("log_", Required = false, HelpText = "-")]
	public bool Launch { get; set; }
	*/
	[Option("kana", Required = false, HelpText = "-")]
	public bool Kana { get; set; } = false;
	[Option("notify_icon", Required = false, HelpText = "-")]
	public bool NotifyIcon { get; set; } = false;
	[Option("debug", Required = false, HelpText = "-")]
	public bool Debug { get; set; } = false;
	[Option("capture_pause", Required = false, HelpText = "-")]
	public float CapturePauseSec { get; set; } = 0.75f;


	[Option("cevio_cast", Required = false, HelpText = "-")]
	public string CeVioCast { get; set; } = "";
	[Option("cevio_volume", Required = false, HelpText = "-")]
	public uint CeVioVolume { get; set; } = 100u;
	[Option("cevio_speed", Required = false, HelpText = "-")]
	public uint CeVioSpeed { get; set; } = 100u;
	[Option("cevio_tone", Required = false, HelpText = "-")]
	public uint CeVioTone { get; set; } = 100u;
	[Option("cevio_tone_scale", Required = false, HelpText = "-")]
	public uint CeVioToneScale { get; set; } = 100u;
	[Option("cevio_alpha", Required = false, HelpText = "-")]
	public uint CeVioAlpha { get; set; } = 100u;
	[Option("cevio_components", Separator = ',', Required = false, HelpText = "-")]
	public IEnumerable<string> CeVioComponents { get; set; } = Array.Empty<string>();

	public (bool IsValid, string ErrorText) Validate() {
		var retText = new StringBuilder();

		if (!string.IsNullOrEmpty(this.LogDir) && !Directory.Exists(this.LogDir)) {
			retText.AppendLine("ログディレクトリが見つかりません");
		}

		if (this.CapturePauseSec < 0) {
			retText.AppendLine("CAPTURE:負の値は指定できません");
		}

		if (this.Voice switch {
			VoiceClientType.ceviocs => true,
			VoiceClientType.cevioai => true,
			_ => false
		}) {
			if(string.IsNullOrEmpty(this.CeVioCast)) {
				retText.AppendLine("CeVIO:cevio_castが入力されていません");
			}

			if(100u < this.CeVioVolume) {
				retText.AppendLine("CeVIO:cevio_volumeの有効範囲は0～100です");
			}
			if (100u < this.CeVioSpeed) {
				retText.AppendLine("CeVIO:cevio_speedの有効範囲は0～100です");
			}
			if (100u < this.CeVioTone) {
				retText.AppendLine("CeVIO:cevio_toneの有効範囲は0～100です");
			}
			if (100u < this.CeVioToneScale) {
				retText.AppendLine("CeVIO:cevio_tone_scaleの有効範囲は0～100です");
			}
			if (100u < this.CeVioAlpha) {
				retText.AppendLine("CeVIO:cevio_componentsの書式が間違っています");
			}
			try {
				this.ParseCeVioComponents();
			}
			catch(FormatException) {
				retText.AppendLine("CeVIO:cevio_alphaの有効範囲は0～100です");
			}
		}
		return (retText.Length == 0, retText.ToString());
	}

	public IEnumerable<(string, uint)> ParseCeVioComponents() {
		var ret = new List<(string, uint)>();
		foreach (var it in this.CeVioComponents) {
			var s = it.Split(':');
			if (s.Length != 2) {
				throw new FormatException();
			}

			if (uint.TryParse(s[1], out var v)) {
				if (100u < v) {
					throw new FormatException();
				}
				ret.Add((s[0], v));
			} else {
				throw new FormatException();
			}
		}
		return ret.AsReadOnly();
	}

}
