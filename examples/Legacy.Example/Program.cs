// This example deliberately targets net462: Wolfgang.Extensions.IAsyncEnumerable.Legacy
// exists precisely for TFMs (net462 / netstandard2.0) where the BCL's
// System.Linq.AsyncEnumerable is not available. On net8.0+ you would use the
// BCL's built-in operators instead — the Legacy package doesn't apply there.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolfgang.Extensions.IAsyncEnumerable;

namespace LegacyExample
{
    internal static class Program
    {
        private static async Task Main()
        {
            Console.WriteLine("=== Wolfgang.Extensions.IAsyncEnumerable.Legacy Example ===");
            Console.WriteLine();

            // CountAsync — count the elements in an async sequence
            var count = await GenerateNumbers(5).CountAsync();
            Console.WriteLine($"CountAsync over 5 numbers: {count}");

            // AnyAsync() — does the sequence contain any elements?
            Console.WriteLine($"AnyAsync on populated stream: {await GenerateNumbers(5).AnyAsync()}");
            Console.WriteLine($"AnyAsync on empty stream: {await GenerateNumbers(0).AnyAsync()}");

            // AnyAsync(predicate) — does any element match? Short-circuits on first match.
            Console.WriteLine($"Any divisible by 3: {await GenerateNumbers(10).AnyAsync(n => n % 3 == 0)}");
            Console.WriteLine($"Any greater than 100: {await GenerateNumbers(10).AnyAsync(n => n > 100)}");

            // FirstAsync — first element (throws InvalidOperationException if empty)
            var first = await GenerateNumbers(5).FirstAsync();
            Console.WriteLine($"FirstAsync: {first}");

            // FirstOrDefaultAsync — first element, or default(T) when the sequence is empty
            var firstOrDefault = await GenerateNumbers(0).FirstOrDefaultAsync();
            Console.WriteLine($"FirstOrDefaultAsync on empty stream: {firstOrDefault}");

            // ToListAsync — materialize the whole sequence into a List<T>
            var list = await GenerateNumbers(5).ToListAsync();
            Console.WriteLine($"ToListAsync: [{string.Join(", ", list)}]");
        }



        private static async IAsyncEnumerable<int> GenerateNumbers(int count)
        {
            for (var i = 1; i <= count; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }
    }
}
