using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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

		//protected override void OnMouseDown(MouseButtonEventArgs e)
		//{
		//	base.OnMouseDown(e);

		//	m_mouseDownPosition = e.GetPosition(this);
		//	m_mouseDownCenter = Center;
		//	m_isMouseDown = true;
		//	CaptureMouse();

		//	e.Handled = true;
		//}

		//protected override void OnMouseUp(MouseButtonEventArgs e)
		//{
		//	m_isDragging = false;
		//	m_isMouseDown = false;
		//	ReleaseMouseCapture();

		//	e.Handled = true;
		//}

		//protected override void OnMouseMove(MouseEventArgs e)
		//{
		//	if (m_isMouseDown)
		//	{
		//		var currentPosition = e.GetPosition(this);
		//		if (!m_isDragging && Math.Max(Math.Abs(currentPosition.X - m_mouseDownPosition.X), Math.Abs(currentPosition.X - m_mouseDownPosition.X)) > 4.0)
		//			m_isDragging = true;

		//		if (m_isDragging)
		//		{
		//			var scale = Scale * Math.Min(ActualWidth / 2.0, ActualHeight / 2.0);
		//			Center = new Point(
		//				m_mouseDownCenter.X + ((m_mouseDownPosition.X - currentPosition.X) / scale),
		//				m_mouseDownCenter.Y + ((m_mouseDownPosition.Y - currentPosition.Y) / scale)
		//			);
		//		}

		//		e.Handled = true;
		//	}
		//}

		protected override void OnRender(DrawingContext context)
		{
			base.OnRender(context);

			var viewRect = new Rect(0, 0, ActualWidth, ActualHeight);
			var background = (Brush) FindResource("ComponentLayoutBackgroundBrush");
			context.DrawRectangle(background, null, viewRect);

			var padding = (Thickness) FindResource("ComponentLayoutPadding");
			var availableWidth = viewRect.Width - padding.Left - padding.Right;
			var availableHeight = viewRect.Height - padding.Top - padding.Bottom;

			var components = Components.ToList().AsReadOnly();
			if (components.Count != 0)
			{
				var resources = Resources.Get(this);
				var unitSize = components.GetDimensions();

				using (context.ScopedTransform(new TranslateTransform(padding.Left, padding.Top)))
					DrawComponents(components, unitSize, Direction.Top, new Size(availableWidth / 2.0, availableHeight), resources, context);

				var angleAvailableHeight = availableHeight - (3.0 * resources.AnglePadding);
				var angleHeight = angleAvailableHeight / 4.0;

				// left
				using (context.ScopedTransform(new TranslateTransform(padding.Left + (availableWidth / 2.0), padding.Top)))
					DrawComponents(components, unitSize, Direction.Left, new Size(availableWidth / 2.0, angleHeight), resources, context);

				using (context.ScopedTransform(new TranslateTransform(padding.Left + (availableWidth / 2.0), padding.Top + (angleHeight + resources.AnglePadding))))
					DrawComponents(components, unitSize, Direction.Right, new Size(availableWidth / 2.0, angleHeight), resources, context);

				using (context.ScopedTransform(new TranslateTransform(padding.Left + (availableWidth / 2.0), padding.Top + (angleHeight + resources.AnglePadding + angleHeight + resources.AnglePadding))))
					DrawComponents(components, unitSize, Direction.Front, new Size(availableWidth / 2.0, angleHeight), resources, context);

				using (context.ScopedTransform(new TranslateTransform(padding.Left + (availableWidth / 2.0), padding.Top + (angleHeight + resources.AnglePadding + angleHeight + resources.AnglePadding + angleHeight + resources.AnglePadding))))
					DrawComponents(components, unitSize, Direction.Back, new Size(availableWidth / 2.0, angleHeight), resources, context);
			}
		}

		private void DrawComponents(IEnumerable<ComponentViewModel> rawComponents, Dimensions unitSize, Direction direction, Size availableSize, Resources resources, DrawingContext context)
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
			var scale = Math.Min(availableSize.Width / (double) (rightEdge - leftEdge), availableSize.Height / (double) (bottomEdge - topEdge));

			var offsetX = -scale * (double) leftEdge - (scale * (double) (rightEdge - leftEdge) / 2.0) + (availableSize.Width / 2.0);
			var offsetY = -scale * (double) topEdge - (scale * (double) (bottomEdge - topEdge) / 2.0) + (availableSize.Height / 2.0);
			using (context.ScopedTransform(new TranslateTransform(offsetX, offsetY)))
			{

				foreach (var component in components)
				{
					if (component.Face.Width == 0 || component.Face.Height == 0)
						continue;

					var rect = new Rect(
						(double) (component.Face.Left/* - leftEdge*/) * scale,
						(double) (component.Face.Top/* - topEdge*/) * scale,
						(double) component.Face.Width * scale,
						(double) component.Face.Height * scale
						);
					context.DrawRectangle(resources.ComponentBrush, resources.ComponentPen, rect);

					var text = new FormattedText(component.Component.Id, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, resources.IdTextTypeface, resources.IdTextSize, resources.IdTextBrush, 1.0);
					context.DrawText(text, new Point(rect.Left + 2, rect.Top + 2));
				}
			}

			var labelText = new FormattedText(OurResources.ResourceManager.EnumToDisplayString(direction), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, resources.LabelTextTypeface, resources.LabelTextSize, resources.LabelTextBrush, 1.0);
			context.DrawText(labelText, new Point(2, 2));
		}

		private static void OnComponentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (ComponentLayoutControl) d;
			if (e.OldValue is not null)
				((ObservableCollection<ComponentViewModel>) e.OldValue).CollectionChanged -= control.Components_CollectionChanged;
			if (e.NewValue is not null)
				((ObservableCollection<ComponentViewModel>) e.NewValue).CollectionChanged += control.Components_CollectionChanged;
		}

		private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateVisual();

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
		}

		private sealed class Resources
		{
			public static Resources Get(FrameworkElement element)
			{
				return new Resources(
					(Pen) element.FindResource("ComponentLayoutComponentPen"),
					(Brush) element.FindResource("ComponentLayoutComponentFill"),
					(Brush) element.FindResource("ComponentLayoutIdTextBrush"),
					(double) element.FindResource("ComponentLayoutIdTextSize"),
					new Typeface((string) element.FindResource("ComponentLayoutIdTextTypeface")),
					(Brush) element.FindResource("ComponentLayoutLabelTextBrush"),
					(double) element.FindResource("ComponentLayoutLabelTextSize"),
					new Typeface((string) element.FindResource("ComponentLayoutLabelTextTypeface")),
					(double) element.FindResource("ComponentLayoutAnglePadding")
				);
			}

			public Pen ComponentPen { get; }
			public Brush ComponentBrush { get; }
			public Brush IdTextBrush { get; }
			public double IdTextSize { get; }
			public Typeface IdTextTypeface { get; }
			public Brush LabelTextBrush { get; }
			public double LabelTextSize { get; }
			public Typeface LabelTextTypeface { get; }
			public double AnglePadding { get; }

			private Resources(Pen componentPen, Brush componentBrush, Brush idTextBrush, double idTextSize, Typeface idTextTypeface, Brush labelTextBrush, double labelTextSize, Typeface labelTextTypeface, double anglePadding)
			{
				ComponentPen = componentPen;
				ComponentBrush = componentBrush;
				IdTextBrush = idTextBrush;
				IdTextSize = idTextSize;
				IdTextTypeface = idTextTypeface;
				LabelTextBrush = labelTextBrush;
				LabelTextSize = labelTextSize;
				LabelTextTypeface = labelTextTypeface;
				AnglePadding = anglePadding;
			}
		}

		private static ILogSource Log { get; } = LogManager.CreateLogSource(nameof(AppModel));
	}
}
