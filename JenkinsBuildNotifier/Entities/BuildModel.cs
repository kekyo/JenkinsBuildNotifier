using System;
using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class BuildModel
	{
		[DataMember]
		public int number
		{
			get;
			set;
		}

		[DataMember]
		public Uri url
		{
			get;
			set;
		}
	}
}
