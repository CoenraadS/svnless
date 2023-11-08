using SharpSvn;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1Async()
        {
            var client = new SvnClient();
            var svnLogArgs = new SvnLogArgs
            {
                Range = new SvnRevisionRange(new SvnRevision(6301), new SvnRevision(6301)),
                Limit = 1,
            };

            var remote = new Uri(@"https://svn/svn/webtegriti/trunk/src");
      
            if (client.GetLog(remote, svnLogArgs, out var logItems))
            {
                foreach (var logItem in logItems)
                {
                    Console.WriteLine(logItem.LogMessage);

                }
            }
        }
    }
}