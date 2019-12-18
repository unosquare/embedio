using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace EmbedIO.Security
{
    public class BannedInfo
    {
        public IPAddress IPAddress { get; set; }

        public long BanUntil { get; set; }

        public bool IsExplicit { get; set; }
    }
}
