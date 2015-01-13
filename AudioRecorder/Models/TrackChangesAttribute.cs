using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight
{
	/// <summary>
	/// An attribute which signals that an attribute should be included
	/// in the change tracking mechanism
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class TrackChangesAttribute : Attribute
	{


	}
}
