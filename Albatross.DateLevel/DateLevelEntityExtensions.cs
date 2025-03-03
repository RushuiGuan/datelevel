using System;
using System.Collections.Generic;
using System.Linq;

namespace Albatross.DateLevel {
	public static class DateLevelEntityExtensions {
		public static IEnumerable<T> SetDateLevel<T, K>(this IEnumerable<T> collection, T src)
			where K : IEquatable<K>
			where T : DateLevelEntity<K> {
			return SetDateLevel<T>(collection, src, (x, y) => x.Key.Equals(y.Key));
		}

		public static IEnumerable<T> SetDateLevel<T>(this IEnumerable<T> series, T src, Func<T, T, bool> isSameSeries)
			where T : DateLevelEntity {

			if (src.StartDate > src.EndDate) { throw new ArgumentException("Start date cannot be greater than end date"); }
			bool isContinuous = false;
			bool isEmpty = true;
			foreach (var item in series) {
				if (!isSameSeries(item, src)) {
					yield return item;
				} else {
					isEmpty = false;
					// source completely overlaps item, if same value, extend current and replace source with current.  otherwise do nothing
					if (src.StartDate <= item.StartDate && item.EndDate <= src.EndDate) {
						isContinuous = true;
						if (src.HasSameValue(item)) {
							item.StartDate = src.StartDate;
							item.EndDate = src.EndDate;
							src = item;
						}
					} else if (item.StartDate < src.StartDate && src.EndDate < item.EndDate) {
						isContinuous = true;
						// item completely overlaps source, if same value, do nothing.  otherwise split the item
						if (src.HasSameValue(item)) {
							src = item;
						} else {
							var after = (T)item.Clone();
							after.StartDate = src.EndDate.AddDays(1);
							// note that item.EndDate is modified.  therefore after.EndDate should be set first.
							after.EndDate = item.EndDate;
							item.EndDate = src.StartDate.AddDays(-1);
							yield return item;
							yield return after;
						}
					} else if (src.StartDate <= item.StartDate && item.StartDate <= src.EndDate && src.EndDate < item.EndDate) {
						isContinuous = true;
						// source overlaps the start of item, if same value, extend item.  otherwise, reduce item start date
						if (src.HasSameValue(item)) {
							item.StartDate = src.StartDate;
							src = item;
						} else {
							item.StartDate = src.EndDate.AddDays(1);
							yield return item;
						}
					} else if (item.StartDate < src.StartDate && src.StartDate <= item.EndDate && item.EndDate <= src.EndDate) {
						isContinuous = true;
						if (src.HasSameValue(item)) {
							item.EndDate = src.EndDate;
							src = item;
						} else {
							item.EndDate = src.StartDate.AddDays(-1);
							yield return item;
						}
					} else if (src.EndDate == item.StartDate.AddDays(-1)) {
						isContinuous = true;
						if (src.HasSameValue(item)) {
							item.StartDate = src.StartDate;
							src = item;
						} else {
							yield return item;
						}
					} else if (src.StartDate == item.EndDate.AddDays(1)) {
						isContinuous = true;
						if (src.HasSameValue(item)) {
							item.EndDate = src.EndDate;
							src = item;
						} else {
							yield return item;
						}
					} else {
						yield return item;
					}
				}
			}
			// yield return first so that the data is as closed to correct as possible.  caller might try catch the exception and it can still receive data close to correct
			yield return src;
			if (!isContinuous && !isEmpty) {
				throw new ArgumentException($"Cannot add this date level item since it will break the continuity of dates in the series.  Adjust its start date and end date to fix");
			}
		}
		public static IEnumerable<T> SetDateLevel<T>(this IEnumerable<T> series, T src) where T : DateLevelEntity 
			=> SetDateLevel<T>(series, src, (x, y) => true);

		/// <summary>
		/// This function will attempt to update date level values between <paramref name="start"/> and <paramref name="endDate"/>.  If the endDate is not specified, 
		/// the function will find the next record in the series and set the end date to the day before its start date.  If the next record does not exist, the end date 
		/// will be set to the max end date. The function will update only.  It will not insert records if no existing records that overlap the specified date range 
		/// are found.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="K"></typeparam>
		/// <param name="series"></param>
		/// <param name="clone">function pointer to clone an instance of T</param>
		/// <param name="modify">action pointer to modify the value</param>
		/// <param name="start"></param>
		/// <param name="endDate"></param>
		/// <exception cref="ArgumentException"></exception>
		public static void UpdateDateLevel<T>(this ICollection<T> series, Action<T> modify, DateOnly start, DateOnly end, bool rebuild = true)
			where T : DateLevelEntity {
			if (start > end) {
				throw new ArgumentException("Start date cannot be greater than end date");
			}
			foreach (var current in series.ToArray()) {
				if (end < current.StartDate || current.EndDate < start) {
					// no overlap
					continue;
				} else if (start <= current.StartDate && current.EndDate <= end) {
					// new value overlap current
					modify(current);
				} else if (current.StartDate < start && end < current.EndDate) {
					// current overlap new value
					var after = (T)current.Clone();
					after.StartDate = end.AddDays(1);
					after.EndDate = current.EndDate;
					series.Add(after);

					var newItem = (T)current.Clone();
					modify(newItem);
					newItem.StartDate = start;
					newItem.EndDate = end;
					series.Add(newItem);

					current.EndDate = start.AddDays(-1);
				} else if (start <= current.StartDate && current.StartDate <= end && end < current.EndDate) {
					var newItem = (T)current.Clone();
					modify(newItem);
					newItem.StartDate = current.StartDate;
					newItem.EndDate = end;
					series.Add(newItem);
					current.StartDate = end.AddDays(1);
				} else if (current.StartDate < start && start <= current.EndDate && end >= current.EndDate) {
					var newItem = (T)current.Clone();
					modify(newItem);
					newItem.StartDate = start;
					newItem.EndDate = current.EndDate;
					series.Add(newItem);
					current.EndDate = start.AddDays(-1);
				}
			}
			if (rebuild) {
				series.RebuildDateLevelSeries(x => series.Remove(x));
			}
		}

