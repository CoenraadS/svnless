using LibGit2Sharp;
using SharpSvn;
using SvnLess.Classes;
using SvnLess.Extensions;

namespace SvnLess.Actions;

internal static partial class ContextExtensions
{
    public static async Task<long> IterateToLocalSVNRevision(this Context context)
    {
        var git = context.Git;
        context.Svn.Client.Update(context.Svn.RealSvnPath);
        var localRevision = git.GetLatestLocalRevision();
        var latestRevision = (await context.Svn.InfoAsync(context.Svn.RealSvnPath)).Revision;

        var svnLogArgs = new SvnLogArgs
        {
            Range = new SvnRevisionRange(new SvnRevision(localRevision), SvnRevision.Head)
        };

        var logResults = context.Svn.GetLogs(svnLogArgs, context.Svn.RealSvnPath);

        var previous = logResults.First();
        foreach (var current in logResults.Skip(1))
        {
            var diff = context.Svn.GetGitDiff(previous.Revision, current.Revision);

            await context.ApplyDiffToGitRepository(diff);

            var signature = new Signature(current.Author ?? Constants.UNKNOWN, Constants.DEFAULT_EMAIL, current.Time);
            git.StageAndCommit(current.LogMessage ?? "", signature);

            var tag = $"Revision/{current.Revision}";
            git.ApplyTag(tag);
            previous = current;
        }

        return latestRevision;
    }


    /// <summary>
    /// This will squash together any commits between local head and remote head
    /// It will do a delete + full checkout, so performance may be slow
    /// </summary>
    public static async Task<long> FastForwardToLatestRevision(this Context context, long? requestedRevision = null)
    {
        var git = context.Git;
        var localRevision = git.GetLatestLocalRevision();
        git.CheckoutRevisionBranch();

        var latestRevision = requestedRevision ?? (await context.Svn.InfoAsync(context.Svn.Remote.Uri.ToString())).Revision;

        if (latestRevision == localRevision)
        {
            return localRevision;
        }

        if (latestRevision < localRevision)
        {
            throw new InvalidOperationException("It should not be possible remote revision is less than local revision");
        }

        git.CheckoutBranch(Constants.SVN_BRANCH_NAME);

        Directory.Delete(context.Svn.GitPath, true);

        context.Svn.Export();

        if (latestRevision - localRevision == 1)
        {
            var svnLogArgs = new SvnLogArgs
            {
                Range = new SvnRevisionRange(new SvnRevision(localRevision), new SvnRevision(localRevision)),
                Limit = 1,
            };

            var logResult = context.Svn.GetLogs(svnLogArgs, context.Svn.RealSvnPath).First();
            var signature = new Signature(logResult.Author ?? Constants.UNKNOWN, Constants.DEFAULT_EMAIL, logResult.Time);
            git.StageAndCommit(logResult.LogMessage ?? "", signature);
        }
        else
        {
            git.StageAndCommit($"[{localRevision}..{latestRevision}]");
        }

        var tag = $"Revision/{latestRevision}";
        git.ApplyTag(tag);

        return latestRevision;
    }
}
