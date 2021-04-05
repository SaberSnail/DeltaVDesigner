using System;
using System.Collections.Generic;
using System.Linq;
using GoldenAnvil.Utility;

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
				Direction.Top => components.OrderByDescending(x => x.Width * x.Height),
				Direction.Left => components.OrderByDescending(x => x.GetFaceCoordinate(direction)).ThenByDescending(x => x.Height * x.Length),
				Direction.Right => components.OrderBy(x => x.GetFaceCoordinate(direction)).ThenByDescending(x => x.Height * x.Length),
				Direction.Front => components.OrderByDescending(x => x.GetFaceCoordinate(direction)).ThenByDescending(x => x.Height * x.Width),
				Direction.Back => components.OrderBy(x => x.GetFaceCoordinate(direction)).ThenByDescending(x => x.Height * x.Width),
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

		public static Face SnapFaceToGuides(Face face, IEnumerable<Face> guideFaces, decimal tolerance)
		{
			var localGuideFaces = guideFaces.AsReadOnlyList();

			var newX = localGuideFaces
				.Select(guideFace =>
				{
					return GetBestGuideline(
						guideFace.Left - face.Left,
						guideFace.Right - face.Left,
						guideFace.Left - face.Right,
						guideFace.Right - face.Right,
						guideFace.CenterX - face.CenterX);
				})
				.Where(x => x.Distance <= tolerance)
				.OrderBy(x => x.Distance)
				.Select(x => x.Offset + face.X)
				.FirstOrDefault(face.X);

			var newY = localGuideFaces
				.Select(guideFace =>
				{
					return GetBestGuideline(
						guideFace.Top - face.Top,
						guideFace.Bottom - face.Top,
						guideFace.Top - face.Bottom,
						guideFace.Bottom - face.Bottom,
						guideFace.CenterY - face.CenterY);
				})
				.Where(x => x.Distance <= tolerance)
				.OrderBy(x => x.Distance)
				.Select(x => x.Offset + face.Y)
				.FirstOrDefault(face.Y);

			return new Face(newX, newY, face.Width, face.Height);

			(decimal Offset, decimal Distance) GetBestGuideline(params decimal[] offsets)
			{
				return offsets
					.Select(x => (Offset: x, Distance: Math.Abs(x)))
					.OrderBy(x => x.Distance)
					.First();
			}
		}
	}
}
