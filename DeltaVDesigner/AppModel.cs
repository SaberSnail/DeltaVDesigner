using System;
using DeltaVDesigner.UI.MainWindow;
using GoldenAnvil.Utility;
using GoldenAnvil.Utility.Logging;
using GoldenAnvil.Utility.Windows;

namespace DeltaVDesigner
{
	public sealed class AppModel : NotifyPropertyChangedDispatcherBase
	{
		public AppModel()
		{
			CurrentTheme = new Uri(@"/UI/Themes/Default/Default.xaml", UriKind.Relative);
		}

		public event EventHandler StartupFinished;

		public MainWindowViewModel MainWindowViewModel => m_mainWindow;

		public Uri CurrentTheme
		{
			get
			{
				VerifyAccess();
				return m_currentTheme;
			}
			set
			{
				if (SetPropertyField(value, ref m_currentTheme))
					Log.Info($"Changing theme to \"{m_currentTheme.OriginalString}\"");
			}
		}

		public void Startup()
		{
			m_mainWindow = new MainWindowViewModel(this);

			StartupFinished.Raise(this);
		}

		public void Shutdown()
		{
			DisposableUtility.Dispose(ref m_mainWindow);
		}

		private static ILogSource Log { get; } = LogManager.CreateLogSource(nameof(AppModel));

		MainWindowViewModel m_mainWindow;
		Uri m_currentTheme;
	}
}
