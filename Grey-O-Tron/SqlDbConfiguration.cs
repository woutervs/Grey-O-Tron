﻿using System;
using System.Data.Common;
using GreyOTron.Library.Interfaces;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GreyOTron
{
    public class SqlDbConfiguration : IDbConfiguration
    {
        private readonly IConfiguration configuration;

        public SqlDbConfiguration(IConfiguration configuration, AzureServiceTokenProvider tokenProvider)
        {
            this.configuration = configuration;
            AuthenticateDbConnection = connection =>
            {
                switch (connection)
                {
                    case SqlConnection sqlConnection:
                        sqlConnection.Credential = null;
                        sqlConnection.AccessToken = tokenProvider
                            .GetAccessTokenAsync("https://database.windows.net/", configuration?["tenantId"]).Result;
                        break;
                }
            };
        }

        public string ConnectionString => configuration.GetConnectionString("SqlConnection");
        public Action<DbConnection> AuthenticateDbConnection { get; }
    }
}
