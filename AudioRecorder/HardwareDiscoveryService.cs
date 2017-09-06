using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using AudioKnight.Models;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AudioKnight
{
	/// <summary>
	/// Provides functionality to find audio devices on the system
	/// </summary>
	public class HardwareDiscoveryService : IDisposable
	{
		static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(HardwareDiscoveryService));

		private const int PID_FIRST_USABLE = 2;

		private Guid DeviceDescription = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0);
		private int DeviceDescriptionPid = 2;

		private Guid DeviceFriendlyName = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0);
		private int DeviceFriendlyNamePid = 14;

		private Guid DeviceInterfaceFriendlyName = new Guid(0x026e516e, 0xb814, 0x414b, 0x83, 0xcd, 0x85, 0x6d, 0x6f, 0xef, 0x48, 0x22);
		private int DeviceInterfaceFriendlyNamePid = 2;

		private Guid DeviceInterfaceEnabled = new Guid(0x026e516e, 0xb814, 0x414b, 0x83, 0xcd, 0x85, 0x6d, 0x6f, 0xef, 0x48, 0x22);
		private int DeviceInterfaceEnabledPid = 3;

		private Guid DeviceClassIcon = new Guid(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66);
		private int DeviceClassIconPid = 4;

		private Guid DeviceClassIconPath = new Guid(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66);
		private int DeviceClassIconPathPid = 12;

		[DllImport("shell32.dll")]
		static extern IntPtr ExtractIcon(IntPtr hInst, string Filename, int IconIndex);

		[DllImport("user32.dll")]
		static extern IntPtr LoadBitmap(IntPtr hInstance, string lpBitmapName);

		[DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
		public static extern int PathParseIconLocation([In, Out] string pszIconFile);

		/// <summary>
		/// Used to listen for events affecting sound devices
		/// </summary>
		private ManagementEventWatcher _watcher;

		/// <summary>
		/// An event used to indicate that something 
		/// about the device configuration has changed
		/// </summary>
		public event EventHandler DevicesChanged;
		
		/// <summary>
		/// The handle of the window
		/// </summary>
		private IntPtr _primaryWindowHandle;

		/// <summary>
		/// Creates a hardware discovery service with the given primary window handle
		/// </summary>
		/// <param name="primaryWindowHandle">The handle of the primary window</param>
		public HardwareDiscoveryService(IntPtr primaryWindowHandle)
		{
			_primaryWindowHandle = primaryWindowHandle;
		}

		/// <summary>
		/// Initiates a process that listens for hardware change notifications
		/// </summary>
		public void StartListening()
		{
			if (_watcher == null)
			{
				WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent");
				_watcher = new ManagementEventWatcher(query);

				_watcher.EventArrived += new EventArrivedEventHandler((sender, args) =>
				{
					var handler = DevicesChanged;
					if (handler != null)
					{
						handler(this, new EventArgs());
					}
				});

				_watcher.Start();
			}
		}

		/// <summary>
		/// Gets all audio devices that are either active, disabled, or unplugged
		/// </summary>
		/// <param name="primaryWindow">The window to use when reading bitmaps with P/invoke</param>
		/// <returns>A generator of audio devices</returns>
		public IEnumerable<DeviceViewModel> GetAudioDevices(MainWindow primaryWindow)
		{
			// create an enumerator to list devices
			MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
			MMDeviceCollection deviceCol = deviceEnum.EnumerateAudioEndPoints(
				DataFlow.All, NAudio.CoreAudioApi.DeviceState.Active 
				);

            //NAudio.CoreAudioApi.DeviceState.Disabled | 
			//NAudio.CoreAudioApi.DeviceState.Unplugged
			
			var deviceList = new List<DeviceViewModel>();

			foreach(MMDevice device in deviceCol)
			{
				DeviceViewModel deviceViewModel = new DeviceViewModel();
				deviceViewModel.UniqueId = device.ID;

				// determine if the device is playback or recording based on the data flow direction
				if (device.DataFlow == DataFlow.Render)
				{
					deviceViewModel.IsPlayback = true;
				}

				if(device.State == NAudio.CoreAudioApi.DeviceState.Active)
				{
					deviceViewModel.DeviceState = AudioKnight.Models.DeviceState.Active;
				} else if (device.State == NAudio.CoreAudioApi.DeviceState.Disabled)
				{
					deviceViewModel.DeviceState = AudioKnight.Models.DeviceState.Disabled;
				} else if (device.State == NAudio.CoreAudioApi.DeviceState.Unplugged)
				{
					deviceViewModel.DeviceState = AudioKnight.Models.DeviceState.Unplugged;
				} else
				{
					deviceViewModel.DeviceState = AudioKnight.Models.DeviceState.Unknown;
				}

				// read the device name, description, and icon
				deviceViewModel.InterfaceName = device.DeviceFriendlyName;

				// scan for device icon property
				string iconPath = null, displayName = null;
				for(int i = 0; i < device.Properties.Count; i++)
				{
					var currProp = device.Properties.Get(i);
					if(currProp.propertyId == DeviceClassIconPathPid && 
						currProp.formatId == DeviceClassIconPath)
					{
						iconPath = GetValue(device.Properties.GetValue(i)) as string;
					} else if (currProp.propertyId == DeviceDescriptionPid &&
						currProp.formatId == DeviceDescription)
					{
						displayName = GetValue(device.Properties.GetValue(i)) as string;
					}
				}

				deviceViewModel.DisplayName = displayName;

				// parse the icon path into DLL, index components
				int iconIndex = PathParseIconLocation(iconPath);

				// read the image from a DLL into a bitmap
				var icon = Icon.FromHandle(ExtractIcon(_primaryWindowHandle, iconPath, iconIndex));
				Image img = icon.ToBitmap();
				MemoryStream conversionStream = new MemoryStream();
				img.Save(conversionStream, ImageFormat.Png);

				conversionStream.Seek(0, SeekOrigin.Begin);
				BitmapImage bitmapImg = new BitmapImage();
				bitmapImg.BeginInit();
				bitmapImg.StreamSource = conversionStream;
				bitmapImg.EndInit();
				bitmapImg.Freeze();

				deviceViewModel.DeviceIcon = bitmapImg;

				yield return deviceViewModel;
			}
		}

		/// <summary>
		/// Converts a variant type into a .NET object
		/// </summary>
		/// <returns>The converted property value</returns>
		public static object GetValue(PropVariant propVariant)
        {
            switch (propVariant.DataType)
            {
                case VarEnum.VT_BOOL:
                    return (bool) propVariant.Value;
                case VarEnum.VT_UI4:
					return (uint)propVariant.Value;
                case VarEnum.VT_LPWSTR:
					return (string)propVariant.Value;
                case VarEnum.VT_CLSID:
                    return (Guid) propVariant.Value;
                case VarEnum.VT_BLOB:
                    return (byte[]) propVariant.Value;
				default:
					return null;
            }
        }

		/// <summary>
		/// Disposes of the discovery service by stopping the management event watcher
		/// </summary>
		public void Dispose()
		{
			if (_watcher != null)
			{
				_watcher.Stop();
				_watcher.Dispose();
				_watcher = null;
			}
		}
	}
}
