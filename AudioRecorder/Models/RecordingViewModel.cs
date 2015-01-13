using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight.Models
{
	/// <summary>
	/// A view model used to present the progress and status of the recording operation
	/// </summary>
	public class RecordingViewModel : BindableObject, IDisposable
	{
		/// <summary>
		/// A timer used to periodically update the recording duration
		/// </summary>
		private System.Timers.Timer _durationUpdateTimer;

		/// <summary>
		/// Creates a recording view model by 
		/// setting up periodic property updates
		/// </summary>
		public RecordingViewModel()
		{
			_durationUpdateTimer = new System.Timers.Timer(900);
			_durationUpdateTimer.Elapsed += (sender, args) =>
				{
					this.RecordingDuration = DateTime.Now - StartRecordingTime;
				};
			_durationUpdateTimer.Start();
		}

		///<summary>Backs the <code>StartRecordingTime</code> property</summary>
		private DateTime _startRecordingTime = DateTime.Now;

		/// <summary>
		/// Gets or sets the time that recording started
		/// </summary>
		public DateTime StartRecordingTime
		{
			get { return _startRecordingTime; }
			set
			{
				_startRecordingTime = value;
				OnPropertyChanged();
			}
		}


		///<summary>Backs the <code>OutputFiles</code> property</summary>
		private ObservableCollection<OutputFileViewModel> _outputFiles;

		/// <summary>
		/// Gets or sets the output files to present in the view
		/// </summary>
		public ObservableCollection<OutputFileViewModel> OutputFiles
		{
			get { return _outputFiles; }
			set
			{
				_outputFiles = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>RecordingDuration</code> property</summary>
		private TimeSpan _recordingDuration;

		/// <summary>
		/// Gets or sets the length of the current recording
		/// </summary>
		public TimeSpan RecordingDuration
		{
			get { return _recordingDuration; }
			set
			{
				_recordingDuration = value;
				OnPropertyChanged();
				OnPropertyChanged("RecordingDurationDisplay");
			}
		}

		/// <summary>
		/// Gets a representation of the current recording duration
		/// </summary>
		public string RecordingDurationDisplay
		{
			get { return RecordingDuration.ToString("mm\\:ss"); }
		}

		/// <summary>
		/// Disposes of this object by stopping timers and disposing of components
		/// </summary>
		public void Dispose()
		{
			if (_durationUpdateTimer != null)
			{
				_durationUpdateTimer.Stop();
				_durationUpdateTimer.Dispose();
				_durationUpdateTimer = null;
			}

			if (_outputFiles != null)
			{
				foreach (var f in _outputFiles)
				{
					f.Dispose();
				}
				_outputFiles = null;

			}
		}
	}
}
