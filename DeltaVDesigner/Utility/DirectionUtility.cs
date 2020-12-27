using System;
using DeltaVDesigner.Models;

namespace DeltaVDesigner.Utility
{
	public static class DirectionUtility
	{
		public static Direction GetClockwise(this Direction direction)
		{
			return direction switch
			{
				Direction.Left => Direction.Front,
				Direction.Front => Direction.Right,
				Direction.Right => Direction.Back,
				Direction.Back => Direction.Left,
				_ => throw new InvalidOperationException($"No clockwise direction for '{direction}'."),
			};
		}

		public static Direction GetCounterClockwise(this Direction direction)
		{
			return direction switch
			{
				Direction.Left => Direction.Back,
				Direction.Front => Direction.Left,
				Direction.Right => Direction.Front,
				Direction.Back => Direction.Right,
				_ => throw new InvalidOperationException($"No counterclockwise direction for '{direction}'."),
			};
		}

		public static Direction GetOpposite(this Direction direction)
		{
			return direction switch
			{
				Direction.Left => Direction.Right,
				Direction.Front => Direction.Back,
				Direction.Right => Direction.Left,
				Direction.Back => Direction.Front,
				_ => throw new InvalidOperationException($"No opposite direction for '{direction}'."),
			};
		}
	}
}
