using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GoldenAnvil.Utility;

namespace DeltaVDesigner.Models
{
	public sealed class IntersectionGroup
	{
		public static IntersectionGroup TryCreate(ComponentViewModel component, Direction direction, Face? bounds, Dimensions unitSize)
		{
			var face = component.GetFace(direction, unitSize);
			if (bounds.HasValue)
				face = face.IntersectionWith(bounds.Value);
			return face.IsEmpty ? null : new IntersectionGroup(EnumerableUtility.Enumerate(component), face, direction, unitSize);
		}

		private IntersectionGroup(IEnumerable<ComponentViewModel> components, Face face, Direction direction, Dimensions unitSize)
		{
			Face = face;
			Components = new HashSet<ComponentViewModel>(components, new GenericEqualityComparer<ComponentViewModel>((left, right) => left.Id.Equals(right.Id), x => x.Id.GetHashCode()));
			m_direction = direction;
			m_unitSize = unitSize;
		}

		public Face Face { get; }

		public HashSet<ComponentViewModel> Components { get; }

		public IntersectionGroup CreateIfIntersects(ComponentViewModel component, Face? bounds)
		{
			if (Components.Contains(component))
				return null;

			var newFace = component.GetFace(m_direction, m_unitSize);
			if (bounds.HasValue)
				newFace = newFace.IntersectionWith(bounds.Value);
			var intersection = Face.IntersectionWith(newFace);
			return intersection.IsEmpty ? null : new IntersectionGroup(Components.Append(component), intersection, m_direction, m_unitSize);
		}

		readonly Direction m_direction;
		readonly Dimensions m_unitSize;
	}
}
