// See https://aka.ms/new-console-template for more information

internal partial class Program
{
    public enum Action
    {
        Unknown,
        GitInit,
        GitToSVNSync,
        SVNToGitDiffSync, // Sync by getting the diff between each revision until the latest, and applying each diff as a commit
        SVNToGitRemakeSync // Sync by deleting the contents of the repo, and re-checkout the latest revision (Use this if something goes wrong with the diffing strategy)
    }
}
