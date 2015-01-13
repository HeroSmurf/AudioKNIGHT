using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AudioKnight.Models;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Collections.ObjectModel;
using AudioKnight.WpfExtensions;

namespace AudioKnight
{
	/// <summary>
	/// Provides functionality to capture from multiple audio input streams concurrently
	/// </summary>
	public class CaptureService
	{

		static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(CaptureService));

		const uint SPEAKER_FRONT_LEFT = 0x1;
		const uint SPEAKER_FRONT_RIGHT = 0x2;
		const uint KSAUDIO_SPEAKER_STEREO = (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT);

		/// <summary>
		/// Represents information about a currently recording recording job
		/// </summary>
		private class RunningJob
		{
			public RunningJob(WasapiCapture capture, WaveFileWriter writer, WasapiOut silence)
			{
				this.Capture = capture;
				this.WaveWriter = writer;
				this.SilencePlayer = silence;
			}

			/// <summary>
			/// Gets or sets the capture device used to read the audio signal
			/// </summary>
			public WasapiCapture Capture { get; set; }

			/// <summary>
			/// Gets or sets a device used to play silence to pad out the stream length
			/// </summary>
			public WasapiOut SilencePlayer { get; set; }

			/// <summary>
			/// Gets or sets the writer used to create the WAV file
			/// </summary>
			public WaveFileWriter WaveWriter { get; set; }
		}

		/// <summary>
		/// The output formats supported by the service
		/// </summary>
		private List<AudioKnight.Models.OutputFormat> _allowableOutputFormats =
			new List<AudioKnight.Models.OutputFormat>(new[] 
			{ 
				AudioKnight.Models.OutputFormat.WAV, 
				AudioKnight.Models.OutputFormat.WMA, 
				AudioKnight.Models.OutputFormat.MP3, 
				AudioKnight.Models.OutputFormat.M4A 
			});

		/// <summary>
		/// Gets the output formats supported by this service
		/// </summary>
		public List<AudioKnight.Models.OutputFormat> AllowableOutputFormats
		{
			get { return _allowableOutputFormats; }
		}

		private List<RunningJob> _runningJobs = new List<RunningJob>();
		private List<OutputFileViewModel> _activeOutputs = new List<OutputFileViewModel>();

		private ulong RefTimesPerMillisec = 10000;

		/// <summary>Our progress into the conversion process</summary>
		private ConversionStatusViewModel ConversionStatus; 

		/// <summary>
		/// Creates a capture service
		/// </summary>
		public CaptureService()
		{
		}

