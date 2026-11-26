type: feature

`ChunkAsync` and `SlidingWindowAsync` gain a `(source, size)` overload, and their `(source, size, token)` overload no longer defaults the token, so the `initialCapacity` overload is the only one with an optional parameter; every existing call still compiles and behaves the same.
