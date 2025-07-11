using CommandLine;
using System;
using System.Collections.Generic;
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

	[Option("capture_pause", Required = false, HelpText = "-")]
	public float CapturePauseSec { get; set; } = 0.75f;

}
