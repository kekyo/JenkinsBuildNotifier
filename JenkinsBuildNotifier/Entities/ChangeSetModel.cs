using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class ChangeSetModel
	{
		[DataMember]
		public List<ChangeSetItemModel> items
		{
			get;
			set;
		}
	}
}
