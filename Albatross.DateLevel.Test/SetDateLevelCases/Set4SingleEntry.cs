using System;
using System.Collections.Generic;
using Xunit;

namespace Albatross.DateLevel.Test {
	public class Set4SingleEntry {
		
		// start_input < start && end = end_input
		[Fact]
		public void Input_Full_Overlap_Current() {
			var list = new List<Spec>(); 
			list.SetDateLevel<Spec, int>(new Spec(1, DateOnlyValues.May1_2022, 100));
			list.SetDateLevel<Spec, int>(new Spec(1, DateOnlyValues.Feb1_2022, 200) {
				EndDate = DateOnlyValues.MaxDate
			});

			Assert.Collection(list,
				args => {
					Assert.Equal(DateOnlyValues.Feb1_2022, args.StartDate);
					Assert.Equal(DateOnlyValues.MaxDate, args.EndDate);
					Assert.Equal(200, args.Value);
				}
			);
		}

		[Fact]
		public void Input_Has_Same_DateRange() {
			var list = new List<Spec>();
			list.SetDateLevel<Spec, int>(new Spec(1, DateOnlyValues.May1_2022, 100));
			list.SetDateLevel<Spec, int>(new Spec(1, DateOnlyValues.May1_2022, 200) {
				EndDate = DateOnlyValues.MaxDate
			});

			Assert.Collection(list,
				args => {
					Assert.Equal(DateOnlyValues.May1_2022, args.StartDate);
					Assert.Equal(DateOnlyValues.MaxDate, args.EndDate);
					Assert.Equal(200, args.Value);
				}
			);
		}
	}
}