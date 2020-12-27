using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DeltaVDesigner.Models
{
	public static class ComponentUtility
	{
		public static Face GetFace(this ComponentViewModel component, Direction direction, Dimensions unitSize)
		{
			return direction switch
			{
				Direction.Top => new Face(component.X, component.Y, component.Width, component.Length),
				Direction.Left => new Face(component.Y, unitSize.Height - component.Height, component.Length, component.Height),
				Direction.Right => new Face(unitSize.Length - component.Y - component.Length, unitSize.Height - component.Height, component.Length, component.Height),
				Direction.Front => new Face(unitSize.Width - component.X - component.Width, unitSize.Height - component.Height, component.Width, component.Height),
				Direction.Back => new Face(component.X, unitSize.Height - component.Height, component.Width, component.Height),
				_ => throw new NotImplementedException($"Unhandled direction: '{direction}'"),
			};
		}

		public static decimal GetFaceCoordinate(this ComponentViewModel component, Direction direction)
		{
			return direction switch
			{
				Direction.Top => component.Height,
				Direction.Left => component.X,
				Direction.Right => component.X + component.Width,
				Direction.Front => component.Y,
				Direction.Back => component.Y + component.Length,
				_ => throw new NotImplementedException($"Unhandled direction: '{direction}'"),
			};
		}

		public static IEnumerable<ComponentViewModel> OrderBackToFront(this IEnumerable<ComponentViewModel> components, Direction direction)
		{
			return direction switch
			{
				Direction.Top => components,
				Direction.Left => components.OrderByDescending(x => x.GetFaceCoordinate(direction)),
				Direction.Right => components.OrderBy(x => x.GetFaceCoordinate(direction)),
				Direction.Front => components.OrderByDescending(x => x.GetFaceCoordinate(direction)),
				Direction.Back => components.OrderBy(x => x.GetFaceCoordinate(direction)),
				_ => throw new NotImplementedException($"Unhandled direction: '{direction}'"),
			};
		}

		public static Dimensions GetDimensions(this IReadOnlyList<ComponentViewModel> components)
		{
			if (components.Count == 0)
				return new Dimensions(0.0M, 0.0M, 0.0M);

			return new Dimensions(
				components.Max(x => x.GetFaceCoordinate(Direction.Right)) - components.Min(x => x.GetFaceCoordinate(Direction.Left)),
				components.Max(x => x.GetFaceCoordinate(Direction.Back)) - components.Min(x => x.GetFaceCoordinate(Direction.Front)),
				components.Max(x => x.GetFaceCoordinate(Direction.Top)));
		}
	}
}
