using System;
using System.Data.Common;

namespace GreyOTron.Library.RepositoryInterfaces
{
    public interface IDbConfiguration
    {
        string ConnectionString { get; }
        Action<DbConnection> AuthenticateDbConnection { get; }
    }
}
