using System;
using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class AuthorModel
	{
		[DataMember]
		public Uri absoluteUrl
		{
			get;
			set;
		}

		[DataMember]
		public string fullName
		{
			get;
			set;
		}
	}
}
