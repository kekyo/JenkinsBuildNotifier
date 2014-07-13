using System;
using System.Deployment.Application;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;

namespace JenkinsBuildNotifier
{
	public static class Program
	{
		[STAThread]
		public static int Main(string[] args)
		{
			try
			{
				Uri baseUrl;
				NetworkCredential credential = null;

				if (args.Length == 0)
				{
					if (ApplicationDeployment.IsNetworkDeployed == true)
					{
						MessageBox.Show(
							string.Format("usage: {0} <Jenkins baseUrl>", Path.GetFileName(typeof(Program).Assembly.Location)),
							typeof(Program).Namespace,
							MessageBoxButton.OK,
							MessageBoxImage.Information);

						return 1;
					}

					baseUrl = ApplicationDeployment.CurrentDeployment.ActivationUri;
				}
				else
				{
					baseUrl = new Uri(args[0].EndsWith("/") ? args[0] : (args[0] + "/"));

					if (args.Length >= 3)
					{
						credential = new NetworkCredential(args[1].Trim(), args[2].Trim());
					}
				}

				var controller = new Controller(baseUrl, credential);

				var application = new Application();
				application.Run();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					ex.Message
					, ex.GetType().FullName,
					MessageBoxButton.OK,
					MessageBoxImage.Error);
				return Marshal.GetHRForException(ex);
			}

			return 0;
		}
	}
}
