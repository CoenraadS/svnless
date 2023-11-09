// See https://aka.ms/new-console-template for more information
using CommandLine;
using LibGit2Sharp;
using SharpSvn;
using SvnLess.Actions;
using SvnLess.Classes;

internal partial class Program
{
    public class Options
    {
        [Option(nameof(GitLocal), Required = true, HelpText = "Path to local git repository")]
        public required string GitLocal { get; set; }

        [Option(nameof(SVNRemote), Required = true, HelpText = "Path to remote SVN repository")]
        public required string SVNRemote { get; set; }

        [Option(nameof(SVNLocal), Required = true, HelpText = "Path to local SVN repository")]
        public required string SVNLocal { get; set; }

        [Option(nameof(Action), Required = true, HelpText = "Sync Git to SVN, or SVN to Git")]
        public required Action Action { get; set; }
    }

    private static void Main(string[] args)
    {
        try
        {
            HandleArgs(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            throw;
        }
    }

    private static void HandleArgs(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
    {
        if (o.Action == Action.Unknown)
        {
            throw new InvalidOperationException("Failed to parse action");
        }

        var svnRepo = new SvnRepo(new SvnClient(), new SvnUriTarget(o.SVNRemote), Path.Combine(o.GitLocal, "svn"), o.SVNLocal);

        if (o.Action == Action.GitInit)
        {
            Init.Execute(o.GitLocal, svnRepo);
            return;
        }

        if (!Repository.IsValid(o.GitLocal))
        {
            throw new InvalidOperationException($"Not a valid git path: {o.GitLocal}");
        }

        using var repo = new Repository(o.GitLocal);
        var context = new Context(repo, svnRepo);

        if (o.Action == Action.GitToSVNSync)
        {
            // If there is exactly one commit difference until a Revision Tag, apply that commit from Git -> SVN
            var revision = GitToSVNSyncAsync(context).GetAwaiter().GetResult();
            Console.WriteLine($"Updated SVN to Revision {revision}");
            return;
        }

        if (o.Action == Action.SVNToGitDiffSync)
        {
            var revision = context.IterateToLocalSVNRevision().GetAwaiter().GetResult();
            Console.WriteLine($"Updated Git to Revision {revision}");
            return;
        }

        if (o.Action == Action.SVNToGitRemakeSync)
        {
            var revision = context.FastForwardToLatestRevision().GetAwaiter().GetResult();
            Console.WriteLine($"Updated Git to Revision {revision}");
            return;
        }
    });

    private static async Task<long> GitToSVNSyncAsync(Context context)
    {
        var currentBranch = context.Git.Head;

        if (currentBranch.FriendlyName.StartsWith(Constants.REVISION))
        {
            throw new InvalidOperationException($"Switch to a non revision branch. Current branch: {currentBranch.FriendlyName}");
        }

        var commit = currentBranch.Commits.First();
        var description = context.Git.Describe(commit, new DescribeOptions() { Strategy = DescribeStrategy.Tags });

        var split = description.Split('-');

        if (split.Length == 1)
        {
            throw new InvalidOperationException($"There are no changes to sync");
        }

        if (!description.StartsWith(Constants.REVISION))
        {
            throw new InvalidOperationException($"Non revision tag encountered: {description}");
        }

        var commitCountSinceLastRevisionTag = int.Parse(split[1]);

        if (commitCountSinceLastRevisionTag > 1)
        {
            throw new InvalidOperationException($"Squash your changes, so only 1 commit will be synced. Currently {commitCountSinceLastRevisionTag} commits present");
        }

        var revision = long.Parse(split[0][$"{Constants.REVISION}/".Length..]);

        var previousCommit = currentBranch.Commits.Skip(1).First();

        var diff = context.Git.Diff.Compare<Patch>(previousCommit.Tree, commit.Tree, new CompareOptions());

        var gitToSVNDiff = new GitToSVNDiff(diff, revision, commit.Message);

        await context.ApplyDiffToSvnRepositoryAsync(gitToSVNDiff);

        return 0;

        //if (context.Svn.Client.Update(context.Svn.RealSvnPath, out SvnUpdateResult result))
        //{
        //    return result.Revision;
        //}
        //else
        //{
        //    throw new InvalidOperationException("Could not update SVN");
        //}
    }
}
