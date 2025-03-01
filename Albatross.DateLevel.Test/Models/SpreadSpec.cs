using System;

namespace Albatross.DateLevel.Test {
	public class SpreadSpec : DateLevelEntity<int> {
		public SpreadSpec(int marketId, DateOnly startDate, int value) : base(startDate) {
			this.MarketId = marketId;
			this.Value = value;
		}


		public SpreadSpec(int marketId, DateOnly startDate, DateOnly endDate, int value) : base(startDate) {
			this.MarketId = marketId;
			this.EndDate = endDate;
			this.Value = value;
		}

		public int Id { get; set; }
		public int MarketId { get; set; }
		public int Value { get; init; }

		public override int Key => MarketId;


		public override bool HasSameValue(DateLevelEntity src) {
			if (src is SpreadSpec other) {
				return MarketId == other.MarketId && other.Value == this.Value;
			} else {
				return false;
			}
		}
		public override object Clone() {
			return new SpreadSpec(MarketId, StartDate, Value) {
				EndDate = this.EndDate,
			};
		}
	}
}