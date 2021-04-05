using System;

namespace DeltaVDesigner.Models
{
	public struct Face
	{
		public static Face Empty => new(0, 0, Decimal.MinValue, Decimal.MinValue);

		public Face(decimal x, decimal y, decimal width, decimal height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public bool IsEmpty => Width < 0;

		public decimal X { get; }
		public decimal Y { get; }
		public decimal Width { get; }
		public decimal Height { get; }

		public decimal Left => X;
		public decimal Top => Y;
		public decimal Right => X + Width;
		public decimal Bottom => Y + Height;

		public bool IntersectsWith(Face that)
		{
			if (IsEmpty || that.IsEmpty)
				return false;
			return (that.Left <= Right) &&
				(that.Right >= Left) &&
				(that.Top <= Bottom) &&
				(that.Bottom >= Top);
		}

		public Face IntersectionWith(Face that)
		{
			if (!IntersectsWith(that))
				return Empty;

			var left = Math.Max(Left, that.Left);
			var top = Math.Max(Top, that.Top);
			var width = Math.Min(Right, that.Right) - left;
			var height = Math.Min(Bottom, that.Bottom) - top;

			return new Face(left, top, width, height);
		}
	}
}
