using System;
using System.Collections.Generic;
using System.Linq;

namespace Albatross.DateLevel.Test {
	public class DateLevelTestRunner {
		/// <summary>
		/// create date level series with random values.  The first entry will have the start date of seriesStart and the last entry will have the end date of seriesEnd.  
		/// The rest of the entries will always have its start date as the first on the month and end date as the last day of the month.
		/// </summary>
		/// <param name="seriesStart"></param>
		/// <param name="seriesEnd"></param>
		/// <returns></returns>
		public static IEnumerable<SpreadSpec> CreateMonthlySeries(int key, DateOnly seriesStart, DateOnly seriesEnd, int? value) {
			for (var start = seriesStart; start <= seriesEnd; start = start.AddMonths(1)) {
				var actual_value = value ?? Random.Shared.Next(1, 100);
				var end = start.AddMonths(1).AddDays(-1);
				if (end > seriesEnd) {
					end = seriesEnd;
				}
				var item = new SpreadSpec(key, start, end, actual_value);
				yield return item;
			}
		}
		public static IEnumerable<SpreadSpec> CreateRandomSeries(int key, DateOnly seriesStart, DateOnly seriesEnd, Dictionary<DateOnly, int> dict) {
			DateOnly start, end;
			for(start = seriesStart, end = seriesStart.AddDays(Random.Shared.Next(0, 10)); end <= seriesEnd; start = end.AddDays(1), end = start.AddDays(Random.Shared.Next(0, 10))) {
				var value = Random.Shared.Next(1, 100);
				var item = new SpreadSpec(key, start, end, value);
				yield return item;
				for (var date = start; date <= end; date = date.AddDays(1)) {
					dict[date] = value;
				}
			}
			if (end > seriesEnd) {
				var value = Random.Shared.Next(1, 100);
				var item = new SpreadSpec(key, start, seriesEnd, value);
				yield return item;
				for (var date = start; date <= seriesEnd; date = date.AddDays(1)) {
					dict[date] = value;
				}
			}
		}

		const int Key = 1;
		public DateLevelTestRunner(string seriesStart, string seriesEnd, string start, string end, int value) : this(seriesStart, seriesEnd, start, end, value, value) { }

		public DateLevelTestRunner(string seriesStart, string seriesEnd, string start, string end, int? seriesValue, int srcValue) {
			SeriesStart = DateOnly.Parse(seriesStart, null);
			SeriesEnd = DateOnly.Parse(seriesEnd, null);
			Start = DateOnly.Parse(start, null);
			End = DateOnly.Parse(end, null);
			SourceValue = srcValue;
			SeriesValue = seriesValue;
			List = CreateMonthlySeries(Key, SeriesStart, SeriesEnd, seriesValue).ToList();
			this.SourceItem = new SpreadSpec(Key, Start, End, SourceValue);
			this.Results = Run();
		}

		public DateOnly SeriesStart { get; }
		public DateOnly SeriesEnd { get; }
		public int? SeriesValue { get; }
		public int RequiredSeriesValue => SeriesValue ?? throw new InvalidOperationException("SeriesValue is random");
		public List<SpreadSpec> List { get; private set; }

		public DateOnly Start { get; private set; }
		public DateOnly End { get; private set; }
		public int SourceValue { get; private set; }
		public SpreadSpec SourceItem { get; private set; }

		public List<SpreadSpec> Results { get; private set; }

		public List<SpreadSpec> Run() {
			Results = List.SetDateLevel<SpreadSpec, int>(SourceItem).OrderBy(x => x.StartDate).ToList();
			return Results;
		}
	}
}
