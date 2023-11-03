using SvnLess.Classes;
using SvnLess.Extensions;
using System.Diagnostics;

namespace SvnLess.Actions;

internal static partial class ContextExtensions
{
    public static async Task ApplyGitDiffAsync(this Context context, GitDiff gitDiff)
    {
        var latestLocalRevision = context.Git.GetLatestLocalRevision();
        if (latestLocalRevision != gitDiff.From)
        {
            throw new InvalidOperationException($"Git diff expected revision: {gitDiff.From}, but was {latestLocalRevision}");
        }

        context.Git.CheckoutSVNBranch();

        await Task.Run(async () =>
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.WorkingDirectory = new DirectoryInfo(context.Git.Info.Path).Parent!.FullName;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.FileName = "git";
            processStartInfo.Arguments = $"apply --directory=svn --ignore-space-change --ignore-whitespace --whitespace=nowarn";

            var process = new Process
            {
                StartInfo = processStartInfo
            };
            process.Start();

            var diff = gitDiff.Diff.ReplaceLineEndings("\n");

            var streamWriter = process.StandardInput;
            streamWriter.WriteLine(diff);
            streamWriter.Close();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                var tmp = Path.GetTempFileName();
                await File.WriteAllTextAsync(tmp, diff);
                throw new InvalidOperationException($"Git Apply Failed.{Environment.NewLine}" +
                    $"{error}{Environment.NewLine}" +
                    $"Run: git {processStartInfo.Arguments} --reject");
            }
        });
    }
}
