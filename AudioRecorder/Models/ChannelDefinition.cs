using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioKnight.Models;

namespace AudioKnight.Models
{
	/// <summary>
	/// Represents a user-configured audio input channel for persistent storage.
	/// </summary>
	[Serializable]
	public class ChannelDefinition
	{
		/// <summary>
		/// Gets or sets the user-friendly name of the channel
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the name which is used to look up registered hardware devices
		/// </summary>
		public string DeviceName { get; set; }

		/// <summary>
		/// Gets or sets the a unique identifier for the device
		/// </summary>
		public string DeviceId { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if this channel will be recorded from
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Gets or sets the format of the recorded content
		/// </summary>
		public OutputFormat Format { get; set; }

		/// <summary>
		/// Gets or sets the name of the device preset that 
		/// this channel definition is associated with
		/// </summary>
		public string PresetName { get; set; }
				
	}
}
