using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JenkinsBuildNotifier.Entities
{
	[DataContract]
	internal sealed class BuildDetailModel
	{
		[DataMember]
		public List<ActionModel> actions
		{
			get;
			set;
		}

		[DataMember]
		public bool building
		{
			get;
			set;
		}

		[DataMember]
		public string description
		{
			get;
			set;
		}

		[DataMember]
		public long duration
		{
			get;
			set;
		}

		[DataMember]
		public long estimatedDuration
		{
			get;
			set;
		}

		[DataMember]
		public string fullDisplayName
		{
			get;
			set;
		}

		[DataMember]
		public string id
		{
			get;
			set;
		}

		[DataMember]
		public int number
		{
			get;
			set;
		}

		[DataMember(Name = "result")]
		private string _result
		{
			get
			{
				return (this.result != null) ? this.result.ToString() : null;
			}
			set
			{
				this.result = (value != null) ? (Result?)Enum.Parse(typeof(Result), value) : null;
			}
		}

		public Result? result
		{
			get;
			set;
		}

		[DataMember(Name = "timestamp")]
		public long _timestamp
		{
			get
			{
				return this.timestamp.ToTimestamp();
			}
			set
			{
				this.timestamp = value.ToDateTime();
			}
		}

		public DateTime timestamp
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
		public string builtOn
		{
			get;
			set;
		}

		[DataMember]
		public ChangeSetModel changeSet
		{
			get;
			set;
		}

		[DataMember]
		public List<RevisionModel> revisions
		{
			get;
			set;
		}
	}
}
