using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight.Models
{
	/// <summary>
	/// Models a channel definition in the view
	/// </summary>
	public class ChannelDefinitionViewModel : BindableObject
	{
		///<summary>Backs the <code>Name</code> property</summary>
		private string _name;

		/// <summary>
		/// Gets or sets the user-friendly name of the channel
		/// </summary>
		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>DeviceName</code> property</summary>
		private string _deviceName;

		/// <summary>
		/// Gets or sets the token used to identify a hardware device
		/// </summary>
		public string DeviceName
		{
			get { return _deviceName; }
			set
			{
				_deviceName = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>DeviceId</code> property</summary>
		private string _deviceId;

		/// <summary>
		/// Gets or sets a unique identifier for the device to be recorded from
		/// </summary>
		public string DeviceId
		{
			get { return _deviceId; }
			set
			{
				_deviceId = value;
				OnPropertyChanged();
			}
		}


		///<summary>Backs the <code>IsEnabled</code> property</summary>
		private bool _isEnabled;

		/// <summary>
		/// Gets or sets a value indicating if the channel will be recorded from
		/// </summary>
		public bool IsEnabled
		{
			get { return _isEnabled; }
			set
			{
				_isEnabled = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>Format</code> property</summary>
		private OutputFormat _format;

		/// <summary>
		/// Gets or sets the format of the output file
		/// </summary>
		public OutputFormat Format
		{
			get { return _format; }
			set
			{
				_format = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>AllowableOutputFormats</code> property</summary>
		private ObservableCollection<OutputFormat> _outputFormats;

		/// <summary>
		/// Gets or sets the output formats that can be assigned to this channel
		/// </summary>
		public ObservableCollection<OutputFormat> AllowableOutputFormats
		{
			get { return _outputFormats; }
			set
			{
				_outputFormats = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>PresetName</code> property</summary>
		private string _presetName;

		/// <summary>
		/// Gets or sets the name of the preset that this definition is created for
		/// </summary>
		public string PresetName
		{
			get { return string.IsNullOrWhiteSpace(_presetName) ? ChannelPresetViewModel.DefaultName : _presetName; }
			set
			{
				_presetName = value;
				OnPropertyChanged();
			}
		}
	}
}
