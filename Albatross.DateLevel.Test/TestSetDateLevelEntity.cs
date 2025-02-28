using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Albatross.DateLevel.Test {
	public class TestSetDateLevelEntity {
		public class Runner {
			const int Key = 1;
			public Runner(string seriesStart, string seriesEnd, string start, string end, int value) : this(seriesStart, seriesEnd, start, end, value, value) { }

			public Runner(string seriesStart, string seriesEnd, string start, string end, int? seriesValue, int srcValue) {
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
			public DateOnly Start { get; }
			public DateOnly End { get; }
			public int SourceValue { get; }
			public int? SeriesValue { get; }
			public int RequiredSeriesValue => SeriesValue ?? throw new InvalidOperationException("SeriesValue is random");

			public List<SpreadSpec> List { get; }
			public SpreadSpec SourceItem { get; }
			public List<SpreadSpec> Results { get; private set; }

			List<SpreadSpec> Run() {
				Results = List.SetDateLevel<SpreadSpec, int>(SourceItem).OrderBy(x => x.StartDate).ToList();
				return Results;
			}
		}


		[Fact]
		public void InvalidDates() {
			var list = new List<SpreadSpec>();
			var src = new SpreadSpec(1, DateOnlyValues.Mar1_2022, DateOnlyValues.Feb1_2022, 100);
			Assert.Throws<ArgumentException>(() => list.SetDateLevel<SpreadSpec, int>(src).ToArray());
		}

		[Fact]
		public void EmptyCollection() {
			var list = new List<SpreadSpec>();
			var result = list.SetDateLevel<SpreadSpec, int>(new SpreadSpec(1, DateOnlyValues.Mar1_2022, DateOnlyValues.Mar31_2022, 100)).ToArray();

			Assert.Collection(result,
				args => {
					Assert.Equal(DateOnlyValues.Mar1_2022, args.StartDate);
					Assert.Equal(DateOnlyValues.Mar31_2022, args.EndDate);
					Assert.Equal(100, args.Value);
				}
			);
		}

		[Fact]
		public void EmptyCollection_MixKeys() {
			var list = new List<SpreadSpec> {
				new SpreadSpec(2, DateOnlyValues.Mar1_2022, DateOnlyValues.Mar31_2022, 200)
			};
			var result = list.SetDateLevel<SpreadSpec, int>(new SpreadSpec(1, DateOnlyValues.Mar1_2022, DateOnlyValues.Mar31_2022, 100)).ToArray();

			Assert.Collection(result,
				args => {
					Assert.Equal(2, args.Key);
					Assert.Equal(DateOnlyValues.Mar1_2022, args.StartDate);
					Assert.Equal(DateOnlyValues.Mar31_2022, args.EndDate);
					Assert.Equal(200, args.Value);
				},
				args => {
					Assert.Equal(1, args.Key);
					Assert.Equal(DateOnlyValues.Mar1_2022, args.StartDate);
					Assert.Equal(DateOnlyValues.Mar31_2022, args.EndDate);
					Assert.Equal(100, args.Value);
				}
			);
		}

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

		[Theory]
		[InlineData("2022-02-01", "2022-02-01", 1)]
		[InlineData("2022-02-01", "2022-01-01", 0)]
		[InlineData("2022-02-01", "2022-03-01", 2)]
		[InlineData("2022-02-01", "2022-04-01", 3)]
		public void TestCreateMonthlySeries(string start, string end, int expectedCount) {
			var seriesStart = DateOnly.Parse(start, null);
			var seriesEnd = DateOnly.Parse(end, null);
			var list = CreateMonthlySeries(1, seriesStart, seriesEnd, null).ToList();
			Assert.Equal(expectedCount, list.Count);
			if (list.Count > 0) {
				Assert.Equal(seriesStart, list.First().StartDate);
				Assert.Equal(seriesEnd, list.Last().EndDate);
			}
		}


		[Theory]
		// single entry
		[InlineData("2022-02-01", "2022-03-30", "2022-03-01", "2022-03-02")]    // extra in the front and in the back
		[InlineData("2022-02-01", "2022-03-30", "2022-03-01", "2022-03-01")]    // extra in the front and in the back, single day
		[InlineData("2022-03-01", "2022-03-30", "2022-03-01", "2022-03-02")]    // extra in the back
		[InlineData("2022-03-01", "2022-03-30", "2022-03-01", "2022-03-01")]    // extra in the back, single day
		[InlineData("2022-02-01", "2022-03-02", "2022-03-01", "2022-03-02")]    // extra in the front
		[InlineData("2022-02-01", "2022-03-01", "2022-03-01", "2022-03-01")]    // extra in the front, single day
		[InlineData("2022-03-01", "2022-03-02", "2022-03-01", "2022-03-02")]    // same
		[InlineData("2022-03-01", "2022-03-01", "2022-03-01", "2022-03-01")]    // same and single day
																				// multiple entries
		[InlineData("2022-02-01", "2022-07-30", "2022-03-01", "2022-06-30")]    // extra in the front and
		[InlineData("2022-03-01", "2022-07-30", "2022-03-01", "2022-06-30")]    // extra in the back
		[InlineData("2022-02-01", "2022-06-30", "2022-03-01", "2022-06-30")]    // extra in the front
		[InlineData("2022-03-01", "2022-06-30", "2022-03-01", "2022-06-30")]    // same
		public void Source_Overlap_Current_Inclusive_SameValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1);
			Assert.Single(runner.Results);
			var result = runner.Results.First();
			Assert.Equal(runner.Start, result.StartDate);
			Assert.Equal(runner.End, result.EndDate);
			Assert.Equal(runner.SourceValue, result.Value);

			// because it is the same value.  the method will actually reused one of the existing entry
			Assert.NotSame(runner.SourceItem, result);
			Assert.Single(runner.Results.Where(x => object.ReferenceEquals(x, result)).ToArray());
		}

		[Theory]
		// single entry
		[InlineData("2022-02-01", "2022-03-30", "2022-03-01", "2022-03-02")]    // extra in the front and in the back
		[InlineData("2022-03-01", "2022-03-30", "2022-03-01", "2022-03-02")]    // extra in the back
		[InlineData("2022-02-01", "2022-03-02", "2022-03-01", "2022-03-02")]    // extra in the front
		[InlineData("2022-03-01", "2022-03-02", "2022-03-01", "2022-03-02")]    // same
		[InlineData("2022-03-01", "2022-03-01", "2022-03-01", "2022-03-01")]    // same and single day
																				// multiple entries
		[InlineData("2022-02-01", "2022-07-30", "2022-03-01", "2022-06-30")]    // extra in the front and
		[InlineData("2022-03-01", "2022-07-30", "2022-03-01", "2022-06-30")]    // extra in the back
		[InlineData("2022-02-01", "2022-06-30", "2022-03-01", "2022-06-30")]    // extra in the front
		[InlineData("2022-03-01", "2022-06-30", "2022-03-01", "2022-06-30")]    // same
		public void Source_Overlap_Current_Inclusive_DiffValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1, 2);
			Assert.Collection(runner.Results,
				args => {
					Assert.Equal(runner.Start, args.StartDate);
					Assert.Equal(runner.End, args.EndDate);
					Assert.Equal(runner.SourceValue, args.Value);
				}
			);
		}


		[Theory]
		// single entry
		[InlineData("2022-03-15", "2022-03-15", "2022-03-14", "2022-03-16")]    // src is single day
		[InlineData("2022-03-10", "2022-03-15", "2022-03-01", "2022-03-31")]    // src is multiple days
		public void Current_Overlap_Source_Exclusive_SameValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1);
			Assert.Single(runner.Results);
			var result = runner.Results.First();
			Assert.Equal(runner.SeriesStart, result.StartDate);
			Assert.Equal(runner.SeriesEnd, result.EndDate);
			Assert.Equal(runner.SourceValue, result.Value);
			Assert.NotSame(result, runner.SourceItem);
			Assert.Same(result, runner.List.First());
		}
		[Theory]
		[InlineData("2022-03-15", "2022-03-15", "2022-03-14", "2022-03-16")]    // src is single day
		[InlineData("2022-03-10", "2022-03-15", "2022-03-01", "2022-03-31")]    // src is multiple days
		public void Current_Overlap_Source_Exclusive_DiffValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1, 2);
			Assert.Collection(runner.Results,
				x => {
					Assert.Equal(runner.SeriesStart, x.StartDate);
					Assert.Equal(runner.Start.AddDays(-1), x.EndDate);
					Assert.Equal(runner.RequiredSeriesValue, x.Value);
					Assert.Same(x, runner.List.First());
				},
				x => {
					Assert.Equal(runner.Start, x.StartDate);
					Assert.Equal(runner.End, x.EndDate);
					Assert.Equal(runner.SourceValue, x.Value);
					Assert.Same(runner.SourceItem, x);
				},
				x => {
					Assert.Equal(runner.End.AddDays(1), x.StartDate);
					Assert.Equal(runner.SeriesEnd, x.EndDate);
					Assert.Equal(runner.RequiredSeriesValue, x.Value);
				}
			);
		}

		[Theory]
		/// single day current is equalvalent to source overlap current
		[InlineData("2022-03-01", "2022-03-15", "2022-03-01", "2022-03-31")]    // current is multiple day, src start is the same as current start
		[InlineData("2022-02-01", "2022-03-01", "2022-03-01", "2022-03-31")]    // current is multiple day, src end is the same as current start
		[InlineData("2022-02-01", "2022-03-15", "2022-03-01", "2022-03-31")]    // current is multiple day, src start is less than current start and src end is greater than current start
		public void Source_Overlap_Current_Partially_On_The_Left_SameValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1);
			var expected = new[] { runner.SourceItem }.Union(runner.List.Skip(1)).ToList();
			expected.SequenceEqual(runner.Results);
			runner.Results.First().Should().NotBeSameAs(runner.SourceItem);
			runner.Results.First().Should().BeSameAs(runner.List.First());
		}

		[Theory]
		/// single day current is equalvalent to source overlap current
		[InlineData("2022-03-01", "2022-03-15", "2022-03-01", "2022-03-31")]    // current is multiple day, src start is the same as current start
		[InlineData("2022-02-01", "2022-03-01", "2022-03-01", "2022-03-31")]    // current is multiple day, src end is the same as current start
		[InlineData("2022-02-01", "2022-03-15", "2022-03-01", "2022-03-31")]    // current is multiple day, src start is less than current start and src end is greater than current start
		public void Source_Overlap_Current_Partially_On_The_Left_DiffValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1, 2);
			var expected = new[] { runner.SourceItem }.Union(runner.List.Skip(1)).ToList();
			expected.SequenceEqual(runner.Results);
			runner.Results.First().Should().BeSameAs(runner.SourceItem);
		}

		[Theory]
		/// single day current is equalvalent to source overlap current
		[InlineData("2022-03-15", "2022-03-31", "2022-03-01", "2022-03-31")]    // current is multiple day, src end is the same as current end
		[InlineData("2022-03-31", "2022-04-30", "2022-03-01", "2022-03-31")]    // current is multiple day, src start is the same as current end
		[InlineData("2022-03-15", "2022-04-30", "2022-03-01", "2022-03-31")]    // current is multiple day, src start is greater than current start and src end is greater than current end
		public void Source_Overlap_Current_Partially_On_The_Right_SameValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1);
			var expected = runner.List.SkipLast(1).Union(new[] { runner.SourceItem }).ToList();
			expected.SequenceEqual(runner.Results);
			runner.Results.Last().Should().NotBeSameAs(runner.SourceItem);
			runner.Results.Last().Should().BeSameAs(runner.List.Last());
		}

		[Theory]
		/// single day current is equalvalent to source overlap current
		[InlineData("2022-03-15", "2022-03-31", "2022-03-01", "2022-03-31")]    // current is multiple day, src end is the same as current end
		[InlineData("2022-03-31", "2022-04-30", "2022-03-01", "2022-03-31")]    // current is multiple day, src start is the same as current end
		[InlineData("2022-03-15", "2022-04-30", "2022-03-01", "2022-03-31")]    // current is multiple day, src start is greater than current start and src end is greater than current end
		public void Source_Overlap_Current_Partially_On_The_Right_DiffValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1, 2);
			var expected = runner.List.SkipLast(1).Union(new[] { runner.SourceItem }).ToList();
			expected.SequenceEqual(runner.Results);
			runner.Results.Last().Should().BeSameAs(runner.SourceItem);
		}

		[Theory]
		[InlineData("2022-01-01", "2022-01-31", "2022-02-02", "2022-02-28", 1, 1)]
		[InlineData("2022-01-01", "2022-01-31", "2022-02-02", "2022-02-28", 1, 2)]
		[InlineData("2022-03-02", "2022-03-31", "2022-02-02", "2022-02-28", 1, 1)]
		[InlineData("2022-03-02", "2022-03-31", "2022-02-02", "2022-02-28", 1, 2)]
		public void Source_Does_Not_Overlap_With_Current_Not_Continuous(string startText, string endText, string series_start_text, string series_end_text, int seriesValue, int sourceValue) {
			Assert.Throws<ArgumentException>(() => new Runner(series_start_text, series_end_text, startText, endText, seriesValue, sourceValue));
		}

		[Theory]
		[InlineData("2022-01-01", "2022-01-31", "2022-02-01", "2022-02-28")]
		[InlineData("2022-01-01", "2022-01-31", "2022-02-01", "2022-02-01")]
		public void Source_Is_Left_Of_Series_But_Continuous_SameValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1);
			runner.List.SequenceEqual(runner.Results);
			runner.List.First().StartDate.Should().Be(runner.Start);
		}

		[Theory]
		[InlineData("2022-03-01", "2022-03-31", "2022-02-01", "2022-02-28")]
		[InlineData("2022-02-02", "2022-02-28", "2022-02-01", "2022-02-01")]
		public void Source_Is_Right_Of_Series_But_Continuous_SameValue(string startText, string endText, string series_start_text, string series_end_text) {
			var runner = new Runner(series_start_text, series_end_text, startText, endText, 1);
			runner.List.SequenceEqual(runner.Results);
			runner.List.Last().EndDate.Should().Be(runner.End);
		}
	}
}
