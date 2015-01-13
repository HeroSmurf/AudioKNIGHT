using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight.Models
{
	/// <summary>
	/// Used to model application settings in the view
	/// </summary>
	public class SettingsViewModel : BindableObject
	{
		///<summary>Backs the <code>OutputFolder</code> property</summary>
		private string _outputFolder;

		/// <summary>
		/// Gets or sets the output folder to store settings in
		/// </summary>
		public string OutputFolder
		{
			get { return _outputFolder; }
			set
			{
				_outputFolder = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>UseTempFolder</code> property</summary>
		private bool _useTempFolder;

		/// <summary>
		/// Gets or sets a value indicating if we should use a temp directory when storing active recordings
		/// </summary>
		public bool UseTempFolder
		{
			get { return _useTempFolder; }
			set
			{
				_useTempFolder = value;
				OnPropertyChanged();
			}
		}


		///<summary>Backs the <code>Hotkey</code> property</summary>
		private HotKey _hotkey;

		/// <summary>
		/// Gets or sets the hotkey that will be used to start/stop recording
		/// </summary>
		public HotKey Hotkey
		{
			get { return _hotkey; }
			set
			{
				_hotkey = value;
				OnPropertyChanged();
				OnPropertyChanged("HotkeyDisplay");
			}
		}

		/// <summary>
		/// Represents a the hotkey that will be used to start and stop recording
		/// </summary>
		public string HotkeyDisplay
		{
			get { return Hotkey.ToString(); }
		}

	}
}
