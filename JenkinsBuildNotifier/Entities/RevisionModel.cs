using System;
using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class RevisionModel
	{
		[DataMember]
		public Uri module
		{
			get;
			set;
		}

		[DataMember]
		public int revision
		{
			get;
			set;
		}
	}
}
