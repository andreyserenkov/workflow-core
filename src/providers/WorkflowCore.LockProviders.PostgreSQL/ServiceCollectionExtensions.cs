using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.LockProviders.PostgreSQL;
using WorkflowCore.Models;


namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UsePostgresDistributedLockManager(this WorkflowOptions options, string connectionString)
        {
            options.UseDistributedLockManager(sp => new PostgresLockProvider(connectionString, sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}