		/// <summary>
		/// Starts recording on zero on a sequence of channels
		/// </summary>
		/// <param name="channelDefinitions">The channels to record on</param>
		/// <param name="outputFolder">The folder to store content in while recording</param>
		public IEnumerable<OutputFileViewModel> StartRecording(
			IEnumerable<ChannelDefinitionViewModel> channelDefinitions, 
			string outputFolder)
		{
			// first get only enabled channels
			var enabledChannels = channelDefinitions.Where(c => c.IsEnabled);

			// now get channels for devices that are plugged in 
			MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
			MMDeviceCollection deviceCol = deviceEnum.EnumerateAudioEndPoints(
				DataFlow.All, NAudio.CoreAudioApi.DeviceState.Active);

			// prepare a directory to contain the recordings
			string timestamp = DateTime.Now.ToString("s").Replace(":", ".");
			string fullRecordDirectoryPath = Path.Combine(outputFolder, string.Format("Recording_{0}", timestamp));
			if (!Directory.Exists(fullRecordDirectoryPath))
			{
				Directory.CreateDirectory(fullRecordDirectoryPath);
			}

			// look for devices that match the enabled channels
			_activeOutputs = new List<OutputFileViewModel>();
			_runningJobs = new List<RunningJob>();
			foreach (var channel in enabledChannels)
			{
				var device = deviceCol.FirstOrDefault(d => d.ID == channel.DeviceId || 
							string.IsNullOrEmpty(channel.DeviceId) && d.DeviceFriendlyName == channel.DeviceName);
				if (device != null)
				{
					OutputFileViewModel outputFile = new OutputFileViewModel();
					outputFile.ChannelDefinition = channel;

					WasapiCapture capture;
					WasapiOut silencePlayer = null;
					if (device.DataFlow == DataFlow.Capture)
					{
						capture = new WasapiCapture(device);
					} else
					{
						capture = new WasapiLoopbackCapture(device);
						silencePlayer = new WasapiOut(device, AudioClientShareMode.Shared, false, 100);
						silencePlayer.Init(new SilentSampleProvider());
						silencePlayer.Play();
					}

					// output file name is the channel name
					string filename = SafeName(string.Format("{0}.wav", channel.Name));

					outputFile.OutputPath = Path.Combine(fullRecordDirectoryPath, filename);
					WaveFileWriter writer = new WaveFileWriter(outputFile.OutputPath, capture.WaveFormat);

					capture.DataAvailable += (sender, args) => writer.Write(args.Buffer, 0, args.BytesRecorded);

					try
					{
						capture.StartRecording();
						_runningJobs.Add(new RunningJob(capture, writer, silencePlayer));
						_activeOutputs.Add(outputFile);
						Log.Debug(string.Format("Starting to record {0}", outputFile.OutputPath));
					} catch (ArgumentException ex) // thrown when the wave format is not valid
					{
						if (capture.WaveFormat.Channels > 2 && device.DataFlow == DataFlow.Render)
						{
							MessageBox.Show(String.Format(@"Failed to start recording from {0}.
This device uses a {1}-channel format, which is not supported. You may:
	1) Try to enable 'Stereo Mix' in the Control Panel, 
	2) Use the control panel to switch {0} to 
	   Stereo rather than a Surround format", 
									channel.DeviceName, capture.WaveFormat.Channels),
									"Unsupported Format", MessageBoxButton.OK, MessageBoxImage.Error);
						} else
						{
							throw;
						}
					}
				}
			}

			ConversionStatus = new ConversionStatusViewModel();
			ConversionStatus.FilesPendingConversion.AddAll(_activeOutputs.Select(o => new FileInfo(o.OutputPath)));

			return _activeOutputs;
		}

		/// <summary>
		/// Retrieves the conversion status instance used to report on new conversions
		/// </summary>
		/// <returns>The conversion status view model to report on</returns>
		public ConversionStatusViewModel GetConversionStatus()
		{
			return ConversionStatus;
		}

		/// <summary>
		/// Stops recording and begins conversion of the 
		/// source files to their requested format
		/// </summary>
		/// <param name="targetFolder">The folder to copy content to upon completion</param>
		public async Task StopRecording(string targetFolder)
		{
			foreach (var activeJob in _runningJobs.ToList())
			{
				activeJob.Capture.StopRecording();
				activeJob.Capture.Dispose();

				activeJob.WaveWriter.Close();
				activeJob.WaveWriter.Dispose();

				if (activeJob.SilencePlayer != null)
				{
					activeJob.SilencePlayer.Stop();
					activeJob.SilencePlayer.Dispose();
					activeJob.SilencePlayer = null;
				}

				_runningJobs.Remove(activeJob);
			}

			// now, perform format conversion
			await Task.Run(() =>
				{
					// prepare a directory to contain the recordings
					string timestamp = DateTime.Now.ToString("s").Replace(":", ".");
					string fullRecordDirectoryPath = Path.Combine(targetFolder, string.Format("Recording_{0}", timestamp));
					if (!Directory.Exists(fullRecordDirectoryPath))
					{
						Directory.CreateDirectory(fullRecordDirectoryPath);
					}

					foreach (var outputFile in _activeOutputs.ToList())
					{
						Log.Debug(string.Format("Stopping recording to {0}", outputFile.OutputPath));
						var oldPath = outputFile.OutputPath;
						var oldDir = Path.GetDirectoryName(oldPath);

						if (outputFile.ChannelDefinition.Format != Models.OutputFormat.WAV)
						{
							Log.Debug(string.Format("Converting {0} to {1}", outputFile.OutputPath, 
								outputFile.ChannelDefinition.Format));
							outputFile.OutputPath = ConvertFile(
								outputFile.OutputPath, 
								outputFile.ChannelDefinition.Format);

							ConversionStatus.FilesCompletedConversion.Add(new FileInfo(outputFile.OutputPath));
						} 

						// move the file from the recording directory to the storage directory
						if (!PathsEqual(oldDir, targetFolder))
						{
							File.Move(outputFile.OutputPath,
								Path.Combine(fullRecordDirectoryPath, Path.GetFileName(outputFile.OutputPath)));
						}

						ConversionStatus.FilesPendingConversion.RemoveAll(t => t.FullName == oldPath);
						_activeOutputs.Remove(outputFile);

						// remove the original folder if it's empty
						var oldDirInfo = new DirectoryInfo(oldDir);
						if (!oldDirInfo.EnumerateFiles().Any())
							oldDirInfo.Delete();
					}
				});
		}

		/// <summary>
		/// Gets the extension (with the dot) corresponding to an output format
		/// </summary>
		/// <param name="format">The format to get a file extension for</param>
		/// <returns>Returns a file extension with a dot</returns>
		private string GetExtension(AudioKnight.Models.OutputFormat format)
		{
			switch (format)
			{
				case Models.OutputFormat.M4A:
					return ".m4a";
				case Models.OutputFormat.MP3:
					return ".mp3";
				case Models.OutputFormat.WMA:
					return ".wma";
				case OutputFormat.WAV:
					return ".wav";
				default:
					return ".wma";
			}
		}

		/// <summary>
		/// Gets the audio codec to use when encoding the given format
		/// </summary>
		/// <param name="format">The format to get an audio codec for</param>
		/// <returns>Returns the audio codec to use for encoding</returns>
		private string GetAudioCodec(AudioKnight.Models.OutputFormat format)
		{
			switch (format)
			{
				case Models.OutputFormat.WAV:
					return "pcm_s16le";
				case Models.OutputFormat.WMA:
					return "wmav2";
				case Models.OutputFormat.MP3:
					return "libmp3lame";
				case Models.OutputFormat.M4A:
					return "aac -strict experimental";
				default:
					return "aac";
			}
		}

		/// <summary>
		/// Converts a file from WMA to another format
		/// </summary>
		/// <param name="path">The source file to convert</param>
		/// <param name="targetFormat">The format to convert the file to</param>
		/// <returns>The path at which the converted output resides</returns>
		private string ConvertFile(string path, AudioKnight.Models.OutputFormat targetFormat)
		{
			// try to find FFmpeg in the EXE's directory or die
			var exeFile = new Uri(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;
			var exeDir = Path.GetDirectoryName(exeFile);
			var ffmpegPath = Path.Combine(exeDir, "ffmpeg.exe");
			if (!File.Exists(ffmpegPath))
			{
				throw new ApplicationException("The ffmpeg executable could not be found. " +
					"Conversion to types other than WMA is unavailable. Please try re-running the installer.");
			}

			var outputFile = Path.Combine(Path.GetDirectoryName(path),
				Path.GetFileNameWithoutExtension(path) + this.GetExtension(targetFormat));
			string args = string.Format("-y -i \"{0}\" -c:a {1} \"{2}\"",
				path, this.GetAudioCodec(targetFormat), outputFile);

			Process p = new Process();
			p.StartInfo = new ProcessStartInfo(ffmpegPath);
			p.StartInfo.Arguments = args;
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();
			p.Start();

			string stdOutput = "";
			string stdErr = "";
			p.OutputDataReceived += (sender, outputArgs) =>
				stdOutput += outputArgs.Data + "\n";
			p.ErrorDataReceived += (sender, errorArgs) =>
				stdErr += errorArgs.Data + "\n";

			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			bool exited = p.WaitForExit(1000 * 60 * 5);

			string truncatedError = stdErr.Substring(Math.Max(0, stdErr.Length - 500),
													 Math.Min(stdErr.Length, 500));
			string truncatedOutput = stdOutput.Substring(Math.Max(0, stdOutput.Length - 500),
														 Math.Min(stdOutput.Length, 500));
			if (!exited)
			{
				throw new ApplicationException(string.Format(
					"FFmpeg did not terminate within the 5 minute time limit." +
					"\nError Output:{0}\nStandard Output:{1} ", truncatedError, truncatedOutput));
			}

			if (p.ExitCode != 0)
			{
				throw new ApplicationException(string.Format(
					"FFmpeg terminated with non-zero exit code." +
					"\nError Output: {0}\nStandard Output{1}", truncatedError, truncatedOutput));
			}

			// conversion was a success, remove the original
			File.Delete(path);
			return outputFile;
		}

		/// <summary>
		/// Converts a file to a 'safe' format
		/// </summary>
		/// <param name="name">The potentially unsafe file name</param>
		/// <returns>The converted file name, which is safe</returns>
		private string SafeName(string name)
		{
			foreach (var c in Path.GetInvalidFileNameChars())
			{
				name = name.Replace(c, '_');
			}
			return name;
		}

		/// <summary>
		/// Determines if two paths point to the same location
		/// </summary>
		/// <param name="path1">The first of the paths to check</param>
		/// <param name="path2">The second path to check</param>
		/// <returns>A flag indicating if the paths are equivalent</returns>
		private bool PathsEqual(string path1, string path2)
		{
			return String.Compare(
				Path.GetFullPath(path1).TrimEnd('\\'),
				Path.GetFullPath(path2).TrimEnd('\\'),
				StringComparison.InvariantCultureIgnoreCase) == 0;
		}

	}
}
