using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AudioKnight.Models;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace AudioKnight
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : MetroWindow
	{
		/// <summary>
		/// Gets or sets the service used to persist settings
		/// </summary>
		private PersistenceService _persistService;

		/// <summary>
		/// Gets or sets a view model representing the settings
		/// </summary>
		private SettingsViewModel _viewModel;

		/// <summary>
		/// Used to indicate that the user has changed the recording hotkey
		/// </summary>
		public event EventHandler<HotKey> HotkeyChanged;

		public SettingsWindow(PersistenceService persistService)
		{
			InitializeComponent();
			_persistService = persistService;
			_viewModel = new SettingsViewModel();
			_viewModel.OutputFolder = _persistService.GetOutputFolder();
			_viewModel.Hotkey = _persistService.GetRecordingHotkey();
			_viewModel.UseTempFolder = _persistService.GetUseTempFolder();

			RecordingBox.Text = _viewModel.HotkeyDisplay;
			RecordingBox.PreviewKeyDown += HandleRecordingShortcutInput;

			this.DataContext = _viewModel;
		}

		/// <summary>
		/// Handles an update to the recording hotkey -- can't data bind here
		/// </summary>
		/// <param name="args">Indicates what changed</param>
		public void HandleRecordingShortcutInput(
			object sender, KeyEventArgs args)
		{
			args.Handled = true; // this method processes all input

			// determine what key was entered, ignoring modifiers
			Key key = (args.Key == Key.System ? args.SystemKey : args.Key);

			if (key == Key.LeftShift || key == Key.RightShift
				|| key == Key.LeftCtrl || key == Key.RightCtrl
				|| key == Key.LeftAlt || key == Key.RightAlt
				|| key == Key.LWin || key == Key.RWin)
			{
				return;
			}

			HotKey hotkey = new HotKey(key, 
				Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Shift | ModifierKeys.Control), true);
			_viewModel.Hotkey = hotkey;
			RecordingBox.Text = _viewModel.HotkeyDisplay;

			var handler = HotkeyChanged;
			if (handler != null)
			{
				handler(this, hotkey);
			}
		}

		/// <summary>
		/// Indicates if we can save the form, which is true 
		/// whenever all of the fields are valid
		/// </summary>
		private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (this._viewModel == null)
			{
				e.CanExecute = false;
				return;
			}

			// validate the view model
			if (!Directory.Exists(this._viewModel.OutputFolder))
			{
				this._viewModel.AddError("OutputFolder", "The output folder must exist.");
				e.CanExecute = false;
			} else
			{
				this._viewModel.RemoveError("OutputFolder");
				e.CanExecute = true;
			}
		}


		/// <summary>
		/// Saves the form by persisting and re-applying the settings
		/// </summary>
		private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_persistService.PersistOutputFolder(_viewModel.OutputFolder);
			_persistService.PersistRecordingHotkey(_viewModel.Hotkey);

			this.DialogResult = true;
			this.Close();
		}


		/// <summary>
		/// Indicates if the setting change can be cancelled, which is always true
		/// </summary>
		private void Cancel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}


		/// <summary>
		/// Cancels the setting change by closing the form
		/// </summary>
		private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Determines if we can browse for an output folder, which is always true
		/// </summary>
		private void Browse_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		/// <summary>
		/// Opens a dialog used to browse for an output folder
		/// </summary>
		private void Browse_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
			bool? result = dialog.ShowDialog(this);
			if (result.HasValue && result.Value)
			{
				this._viewModel.OutputFolder = dialog.SelectedPath;
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}
}
