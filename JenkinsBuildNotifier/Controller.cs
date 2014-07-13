using JenkinsBuildNotifier.Entities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows;

namespace JenkinsBuildNotifier
{
	internal sealed class Controller
	{
		private static readonly NotificationInformation runningNotificationInformation_ = new NotificationInformation(
			Properties.Resources.Running, System.Windows.Forms.ToolTipIcon.Info, ToastIcons.Running, PlaySymbols.DeviceConnect, true, false);
		private static readonly NotificationInformation successNotificationInformation_ = new NotificationInformation(
			Properties.Resources.OK, System.Windows.Forms.ToolTipIcon.Info, ToastIcons.OK, PlaySymbols.Notification_Default, false, false);
		private static readonly NotificationInformation unstableNotificationInformation_ = new NotificationInformation(
			Properties.Resources.Warning, System.Windows.Forms.ToolTipIcon.Warning, ToastIcons.Warning, PlaySymbols.DeviceDisconnect, false, true);
		private static readonly NotificationInformation failedNotificationInformation_ = new NotificationInformation(
			Properties.Resources.Error, System.Windows.Forms.ToolTipIcon.Error, ToastIcons.Error, PlaySymbols.DeviceFail, false, true);
		private static readonly NotificationInformation unknownNotificationInformation_ = new NotificationInformation(
			Properties.Resources.Unknown, System.Windows.Forms.ToolTipIcon.Warning, ToastIcons.Unknown, PlaySymbols.DeviceFail, false, false);
		private static readonly NotificationInformation exceptionNotificationInformation_ = new NotificationInformation(
			Properties.Resources.Unknown, System.Windows.Forms.ToolTipIcon.Error, ToastIcons.Unknown, PlaySymbols.DeviceFail, false, false);

		private readonly Uri baseUrl_;
		private NetworkCredential credential_;
		private readonly AutoResetEvent fetch_ = new AutoResetEvent(false);
		private readonly AutoResetEvent abort_ = new AutoResetEvent(false);
		private readonly Thread pollerThread_;
		private readonly Notifier notifier_ = new Notifier();

		public Controller(Uri url, NetworkCredential credential)
		{
			Debug.Assert(url != null);

			baseUrl_ = url;
			credential_ = credential;

			ServicePointManager.ServerCertificateValidationCallback = OnRemoteCertificateValidationCallback;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;

			notifier_.ShowJenkins += this.OnShowJenkins;
			notifier_.ShowConfiguration += this.OnShowConfiguration;
			notifier_.BuildTrigger += this.OnBuildTrigger;
			notifier_.Exit += this.OnExit;

			pollerThread_ = new Thread(this.Poller);
			pollerThread_.IsBackground = true;
			pollerThread_.Start();
		}

		private static bool OnRemoteCertificateValidationCallback(
		   object sender,
		   X509Certificate certificate,
		   X509Chain chain,
		   SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		private void OnBuildTrigger(object sender, EventArgs e)
		{
			var buildUrl = new Uri(baseUrl_, "build?delay=0sec");

			while (true)
			{
				using (var webClient = new WebClient())
				{
					SetCredential(webClient);

					try
					{
						webClient.UploadString(buildUrl, "POST", string.Empty);
						break;
					}
					catch (WebException ex)
					{
						if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Unauthorized)
						{
							var credential = NativeMethods.ShowCredentialDialog(baseUrl_.ToString(), "Enter Jenkins credential...");
							if (credential != null)
							{
								credential_ = credential;
							}
							else
							{
								break;
							}
						}
					}
				}
			}

			fetch_.Set();
		}

		private void OnShowJenkins(object sender, EventArgs e)
		{
			var psi = new ProcessStartInfo
			{
				UseShellExecute = true,
				FileName = baseUrl_.ToString()
			};

			Process.Start(psi);
		}

