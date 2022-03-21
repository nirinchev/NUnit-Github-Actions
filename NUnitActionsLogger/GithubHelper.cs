﻿using System;
using System.Collections.Generic;

namespace NUnitActionsLogger
{
    internal class GithubHelper
    {
        public static bool IsRunningOnAgent => string.Equals(
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        private static string Escape(string value) => value
            // URL-encode certain characters to escape them from being processed as command tokens
            // https://pakstech.com/blog/github-actions-workflow-commands
            .Replace("%", "%25")
            .Replace("\n", "%0A")
            .Replace("\r", "%0D");

        private static string FormatWorkflowCommand(
            string label,
            string message,
            string options) =>
            $"::{label} {options}::{Escape(message)}";

        private static string FormatOptions(
            string? filePath = null,
            int? line = null,
            string? title = null)
        {
            var options = new List<string>(3);

            if (!string.IsNullOrWhiteSpace(filePath))
                options.Add($"file={Escape(filePath)}");

            if (line is not null)
            {
                options.Add($"line={Escape(line.Value.ToString())}");
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                options.Add($"title={Escape(title)}");
            }

            return string.Join(",", options);
        }

        public static string FormatError(
            string title,
            string message,
            StackFrame? frame) =>
            FormatWorkflowCommand("error", message, FormatOptions(frame?.FilePath, frame?.Line, title));

        public static string FormatWarning(
            string title,
            string message,
            string? filePath = null,
            int? line = null) =>
            FormatWorkflowCommand("warning", message, FormatOptions(filePath, line, title));
    }
}
