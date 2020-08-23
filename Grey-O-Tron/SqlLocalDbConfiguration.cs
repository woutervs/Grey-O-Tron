using System;
using System.Data.Common;
using GreyOTron.Library.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GreyOTron
{
    public class SqlLocalDbConfiguration : IDbConfiguration
    {
        private readonly IConfiguration configuration;

        public SqlLocalDbConfiguration(IConfiguration configuration)
        {
            this.configuration = configuration;
            AuthenticateDbConnection = connection =>
            {
                
            };
        }

        public string ConnectionString => configuration.GetConnectionString("SqlConnection");
        public Action<DbConnection> AuthenticateDbConnection { get; }
    }
}
