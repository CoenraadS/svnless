using LibGit2Sharp;

namespace SvnLess.Classes;

public sealed record Context(Repository Git, SvnRepo Svn) : IDisposable
{
    public void Dispose() => Git.Dispose();
}
