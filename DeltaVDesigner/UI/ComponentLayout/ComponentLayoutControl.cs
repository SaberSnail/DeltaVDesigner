using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DeltaVDesigner.Models;
using GoldenAnvil.Utility;
using GoldenAnvil.Utility.Logging;
using GoldenAnvil.Utility.Windows;

namespace DeltaVDesigner.UI.ComponentLayout
{
	public sealed class ComponentLayoutControl : Canvas
	{
		public static readonly DependencyProperty ComponentsProperty = DependencyPropertyUtility<ComponentLayoutControl>.Register(x => x.Components, OnComponentsChanged, FrameworkPropertyMetadataOptions.AffectsRender);

		public ObservableCollection<ComponentViewModel> Components
		{
			get => (ObservableCollection<ComponentViewModel>) GetValue(ComponentsProperty);
			set => SetValue(ComponentsProperty, value);
		}

		public static readonly DependencyProperty DirectionProperty = DependencyPropertyUtility<ComponentLayoutControl>.Register(x => x.Direction, null, FrameworkPropertyMetadataOptions.AffectsRender, Direction.Top);

		public Direction Direction
		{
			get => (Direction) GetValue(DirectionProperty);
			set => SetValue(DirectionProperty, value);
		}

		public static readonly DependencyProperty PaddingProperty = DependencyPropertyUtility<ComponentLayoutControl>.Register(x => x.Padding, null, FrameworkPropertyMetadataOptions.AffectsRender, new Thickness(0.0));

		public Thickness Padding
		{
			get => (Thickness) GetValue(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			var position = e.GetPosition(this);
			var component = m_currentFaces?
				.Reverse<(ComponentViewModel Component, Rect Rect)>()
				.FirstOrDefault(x => x.Rect.Contains(position));

			m_isMouseDown = true;
			m_mouseDownPosition = position;
			m_mouseDownComponent = Direction != Direction.Top || component?.Component is null ? null : component;

			CaptureMouse();

			e.Handled = true;
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (m_isDragging)
				InvalidateVisual();

			m_isDragging = false;
			m_isMouseDown = false;
			ReleaseMouseCapture();

			e.Handled = true;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (m_isMouseDown && m_mouseDownComponent is not null)
			{
				var currentPosition = e.GetPosition(this);
				if (!m_isDragging && Math.Max(Math.Abs(currentPosition.X - m_mouseDownPosition.X), Math.Abs(currentPosition.X - m_mouseDownPosition.X)) > 4.0)
					m_isDragging = true;

				var comp = m_mouseDownComponent.Value;
				if (m_isDragging)
				{
					decimal newX = comp.Component.X;
					decimal newY = comp.Component.Y;

					var direction = Direction;
					if (direction == Direction.Top)
					{
						newX = (decimal) ((currentPosition.X - m_mouseDownPosition.X + comp.Rect.Left - m_currentOffsetX) / m_currentScale);
						newY = (decimal) ((currentPosition.Y - m_mouseDownPosition.Y + comp.Rect.Top - m_currentOffsetY) / m_currentScale);
					}
					else if (direction == Direction.Left)
					{
						newY = (decimal) ((currentPosition.X - m_mouseDownPosition.X + comp.Rect.Left - m_currentOffsetX) / m_currentScale);
					}
					else if (direction == Direction.Right)
					{
						// fix
						var unitSize = Components.GetDimensions();
						newY = unitSize.Length - comp.Component.Length - (decimal) ((currentPosition.X - m_mouseDownPosition.X + comp.Rect.Left - m_currentOffsetX) / m_currentScale);
					}
					else if (direction == Direction.Front)
					{
						// fix
						newX = (decimal) ((currentPosition.X - m_mouseDownPosition.X + comp.Rect.Left - m_currentOffsetX) / m_currentScale);
					}
					else if (direction == Direction.Back)
					{
						newX = (decimal) ((currentPosition.X - m_mouseDownPosition.X + comp.Rect.Left - m_currentOffsetX) / m_currentScale);
					}

					const decimal c_tolerance = 4.0M;
					var tolerance = c_tolerance / (decimal) m_currentScale;
					var newFace = new Face(newX, newY, comp.Component.Width, comp.Component.Length);
					var otherComponents = Components.Where(x => x != comp.Component).AsReadOnlyList();
					var horizontalGuides = otherComponents.SelectMany(x => EnumerableUtility.Enumerate(x.Y, x.Y + x.Length));
					var verticalGuides = otherComponents.SelectMany(x => EnumerableUtility.Enumerate(x.X, x.X + x.Width));
					newFace = ComponentUtility.SnapFaceToGuides(newFace, horizontalGuides, verticalGuides, tolerance);

					m_mouseDownComponent.Value.Component.X = Math.Round(newFace.X, 3);
					m_mouseDownComponent.Value.Component.Y = Math.Round(newFace.Y, 3);
				}

				e.Handled = true;
			}
		}

		protected override void OnRender(DrawingContext context)
		{
			base.OnRender(context);

			var direction = Direction;
			var padding = Padding;
			var viewRect = new Rect(0, 0, ActualWidth, ActualHeight);
			var resources = RenderResources.Get(this);

			using (context.ScopedClip(new RectangleGeometry(viewRect)))
			{
				context.DrawRectangle(resources.BackgroundBrush, null, viewRect);

				var components = Components.ToList().AsReadOnly();
				if (components.Count != 0)
				{
					var unitSize = m_currentUnitSize;
					if (!m_isMouseDown)
					{
						unitSize = components.GetDimensions();
						m_currentUnitSize = unitSize;
					}

					var availableWidth = viewRect.Width - padding.Left - padding.Right;
					var availableHeight = viewRect.Height - padding.Top - padding.Bottom;

					using (context.ScopedTransform(new TranslateTransform(padding.Left, padding.Top)))
						DrawComponents(components, unitSize, direction, new Size(availableWidth, availableHeight), resources, context);
				}
			}
		}

		private void DrawComponents(IEnumerable<ComponentViewModel> rawComponents, Dimensions unitSize, Direction direction, Size availableSize, RenderResources resources, DrawingContext context)
		{
			var components = rawComponents
				.OrderBackToFront(direction)
				.Select(x => (Component: x, Face: x.GetFace(direction, unitSize)))
				.ToList()
				.AsReadOnly();

			var leftEdge = components.Min(x => x.Face.Left);
			var rightEdge = components.Max(x => x.Face.Right);
			var topEdge = components.Min(x => x.Face.Top);
			var bottomEdge = components.Max(x => x.Face.Bottom);

			if (!m_isMouseDown)
			{
				m_currentScale = Math.Min(availableSize.Width / (double) (rightEdge - leftEdge), availableSize.Height / (double) (bottomEdge - topEdge));

				m_currentOffsetX = -m_currentScale * (double) leftEdge - (m_currentScale * (double) (rightEdge - leftEdge) / 2.0) + (availableSize.Width / 2.0);
				m_currentOffsetY = -m_currentScale * (double) topEdge - (m_currentScale * (double) (bottomEdge - topEdge) / 2.0) + (availableSize.Height / 2.0);
			}

			var currentFaces = new List<(ComponentViewModel Component, Rect Rect)>();
			foreach (var component in components)
			{
				if (component.Face.Width == 0 || component.Face.Height == 0)
					continue;

				var rect = new Rect(
					m_currentOffsetX + ((double) component.Face.Left * m_currentScale),
					m_currentOffsetY + ((double) component.Face.Top * m_currentScale),
					(double) component.Face.Width * m_currentScale,
					(double) component.Face.Height * m_currentScale
					);
				context.DrawRectangle(resources.ComponentBrush, resources.ComponentPen, rect);
				currentFaces.Add((Component: component.Component, Rect: rect));

				var text = new FormattedText(component.Component.Id, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, resources.IdTextTypeface, resources.IdTextSize, resources.IdTextBrush, 1.0);
				context.DrawText(text, new Point(rect.Left + 2, rect.Top + 2));
			}

			if (!m_isMouseDown)
				m_currentFaces = currentFaces;
		}

		private static void OnComponentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (ComponentLayoutControl) d;
			control.ResetCurrentValues();

			if (e.OldValue is not null)
				((ObservableCollection<ComponentViewModel>) e.OldValue).CollectionChanged -= control.Components_CollectionChanged;
			if (e.NewValue is not null)
				((ObservableCollection<ComponentViewModel>) e.NewValue).CollectionChanged += control.Components_CollectionChanged;

			foreach (var item in (ObservableCollection<ComponentViewModel>) e.NewValue)
				item.PropertyChanged += control.OnComponentPropertyChanged;
		}

