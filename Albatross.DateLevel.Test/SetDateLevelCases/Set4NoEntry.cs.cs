using System;
using System.Collections.Generic;
using Xunit;

namespace Albatross.DateLevel.Test {
	public class Set4NoEntry {
		[Fact]
		public void Run() {
			var list = new List<Spec>();
			list.SetDateLevel<Spec, int>(new Spec(1, DateOnlyValues.May1_2022, 100) {
				EndDate = DateOnlyValues.Jun30_2022
			});

			Assert.Collection(list,
				args => {
					Assert.Equal(DateOnlyValues.May1_2022, args.StartDate);
					Assert.Equal(DateOnlyValues.MaxDate, args.EndDate);
					Assert.Equal(100, args.Value);
				}
			);
		}

		[Fact]
		public void RunInvalidUseCase() {
			var entry = new Spec(1, DateOnlyValues.May1_2022, 100) {
				EndDate = DateOnlyValues.Apr1_2022
			};
			Assert.Throws<ArgumentException>(() => new List<Spec>().SetDateLevel<Spec, int>(entry));
		}
	}
}