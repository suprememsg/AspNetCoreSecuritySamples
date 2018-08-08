// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Serilog.Settings.Configuration;
namespace AspNetCoreSecurity
{
    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
            .Build();
        public static void Main(string[] args)
        {
       
        Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
                .WriteTo.MSSqlServer(Configuration["Serilog:ConnectionString"], Configuration["Serilog:TableName"],
                    autoCreateSqlTable: Convert.ToBoolean(Configuration["Serilog:autoCreateSqlTable"] ?? "false"),
                    schemaName: Configuration["Serilog:SchemaName"],
                    columnOptions: buildLoggerColumnOptions())
                .CreateLogger();
            Serilog.Debugging.SelfLog.Enable(msg =>
            {
                Debug.Print(msg);
                Debugger.Break();
            });
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseIISIntegration().UseKestrel().UseSerilog();
        private static ColumnOptions buildLoggerColumnOptions()
        {
            var columnOptions = new ColumnOptions
            {
                AdditionalDataColumns = new Collection<DataColumn>
                {
                    new DataColumn {DataType = typeof (string), ColumnName = "Application"},
                    new DataColumn {DataType = typeof (string), ColumnName = "UserName"},
                }
            };

            //Don't include the Properties XML column.
            //columnOptions.Store.Remove(StandardColumn.Properties);
            //Do include the log event data as JSON.
            columnOptions.Store.Add(StandardColumn.LogEvent);

            columnOptions.Properties.UsePropertyKeyAsElementName = true;
            return columnOptions;
        }
    }
}
