# Albatross DateLevel
Date level data is a series of data with start date and end date.  This api provides functionality to insert, update and delete data within a series of data.

## Overview
A date level data has a start date and an end date.  It can also have a key if multiple series of data are being stored within the same list.  Data with the same key will be considered as the same series of data.  

In order to organize data in date series, the following rules apply.
	1. the dates should not overlap
	2. the dates should not have gaps
	3. if a series exists, its last end date of the series should always be 9999-12-31.  This rule makes it easier to determine the value of a series at for a particular date.

The api contains methods that will insert data into a date level data series without breaking these rules.