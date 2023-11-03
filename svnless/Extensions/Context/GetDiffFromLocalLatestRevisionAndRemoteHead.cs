using SharpSvn;
using SvnLess.Classes;
using SvnLess.Extensions;

namespace SvnLess.Actions;

internal static partial class ContextExtensions
{
    public static GitDiff GetGitDiff(this Context context, long from, long to)
    {
        var svn = context.Svn;

        var diff = svn.Diff(from, to, new SvnDiffArgs() { UseGitFormat = true });
        return new GitDiff(diff, from, to);
    }
}
