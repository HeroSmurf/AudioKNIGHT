using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioKnight
{
	/// <summary>
	/// An attribute used to signal that a property is subject to validation
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ValidateAttribute : Attribute
	{

	}
}
