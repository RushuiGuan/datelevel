using System;

namespace Albatross.DateLevel.Test {
	public class ContractSpec : DateLevelEntity<int> {
		public int Id { get; set; }
		public ContractSpec(int marketId, DateOnly startDate, decimal value) : base(startDate) {
			this.MarketId = marketId;
			this.Value = value;
		}
		public int MarketId { get; set; }
		public decimal Value { get; init; }
		public override int Key => MarketId;

		public override bool HasSameValue(DateLevelEntity src) {
			if (src is ContractSpec other) {
				return Value == other.Value && other.Value == this.Value;
			} else {
				return false;
			}
		}

		public override object Clone() {
			return new ContractSpec(MarketId, StartDate, Value) {
				EndDate = this.EndDate,
			};
		}
	}
}