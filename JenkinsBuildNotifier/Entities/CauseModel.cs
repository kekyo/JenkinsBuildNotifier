using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class CauseModel
	{
		[DataMember]
		public string shortDescription
		{
			get;
			set;
		}
	}
}
