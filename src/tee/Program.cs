using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Haru.Kei {
	static class Tee {
		static int Main(string[] args) {
			if(!args.Any()) {
				Console.WriteLine("usage: tee [OPTION] output-file");
				return 1;
			}

			var regex = new Regex(@"(c:\\{1,2}users\\{1,2})([^\\]+)", RegexOptions.IgnoreCase);
			var file = args.Last();
			var isMask = (args.Reverse().Skip(1).Where(x => x == "--mask").Any());
			var line = "";
			try {
				using(var stream = new StreamWriter(file, true, Encoding.UTF8)) {
					while((line = Console.ReadLine()) != null) {
						Console.WriteLine(line);
						if(isMask) {
							line = regex.Replace(line, @"$1***");
						}
						stream.WriteLine(line);
						stream.Flush();
					}
				}
				return 0;
			}
			catch(Exception ex) {
				Console.WriteLine(ex);
			}
			return 1;
		}
	}
}




