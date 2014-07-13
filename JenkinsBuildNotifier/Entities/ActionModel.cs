using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class ActionModel
	{
		[DataMember]
		public List<CauseModel> causes
		{
			get;
			set;
		}

		[DataMember]
		public int? failCount
		{
			get;
			set;
		}

		[DataMember]
		public int? skipCount
		{
			get;
			set;
		}

		[DataMember]
		public int? totalCount
		{
			get;
			set;
		}

		[DataMember]
		public string urlName
		{
			get;
			set;
		}
	}
}
