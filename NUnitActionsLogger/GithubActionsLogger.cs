using System;
using System.IO;
using System.Linq;
using NUnit.Framework.Interfaces;

namespace NUnitActionsLogger
{
    public class GithubActionsLogger : ITestListener
    {
        private static readonly TextWriter _consoleOut = Console.Out;
        private readonly Action<string> _logMessage;

        public GithubActionsLogger(Action<string>? logMessage = null)
        {
            _logMessage = logMessage ?? _consoleOut.WriteLine;
        }

        public void SendMessage(TestMessage message)
        {
        }

        public void TestFinished(ITestResult result)
        {
            if (!result.Test.IsSuite)
            {
                var className = result.Test.ClassName?.SubstringAfterLast(".");

                var status = result.ResultState.Status switch
                {
                    TestStatus.Passed => "✅",
                    TestStatus.Failed => "❌",
                    _ => "⚠️"
                };

                var suffix = result.ResultState.Status == TestStatus.Skipped
                    ? $"- {result.Message ?? result.ResultState.Label}) "
                    : string.Empty;

                var message = $"{status} {className}.{result.Test.Name} {suffix}({(result.EndTime - result.StartTime).TotalMilliseconds} ms)";

                if (result.ResultState.Status == TestStatus.Failed)
                {
                    message += $"{Environment.NewLine}{Tabify(result.Message, 2)}{Environment.NewLine}{Tabify(result.StackTrace, 2)}";
                }

                _logMessage(message);

                if (GithubHelper.IsRunningOnAgent && result.ResultState.Status == TestStatus.Failed)
                {
                    var frame = StackFrame.TryGetTestStackFrame(result);
                    var log = GithubHelper.FormatError($"❌ {className}.{result.Test.Name}", result.Message ?? "Test failed", frame);
                    _logMessage(log);
                }
            }

            if (result.Test.IsSuite && result.Test.TestType == "Assembly")
            {
                _logMessage(Environment.NewLine);
                _logMessage($"Test Run finished ({result.EndTime - result.StartTime:c}):");
                _logMessage($"  ✅ Passed: {result.PassCount}");
                _logMessage($"  ❌ Failed: {result.FailCount}");
                _logMessage($"  ⚠️ Skipped: {result.SkipCount}");

                _logMessage(Environment.NewLine);
                _logMessage($"Failed tests:");
                LogFailedTests(result);
            }
        }

        public void TestOutput(TestOutput output)
        {
        }

        public void TestStarted(ITest test)
        {
        }

        private void LogFailedTests(ITestResult test)
        {
            if (test.ResultState.Status != TestStatus.Failed)
            {
                return;
            }

            if (test.Test.IsSuite)
            {
                foreach (var child in test.Children)
                {
                    LogFailedTests(child);
                }
            }
            else
            {
                _logMessage($"  ❌ {test.Test.FullName}");
                _logMessage(Tabify(test.Message, 2));
                _logMessage(Tabify(test.StackTrace, 2));
            }
        }

        private static string Tabify(string? message, int tabs)
        {
            if (string.IsNullOrEmpty(message))
            {
                return "";
            }

            var tabString = string.Join("", Enumerable.Repeat("  ", tabs));
            return tabString + message.Replace(Environment.NewLine, $"{Environment.NewLine}{tabString}");
        }
    }
}
