// See https://aka.ms/new-console-template for more information
using LibGit2Sharp;
using SharpSvn;
using SvnLess.Actions;
using SvnLess.Classes;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var gitPath = args[0];
        var svnUri = new SvnUriTarget(args[1]);
        var svnRepo = new SvnRepo(new SvnClient(), svnUri, Path.Combine(gitPath, "svn"));
        Lazy<Context> context = new(() => new Context(new Repository(gitPath), svnRepo));

        while (true)
        {
            Console.WriteLine("1: Init");
            Console.WriteLine("2: Jump to latest revision");
            Console.WriteLine("3: Iterate to latest revision");

            var option = Console.ReadKey();
            Console.WriteLine();

            if (option.KeyChar == '1')
            {
                await Init.ExecuteAsync(gitPath, svnRepo);
                Console.WriteLine("Repository Initialized");
                continue;
            }

            if (option.KeyChar == '2')
            {
                var revision = await context.Value.FastForwardToLatestRevision();
                Console.WriteLine($"Updated to Revision {revision}");
                continue;
            }

            if (option.KeyChar == '3')
            {
                var revision = await context.Value.IterateToLatestRevision();
                Console.WriteLine($"Updated to Revision {revision}");
                continue;
            }
            break;
        }

        if (context.IsValueCreated)
        {
            context.Value.Dispose();
        }
    }
}
