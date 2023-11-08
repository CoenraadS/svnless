// See https://aka.ms/new-console-template for more information
using SharpSvn;

public record SvnRepo(SvnClient Client, SvnUriTarget Remote, string GitPath, string RealSvnPath)
{
    public static implicit operator SvnUriTarget(SvnRepo svnRepo) => svnRepo.Remote;
};
