using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace JenkinsBuildNotifier
{
	internal sealed class Notifier : IDisposable
	{
		private static readonly Action<string, string, string, bool> showToast_;

		static Notifier()
		{
			try
			{
				var assembly = typeof(Controller).Assembly;
				var windows8AssemblyPath = Path.Combine(Path.GetDirectoryName(assembly.Location), assembly.GetName().Name + ".Windows8.dll");
				var windows8Assembly = Assembly.LoadFrom(windows8AssemblyPath);

				var toastType = windows8Assembly.GetType(typeof(Controller).Namespace + ".Toast");
				var showToastMethod = toastType.GetMethod("Show");

				showToast_ = (Action<string, string, string, bool>)Delegate.CreateDelegate(typeof(Action<string, string, string, bool>), showToastMethod);
			}
			catch
			{
			}
		}

		private NotifyIcon notifyIcon_ = new NotifyIcon();
		private Timer timer_;
		private int iconState_;
		private Icon currentIcon_;
		private bool notifyBaloonTip_ = true;
		private bool notifyToast_ = true;
		private bool notifySound_ = true;

		public Notifier()
		{
			notifyIcon_.Text = "Jenkins: [Unknown]";
			notifyIcon_.Icon = currentIcon_ = Properties.Resources.Unknown;
			notifyIcon_.DoubleClick += (s, e) => this.ShowJenkins(s, e);
			notifyIcon_.ContextMenuStrip = new ContextMenuStrip();
			notifyIcon_.ContextMenuStrip.Items.AddRange(
				new ToolStripItem[]
					{
						new ToolStripMenuItem("Manually build &trigger")
							{
								Image = ToImage(Properties.Resources.Running),
								Tag = new EventHandler((s, e) => InvokeEvent(this.BuildTrigger, s, e))
							},
						new ToolStripMenuItem("&Show jenkins on browser")
							{
								Image = ToImage(Properties.Resources.App),
								Tag = new EventHandler((s, e) => InvokeEvent(this.ShowJenkins, s, e))
							},
						new ToolStripSeparator(),
						new ToolStripMenuItem("Notify by &baloon tip")
							{
								Checked = true,
								CheckOnClick = true,
								Tag = new EventHandler(this.OnNotifyBaloonTipMenu)
							},
						new ToolStripMenuItem("Notify by &toast")
							{
								Checked = true,
								CheckOnClick = true,
								Enabled = showToast_ != null,
								Tag = new EventHandler(this.OnNotifyToastMenu)
							},
						new ToolStripMenuItem("Notify by &sound")
							{
								Checked = true,
								CheckOnClick = true,
								Tag = new EventHandler(this.OnNotifySoundMenu)
							},
						new ToolStripSeparator(),
						new ToolStripMenuItem("&Configuration")
							{
								Image = ToImage(Properties.Resources.Configuration),
								Tag = new EventHandler((s, e) => InvokeEvent(this.ShowConfiguration, s, e))
							},
						new ToolStripSeparator(),
						new ToolStripMenuItem("E&xit")
							{
								Tag = new EventHandler((s, e) => InvokeEvent(this.Exit, s, e))
							}
					});

			notifyIcon_.ContextMenuStrip.ItemClicked += this.OnItemClicked;

			notifyIcon_.Visible = true;

			timer_ = new Timer();
			timer_.Tick += this.OnElapsed;
		}

		public event EventHandler ShowJenkins;
		public event EventHandler BuildTrigger;
		public event EventHandler ShowConfiguration;
		public event EventHandler Exit;

		private static Image ToImage(Icon icon)
		{
			using (var icon16 = new Icon(icon, 16, 16))
			{
				return icon16.ToBitmap();
			}
		}

		private static void InvokeEvent(EventHandler handler, object sender, EventArgs e)
		{
			if (handler != null)
			{
				handler(sender, e);
			}
		}

		private void OnItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			var handler = (EventHandler)e.ClickedItem.Tag;
			handler(this, e);
		}

		private void OnNotifyBaloonTipMenu(object sender, EventArgs e)
		{
			var e2 = (ToolStripItemClickedEventArgs)e;

			notifyBaloonTip_ = ((ToolStripMenuItem)e2.ClickedItem).Checked == false;
		}

		private void OnNotifyToastMenu(object sender, EventArgs e)
		{
			var e2 = (ToolStripItemClickedEventArgs)e;

			notifyToast_ = ((ToolStripMenuItem)e2.ClickedItem).Checked == false;
		}

		private void OnNotifySoundMenu(object sender, EventArgs e)
		{
			var e2 = (ToolStripItemClickedEventArgs)e;

			notifySound_ = ((ToolStripMenuItem)e2.ClickedItem).Checked == false;
		}

		public void Dispose()
		{
			if (notifyIcon_ != null)
			{
				timer_.Dispose();
				notifyIcon_.Visible = false;
				notifyIcon_.Dispose();

				timer_ = null;
				notifyIcon_ = null;
				currentIcon_ = null;
			}

			GC.SuppressFinalize(this);
		}

		private void OnElapsed(object sender, EventArgs e)
		{
			switch (iconState_)
			{
				case 1:
					iconState_ = 2;
					notifyIcon_.Icon = Properties.Resources.Transparent;
					break;
				case 2:
					iconState_ = 1;
					notifyIcon_.Icon = currentIcon_;
					break;
			}
		}

		public void Update(
			NotificationInformation notifyInformation,
			string title,
			string message,
			params string[] details)
		{
			var baloonTipText = string.Join("\r\n", message, string.Join("\r\n", details));
			var requireUpdate = (notifyIcon_.BalloonTipTitle != title) || (notifyIcon_.BalloonTipText != baloonTipText);

			if (requireUpdate == false)
			{
				return;
			}

			var requirePlaySoundAndToast = requireUpdate && (notifyIcon_.BalloonTipText.StartsWith(message) == false);

			if ((iconState_ != 0) && (notifyInformation.IsBlink == false))
			{
				iconState_ = 0;
				timer_.Stop();
			}

			notifyIcon_.Icon = currentIcon_ = notifyInformation.Icon;
			notifyIcon_.BalloonTipIcon = notifyInformation.ToolTipIcon;
			notifyIcon_.BalloonTipTitle = title;
			notifyIcon_.BalloonTipText = baloonTipText;

			var text = string.Format("{0}\r\n{1}", title, baloonTipText);
			if (text.Length > 60)
			{
				text = text.Substring(0, 60) + "...";
			}

			notifyIcon_.Text = text;

			if (notifyBaloonTip_ == true)
			{
				notifyIcon_.ShowBalloonTip(notifyInformation.IsLong ? 25000 : 7000);
			}

			if ((iconState_ == 0) && (notifyInformation.IsBlink == true))
			{
				iconState_ = 1;
				timer_.Interval = 500;
				timer_.Start();
			}

			if (requirePlaySoundAndToast == false)
			{
				return;
			}

			if ((showToast_ != null) && (notifyToast_ == true))
			{
				showToast_(title, baloonTipText, notifyInformation.ToastIcon.ToString(), notifyInformation.IsLong);
			}

			if (notifySound_ == true)
			{
				SystemSoundPlayer.Play(notifyInformation.PlaySymbol);
			}
		}
	}
}
