using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using AudioKnight.Models;
using Omu.ValueInjecter;

namespace AudioKnight
{
	/// <summary>
	/// Provides mapping between view modeling and storage objects, 
	/// and provides mechanisms to permanantly store application settings and objects.
	/// </summary>
	public class PersistenceService
	{
		static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(PersistenceService));

		/// <summary>
		/// Maps a channel definition to its view model
		/// </summary>
		/// <param name="definition">The channel definition to map to a view model</param>
		/// <returns>A view model representing the channel definition</returns>
		public ChannelDefinitionViewModel GetChannelViewModel(ChannelDefinition definition)
		{
			return new ChannelDefinitionViewModel().InjectFrom(definition) as ChannelDefinitionViewModel;
		}

		/// <summary>
		/// Maps a channel definition view model to its persistence model
		/// </summary>
		/// <param name="viewModel">The view model to map to a serializable entity</param>
		/// <returns>A persistable channel definition</returns>
		public ChannelDefinition GetChannel(ChannelDefinitionViewModel viewModel)
		{
			return new ChannelDefinition().InjectFrom(viewModel) as ChannelDefinition;
		}

		/// <summary>
		/// Gets a value indicating active recordings will be 
		/// stored in a temp directory
		/// </summary>
		public bool GetUseTempFolder()
		{
			return Properties.Settings.Default.UseTempFolderWhenRecording;
		}

		/// <summary>
		/// Reads the channel definitions from storage
		/// </summary>
		/// <returns>The channels which were persisted</returns>
		public IEnumerable<ChannelDefinition> LoadChannels()
		{
			try
			{
				Log.Debug("Loading channel definitions ...");
				var serializer = new XmlSerializer(typeof(List<ChannelDefinition>));
				return this.XmlDeserialze<List<ChannelDefinition>>(
					serializer, Properties.Settings.Default.ChannelDefinitions);
			} catch (InvalidOperationException ex) // invalid / corrupt XML
			{
				return Enumerable.Empty<ChannelDefinition>();
			}
		}

		/// <summary>
		/// Saves the channel definitions to the user-level settings
		/// </summary>
		/// <param name="channels">The channel definitions to persist</param>
		public void PersistChannels(IEnumerable<ChannelDefinition> channels)
		{
			Log.Debug("Storing channel definitions ...");
			var toSerialize = channels.ToList();
			var serializer = new XmlSerializer(typeof(List<ChannelDefinition>));
			var result = XmlSerialize(serializer, toSerialize);
			Properties.Settings.Default.ChannelDefinitions = result;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Gets the preset selected by the user or the default
		/// </summary>
		/// <returns>The user's selected channel preset</returns>
		public string GetSelectedPreset()
		{
			Log.Debug(string.Format("Getting selected preset {0} ...", Properties.Settings.Default.Preset));
			var stored = Properties.Settings.Default.Preset;
			if (string.IsNullOrWhiteSpace(stored))
				return ChannelPresetViewModel.DefaultName;
			else
				return stored;
		}

		/// <summary>
		/// Saves the selected preset name
		/// </summary>
		/// <param name="presetName">The name of the preset</param>
		public void StoreSelectedPreset(string presetName)
		{
			Log.Debug(string.Format("Storing selected preset {0} ...", presetName));
			if (string.IsNullOrWhiteSpace(presetName))
				Properties.Settings.Default.Preset = ChannelPresetViewModel.DefaultName;
			else
				Properties.Settings.Default.Preset = presetName;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Gets the output folder for recordings
		/// </summary>
		/// <returns>The folder to store recordings in</returns>
		public string GetOutputFolder()
		{
			if (!Directory.Exists(Properties.Settings.Default.OutputFolder))
			{
				string dir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					"AudioKnight");
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				return dir;
			} else
			{
				return Properties.Settings.Default.OutputFolder;
			}
		}

		/// <summary>
		/// Gets the folder used to store recordings temporarily
		/// </summary>
		/// <returns>The folder used to store active recordings in</returns>
		public string GetTempFolder()
		{
			return Path.Combine(this.GetOutputFolder(), "Temp");
		}

		/// <summary>
		/// Saves the output folder to the user-level settings
		/// </summary>
		/// <param name="outputFolder">The folder to persist</param>
		public void PersistOutputFolder(string outputFolder)
		{
			Properties.Settings.Default.OutputFolder = outputFolder;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Retrieves the hotkey used to initiate and stop the recording process
		/// </summary>
		/// <returns>The hotkey that is used for starting and stopping recording</returns>
		public HotKey GetRecordingHotkey()
		{
			try
			{
				var serializer = new XmlSerializer(typeof(HotKey));
				return XmlDeserialze<HotKey>(serializer, Properties.Settings.Default.RecordingHotkey);
			} catch (InvalidOperationException ex) // invalid / corrupt XML
			{
				return new HotKey(Key.F11, ModifierKeys.None, true);
			}
		}

		/// <summary>
		/// Saves the hotkey used to initiate and stop the recording process
		/// </summary>
		/// <param name="recordingHotkey">The hotkey to persist</param>
		public void PersistRecordingHotkey(HotKey recordingHotkey)
		{
			var serializer = new XmlSerializer(typeof(HotKey));
			var result = XmlSerialize(serializer, recordingHotkey);
			Properties.Settings.Default.RecordingHotkey = result;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Performs in-memory XML serialization
		/// </summary>
		/// <param name="serializer">The serializer to use when writing the output</param>
		/// <param name="toSerialize">the object to write out</param>
		/// <returns>Returns the serialized result</returns>
		private string XmlSerialize(XmlSerializer serializer, object toSerialize)
		{
			StringBuilder builder = new StringBuilder();
			using (StringWriter stringWriter = new StringWriter(builder))
			using(XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
			{
				serializer.Serialize(xmlWriter, toSerialize);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Performs XML deserialization from memory
		/// </summary>
		/// <typeparam name="T">The type to be deserialized</typeparam>
		/// <param name="serializer">The serializer to use when reading the object</param>
		/// <param name="source">The in-memory XML representation to read from</param>
		/// <returns>Returns an instance of the specified object type</returns>
		private T XmlDeserialze<T>(XmlSerializer serializer, string source) where T : class
		{
			using(StringReader reader = new StringReader(source))
			using (XmlTextReader xmlReader = new XmlTextReader(reader))
			{
				return serializer.Deserialize(xmlReader) as T;
			}
		}
	}
}
