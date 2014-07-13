using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class ProjectModel
	{
		[DataMember]
		public string description
		{
			get;
			set;
		}

		[DataMember]
		public string displayName
		{
			get;
			set;
		}

		[DataMember]
		public string name
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

		[DataMember]
		public bool buildable
		{
			get;
			set;
		}

		[DataMember]
		public List<BuildModel> builds
		{
			get;
			set;
		}

		[DataMember(Name = "color")]
		private string _color
		{
			get
			{
				return this.color.Name.ToLowerInvariant();
			}
			set
			{
				this.color = Color.FromName(value);
			}
		}

		public Color color
		{
			get;
			set;
		}

		[DataMember]
		public bool inQueue
		{
			get;
			set;
		}
	}
}
