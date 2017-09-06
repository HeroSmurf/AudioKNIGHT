using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AudioKnight.Models;
using MahApps.Metro.Controls;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using AudioKnight.WpfExtensions;

namespace AudioKnight
{
	/// <summary>
	/// The primary window for the Audio Recorder, presenting controls to manage the audio capture process
	/// </summary>
	public partial class MainWindow
	{
		static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(MainWindow));

		/// <summary>
		/// The view model to use when presenting data
		/// </summary>
		private MainWindowViewModel _viewModel;

		/// <summary>
		/// The service used to list and manipulate audio devices
		/// </summary>
		private HardwareDiscoveryService _hardwareService;

		private PersistenceService _persistenceService;

		private CaptureService _captureService;

		private object _audioDevicesLock = new object();


		/// <summary>
		/// Used to add and remove hotkeys from the application
		/// </summary>
		private HotKeyHost _hotkeyHost;

		/// <summary>
		/// The hotkey used to start and stop recording
		/// </summary>
		private HotKey _hotkey;

		/// <summary>
		/// Initializes the main window by hooking up events
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

			_viewModel = new MainWindowViewModel();
			this.DataContext = _viewModel;

			this.Loaded += MainWindow_Loaded;
			this.Closing += MainWindow_Closing;

			_viewModel.RecordingChannels = new ObservableCollection<ChannelDefinitionViewModel>();
			_viewModel.AudioDevices = new ObservableCollection<DeviceViewModel>();
			BindingOperations.EnableCollectionSynchronization(_viewModel.AudioDevices, _audioDevicesLock);

			_persistenceService = new PersistenceService();
			_captureService = new CaptureService();

			// load the set of devices
			_hardwareService = new HardwareDiscoveryService(new WindowInteropHelper(this).Handle);
			_hardwareService.DevicesChanged += (sender, args) => LoadDevices();
			_hardwareService.StartListening();
            //_hardwareService.
			LoadDevices();

			// load the stored channels and convert them to view models
			var channels = _persistenceService.LoadChannels();
			foreach (var channel in channels)
			{
				var vm = new ChannelDefinitionViewModel()
				{ 
					AllowableOutputFormats = new ObservableCollection<OutputFormat>(_captureService.AllowableOutputFormats)
				};

				vm.InjectFrom(channel);
				_viewModel.RecordingChannels.Add(vm);
			}

			_viewModel.SelectedPreset = _persistenceService.GetSelectedPreset();

