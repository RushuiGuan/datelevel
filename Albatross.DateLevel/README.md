# Albatross DateLevel
Date level data is a series of data with start date and end date.  This api provides functionality to insert, update and delete data within a series of data.

## Overview
The date level entity class [DateLevelEntity](./DateLevelEntity.cs) has a `StartDate` and an `EndDate` property.  It can also have a `Key` property  to seperate multiple series of data stored within the same list.  Data with the same key will be considered as the same series of data.  

In order to organize data in date series, the following rules apply.
	1. the dates should not overlap
	2. the dates should not have gaps

Use extension methods in the `DateLevelEntityExtensions` class to manipulate DateLevel collections.
*  `SetDateLevel<T>(this IEnumerable<T> collection, T src, Func<T, T, bool> isSameSeries)`  will insert a date level entity `src` to the `collection`.  If an item in the collection is not the same series as the input field `src`, it would be ignored and returned without any processing.  For data of the same series, the method will apply the value of `src` to the series, therefore overwriting the value of the series between the start date and end date of `src` with its value.  If the dates of `src` produces a gap in the date series, an `ArgumentException` will be thrown but the resulting data would still be correct.  The method uses `yield return` internally and is therefore lazy.  The method has a time complexity of O(n).
* `SetDateLevel<T, K>(this IEnumerable<T> collection, T src)` does the same thing with a builtin `isSameSeries` method since it has an additional generic parameter for the `Key`.