		private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateVisual();
			ResetCurrentValues();

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
		}

		private void OnComponentPropertyChanged(object source, PropertyChangedEventArgs args)
		{
			InvalidateVisual();
			ResetCurrentValues();
		}

		private void ResetCurrentValues()
		{
			m_currentFaces = null;
		}

		private sealed class RenderResources
		{
			public static RenderResources Get(FrameworkElement element)
			{
				return new RenderResources(
					(Brush) element.FindResource("ComponentLayoutBackgroundBrush"),
					(Pen) element.FindResource("ComponentLayoutComponentPen"),
					(Brush) element.FindResource("ComponentLayoutComponentFill"),
					(Brush) element.FindResource("ComponentLayoutIdTextBrush"),
					(double) element.FindResource("ComponentLayoutIdTextSize"),
					new Typeface((string) element.FindResource("ComponentLayoutIdTextTypeface"))
				);
			}

			public Brush BackgroundBrush { get; }
			public Pen ComponentPen { get; }
			public Brush ComponentBrush { get; }
			public Brush IdTextBrush { get; }
			public double IdTextSize { get; }
			public Typeface IdTextTypeface { get; }

			private RenderResources(Brush backgroundBrush, Pen componentPen, Brush componentBrush, Brush idTextBrush, double idTextSize, Typeface idTextTypeface)
			{
				BackgroundBrush = backgroundBrush;
				ComponentPen = componentPen;
				ComponentBrush = componentBrush;
				IdTextBrush = idTextBrush;
				IdTextSize = idTextSize;
				IdTextTypeface = idTextTypeface;
			}
		}



		private static ILogSource Log { get; } = LogManager.CreateLogSource(nameof(AppModel));

		double m_currentOffsetX;
		double m_currentOffsetY;
		double m_currentScale;
		Dimensions m_currentUnitSize;
		List<(ComponentViewModel Component, Rect Rect)> m_currentFaces;
		bool m_isDragging;
		bool m_isMouseDown;
		Point m_mouseDownPosition;
		(ComponentViewModel Component, Rect Rect)? m_mouseDownComponent;
	}
}
