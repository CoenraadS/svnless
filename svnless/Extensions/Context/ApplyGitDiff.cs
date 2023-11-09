using SvnLess.Classes;
using SvnLess.Extensions;
using System.Diagnostics;

namespace SvnLess.Actions;

internal static partial class ContextExtensions
{
    public static async Task ApplyDiffToSvnRepositoryAsync(this Context context, GitToSVNDiff diff)
    {
        context.Svn.Client.Update(context.Svn.RealSvnPath, new SharpSvn.SvnUpdateArgs() { Revision = diff.Revision });

        await Task.Run(async () =>
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.WorkingDirectory = context.Svn.RealSvnPath;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.FileName = "git";
            processStartInfo.Arguments = $"apply --ignore-space-change --ignore-whitespace --whitespace=nowarn --unsafe-paths --directory=svn";

            var process = new Process
            {
                StartInfo = processStartInfo
            };
            process.Start();

            var diffSanitized = diff.Content.ReplaceLineEndings("\n");

            var streamWriter = process.StandardInput;
            streamWriter.WriteLine(diff);
            streamWriter.Close();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                var tmp = Path.GetTempFileName();
                await File.WriteAllTextAsync(tmp, diffSanitized);
                throw new InvalidOperationException($"Git Apply Failed.{Environment.NewLine}" +
                    $"{error}{Environment.NewLine}" +
                    $"Run: git {processStartInfo.Arguments} --reject");
            }
        });

        //context.Svn.Client.Commit(context.Svn.RealSvnPath, new SharpSvn.SvnCommitArgs() { LogMessage = diff.Message });
    }

    public static async Task ApplyDiffToGitRepository(this Context context, SVNToGitDiff gitDiff)
    {
        var latestLocalRevision = context.Git.GetLatestLocalRevision();
        if (latestLocalRevision != gitDiff.From)
        {
            throw new InvalidOperationException($"Git diff expected revision: {gitDiff.From}, but was {latestLocalRevision}");
        }

        context.Git.CheckoutRevisionBranch();

        await Task.Run(async () =>
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.WorkingDirectory = new DirectoryInfo(context.Git.Info.Path).Parent!.FullName;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.FileName = "git";
            processStartInfo.Arguments = $"apply --ignore-space-change --ignore-whitespace --whitespace=nowarn --directory=svn";

            var process = new Process
            {
                StartInfo = processStartInfo
            };
            process.Start();

            var diff = gitDiff.Diff.ReplaceLineEndings("\n").Replace("a/trunk", "a").Replace("b/trunk", "b");

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