		private void OnShowConfiguration(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void OnExit(object sender, EventArgs e)
		{
			abort_.Set();
			notifier_.Dispose();
			Application.Current.Shutdown();
		}

		private void SetCredential(WebClient webClient)
		{
			Debug.Assert(webClient != null);

			var credentialCache = new CredentialCache();
			credentialCache.Add(baseUrl_, "Basic", credential_);

			webClient.Credentials = credentialCache;
		}

		private void UpdateNotifier(
			NotificationInformation notifyInformation,
			string title,
			string message,
			params string[] details)
		{
			Debug.Assert(notifyInformation != null);
			Debug.Assert(title != null);
			Debug.Assert(message != null);

			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				notifier_.Update(notifyInformation, title, message, details);
			}));
		}

		private static string GetCauseDescription(BuildDetailModel buildDetail)
		{
			Debug.Assert(buildDetail != null);

			var action = buildDetail.actions.FirstOrDefault();
			if (action == null)
			{
				return null;
			}

			var cause = action.causes.FirstOrDefault();
			if (cause == null)
			{
				return null;
			}

			if (string.IsNullOrWhiteSpace(cause.shortDescription) == true)
			{
				return null;
			}

			return cause.shortDescription.Trim() + "\r\n";
		}

		private static ProjectModel FetchProject(WebClient webClient, Uri projectApiUrl)
		{
			Debug.Assert(webClient != null);
			Debug.Assert(projectApiUrl != null);

			using (var requestStream = webClient.OpenRead(projectApiUrl))
			{
				var projectSerializer = new DataContractJsonSerializer(typeof(ProjectModel));
				return (ProjectModel)projectSerializer.ReadObject(requestStream);
			}
		}

		private static BuildDetailModel FetchBuildDetail(WebClient webClient, Uri buildDetailApiUrl)
		{
			Debug.Assert(webClient != null);
			Debug.Assert(buildDetailApiUrl != null);

			using (var requestStream = webClient.OpenRead(buildDetailApiUrl))
			{
				var buildDetailSerializer = new DataContractJsonSerializer(typeof(BuildDetailModel));
				return (BuildDetailModel)buildDetailSerializer.ReadObject(requestStream);
			}
		}

		private void UpdateNotifierByModel(ProjectModel project, BuildDetailModel buildDetail)
		{
			Debug.Assert(project != null);
			Debug.Assert(buildDetail != null);

			if (buildDetail.building == true)
			{
				this.UpdateNotifier(
					runningNotificationInformation_,
					project.displayName,
					string.Format("Building [{0}]...", buildDetail.number),
					string.Format("{0}{1}",
						GetCauseDescription(buildDetail),
						string.Join("\r\n", buildDetail.changeSet.items.
							Select(item => string.Format("{0}: {1}", item.commitId.Trim(), item.author.fullName.Trim())))));
			}
			else if (buildDetail.result == Result.SUCCESS)
			{
				this.UpdateNotifier(
					successNotificationInformation_,
					project.displayName,
					string.Format("Build SUCCESS [{0}].", buildDetail.number));
			}
			else if (buildDetail.result == Result.UNSTABLE)
			{
				this.UpdateNotifier(
					unstableNotificationInformation_,
					project.displayName,
					string.Format("Build UNSTABLE [{0}].", buildDetail.number));
			}
			else if (buildDetail.result == Result.FAILURE)
			{
				this.UpdateNotifier(
					failedNotificationInformation_,
					project.displayName,
					string.Format("Build FAILED [{0}].", buildDetail.number));
			}
			else
			{
				this.UpdateNotifier(
					unknownNotificationInformation_,
					project.displayName,
					string.Format("Build status not recognized [{0}].", buildDetail.number));
			}
		}

		private enum FetchResults
		{
			Done,
			RefreshConnection,
			RetryNow
		}

		private FetchResults FetchFromJenkins(WebClient webClient, Uri projectApiUrl)
		{
			Debug.Assert(webClient != null);
			Debug.Assert(projectApiUrl != null);

			try
			{
				var project = FetchProject(webClient, projectApiUrl);
				if (project == null)
				{
					this.UpdateNotifier(
						unknownNotificationInformation_,
						string.Format("Jenkins: [{0}]", baseUrl_),
						"Build status not recognized.");

					return FetchResults.Done;
				}

				if (project.builds == null)
				{
					this.UpdateNotifier(
						unknownNotificationInformation_,
						project.displayName,
						"Build status not recognized.");

					return FetchResults.Done;
				}

				var latestBuild = project.builds.OrderByDescending(build => build.number).FirstOrDefault();
				if (latestBuild == null)
				{
					this.UpdateNotifier(
						unknownNotificationInformation_,
						project.displayName,
						"Build status not recognized.");

					return FetchResults.Done;
				}

				var buildDetailApiUrl = new Uri(latestBuild.url, "api/json");
				var buildDetail = FetchBuildDetail(webClient, buildDetailApiUrl);
				if (buildDetail == null)
				{
					this.UpdateNotifier(
						unknownNotificationInformation_,
						project.displayName,
						"Build status not recognized.");

					return FetchResults.Done;
				}

				this.UpdateNotifierByModel(project, buildDetail);
			}
			catch (WebException ex)
			{
				if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Unauthorized)
				{
					var credential = NativeMethods.ShowCredentialDialog(baseUrl_.ToString(), "Enter Jenkins credential...");
					if (credential != null)
					{
						credential_ = credential;
						SetCredential(webClient);
						return FetchResults.RetryNow;
					}
				}

				this.UpdateNotifier(
					exceptionNotificationInformation_,
					string.Format("Jenkins: [{0}]", baseUrl_),
					string.Format("{0}: {1}", ex.GetType().Name, ex.Message));

				return FetchResults.RefreshConnection;
			}
			catch (Exception ex)
			{
				this.UpdateNotifier(
					exceptionNotificationInformation_,
					string.Format("Jenkins: [{0}]", baseUrl_),
					string.Format("{0}: {1}", ex.GetType().Name, ex.Message));

				return FetchResults.RefreshConnection;
			}

			return FetchResults.Done;
		}

		private void Poller()
		{
			var projectApiUrl = new Uri(baseUrl_, "api/json");

			var webClient = new WebClient();
			SetCredential(webClient);

			try
			{
				while (true)
				{
					var fetchResult = this.FetchFromJenkins(webClient, projectApiUrl);
					if (fetchResult == FetchResults.RefreshConnection)
					{
						webClient.Dispose();
						webClient = new WebClient();
						SetCredential(webClient);
					}
					else if (fetchResult == FetchResults.RetryNow)
					{
						continue;
					}

					var index = WaitHandle.WaitAny(new[] { abort_, fetch_ }, 10000);
					if (index == 0)
					{
						break;
					}
				}
			}
			finally
			{
				webClient.Dispose();
			}
		}
	}
}
