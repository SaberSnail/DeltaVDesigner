using System.Collections.Generic;

namespace DeltaVDesigner.Models
{
	public sealed class HitTableRow
	{
		public HitTableRow(ComponentViewModel component, int portion, IReadOnlyList<HitTableRow> excessHitRows)
		{
			Component = component;
			Portion = portion;
			ExcessHitRows = excessHitRows;
		}

		public string Title => Component?.Id ?? "-";
		public ComponentViewModel Component { get; }
		public int Portion { get; }
		public IReadOnlyList<HitTableRow> ExcessHitRows { get; }
	}
}
