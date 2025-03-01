using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Albatross.DateLevel.Test {
	public class SetDateLevelStressTest {
		[Theory]
		[InlineData("2021-01-01", "2021-12-31", "2021-01-01", "2021-12-31", 1)]
		public void Source_Overlap_Current_Inclusive(string series_start, string series_end, string start, string end, int value) {
			var runner = new DateLevelTestRunner(series_start, series_end, start, end, value);
			Assert.Single(runner.Results);
			runner.SourceItem.Should().BeEquivalentTo(runner.Results[0]);
		}

		[Theory]
		[InlineData("2021-01-01", "2021-12-31")]
		[InlineData("2021-01-01", "2021-01-01")]
		[InlineData("2021-01-01", "2021-01-10")]
		public void TestCreateRandomSeries(string seriesStart_text, string seriesEnd_text) {
			var dict = new Dictionary<DateOnly, int>();
			var seriesStart = DateOnly.Parse(seriesStart_text, null);
			var seriesEnd = DateOnly.Parse(seriesEnd_text, null);
			var list = DateLevelTestRunner.CreateRandomSeries(1, seriesStart, seriesEnd, dict).ToList();
			dict.Should().HaveCount(seriesEnd.DayNumber - seriesStart.DayNumber + 1);
			list.First().StartDate.Should().Be(seriesStart);
			list.Last().EndDate.Should().Be(seriesEnd);
			dict.Keys.First().Should().Be(seriesStart);
			dict.Keys.Last().Should().Be(seriesEnd);
		}


		[Theory]
		[InlineData("2021-01-01", "2021-12-31", 10000)]
		[InlineData("2021-01-01", "2021-01-01", 10)]
		[InlineData("2021-01-01", "2021-01-31", 10000)]
		[InlineData("2020-01-01", "2025-12-31", 10000)]
		public void StressTest(string seriesStart_text, string seriesEnd_text, int count) {
			for (int i = 0; i < count; i++) {
				var seriesStart = DateOnly.Parse(seriesStart_text, null);
				var seriesEnd = DateOnly.Parse(seriesEnd_text, null);
				const int key = 1;
				var dict = new Dictionary<DateOnly, int>();
				var series = DateLevelTestRunner.CreateRandomSeries(key, seriesStart, seriesEnd, dict).ToList();

				var d1 = DateOnly.FromDayNumber(Random.Shared.Next(seriesStart.DayNumber - 100, seriesEnd.DayNumber + 100));
				var d2 = DateOnly.FromDayNumber(Random.Shared.Next(seriesStart.DayNumber - 100, seriesEnd.DayNumber + 100));
				
				var start = d1 < d2 ? d1 : d2;
				var end = d1 < d2 ? d2 : d1;
				var source = new SpreadSpec(key, start, end, Random.Shared.Next(1, 100));
				for(var date = start; date <= end; date = date.AddDays(1)) {
					dict[date] = source.Value;
				}

				if (end < seriesStart.AddDays(-1) || start > seriesEnd.AddDays(1)) {
					Assert.Throws<ArgumentException>(() => series.SetDateLevel<SpreadSpec, int>(source).ToArray());
				} else {
					var results = series.SetDateLevel<SpreadSpec, int>(source).OrderBy(x => x.StartDate).ToArray();
					foreach (var item in results) {
						for (var date = item.StartDate; date <= item.EndDate; date = date.AddDays(1)) {
							item.Value.Should().Be(dict[date]);
						}
					}
				}
			}
		}
	}
}
