using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

/// <summary>
/// One scripted sequence, readable as a synchronous <see cref="IEnumerable{T}"/> (for LINQ) or an
/// <see cref="IAsyncEnumerable{T}"/> (for the operator under test), so both see the same items,
/// the same failures and the same disposal behaviour. Records how many items were pulled and
/// whether the enumerator was disposed.
/// </summary>
/// <remarks>
/// Both views are iterator methods, so the compiler supplies every enumerator member and the
/// helper carries no hand-written member that a passing test would never execute.
/// </remarks>
internal sealed class ScriptedSource(int[] items, int throwAt, bool disposeThrows)
{
    public static readonly string SourceFailure = "source failed";

    public static readonly string DisposeFailure = "dispose failed";

    public int Pulled { get; private set; }

    public bool Disposed { get; private set; }



    public IEnumerable<int> Sync()
    {
        try
        {
            for (var i = 0; i < items.Length; i++)
            {
                ThrowIfScripted(i);
                Pulled++;
                yield return items[i];
            }
        }
        finally
        {
            Dispose();
        }
    }



    public async IAsyncEnumerable<int> Async()
    {
        try
        {
            for (var i = 0; i < items.Length; i++)
            {
                await Task.Yield();
                ThrowIfScripted(i);
                Pulled++;
                yield return items[i];
            }
        }
        finally
        {
            Dispose();
        }
    }



    private void ThrowIfScripted(int index)
    {
        if (index == throwAt)
        {
            throw new InvalidOperationException(SourceFailure);
        }
    }



    private void Dispose()
    {
        Disposed = true;
        if (disposeThrows)
        {
            throw new InvalidOperationException(DisposeFailure);
        }
    }
}
