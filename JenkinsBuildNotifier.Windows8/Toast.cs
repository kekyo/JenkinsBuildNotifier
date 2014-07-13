using JenkinsBuildNotifier.Interops;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace JenkinsBuildNotifier
{
	public static class Toast
	{
		private static readonly string applicationID_ = typeof(Toast).Namespace;

		static Toast()
		{
			var assembly = typeof(Toast).Assembly;
			var assetsPath = Path.Combine(Path.GetDirectoryName(assembly.Location), "Assets");

			Directory.CreateDirectory(assetsPath);

			foreach (var name in
				from name in assembly.GetManifestResourceNames()
				where name.EndsWith(".png")
				select name)
			{
				using (var assetStream = assembly.GetManifestResourceStream(name))
				{
					using (var appStream = new FileStream(Path.Combine(assetsPath, name), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
					{
						assetStream.CopyTo(appStream);
						appStream.Close();
					}
				}
			}

			var shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
				"\\Microsoft\\Windows\\Start Menu\\Programs\\" + applicationID_ + ".lnk";

			File.Delete(shortcutPath);
			InstallShortcut(shortcutPath);
		}

		private static void InstallShortcut(string shortcutPath)
		{
			// Find the path to the current executable 
			var exePath = Process.GetCurrentProcess().MainModule.FileName;
			var newShortcut = (IShellLinkW)new CShellLink();

			// Create a shortcut to the exe 
			ErrorHelper.VerifySucceeded(newShortcut.SetPath(exePath));
			ErrorHelper.VerifySucceeded(newShortcut.SetArguments(""));

			// Open the shortcut property store, set the AppUserModelId property 
			var newShortcutProperties = (IPropertyStore)newShortcut;

			using (var appId = new PropVariant(applicationID_))
			{
				ErrorHelper.VerifySucceeded(newShortcutProperties.SetValue(SystemProperties.System.AppUserModel.ID, appId));
				ErrorHelper.VerifySucceeded(newShortcutProperties.Commit());
			}

			// Commit the shortcut to disk 
			var newShortcutSave = (IPersistFile)newShortcut;

			ErrorHelper.VerifySucceeded(newShortcutSave.Save(shortcutPath, true));
		}

		public static void Show(string title, string message, string toastIcon, bool isLong)
		{
			var imagePath = string.Format("/Assets/{0}.Assets.{1}.png", typeof(Toast).Namespace, toastIcon);

			var template = ToastTemplateType.ToastImageAndText02;
			var toastXml = ToastNotificationManager.GetTemplateContent(template);

			var imageTags = toastXml.GetElementsByTagName("image").ToArray();
			((XmlElement)imageTags[0]).SetAttribute("src", imagePath);

			var textTags = toastXml.GetElementsByTagName("text").ToArray();
			textTags[0].AppendChild(toastXml.CreateTextNode(title));
			textTags[1].AppendChild(toastXml.CreateTextNode(message));

			var audioTag = toastXml.CreateElement("audio");
			audioTag.SetAttribute("silent", "true");

			var toastTag = (XmlElement)toastXml.GetElementsByTagName("toast").First();
			toastTag.AppendChild(audioTag);
			toastTag.SetAttribute("duration", isLong ? "long" : "short");

			var notifier = ToastNotificationManager.CreateToastNotifier(applicationID_);
			notifier.Show(new ToastNotification(toastXml));
		}
	}
}
