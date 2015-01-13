using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight.Models
{
	/// <summary>
	/// Represents status of conversion for a set of recordings
	/// </summary>
	public class ConversionStatusViewModel : BindableObject
	{
		public ConversionStatusViewModel()
		{
			NotifyCollectionChangedEventHandler update = (sender, args) =>
				{
					OnPropertyChanged("IsComplete");
					OnPropertyChanged("CompressionProgress");
				};
			_filesCompletedConversion.CollectionChanged += update;
			_filesPendingConversion.CollectionChanged += update;
		}

		private double _expectedCompressionRatio = 15.0;

		/// <summary>Gets the factor reduction in file size we expect due to compression</summary>
		public double ExpectedCompressionRatio { get { return _expectedCompressionRatio; } }

		///<summary>Backs the <code>FilesPendingConversion</code> property</summary>
		private ObservableCollection<FileInfo> _filesPendingConversion = new ObservableCollection<FileInfo>();

		/// <summary>
		/// Gets or sets the collection of files that have not been converted yet
		/// </summary>
		public ObservableCollection<FileInfo> FilesPendingConversion
		{
			get { return _filesPendingConversion;}
		}

		///<summary>Backs the <code>FilesCompletedConversion</code> property</summary>
		private ObservableCollection<FileInfo> _filesCompletedConversion = new ObservableCollection<FileInfo>();

		/// <summary>
		/// Gets or sets the collection of files that have finished converting
		/// </summary>
		public ObservableCollection<FileInfo> FilesCompletedConversion
		{
			get { return _filesCompletedConversion;}
		}

		/// <summary>
		/// Gets the completion percentage of the active compression operations
		/// </summary>
		public double CompressionProgress
		{
			get 
			{
				// the size of the compressed output
				double targetSize = FilesPendingConversion.Where(f => f.Exists).Sum(f => f.Length / _expectedCompressionRatio);
				double completedSize = FilesCompletedConversion.Where(f => f.Exists).Sum(c => c.Length);
				targetSize += completedSize;

				return completedSize / targetSize;
			}
		}

		/// <summary>
		/// Gets a vlaue indicating if compression is complete
		/// </summary>
		public bool IsComplete
		{
			get { return !FilesPendingConversion.Any(); }
		}
		
	}
}
