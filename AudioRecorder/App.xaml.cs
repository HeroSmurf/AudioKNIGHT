using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using log4net.Config;

namespace AudioKnight
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			this.Startup += (sender, args) =>
				{
					XmlConfigurator.Configure(new FileInfo("AudioKnight.exe.log4net"));
				};
		}
	}
}
