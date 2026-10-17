type: fix

`IsEmptyAsync`, `NoneAsync()` and `NoneAsync(predicate)` now throw `ArgumentNullException` at the call, before returning a task, as `Enumerable.Any` does. Previously the exception surfaced only when the returned task was awaited. Cancellation is still reported through the task.
