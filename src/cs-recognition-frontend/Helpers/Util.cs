using Haru.Kei.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haru.Kei.Helpers;

static class Util {

	public static bool IsFullPath(string path) {
		var t = global::System.IO.Path.GetFullPath(path);
		return (t.ToLower() == path.ToLower());
	}
}
