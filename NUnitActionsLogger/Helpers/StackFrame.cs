using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;

namespace NUnitActionsLogger
{
    internal partial class StackFrame
    {
        public string MethodCall { get; }

        public string? FilePath { get; }

        public int? Line { get; }

        public StackFrame(string methodCall, string? filePath, int? line)
        {
            MethodCall = methodCall;
            FilePath = filePath;
            Line = line;
        }
    }

    internal partial class StackFrame
    {
        private const string Space = @"[\x20\t]";
        private const string NotSpace = @"[^\x20\t]";

        private static readonly Regex Pattern = new(@"
            ^
            " + Space + @"*
            \w+ " + Space + @"+
            (?<frame>
                (?<type> " + NotSpace + @"+ ) \.
                (?<method> " + NotSpace + @"+? ) " + Space + @"*
                (?<params>  \( ( " + Space + @"* \)
                               |                    (?<pt> .+?) " + Space + @"+ (?<pn> .+?)
                                 (, " + Space + @"* (?<pt> .+?) " + Space + @"+ (?<pn> .+?) )* \) ) )
                ( " + Space + @"+
                    ( # Microsoft .NET stack traces
                    \w+ " + Space + @"+
                    (?<file> ( [a-z] \: # Windows rooted path starting with a drive letter
                             | / )      # *nix rooted path starting with a forward-slash
                             .+? )
                    \: \w+ " + Space + @"+
                    (?<line> [0-9]+ ) \p{P}?
                    | # Mono stack traces
                    \[0x[0-9a-f]+\] " + Space + @"+ \w+ " + Space + @"+
                    <(?<file> [^>]+ )>
                    :(?<line> [0-9]+ )
                    )
                )?
            )
            \s*
            $",
            RegexOptions.IgnoreCase |
            RegexOptions.Multiline |
            RegexOptions.ExplicitCapture |
            RegexOptions.CultureInvariant |
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Compiled,
            // Cap the evaluation time to make it obvious should the expression
            // fall into the "catastrophic backtracking" trap due to over
            // generalization.
            // https://github.com/atifaziz/StackTraceParser/issues/4
            TimeSpan.FromSeconds(5));

        private static IEnumerable<StackFrame> ParseMany(string text) =>
            Pattern.Matches(text).Select(m =>
            {
                var groups = m.Groups;
                return new StackFrame(
                    groups["type"].Value + '.' + groups["method"].Value,
                    groups["file"].Value,
                    groups["line"].Value.TryParseInt()
                );
            });

        public static StackFrame? TryGetTestStackFrame(ITestResult testResult)
        {
            if (string.IsNullOrWhiteSpace(testResult.StackTrace))
            {
                return null;
            }

            var testMethodFullyQualifiedName = testResult.Test.FullName.SubstringUntil("(");

            var testMethodName = testMethodFullyQualifiedName.SubstringAfterLast(".");

            var matchingStackFrames = ParseMany(testResult.StackTrace)
                .Where(f =>
                    // Sync method call
                    // e.g. MyTests.EnsureOnePlusOneEqualsTwo()
                    f.MethodCall.StartsWith(testMethodFullyQualifiedName, StringComparison.OrdinalIgnoreCase) ||
                    // Async method call
                    // e.g. MyTests.<EnsureOnePlusOneEqualsTwo>d__3.MoveNext()
                    f.MethodCall.Contains('<' + testMethodName + '>', StringComparison.OrdinalIgnoreCase)
                );

            return matchingStackFrames.LastOrDefault();
        }
    }
}
