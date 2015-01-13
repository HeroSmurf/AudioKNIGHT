using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace AudioKnight.Models
{
	/// <summary>
	/// A top-level view model
	/// </summary>
	public class MainWindowViewModel : BindableObject
	{
		///<summary>Backs the <code>RecordingChannels</code> property</summary>
		private ObservableCollection<ChannelDefinitionViewModel> _recordingChannels;

		/// <summary>
		/// Gets or sets a collection of channels to record from
		/// </summary>
		public ObservableCollection<ChannelDefinitionViewModel> RecordingChannels
		{
			get { return _recordingChannels; }
			set
			{
				_recordingChannels = value;
				if (value != null)
				{
					RecordingChannels.CollectionChanged += (sender, args) =>
						{
							OnPropertyChanged("AnyRecordingChannels");
							CreatePresets();
						};
				}

				OnPropertyChanged();
				CreatePresets();
			}
		}

		///<summary>Backs the <code>RecordingChannelsView</code> property</summary>
		private CollectionViewSource _recordingViewSource;

		/// <summary>
		/// Gets a filtered view of the recording channels
		/// </summary>
		public ICollectionView RecordingChannelsView
		{
			get 
			{
				if (_recordingViewSource == null && RecordingChannels != null)
				{
					_recordingViewSource = new CollectionViewSource();
					_recordingViewSource.Source = RecordingChannels;
					_recordingViewSource.Filter += (sender, args) =>
						{
							args.Accepted = (args.Item as ChannelDefinitionViewModel).PresetName == SelectedPreset;
						};
				}

				return _recordingViewSource  == null ? null : _recordingViewSource.View; 
			}
		}
		

		private void CreatePresets()
		{
			if (RecordingChannels != null)
			{
				Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
					{
						_availablePresets = new ObservableCollection<string>(
							RecordingChannels.Select(r => r.PresetName).Distinct().OrderBy(t => t));
						OnPropertyChanged("AvailablePresets");
					}));
			} else
			{
				Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
					{
						_availablePresets = new ObservableCollection<string>(new[] { ChannelPresetViewModel.DefaultName  });
						OnPropertyChanged("AvailablePresets");
					}));
			}
		}

		///<summary>Backs the <code>AvailablePresets</code> property</summary>
		private ObservableCollection<string> _availablePresets;

		/// <summary>
		/// Gets or sets the presets that 
		/// </summary>
		public ObservableCollection<string> AvailablePresets
		{
			get { return _availablePresets; }
			set
			{
				_availablePresets = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>SelectedPreset</code> property</summary>
		private string _selectedPreset;

		/// <summary>
		/// The name of the currently selected preset
		/// </summary>
		public string SelectedPreset
		{
			get { return _selectedPreset; }
			set
			{
				_selectedPreset = value;
				OnPropertyChanged();
				if (_recordingViewSource != null)
					_recordingViewSource.View.Refresh();
			}
		}

		private CollectionViewSource _audioDevicesViewSource;

		/// <summary>
		/// Used to guide presentation of audio devices
		/// </summary>
		public ICollectionView AudioDevicesView
		{
			get 
			{
				if (_audioDevicesViewSource == null && AudioDevices != null)
				{
					_audioDevicesViewSource = new CollectionViewSource();
					_audioDevicesViewSource.Source = _audioDevices;
					_audioDevicesViewSource.GroupDescriptions.Add(new PropertyGroupDescription("IsPlayback"));
				}
				return _audioDevicesViewSource.View;
			}
		}

		/// <summary>
		/// Gets a value indicating if there are any recording channels set up
		/// </summary>
		public bool AnyRecordingChannels
		{
			get { return RecordingChannels != null && RecordingChannels.Any() ; }
		}

		///<summary>Backs the <code>ConversionStatus</code> property</summary>
		private ConversionStatusViewModel _conversionStatus;

		/// <summary>
		/// Gets or sets the conversion status
		/// </summary>
		public ConversionStatusViewModel ConversionStatus
		{
			get { return _conversionStatus; }
			set
			{
				_conversionStatus = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>RecordingDevices</code> property</summary>
		private ObservableCollection<DeviceViewModel> _audioDevices;

		/// <summary>
		/// Gets or sets the discovered recording devices
		/// </summary>
		public ObservableCollection<DeviceViewModel> AudioDevices
		{
			get { return _audioDevices; }
			set
			{
				_audioDevices = value;
				_audioDevicesViewSource = null;
				OnPropertyChanged();
				OnPropertyChanged("AudioDevicesView");
			}
		}

		///<summary>Backs the <code>SelectedRecordingDevice</code> property</summary>
		private DeviceViewModel _selectedAudioDevice;

		/// <summary>
		/// Gets or sets the recording device currently selected in the view
		/// </summary>
		public DeviceViewModel SelectedAudioDevice
		{
			get { return _selectedAudioDevice; }
			set
			{
				_selectedAudioDevice = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>RecordingVm</code> property</summary>
		private RecordingViewModel _recordingVm;

		/// <summary>
		/// Gets or sets a view model to be used to render
		/// recording progress
		/// </summary>
		public RecordingViewModel RecordingVm
		{
			get { return _recordingVm; }
			set
			{
				_recordingVm = value;
				OnPropertyChanged();
			}
		}

	}
}
