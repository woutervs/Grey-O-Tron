using System;
using System.Data.Common;

namespace GreyOTron.Library.Interfaces
{
    public interface IDbConfiguration
    {
        string ConnectionString { get; }
        Action<DbConnection> AuthenticateDbConnection { get; }
    }
}
