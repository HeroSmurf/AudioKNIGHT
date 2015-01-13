using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight.Models
{
	/// <summary>
	/// Represents a channel preset which may be mapped to zero or more channel defintions
	/// </summary>
	public class ChannelPresetViewModel : BindableObject
	{

		/// <summary>Name of the default channel preset</summary>
		public static string DefaultName = "Default";

		///<summary>Backs the <code>NameValue</code> property</summary>
		private string _nameValue;

		/// <summary>
		/// Internal representation of the preset's name
		/// </summary>
		public string NameValue
		{
			get { return _nameValue; }
			set
			{
				_nameValue = value;
				OnPropertyChanged();
				OnPropertyChanged("NameDisplay");
			}
		}

		/// <summary>
		/// Gets the name of the preset to be displayed in the view
		/// </summary>
		public string NameDisplay
		{
			get 
			{
				if (string.IsNullOrWhiteSpace(_nameValue))
				{
					return ChannelPresetViewModel.DefaultName;
				}
				return _nameValue;
			}
		}

		///<summary>Backs the <code>ChannelDefinitions</code> property</summary>
		private ObservableCollection<ChannelDefinitionViewModel> channelDefinitions;

		/// <summary>
		/// Gets or sets the channel definitions to present in the view
		/// </summary>
		public ObservableCollection<ChannelDefinitionViewModel> ChannelDefinitions
		{
			get { return channelDefinitions; }
			set
			{
				channelDefinitions = value;
				OnPropertyChanged();
			}
		}
	}
}
