namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public static class Resources
    {
        public const string ServerAddress = "http://localhost:7777/";
        public const string WsServerAddress = "ws://localhost:7777/";
        
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
