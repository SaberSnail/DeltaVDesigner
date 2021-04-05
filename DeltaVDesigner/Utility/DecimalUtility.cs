using System;

namespace DeltaVDesigner.Utility
{
	public static class DecimalUtility
	{
		public static decimal Round(decimal value, int decimals, decimal factor) =>
			Math.Round(value / factor, decimals) * factor;
	}
}
