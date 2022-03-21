using System;
using System.Collections.Generic;
using NUnit.Framework.Api;
using NUnitActionsLogger;
using NUnitLite;

namespace Tests
{
    class Program
    {
        static int Main(string[] args)
        {
            var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
            runner.Load(typeof(Program).Assembly, new Dictionary<string, object>());
            var options = new NUnitLiteOptions(args);
            var filter = TextRunner.CreateTestFilter(options);
            runner.Run(new GithubActionsLogger(), filter);

            return 0;
        }
    }
}
