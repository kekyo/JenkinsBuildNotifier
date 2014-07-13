using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class ChangeSetItemModel
	{
		[DataMember]
		public List<string> affectedPaths
		{
			get;
			set;
		}

		[DataMember]
		public AuthorModel author
		{
			get;
			set;
		}

		[DataMember]
		public string commitId
		{
			get;
			set;
		}

		[DataMember(Name = "timestamp")]
		public long _timestamp
		{
			get
			{
				return 0;
			}
			set
			{
				this.timestamp = EntityExtension.ToDateTime(value);
			}
		}

		public DateTime timestamp
		{
			get;
			set;
		}
	}
}
