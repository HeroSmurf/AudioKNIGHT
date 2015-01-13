using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight.Models
{
	/// <summary>
	/// Represents an individual recording output
	/// </summary>
	public class OutputFileViewModel : BindableObject, IDisposable
	{
		/// <summary>
		/// A timer used to periodically update the file size
		/// </summary>
		private System.Timers.Timer _updateSizeTimer;

		/// <summary>
		/// Sets up an output file by hooking up periodic property updates
		/// </summary>
		public OutputFileViewModel()
		{
			_updateSizeTimer = new System.Timers.Timer(3000);
			_updateSizeTimer.Elapsed += (sender, args) =>
				{
					if(!string.IsNullOrEmpty(this.OutputPath))
					{
						FileInfo info = new FileInfo(this.OutputPath);
						if(info.Exists)
						{
							this.OutputSizeBytes = info.Length;
						}
					}
				};
			_updateSizeTimer.Start();
		}

		///<summary>Backs the <code>ChannelDefinition</code> property</summary>
		private ChannelDefinitionViewModel _channelDef;

		/// <summary>
		/// Gets or sets the channel definition used to create the output file
		/// </summary>
		public ChannelDefinitionViewModel ChannelDefinition
		{
			get { return _channelDef; }
			set
			{
				_channelDef = value;
				OnPropertyChanged();
			}
		}

		///<summary>Backs the <code>OutputPath</code> property</summary>
		private string _outputPath;

		/// <summary>
		/// Gets or sets the full path to the output file being created
		/// </summary>
		public string OutputPath
		{
			get { return _outputPath; }
			set
			{
				_outputPath = value;
				OnPropertyChanged();
				OnPropertyChanged("TruncatedOutputPath");
			}
		}

		/// <summary>
		/// Gets a representation of the output path truncated to 32 characters
		/// </summary>
		public string TruncatedOutputPath
		{
			get 
			{ 
				if (OutputPath == null || OutputPath.Length < 65)
					return OutputPath;
				else
					return OutputPath.Substring(0, 15) + "..." + OutputPath.Substring((OutputPath.Length - 50), 50); 
			}
		}

		///<summary>Backs the <code>OutputSizeBytes</code> property</summary>
		private long _outputSizeBytes;

		/// <summary>
		/// Gets or sets the current length of the output file in bytes
		/// </summary>
		public long OutputSizeBytes
		{
			get { return _outputSizeBytes; }
			set
			{
				_outputSizeBytes = value;
				OnPropertyChanged();
				OnPropertyChanged("OutputSizeBytesDisplay");
			}
		}

		/// <summary>
		/// Gets or sets a user-friendly representation of the current output file size 
		/// </summary>
		public string OutputSizeBytesDisplay
		{
			get
			{
				string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
				if (OutputSizeBytes == 0)
					return "0" + suf[0];

				long bytes = Math.Abs(OutputSizeBytes);
				int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
				double num = Math.Round(bytes / Math.Pow(1024, place), 1);
				return (Math.Sign(OutputSizeBytes) * num).ToString() + suf[place];
			}
		}

		/// <summary>
		/// Disposes of this instance by shutting down update timers
		/// </summary>
		public void Dispose()
		{
			if (_updateSizeTimer != null)
			{
				_updateSizeTimer.Stop();
				_updateSizeTimer.Dispose();
				_updateSizeTimer = null;
			}
		}
	}
}
