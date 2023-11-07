using LibGit2Sharp;
using SvnLess.Actions;

namespace SvnLess.Extensions;

public static class RepositoryExtensions
{
    public static Branch CheckoutRevisionBranch(this Repository repository) => repository.CheckoutBranch(Constants.SVN_BRANCH_NAME);
    public static Branch GetSVNBranch(this Repository repository) => repository.Branches[Constants.SVN_BRANCH_NAME];

    public static long GetLatestLocalRevision(this Repository repository)
    {
        var latestLocalRevision = repository.Tags.Where(e => e.FriendlyName.StartsWith("Revision/"))
            .Select(e => e.FriendlyName.Split('/').Skip(1).First())
            .Select(long.Parse)
            .OrderByDescending(e => e).First();

        return latestLocalRevision;
    }

    public static Signature BuildSignature(this Repository repository, DateTimeOffset? now = null) => repository.Config.BuildSignature(now ?? DateTimeOffset.Now);

    public static Branch CheckoutBranch(this Repository repository, string branchName)
    {
        var existingBranch = repository.Branches.FirstOrDefault(e => e.FriendlyName == branchName);

        if (existingBranch != null)
        {
            return repository.CheckoutBranch(existingBranch);
        }

        var newBranch = repository.CreateBranch(branchName);
        Commands.Checkout(repository, newBranch);
        return newBranch;
    }

    public static Branch CheckoutBranch(this Repository repository, Branch branch)
    {
        Commands.Checkout(repository, branch);
        return branch;
    }

    public static Commit StageAndCommit(this Repository repository, string message, Signature? signature = null)
    {
        Commands.Stage(repository, "*");
        signature ??= repository.BuildSignature();
        return repository.Commit(message, signature, signature);
    }
}
