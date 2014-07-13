using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace JenkinsBuildNotifier
{
	internal static class NativeMethods
	{
		[Flags]
		private enum CREDUI_FLAGS : uint
		{
			INCORRECT_PASSWORD = 0x1,
			DO_NOT_PERSIST = 0x2,
			REQUEST_ADMINISTRATOR = 0x4,
			EXCLUDE_CERTIFICATES = 0x8,
			REQUIRE_CERTIFICATE = 0x10,
			SHOW_SAVE_CHECK_BOX = 0x40,
			ALWAYS_SHOW_UI = 0x80,
			REQUIRE_SMARTCARD = 0x100,
			PASSWORD_ONLY_OK = 0x200,
			VALIDATE_USERNAME = 0x400,
			COMPLETE_USERNAME = 0x800,
			PERSIST = 0x1000,
			SERVER_CREDENTIAL = 0x4000,
			EXPECT_CONFIRMATION = 0x20000,
			GENERIC_CREDENTIALS = 0x40000,
			USERNAME_TARGET_CREDENTIALS = 0x80000,
			KEEP_USERNAME = 0x100000,
		}

		[Flags]
		private enum CREDUIWIN_FLAGS : uint
		{
			GENERIC = 0x1,
			CHECKBOX = 0x2,
			AUTHPACKAGE_ONLY = 0x10,
			IN_CRED_ONLY = 0x20,
			ENUMERATE_ADMINS = 0x100,
			ENUMERATE_CURRENT_USER = 0x200,
			SECURE_PROMPT = 0x1000,
			PACK_32_WOW = 0x10000000,
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct CREDUI_INFO
		{
			public int cbSize;
			public IntPtr hwndParent;
			public string pszMessageText;
			public string pszCaptionText;
			public IntPtr hbmBanner;
		}

		[DllImport("credui.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern int CredUIPromptForCredentials(
			ref CREDUI_INFO creditUR,
			string targetName,
			IntPtr reserved1,
			int iError,
			StringBuilder userName,
			int maxUserName,
			StringBuilder password,
			int maxPassword,
			[MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
			CREDUI_FLAGS flags);

		[DllImport("credui.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern int CredUIPromptForWindowsCredentials(
			ref CREDUI_INFO notUsedHere,
			int authError,
			ref int authPackage,
			IntPtr InAuthBuffer,
			int InAuthBufferSize,
			out IntPtr refOutAuthBuffer,
			out int refOutAuthBufferSize,
			[MarshalAs(UnmanagedType.Bool)] ref bool fSave,
			CREDUIWIN_FLAGS flags);

		[DllImport("credui.dll", CharSet = CharSet.Unicode)]
		private static extern bool CredUnPackAuthenticationBuffer(
			int dwFlags,
			IntPtr pAuthBuffer,
			int cbAuthBuffer,
			StringBuilder pszUserName,
			ref int pcchMaxUserName,
			StringBuilder pszDomainName,
			ref int pcchMaxDomainame,
			StringBuilder pszPassword,
			ref int pcchMaxPassword);

		[DllImport("ole32.dll")]
		private static extern void CoTaskMemFree(IntPtr pv);
		
		public static NetworkCredential ShowCredentialDialog(string title, string message)
		{
			var info = new CREDUI_INFO
			{
				cbSize = Marshal.SizeOf(typeof(CREDUI_INFO)),
				pszMessageText = title,
				pszCaptionText = message
			};

			var userName = new StringBuilder(100);
			var password = new StringBuilder(100);
			var save = false;

			int result = -1;

			if (Environment.OSVersion.Version.Major >= 6)
			{
				var authPackage = 0;
				var outAuthBuffer = IntPtr.Zero;
				var outAuthBufferSize = 0;

				result = CredUIPromptForWindowsCredentials(
					ref info,
					0,
					ref authPackage,
					IntPtr.Zero,
					0,
					out outAuthBuffer,
					out outAuthBufferSize,
					ref save,
					CREDUIWIN_FLAGS.GENERIC);

				if (result == 0)
				{
					var userNameCapacity = userName.Capacity;
					var passwordCapacity = password.Capacity;
					var domain = new StringBuilder(100);
					var domainCapacity = domain.Capacity;

					var r = CredUnPackAuthenticationBuffer(
						0,
						outAuthBuffer,
						outAuthBufferSize,
						userName, 
						ref userNameCapacity,
						domain,
						ref domainCapacity,
						password,
						ref passwordCapacity);

					CoTaskMemFree(outAuthBuffer);
				}
			}
			else
			{
				result = CredUIPromptForCredentials(
					ref info,
					null,
					IntPtr.Zero,
					0,
					userName,
					userName.Capacity,
					password,
					password.Capacity,
					ref save,
					CREDUI_FLAGS.DO_NOT_PERSIST | CREDUI_FLAGS.GENERIC_CREDENTIALS);
			}

			return (result == 0) ? new NetworkCredential(userName.ToString(), password.ToString()) : null;
		}
	}
}
