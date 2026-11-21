type: feature

`ChunkAsync` and `SlidingWindowAsync` gain an overload with an `initialCapacity` parameter, like `new List<T>(int)`: the working buffer starts at that size instead of four and grows from there, so a caller who knows the typical chunk or window length can avoid the growth steps. The chunks and windows produced are the same as the existing overloads.
