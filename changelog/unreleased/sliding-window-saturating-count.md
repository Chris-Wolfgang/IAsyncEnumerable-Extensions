type: internal

`SlidingWindowAsync` keeps its fill counter saturated at the window size, so it cannot overflow on very long sources. No behaviour change.
