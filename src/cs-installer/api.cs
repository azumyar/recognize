using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haru.Kei {
	internal class GitHubApiResponse {
		[JsonProperty("assets")]
		public IEnumerable<GitHubApiAsset> Assets { get; private set; }
	}

	internal class GitHubApiAsset {
		[JsonProperty("name")]
		public string Name { get; private set; }
		[JsonProperty("size")]
		public long Size { get; private set; }
		[JsonProperty("browser_download_url")]
		public string BrowserDownloadUrl { get; private set; }
	}
}
