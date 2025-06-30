using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceLink;
public class VoiceLinkException : Exception {
	public VoiceLinkException() { }
	public VoiceLinkException(string m) : base(m) { }
	public VoiceLinkException(string m, Exception e) : base(m, e) { }
}

