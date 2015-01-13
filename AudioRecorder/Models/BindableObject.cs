using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight
{
	/// <summary>
	/// A base class which provides common functionality for view models
	/// </summary>
	[Serializable]
	public class BindableObject : INotifyPropertyChanged, IChangeTracking, IDataErrorInfo
	{
		/// <summary>
		/// The set of properties that are subject to validation, cached for efficiency
		/// </summary>
		public Dictionary<string, PropertyInfo> _validateProperties;

		/// <summary>
		/// Initializes a new <see cref="BindableObject"/>
		/// </summary>
		public BindableObject()
		{
			this.PropertyChanged += new PropertyChangedEventHandler(OnNotifiedOfPropertyChanged);
			_originalValues = new Dictionary<string, object>();
			_trackChangeProperties = this.GetType().GetProperties()
				.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(TrackChangesAttribute))))
							.ToDictionary(p => p.Name, p => p);
			_validateProperties = this.GetType().GetProperties()
				.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(ValidateAttribute))))
					.ToDictionary(p => p.Name, p => p);
			_changedPropertyNames = new HashSet<string>();
		}

		#region ChangeTracking

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// A map from property name to the objects original/accepted values.
		/// </summary>
		private Dictionary<string, object> _originalValues;

		/// <summary>
		/// The set of properties that have changed since the last accepted state
		/// </summary>
		private HashSet<string> _changedPropertyNames;

		/// <summary>
		/// The set of this object's properties, cached for efficiency
		/// </summary>
		private Dictionary<string, PropertyInfo> _trackChangeProperties;

		/// <summary>
		/// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for this object.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">A <see cref="PropertyChangedEventArgs"/> that contains the event data.</param>
		private void OnNotifiedOfPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e != null && _trackChangeProperties.ContainsKey(e.PropertyName))
			{
				object origValue;
				object propVal = _trackChangeProperties[e.PropertyName].GetValue(this);
				if (!_originalValues.TryGetValue(e.PropertyName, out origValue))
				{
					_originalValues.Add(e.PropertyName, propVal);
				} else if ((propVal == null && origValue == null) ||
					(propVal != null && propVal.Equals(origValue)))
				{
					_changedPropertyNames.Remove(e.PropertyName);
					if (!_changedPropertyNames.Any())
					{
						this.IsChanged = false;
					}
				} else // new value does not equal original value
				{
					this.IsChanged = true;
				}
			}
		}

		/// <summary>
		/// Gets the object's changed status.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the object’s content has changed since the last call to <see cref="AcceptChanges()"/>; otherwise, <see langword="false"/>. 
		/// The initial value is <see langword="false"/>.
		/// </value>
		public bool IsChanged
		{
			get
			{
				lock (_notifyingObjectIsChangedSyncRoot)
				{
					return _notifyingObjectIsChanged;
				}
			}

			protected set
			{
				lock (_notifyingObjectIsChangedSyncRoot)
				{
					if (!Boolean.Equals(_notifyingObjectIsChanged, value))
					{
						_notifyingObjectIsChanged = value;

						this.OnPropertyChanged("IsChanged");
						HandleChanged();
					}
				}
			}
		}
		private bool _notifyingObjectIsChanged;
		private readonly object _notifyingObjectIsChangedSyncRoot = new Object();

		/// <summary>
		/// A method which subclasses can override in order to pump property 
		/// change notifications when <code>IsChanged</code> is modified
		/// </summary>
		protected virtual void HandleChanged()
		{

		}

		/// <summary>
		/// Resets the object’s state to unchanged by accepting the modifications.
		/// </summary>
		public void AcceptChanges()
		{
			this.IsChanged = false;
			_originalValues = _trackChangeProperties.ToDictionary(p => p.Key, p => p.Value.GetValue(this));
			_changedPropertyNames.Clear();
		}

		/// <summary>
		/// Removes any changes made to the object's 
		/// tracked properties since AcceptChanges was last called.
		/// </summary>
		public void RevertChanges()
		{
			this.IsChanged = false;
			foreach (var prop in _originalValues)
			{
				_trackChangeProperties[prop.Key].SetValue(this, prop.Value);
			}
			_changedPropertyNames.Clear();
		}


		#endregion

		/// <summary>
		/// Raises a property changed event
		/// </summary>
		/// <param name="caller"></param>
		protected void OnPropertyChanged([CallerMemberName]string caller = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(caller));
			}
		}

		#region Validation
		private Dictionary<string, string> validationErrors = new Dictionary<string, string>();

		/// <summary>
		/// Force validation of all properties marked for validation
		/// </summary>
		public void Validate()
		{
			foreach (var name in _validateProperties.Keys)
			{
				var prop = _validateProperties[name];
				prop.SetValue(this, prop.GetValue(this));
			}
		}

		public void AddError(string columnName, string message)
		{
			if (!validationErrors.ContainsKey(columnName))
			{
				validationErrors.Add(columnName, message);
			}
		}

		public void RemoveError(string columnName)
		{
			if (validationErrors.ContainsKey(columnName))
			{
				validationErrors.Remove(columnName);
			}
		}

		public string Error
		{
			get
			{
				if (validationErrors.Count > 0)
				{
					return string.Format("{0} data is invalid", this.GetType().Name);
				} else
				{
					return null;
				}

			}
		}

		public string this[string columnName]
		{
			get
			{
				if (validationErrors.ContainsKey(columnName))
				{
					return validationErrors[columnName].ToString();
				} else
				{
					return null;
				}
			}
		}

		#endregion
	}
}
