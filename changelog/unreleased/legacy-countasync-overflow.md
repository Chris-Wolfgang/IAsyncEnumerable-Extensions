type: fix

Legacy `CountAsync` now throws `OverflowException` when the source has more than `int.MaxValue` elements, as `Enumerable.Count` does, instead of silently returning a wrapped negative count.
