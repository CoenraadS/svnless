using LibGit2Sharp;
using SharpSvn;
using SvnLess.Extensions;

namespace SvnLess.Actions;

internal static class Init
{
    public static void Execute(string gitPath, SvnRepo svn)
    {    
        var exportInfo = svn.Export();
        var revision = exportInfo.Revision;
        Console.WriteLine($"SVN Revision: {revision}");

        var svnLogArgs = new SvnLogArgs
        {
            Range = new SvnRevisionRange(new SvnRevision(revision), new SvnRevision(revision)),
            Limit = 1,
        };

        var logResult = svn.GetLogs(svnLogArgs, svn.RealSvnPath).First();
        var signature = new Signature(logResult.Author ?? Constants.UNKNOWN, Constants.DEFAULT_EMAIL, logResult.Time);

        Repository.Init(gitPath);
        using var repo = new Repository(gitPath);

        var author = repo.BuildSignature();
        repo.StageAndCommit(logResult.LogMessage ?? "", signature);
        repo.Branches.Rename(repo.Branches.First(), Constants.SVN_BRANCH_NAME);
        repo.ApplyTag($"Revision/{revision}");
    }
}
