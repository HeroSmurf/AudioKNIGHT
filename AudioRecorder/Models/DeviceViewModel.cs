using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AudioKnight.Models
{

	/// <summary>
	/// Represent allowable status codes for a device
	/// </summary>
	public enum DeviceState
	{
		Active,
		Unplugged,
		Disabled,
		Unknown
	}

	/// <summary>
	/// A view model for a hardware device
	/// </summary>
	public class DeviceViewModel : BindableObject
	{

		///<summary>Backs the <code>UniqueId</code> property</summary>
		private string _uniqueId;

		/// <summary>
		/// An OS-generated unique identifier for this device
		/// </summary>
		public string UniqueId
		{
			get { return _uniqueId; }
			set
			{
				_uniqueId = value;
				OnPropertyChanged();
			}
		}


		///<summary>Backs the <code>DisplayName</code> property</summary>
		private string _displayName;

		/// <summary>
		/// Gets or sets the name of the device
		/// </summary>
		public string DisplayName
		{
			get { return _displayName; }
			set
			{
				_displayName = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>InterfaceName</code> property</summary>
		private string _interfaceName;

		/// <summary>
		/// The more technical name of the sound interface
		/// </summary>
		public string InterfaceName
		{
			get { return _interfaceName; }
			set
			{
				_interfaceName = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>DeviceIcon</code> property</summary>
		private BitmapSource _deviceIcon;

		/// <summary>
		/// An icon to display alongside the device
		/// </summary>
		public BitmapSource DeviceIcon
		{
			get { return _deviceIcon; }
			set
			{
				_deviceIcon = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>IsPlayback</code> property</summary>
		private bool _isPlayback;

		/// <summary>
		/// Indicates if this is a recording or playback device
		/// </summary>
		public bool IsPlayback
		{
			get { return _isPlayback; }
			set
			{
				_isPlayback = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>MyProperty</code> property</summary>
		private DeviceState _deviceState;

		/// <summary>
		/// Gets or sets the state of the device
		/// </summary>
		public DeviceState DeviceState
		{
			get { return _deviceState; }
			set
			{
				_deviceState = value;
				OnPropertyChanged();
				OnPropertyChanged("DeviceStateDisplay");
			}
		}
		
		/// <summary>
		/// Gets a user-friendly view of the device state
		/// </summary>
		public string DeviceStateDisplay
		{
			get
			{
				if (DeviceState == Models.DeviceState.Unknown)
				{
					return "Unknown";
				} else if (DeviceState == Models.DeviceState.Disabled)
				{
					return "Disabled";
				} else if (DeviceState == Models.DeviceState.Active)
				{
					return "Ready";
				} else if (DeviceState == Models.DeviceState.Unplugged)
				{
					return "Not plugged in";
				} else
				{
					return "Unknown";
				}
			}
		}

	}
}
