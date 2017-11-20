namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System.Threading;

    public static class Resources
    {
        private const string ServerAddress = "http://localhost:{0}/";
        public static int Counter = 9699;

        public static string GetServerAddress()
        {
            Interlocked.Increment(ref Counter);
            return string.Format(ServerAddress, Counter);
        }
        
        public static readonly string SubIndex = @"<!DOCTYPE html>

<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""utf-8"" />
    <title></title>
</head>
<body>
    <h1>Sub</h1>
</body>
</html>";

        public static readonly string Index = @"<!DOCTYPE html>

<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""utf-8"" />
    <title></title>
</head>
<body>
    This is a placeholder
</body>
</html>";
    }
}