		/// <summary>
		/// Provided a date level series data for a single entity, the method will set the start date of the series to the new start date by trimming
		/// the any data prior to the new start date.  The method assume that the series is correctly built.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="series"></param>
		/// <param name="newStartDate"></param>
		public static IEnumerable<T> TrimStart<T>(this IEnumerable<T> series, DateOnly newStartDate) where T : DateLevelEntity {
			foreach (var item in series) {
				if (item.EndDate < newStartDate) {
					continue;
				} else if (item.StartDate < newStartDate) {
					item.StartDate = newStartDate;
				}
				yield return item;
			}
		}
	
		public static IEnumerable<T> TrimEnd<T>(this IEnumerable<T> series, DateOnly newEndDate) where T : DateLevelEntity {
			foreach (var item in series) {
				if (item.StartDate > newEndDate) {
					continue;
				} else if (item.EndDate > newEndDate) {
					item.EndDate = newEndDate;
				}
				yield return item;
			}
		}

		/// <summary>
		/// Provided a date level series data for a single entity, the method will rebuild the end dates and remove items if necessary
		/// The method doesnot care if the current data has incorrect end dates.  It will use the key and the start date to rebuild the
		/// end of the date level series.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="remove"></param>
		/// <returns></returns>
		public static void RebuildDateLevelSeries<T, K>(this IEnumerable<T> source, Action<T> remove)
			where T : DateLevelEntity<K>
			where K : IEquatable<K> {
			var groups = source.GroupBy(x => x.Key);
			foreach (var group in groups) {
				RebuildDateLevelSeries<T>(group, remove);
			}
		}

		public static void RebuildDateLevelSeries<T>(this IEnumerable<T> source, Action<T> remove)
			where T : DateLevelEntity {
			var items = source.OrderBy(x => x.StartDate).ToArray();
			T? current = null;
			foreach (var item in items) {
				if (current == null) {
					current = item;
				} else {
					if (current.HasSameValue(item)) {
						remove(item);
						current.EndDate = item.EndDate;
					} else {
						current.EndDate = item.StartDate.AddDays(-1);
						current = item;
					}
				}
			}
			if (current != null) {
				current.EndDate = IDateLevelEntity.MaxEndDate;
			}
		}

		/// <summary>
		/// The method will return the date level entries with the effective date of <paramref name="date"/> in <paramref name="source"/>.  
		/// The method could return multiple entries since the date level entity key is not specified.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="startDate"></param>
		/// <returns></returns>
		public static IEnumerable<T> Effective<T>(this IEnumerable<T> items, DateOnly date) where T : IDateLevelEntity
			=> items.Where(x => x.StartDate <= date && x.EndDate >= date);

		/// <summary>
		/// This method will return the date level entry with the key of <paramref name="key"/> and the effective date of 
		/// <paramref name="date"/>.  The method will return null if the entry is not found.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="K"></typeparam>
		/// <param name="items"></param>
		/// <param name="key"></param>
		/// <param name="date"></param>
		/// <returns></returns>
		public static T? Effective<T, K>(this IEnumerable<T> items, K key, DateOnly date)
			where T : IDateLevelEntity<K> where K : IEquatable<K> {
			var item = items.Where(args => args.Key.Equals(key)
				&& args.StartDate <= date
				&& args.EndDate >= date).FirstOrDefault();
			return item;
		}

		/// <summary>
		/// The method will find the date level entries in <paramref name="source"/> that overlap with the given date range.  
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public static IEnumerable<T> GetOverlappedDateLevelEntities<T, K>(this IEnumerable<T> source, K key, DateOnly start, DateOnly end)
			where T : IDateLevelEntity<K>
			where K : IEquatable<K> {
			source = source.Where(x => x.Key.Equals(key));
			return GetOverlappedDateLevelEntities<T>(source, start, end);
		}
		/// <summary>
		/// The method will find the date level entries in <paramref name="source"/> that overlap with the given date range.
		/// This method does not require a key, it assumes the supplied source only contains a single date level series.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public static IEnumerable<T> GetOverlappedDateLevelEntities<T>(this IEnumerable<T> source, DateOnly start, DateOnly end)
			where T : IDateLevelEntity {
			return source.Where(args => !(start > args.EndDate || end < args.StartDate));
		}

		public static bool VerifySeries<T>(this IEnumerable<T> series, bool throwException) where T : DateLevelEntity {
			T? previous = null;
			foreach (var item in series.OrderBy(x => x.StartDate)) {
				if (item.StartDate > item.EndDate) {
					if (throwException) {
						throw new DateLevelException(item, $"Start date is greater than end date");
					} else {
						return false;
					}
				} else if (previous != null){
					if(previous.EndDate >= item.StartDate) {
						if (throwException) {
							throw new DateLevelException(item, $"Start date overlaps with previous end date");
						} else {
							return false;
						}
					}else if(previous.EndDate.AddDays(1) < item.StartDate) {
						if (throwException) {
							throw new DateLevelException(item, $"Start date is not continuous from previous end date");
						} else {
							return false;
						}
					}
					return false;
				}
				previous = item;
			}
			return true;
		}
	}
}