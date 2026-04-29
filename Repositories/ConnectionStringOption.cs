using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public class ConnectionStringOption
    {
        public const string Key = "ConnectionStrings";
        public string SqlServer { get; set; } = default;
    }
}
