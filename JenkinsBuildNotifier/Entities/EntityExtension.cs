using System;

namespace JenkinsBuildNotifier.Entities
{
	internal static class EntityExtension
	{
		private static readonly DateTime epoch_ = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static long ToTimestamp(this DateTime value)
		{
			return (long)(value - epoch_).TotalMilliseconds;
		}

		public static DateTime ToDateTime(this long value)
		{
			return epoch_.AddMilliseconds(value);
		}
	}
}
