namespace OppStreamer.Core.Tests;

/// <summary>
/// A ~30-line stand-in for a real test framework — see the note in the .csproj for why. Not
/// meant to be clever, just enough to get genuine pass/fail signal without any NuGet package.
/// </summary>
public sealed class TestRunner
{
    private readonly List<(string Name, bool Passed, string? Error)> _results = new();

    public void Test(string name, Action body)
    {
        try
        {
            body();
            _results.Add((name, true, null));
        }
        catch (Exception ex)
        {
            _results.Add((name, false, ex.Message));
        }
    }

    public int ReportAndGetExitCode()
    {
        foreach (var (name, passed, error) in _results)
        {
            Console.WriteLine($"[{(passed ? "PASS" : "FAIL")}] {name}");
            if (error is not null)
                Console.WriteLine($"       {error}");
        }

        int failed = _results.Count(r => !r.Passed);
        Console.WriteLine();
        Console.WriteLine($"{_results.Count - failed}/{_results.Count} passed.");
        return failed == 0 ? 0 : 1;
    }
}

public static class Check
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new Exception("Expected true: " + message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{message}\n       expected: {expected}\n       actual:   {actual}");
    }

    public static void Equal(ReadOnlySpan<float> expected, ReadOnlySpan<float> actual, string message)
    {
        if (!expected.SequenceEqual(actual))
            throw new Exception($"{message}\n       expected: [{string.Join(", ", expected.ToArray())}]\n       actual:   [{string.Join(", ", actual.ToArray())}]");
    }

    public static void Approximately(double expected, double actual, double tolerance, string message)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new Exception($"{message}\n       expected: {expected} (+/- {tolerance})\n       actual:   {actual}");
    }

    public static void Throws<TException>(Action body, string message) where TException : Exception
    {
        try
        {
            body();
        }
        catch (TException)
        {
            return;
        }
        throw new Exception($"Expected {typeof(TException).Name}: {message}");
    }
}
