using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.LockProviders.PostgreSQL
{

    public class PostgresLockProvider : IDistributedLockProvider
    {
        private const string Prefix = "wfc";

        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly Dictionary<string, NpgsqlConnection> _locks = new Dictionary<string, NpgsqlConnection>();
        private readonly AutoResetEvent _mutex = new AutoResetEvent(true);

        public PostgresLockProvider(string connectionString, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<PostgresLockProvider>();
            var csb = new NpgsqlConnectionStringBuilder(connectionString);
            csb.Pooling = true;
            csb.ApplicationName = "Workflow Core Lock Manager";

            _connectionString = csb.ToString();
        }

        const string _pollRunables = "poll runnables";
        const string _unprocessedEvents = "unprocessed events";

        private int convertToInt(string Id)
        {
            int _id = 0;
            switch (Id)
            {
                case _pollRunables:
                    _id = -1;
                    break;
                case _unprocessedEvents:
                    _id = -2;
                    break;
                default:
                    if (!int.TryParse(Id, out _id))
                        throw new Exception($"Could not parse {Id} to int");
                    break;
            }
            return _id;
        }

        public async Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
        {

            int _id = convertToInt(Id);

            if (_mutex.WaitOne())
            {
                try
                {
                    var connection = new NpgsqlConnection(_connectionString);
                    connection.Open();
                    var cmd = new NpgsqlCommand("SELECT pg_try_advisory_lock(@id)", connection);
                    cmd.Parameters.AddWithValue("id", _id);
                    try
                    {
                        
                        bool ret = (bool) await cmd.ExecuteScalarAsync();

                        if (ret)
                        {
                            _logger.LogDebug($"The lock acquired {Id}");
                            _locks[Id] = connection;
                            return true;
                        }
                        else
                        {
                            _logger.LogError($"Lock acuiring failed for {Id}");
                            connection.Close();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                        throw ex;
                    }
                }
                finally
                {
                    _mutex.Set();
                }
            }
            return false;
        }

        public async Task ReleaseLock(string Id)
        {
            int _id = convertToInt(Id);

            if (_mutex.WaitOne())
            {
                try
                {
                    NpgsqlConnection connection = null;
                    connection = _locks[Id];

                    if (connection == null)
                        return;

                    var cmd = new NpgsqlCommand("SELECT pg_advisory_unlock(@id)", connection);
                    cmd.Parameters.AddWithValue("id", _id);
                    try
                    {
                        bool ret = (bool)await cmd.ExecuteScalarAsync();

                        if (!ret)
                            _logger.LogError($"Unable to release lock for {_id}");
                    }
                    finally
                    {
                        connection.Close();
                        _locks.Remove(Id);
                    }
                }
                finally
                {
                    _mutex.Set();
                }
            }
        }

        public async Task Start()
        {
        }

        public async Task Stop()
        {
        }
    }
}
