using System;
using System.Collections.ObjectModel;
using System.Linq;
using DeltaVDesigner.Models;
using DeltaVDesigner.Utility;
using GoldenAnvil.Utility;
using GoldenAnvil.Utility.Logging;

namespace DeltaVDesigner.UI.MainWindow
{
	public sealed class MainWindowViewModel : ViewModelBase, IDisposable
	{
		public MainWindowViewModel(AppModel appModel)
		{
			AppModel = appModel;
			m_rng = new Random();
			Components = new ObservableCollection<ComponentViewModel>();
			m_hitTables = new HitTablesViewModel(Components);

			Initialize();
		}

		public AppModel AppModel { get; }

		public ObservableCollection<ComponentViewModel> Components { get; }

		public HitTablesViewModel HitTables => VerifyAccess(m_hitTables);

		public void AddComponent()
		{
			var component = new ComponentViewModel(DeleteComponent);
			component.Id = NameUtility.GetUniqueName(Components.Select(x => x.Id).ToHashSet(), "ID");
			Components.Add(component);
		}

		public void DeleteComponent(ComponentViewModel component)
		{
			Components.Remove(component);
		}

		public void Dispose()
		{
			DisposableUtility.Dispose(ref m_hitTables);
		}

		private void Initialize()
		{
			Components.Add(new ComponentViewModel(DeleteComponent)
			{
				Id = "CP",
				X = 13.9395M,
				Y = 20.731M,
				Width = 7.99M,
				Length = 13.316M,
				Height = 7.99M,
			});
			Components.Add(new ComponentViewModel(DeleteComponent)
			{
				Id = "DL",
				X = 9.1435M,
				Y = 23.299M,
				Width = 4.796M,
				Length = 10.748M,
				Height = 3.857M,
			});
			Components.Add(new ComponentViewModel(DeleteComponent)
			{
				Id = "EN",
				X = 13.002M,
				Y = 34.047M,
				Width = 9.865M,
				Length = 19.73M,
				Height = 9.865M,
			});
			Components.Add(new ComponentViewModel(DeleteComponent)
			{
				Id = "LFT",
				X = 5.749M,
				Y = 34.047M,
				Width = 7.253M,
				Length = 14.505M,
				Height = 7.253M,
			});
			Components.Add(new ComponentViewModel(DeleteComponent)
			{
				Id = "LA",
				X = 14.4795M,
				Y = 0,
				Width = 6.91M,
				Length = 20.731M,
				Height = 6.91M,
			});
			Components.Add(new ComponentViewModel(DeleteComponent)
			{
				Id = "LMA",
				X = 0,
				Y = 31.305M,
				Width = 5.749M,
				Length = 17.247M,
				Height = 5.749M,
			});
		}

		private static ILogSource Log { get; } = LogManager.CreateLogSource(nameof(MainWindowViewModel));

		private readonly Random m_rng;
		private HitTablesViewModel m_hitTables;
	}
}
