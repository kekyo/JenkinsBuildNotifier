using System.Drawing;
using System.Windows.Forms;

namespace JenkinsBuildNotifier
{
	internal enum ToastIcons
	{
		OK,
		Warning,
		Error,
		Running,
		Unknown
	}

	internal sealed class NotificationInformation
	{
		public NotificationInformation(
			Icon icon,
			ToolTipIcon toolTipIcon,
			ToastIcons toastIcon,
			PlaySymbols playSymbol,
			bool isBlink,
			bool isLong)
		{
			this.Icon = icon;
			this.ToolTipIcon = toolTipIcon;
			this.ToastIcon = toastIcon;
			this.PlaySymbol = playSymbol;
			this.IsBlink = isBlink;
			this.IsLong = isLong;
		}

		public Icon Icon
		{
			get;
			private set;
		}

		public ToolTipIcon ToolTipIcon
		{
			get;
			private set;
		}

		public ToastIcons ToastIcon
		{
			get;
			private set;
		}

		public PlaySymbols PlaySymbol
		{
			get;
			private set;
		}

		public bool IsBlink
		{
			get;
			private set;
		}

		public bool IsLong
		{
			get;
			private set;
		}
	}

}
