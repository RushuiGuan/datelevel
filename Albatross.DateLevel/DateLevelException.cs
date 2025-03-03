using System;

namespace Albatross.DateLevel {
	public class DateLevelException : Exception {
		public IDateLevelEntity Entity { get; }
		public DateLevelException(IDateLevelEntity entity, string msg) : base(msg) {
			Entity = entity;
		}
	}
}
