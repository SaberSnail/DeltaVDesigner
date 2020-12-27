using System;
using System.Windows;
using DeltaVDesigner.UI.MainWindow;
using GoldenAnvil.Utility.Logging;

namespace DeltaVDesigner
{
	public partial class App : Application
	{
		public App()
		{
			LogManager.Initialize(new ConsoleLogDestination(true));

			AppModel = new AppModel();
			AppModel.StartupFinished += AppModel_StartupFinished;
		}

		public AppModel AppModel { get; }

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			AppModel.Startup();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			AppModel.Shutdown();

			base.OnExit(e);
		}

		private void AppModel_StartupFinished(object sender, EventArgs eventArgs)
		{
			AppModel.StartupFinished -= AppModel_StartupFinished;

			new MainWindowView(AppModel.MainWindowViewModel).Show();
		}
	}
}
