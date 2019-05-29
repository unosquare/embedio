using System.Threading;

namespace EmbedIO.Tests.TestObjects
{
    public static class Resources
    {
        public static int Counter = 9699;

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
        
        public static string GetServerAddress()
        {
            const string serverAddress = "http://localhost:{0}/";

            Interlocked.Increment(ref Counter);
            return string.Format(serverAddress, Counter);
        }
    }
}
