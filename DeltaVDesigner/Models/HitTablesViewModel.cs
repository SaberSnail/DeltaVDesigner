using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using DeltaVDesigner.Utility;
using GoldenAnvil.Utility;

namespace DeltaVDesigner.Models
{
	public sealed class HitTablesViewModel : ViewModelBase, IDisposable
	{
		public HitTablesViewModel(ObservableCollection<ComponentViewModel> components)
			: base()
		{
			m_components = components;
			m_components.CollectionChanged += Components_CollectionChanged;
			RefreshTables();
		}

		public decimal TotalFrontArea
		{
			get => VerifyAccess(m_totalFrontArea);
			set => SetPropertyField(value, ref m_totalFrontArea);
		}

		public decimal TotalSideArea
		{
			get => VerifyAccess(m_totalSideArea);
			set => SetPropertyField(value, ref m_totalSideArea);
		}

		public IReadOnlyList<HitTableRow> LeftHitRows
		{
			get => VerifyAccess(m_leftHitRow);
			set => SetPropertyField(value, ref m_leftHitRow);
		}

		public IReadOnlyList<HitTableRow> RightHitRows
		{
			get => VerifyAccess(m_rightHitRow);
			set => SetPropertyField(value, ref m_rightHitRow);
		}

		public IReadOnlyList<HitTableRow> FrontHitRows
		{
			get => VerifyAccess(m_frontHitRow);
			set => SetPropertyField(value, ref m_frontHitRow);
		}

		public IReadOnlyList<HitTableRow> BackHitRows
		{
			get => VerifyAccess(m_backHitRow);
			set => SetPropertyField(value, ref m_backHitRow);
		}

		public void Dispose() =>
			m_components.CollectionChanged -= Components_CollectionChanged;

		private void RefreshTables()
		{
			var unitSize = m_components.GetDimensions();

			var leftGroups = CreateIntersectionGroups(m_components, Direction.Left, unitSize, null);
			TotalSideArea = GetTotalArea(leftGroups);

			var frontGroups = CreateIntersectionGroups(m_components, Direction.Front, unitSize, null);
			TotalFrontArea = GetTotalArea(frontGroups);

			LeftHitRows = CreateHitRows(m_components, Direction.Left, unitSize);
			RightHitRows = CreateHitRows(m_components, Direction.Right, unitSize);
			FrontHitRows = CreateHitRows(m_components, Direction.Front, unitSize);
			BackHitRows = CreateHitRows(m_components, Direction.Back, unitSize);
		}

		private IReadOnlyList<HitTableRow> CreateHitRows(IReadOnlyList<ComponentViewModel> components, Direction direction, Dimensions unitSize)
		{
			var totalArea = direction == Direction.Front || direction == Direction.Back ? TotalFrontArea : TotalSideArea;
			var initialRows = components
				.Select(component =>
				{
					var area = GetVisibleArea(component, components, direction, null, unitSize);
					var portion = (int) Math.Round(100000.0M * area / totalArea);
					var excessHitRows = CreateExcessHitRows(component, components, direction, unitSize);
					return new HitTableRow(component, portion, excessHitRows);
				})
				.ToList();

			return SortAndNormalizeHitTableRows(initialRows, components, direction, false);
		}

		private IReadOnlyList<HitTableRow> SortAndNormalizeHitTableRows(IReadOnlyList<HitTableRow> rows, IEnumerable<ComponentViewModel> components, Direction direction, bool filterNoPortion)
		{
			ReadOnlyCollection<ComponentViewModel> sortedComponents;
			var sortDirection = direction.GetClockwise();
			if (sortDirection == Direction.Left || sortDirection == Direction.Front)
				sortedComponents = components.OrderBy(x => x.GetFaceCoordinate(sortDirection)).ToList().AsReadOnly();
			else
				sortedComponents = components.OrderByDescending(x => x.GetFaceCoordinate(sortDirection)).ToList().AsReadOnly();
			var total = rows.Sum(x => x.Portion / 1000);
			var extra = 100 - total;
			return rows
				.OrderByDescending(x => x.Portion % 1000)
				.Select((x, i) => new HitTableRow(x.Component, (i + 1 <= extra ? 1 : 0) + (x.Portion / 1000), x.ExcessHitRows))
				.Where(x => !filterNoPortion || x.Portion > 0)
				.OrderBy(x => x.Component is null ? -1 : sortedComponents.IndexOf(x.Component))
				.ToList()
				.AsReadOnly();
		}

