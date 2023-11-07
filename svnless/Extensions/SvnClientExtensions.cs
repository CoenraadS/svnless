using SharpSvn;
using SvnLess.Classes;
using System.Text;

namespace SvnLess.Extensions;

internal static class SvnClientExtensions
{
    /// <summary>
    /// Get Info from remote SVN HEAD
    /// </summary>
    public static async Task<SvnInfoEventArgs> InfoAsync(this SvnRepo svn)
    {
        var tcs = new TaskCompletionSource<SvnInfoEventArgs>();
        var eventHandler = new EventHandler<SvnInfoEventArgs>((sender, result) => tcs.SetResult(result));
        svn.Client.Info(svn.Remote, eventHandler);
        var result = await tcs.Task;
        return result;
    }

    public static SvnUpdateResult Export(this SvnRepo svn)
    {
        if (svn.Client.Export(svn.Remote.Uri, svn.GitPath, out var result))
        {
            return result;
        }

        throw new Exception($"Could not export svn from: {svn.Remote.Uri}");
    }

    public static async Task<SvnLogEventArgs> LogAsync(this SvnRepo svn, SvnLogArgs args)
    {
        var tcs = new TaskCompletionSource<SvnLogEventArgs>();
        var eventHandler = new EventHandler<SvnLogEventArgs>((sender, result) => tcs.SetResult(result));
        svn.Client.Log(svn, args, eventHandler);
        var result = await tcs.Task;
        return result;
    }

    public static SVNToGitDiff GetGitDiff(this SvnRepo svn, long from, long to)
    {
        using var ms = new MemoryStream();
        var fromTarget = new SvnUriTarget(svn, from);
        var toTarget = new SvnUriTarget(svn, to);

        svn.Client.Diff(fromTarget, toTarget, new SvnDiffArgs() { UseGitFormat = true }, ms);
        var diffString = Encoding.UTF8.GetString(ms.ToArray());

        return new SVNToGitDiff(diffString, from, to);
    }
}
