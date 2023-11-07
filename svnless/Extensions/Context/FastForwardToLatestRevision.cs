using LibGit2Sharp;
using SharpSvn;
using SvnLess.Classes;
using SvnLess.Extensions;

namespace SvnLess.Actions;

internal static partial class ContextExtensions
{
    public static async Task<long> IterateToLatestRevision(this Context context)
    {
        var git = context.Git;
        var localRevision = git.GetLatestLocalRevision();
        var latestRevision = (await context.Svn.InfoAsync()).Revision;

        for (long i = localRevision; i < latestRevision; i++)
        {
            var diff = context.Svn.GetGitDiff(i, i + 1);

            await context.ApplyDiffToGitRepository(diff);

            var svnLogArgs = new SvnLogArgs
            {
                Range = new SvnRevisionRange(new SvnRevision(localRevision), new SvnRevision(localRevision)),
                Limit = 1,
            };

            var logResult = await context.Svn.LogAsync(svnLogArgs);
            var signature = new Signature(logResult.Author ?? Constants.UNKNOWN, Constants.DEFAULT_EMAIL, logResult.Time);
            git.StageAndCommit(logResult.LogMessage ?? "", signature);

            var tag = $"Revision/{i + 1}";
            git.ApplyTag(tag);
        }

        if (git.GetLatestLocalRevision() != (await context.Svn.InfoAsync()).Revision)
        {
            throw new InvalidOperationException();
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

        var latestRevision = requestedRevision ?? (await context.Svn.InfoAsync()).Revision;

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

            var logResult = await context.Svn.LogAsync(svnLogArgs);
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