		private IReadOnlyList<HitTableRow> CreateExcessHitRows(ComponentViewModel component, IReadOnlyList<ComponentViewModel> components, Direction direction, Dimensions unitSize)
		{
			var backFaceCoordinate = component.GetFaceCoordinate(direction.GetOpposite());
			var componentsBehind = components
				.Where(x =>
				{
					var check = x.GetFaceCoordinate(direction);
					if (direction == Direction.Left || direction == Direction.Front)
						return check >= backFaceCoordinate;
					return check <= backFaceCoordinate;
				})
				.ToList()
				.AsReadOnly();

			var bounds = component.GetFace(direction, unitSize);
			var faceArea = bounds.Width * bounds.Height;
			if (faceArea == 0)
				return new HitTableRow[0];

			var areasBehind = componentsBehind
				.Select(x =>
				{
					var area = GetVisibleArea(x, componentsBehind, direction, bounds, unitSize);
					return (Component: x, Area: area);
				})
				.ToList();
			var totalAreasBehind = areasBehind.Sum(x => x.Area);
			areasBehind.Insert(0, (Component: null, Area: faceArea - totalAreasBehind));

			var initialRows = areasBehind
				.Select(x =>
				{
					var portion = (int) Math.Round(100000.0M * x.Area / faceArea);
					return new HitTableRow(x.Component, portion, null);
				})
				.ToList();

			return SortAndNormalizeHitTableRows(initialRows, components, direction, true);
		}

		private decimal GetVisibleArea(ComponentViewModel component, IReadOnlyList<ComponentViewModel> components, Direction direction, Face? extraBounds, Dimensions unitSize)
		{
			var faceCoordinate = component.GetFaceCoordinate(direction);
			var componentsInFront = components
				.Where(x =>
				{
					var check = x.GetFaceCoordinate(direction);
					if (direction == Direction.Left || direction == Direction.Front)
						return check < faceCoordinate;
					return check > faceCoordinate;
				})
				.ToList()
				.AsReadOnly();

			var bounds = component.GetFace(direction, unitSize);
			if (extraBounds.HasValue)
				bounds = bounds.IntersectionWith(extraBounds.Value);
			if (bounds.IsEmpty)
				return 0;
			var intersectionGroups = CreateIntersectionGroups(componentsInFront, direction, unitSize, bounds);
			var areaInFront = GetTotalArea(intersectionGroups);
			return (bounds.Width * bounds.Height) - areaInFront;
		}

		private IReadOnlyList<IntersectionGroup> CreateIntersectionGroups(IReadOnlyList<ComponentViewModel> components, Direction direction, Dimensions unitSize, Face? bounds)
		{
			var groups = components
				.Select(x => IntersectionGroup.TryCreate(x, direction, bounds, unitSize))
				.WhereNotNull()
				.ToList();
			var currentGroups = groups.ToList();
			do
			{
				currentGroups = currentGroups
					.SelectMany(group =>
					{
						return components
							.Select(component => group.CreateIfIntersects(component, bounds))
							.WhereNotNull();
					})
					.Distinct(new GenericEqualityComparer<IntersectionGroup>((left, right) => left.Components.SetEquals(right.Components), x => 0))
					.ToList();
				groups.AddRange(currentGroups);
			} while (currentGroups.Count != 0 && currentGroups[0].Components.Count != components.Count);

			return groups;
		}

		private decimal GetTotalArea(IReadOnlyList<IntersectionGroup> groups)
		{
			var totalSideArea = 0.0M;
			if (groups.Count != 0)
			{
				var maxGroupMembers = groups.Max(x => x.Components.Count);
				for (int groupMembers = 1; groupMembers <= maxGroupMembers; groupMembers++)
				{
					decimal total = groups.Where(x => x.Components.Count == groupMembers).Sum(x => x.Face.Width * x.Face.Height);
					totalSideArea += (groupMembers % 2 == 1 ? 1 : -1) * total;
				}
			}
			return totalSideArea;
		}

		private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Add:
				foreach (ComponentViewModel item in e.NewItems)
					item.PropertyChanged += OnComponentPropertyChanged;
				break;
			case NotifyCollectionChangedAction.Remove:
				foreach (ComponentViewModel item in e.OldItems)
					item.PropertyChanged -= OnComponentPropertyChanged;
				break;
			case NotifyCollectionChangedAction.Replace:
				foreach (ComponentViewModel item in e.NewItems)
					item.PropertyChanged += OnComponentPropertyChanged;
				foreach (ComponentViewModel item in e.OldItems)
					item.PropertyChanged -= OnComponentPropertyChanged;
				break;
			case NotifyCollectionChangedAction.Reset:
				foreach (var item in (sender as IEnumerable<ComponentViewModel>))
					item.PropertyChanged += OnComponentPropertyChanged;
				break;
			}

			RefreshTables();
		}

		private void OnComponentPropertyChanged(object source, PropertyChangedEventArgs args)
		{
			RefreshTables();
		}

		readonly ObservableCollection<ComponentViewModel> m_components;
		decimal m_totalFrontArea;
		decimal m_totalSideArea;
		IReadOnlyList<HitTableRow> m_leftHitRow;
		IReadOnlyList<HitTableRow> m_rightHitRow;
		IReadOnlyList<HitTableRow> m_frontHitRow;
		IReadOnlyList<HitTableRow> m_backHitRow;
	}
}
