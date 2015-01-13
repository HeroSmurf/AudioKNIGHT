using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight.Models
{
	/// <summary>
	/// Represents an allowable output format
	/// </summary>
	[Serializable]
	public enum OutputFormat
	{
		/// <summary>
		/// Pulse-code modulation
		/// </summary>
		WAV,

		/// <summary>
		/// Windows Media Audio
		/// </summary>
		WMA,

		/// <summary>
		/// Audio based on the MP3 standard
		/// </summary>
		MP3,

		/// <summary>
		/// The audio counterpart of MP4 video
		/// </summary>
		M4A
	}
}
