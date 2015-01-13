using System;
using System.Collections.Generic;
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

namespace AudioKnight
{
	/// <summary>
	/// Interaction logic for NewPresetDialog.xaml
	/// </summary>
	public partial class NewPresetDialog
	{
		/// <summary>
		/// Gets or sets the text that the user entered
		/// </summary>
		public string EnteredText { get; set; }

		public NewPresetDialog()
		{
			InitializeComponent();
			this.DataContext = this;
		}

		private void Create_CanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = !string.IsNullOrWhiteSpace(EnteredText);
		}

		private void Create_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			this.DialogResult = true;
			this.Close();
		}

		private void Cancel_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			this.DialogResult = false;
			this.Close();
		}
	}
}
