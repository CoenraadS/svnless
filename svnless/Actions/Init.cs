using LibGit2Sharp;
using SharpSvn;
using SvnLess.Extensions;

namespace SvnLess.Actions;

internal static class Init
{
    public static async Task ExecuteAsync(string gitPath, SvnRepo svn)
    {
        Repository.Init(gitPath);
        using var repo = new Repository(gitPath);

        var author = repo.BuildSignature();

        var exportInfo = svn.Export();
        var revision = exportInfo.Revision;
        Console.WriteLine($"SVN Revision: {revision}");

        var svnLogArgs = new SvnLogArgs
        {
            Range = new SvnRevisionRange(new SvnRevision(revision), new SvnRevision(revision)),
            Limit = 1,
        };

        var logResult = await svn.LogAsync(svnLogArgs);
        var signature = new Signature(logResult.Author ?? Constants.UNKNOWN, Constants.DEFAULT_EMAIL, logResult.Time);
        repo.StageAndCommit(logResult.LogMessage ?? "", signature);
        repo.Branches.Rename(repo.Branches.First(), Constants.SVN_BRANCH_NAME);
        repo.ApplyTag($"Revision/{revision}");
    }
}
