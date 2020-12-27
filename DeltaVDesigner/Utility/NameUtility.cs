using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaVDesigner.Utility
{
	public static class NameUtility
	{
		public static string GetUniqueName(HashSet<string> names, string newName)
		{
			if (!names.Contains(newName))
				return newName;

			int i = 2;
			while (true)
			{
				var candidate = string.Format(OurResources.UniqueNameFormat, newName, i);
				if (!names.Contains(candidate))
					return candidate;
				i++;
			}
		}
	}
}