			TransitioningControl.Content = new ChannelSetupControl();
		}

		/// <summary>
		/// Opens the settings window
		/// </summary>
		public void OpenSettings_Executed(object sender, EventArgs args)
		{
			SettingsWindow window = new SettingsWindow(_persistenceService);
			window.Owner = this;
			window.HotkeyChanged += (s, hotkey) =>
				{
					_hotkeyHost.RemoveHotKey(_hotkey);
					SetupHotKey(hotkey);
				};
			window.ShowDialog();
		}

		/// <summary>
		/// Handles loading of the window by associating event arguments
		/// </summary>
		public void MainWindow_Loaded(object sender, EventArgs args)
		{
			_hotkeyHost = new HotKeyHost((HwndSource)HwndSource.FromVisual(App.Current.MainWindow));
			SetupHotKey(_persistenceService.GetRecordingHotkey());
		}

		/// <summary>
		/// Handles close events by disposing of internal state
		/// </summary>
		public void MainWindow_Closing(object sender, CancelEventArgs args)
		{

            if (_viewModel.RecordingVm != null)
            {
                this.StopRecording_Executed(null, null);
            }

            if (_viewModel.ConversionStatus != null && !_viewModel.ConversionStatus.IsComplete)
			{
				TaskDialog dialog = new TaskDialog();
				int numPending = _viewModel.ConversionStatus.FilesPendingConversion.Count;
				dialog.Content = string.Format("There {0} {1} file{2} pending compression/conversion. " +
					"Are you sure you want to close the application and stop the compression?", 
					numPending == 1 ? "is" : "are", numPending, numPending == 1 ? "" : "s" );
				dialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));
				dialog.Buttons.Add(new TaskDialogButton(ButtonType.Close));
				var result = dialog.ShowDialog(this);
				if (result.ButtonType == ButtonType.Cancel)
				{
					args.Cancel = true;
					return;
				}
			}

			_hardwareService.Dispose();

			// convert the channel view models into channels
			var channels = (from c in _viewModel.RecordingChannels
							select new ChannelDefinition().InjectFrom(c))
							.Cast<ChannelDefinition>();

			_persistenceService.PersistChannels(channels);
			_persistenceService.StoreSelectedPreset(_viewModel.SelectedPreset);
		}

		/// <summary>
		/// Loads all playback and recording devices into the view, assuming the hardware
		/// service and the view model have been initialized.
		/// </summary>
		private void LoadDevices()
		{
			IEnumerable<DeviceViewModel> audioDevices = _hardwareService.GetAudioDevices(this);

			lock (_audioDevicesLock)
			{
				_viewModel.AudioDevices = new ObservableCollection<DeviceViewModel>(audioDevices);
			}
		}

		/// <summary>
		/// Determines if we can add a channel, true whenever a recording device is selected
		/// </summary>
		private void AddChannel_CanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = _viewModel != null && _viewModel.SelectedAudioDevice != null;
		}

		/// <summary>
		/// Adds a channel for the currently selected recording device
		/// </summary>
		private void AddChannel_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			this._viewModel.RecordingChannels.Add(new ChannelDefinitionViewModel()
				{
					 Name = _viewModel.SelectedAudioDevice.DisplayName,
					 DeviceName = _viewModel.SelectedAudioDevice.DisplayName,
					 DeviceId = _viewModel.SelectedAudioDevice.UniqueId,
					 PresetName = _viewModel.SelectedPreset,
					 Format = OutputFormat.WAV,
					 IsEnabled = true,
					 AllowableOutputFormats = new ObservableCollection<OutputFormat>(_captureService.AllowableOutputFormats)
				});
		}

		/// <summary>
		/// Adds a new preset
		/// </summary>
		private void AddPreset_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			NewPresetDialog dialog = new NewPresetDialog();
			dialog.Owner = this;
			bool? result = dialog.ShowDialog();
			if (result.HasValue && result.Value)
			{
				if (_viewModel.AvailablePresets == null)
				{
					_viewModel.AvailablePresets = new ObservableCollection<string>();	
				}

				if (!_viewModel.AvailablePresets.Contains(dialog.EnteredText))
				{
					_viewModel.AvailablePresets.Add(dialog.EnteredText);
					_viewModel.SelectedPreset = dialog.EnteredText;
				}
			}
		}

		/// <summary>
		/// Removes the currently selected preset and any associated channel definitions
		/// </summary>
		private void DeletePreset_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			if (_viewModel != null && _viewModel.AvailablePresets != null)
			{
				var presetToRemove = _viewModel.SelectedPreset;
				_viewModel.RecordingChannels.RemoveAll(r => r.PresetName == presetToRemove);
				_viewModel.SelectedPreset = _viewModel.AvailablePresets.First();
				_viewModel.AvailablePresets.Remove(presetToRemove);
			}
		}

		/// <summary>
		/// Deletes the channel which is supplied by way of the command parameter
		/// </summary>
		private void DeleteChannel_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			this._viewModel.RecordingChannels.Remove(args.Parameter as ChannelDefinitionViewModel);
		}

		/// <summary>
		/// Determines if a channel can be deleted, true whenever the channel parameter is non-null
		/// </summary>
		private void DeleteChannel_CanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = args.Parameter != null && args.Parameter is ChannelDefinitionViewModel;
		}

		/// <summary>
		/// Determines if we can start recording, which is true whenever there is at least 1 enabled channel
		/// </summary>
		private void StartRecording_CanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = _viewModel.RecordingChannels != null &&
				_viewModel.RecordingChannels.Any(r => r.IsEnabled);
		}

		/// <summary>
		/// Starts recording on all enabled channels
		/// </summary>
		private void StartRecording_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			var recordingVm = new RecordingViewModel();
			this._viewModel.RecordingVm = recordingVm;
			var control = new RecordingStatusControl();
			TransitioningControl.Content = control;

			try
			{
				var selectedChannels = _viewModel.RecordingChannels.Where(c => c.PresetName == _viewModel.SelectedPreset);
				string outputFolder;
				if (_persistenceService.GetUseTempFolder())
					outputFolder = _persistenceService.GetTempFolder();
				else
					outputFolder = _persistenceService.GetOutputFolder();
				
				var outputfiles = _captureService.StartRecording(selectedChannels, outputFolder);

				// nothing to record -- either not plugged in or not enabled
			    var outputFileViewModels = outputfiles as OutputFileViewModel[] ?? outputfiles.ToArray();
			    if (!outputFileViewModels.Any())
				{
					MessageBox.Show("Warning: Either no channels are enabled or none " + 
						"of the associated devices are ready. No recording has been started.");
					StopRecording_Executed(null, null);
					return;
				}

				_viewModel.RecordingVm.OutputFiles =
					new ObservableCollection<OutputFileViewModel>(outputFileViewModels);
			} catch (Exception ex)
			{
				//MessageBox.Show(string.Format("Nope. No recording!\n{0}", ex.Message));
                Log.Debug($"Nope. No recording!\n{ex.Message}");
			}
		}

		/// <summary>
		/// Indicates if we can stop recording, which is true whenever we are actively recording
		/// </summary>
		private void StopRecording_CanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = _viewModel.RecordingVm != null;
		}

		/// <summary>
		/// Stops recording of the audio streams and switches back to the channel definition page
		/// </summary>
		private async void StopRecording_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			try
			{
				TransitioningControl.Content = new ChannelSetupControl();
				_viewModel.ConversionStatus = _captureService.GetConversionStatus();
				await _captureService.StopRecording(_persistenceService.GetOutputFolder());

				_viewModel.RecordingVm.Dispose();
				_viewModel.RecordingVm = null;
			} catch (Exception ex)
			{
				MessageBox.Show(string.Format("Conversion of the recording output failed!\n{0}", ex.Message));
			}
		}

		/// <summary>
		/// Attaches the specified hotkey to this window
		/// </summary>
		/// <param name="hkey">The hotkey to register</param>
		private void SetupHotKey(HotKey hkey)
		{
			_hotkey = hkey;
			_hotkey.HotKeyPressed += new EventHandler<HotKeyEventArgs>(delegate(Object o, HotKeyEventArgs e)
			{
				if (_viewModel.RecordingVm != null)
				{
					this.StopRecording_Executed(null, null);
				} else
				{
					this.StartRecording_Executed(null, null);
				}
			});

			_hotkeyHost.AddHotKey(_hotkey);
		}
		
	}
}
