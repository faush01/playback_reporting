/*
Copyright(C) 2018

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see<http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using SQLitePCL.pretty;
using System.Linq;
using SQLitePCL;
using System.Text;

namespace playback_reporting.Data
{
    public sealed class BaseSqliteLock
    {
        private static readonly BaseSqliteLock instance = new BaseSqliteLock();
        private readonly ReaderWriterLockSlim lock_item = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        static BaseSqliteLock()
        {
        }

        private BaseSqliteLock()
        {
        }

        public static BaseSqliteLock Instance
        {
            get
            {
                return instance;
            }
        }

        public ReaderWriterLockSlim getLockItem()
        {
            return lock_item;
        }
    }

    public abstract class BaseSqliteRepository : IDisposable
    {
        protected string DbFilePath { get; set; }
        protected BaseSqliteLock lock_manager;

        protected ILogger Logger { get; private set; }

        protected BaseSqliteRepository(ILogger logger)
        {
            Logger = logger;
            lock_manager = BaseSqliteLock.Instance;

            Logger.Debug("BaseSqliteRepository Instance : " + this.GetHashCode());
            Logger.Debug("BaseSqliteLock Instance : " + lock_manager.GetHashCode());
        }

        protected TransactionMode TransactionMode
        {
            get { return TransactionMode.Deferred; }
        }

        protected TransactionMode ReadTransactionMode
        {
            get { return TransactionMode.Deferred; }
        }

        internal static int ThreadSafeMode { get; set; }

        private static bool _versionLogged;

        private string _defaultWal;
        protected IDatabaseConnection _connection;

        protected virtual bool EnableSingleConnection
        {
            get { return true; }
        }

        protected IDatabaseConnection CreateConnection(bool isReadOnly = false)
        {
            if (_connection != null)
            {
                return _connection.Clone(false);
            }

            lock (lock_manager.getLockItem())
            {
                if (!_versionLogged)
                {
                    _versionLogged = true;
                    Logger.Info("Sqlite version: " + SQLite3.Version);
                    Logger.Info("Sqlite compiler options: " + string.Join(",", SQLite3.CompilerOptions.ToArray()));
                }

                ConnectionFlags connectionFlags;

                if (isReadOnly)
                {
                    //Logger.Info("Opening read connection");
                    //connectionFlags = ConnectionFlags.ReadOnly;
                    connectionFlags = ConnectionFlags.Create;
                    connectionFlags |= ConnectionFlags.ReadWrite;
                }
                else
                {
                    //Logger.Info("Opening write connection");
                    connectionFlags = ConnectionFlags.Create;
                    connectionFlags |= ConnectionFlags.ReadWrite;
                }

                if (EnableSingleConnection)
                {
                    connectionFlags |= ConnectionFlags.PrivateCache;
                }
                else
                {
                    connectionFlags |= ConnectionFlags.SharedCached;
                }

                connectionFlags |= ConnectionFlags.NoMutex;

                var db = SQLite3.Open(DbFilePath, connectionFlags, null, false);

                try
                {
                    if (string.IsNullOrWhiteSpace(_defaultWal))
                    {
                        using (var statement = PrepareStatement(db, "PRAGMA journal_mode".AsSpan()))
                        {
                            foreach (var row in statement.ExecuteQuery())
                            {
                                _defaultWal = row.GetString(0);
                                break;
                            }
                        }

                        Logger.Info("Default journal_mode for {0} is {1}", DbFilePath, _defaultWal);
                    }

                    var queries = new List<string>
                    {
                        //"PRAGMA cache size=-10000"
                        //"PRAGMA read_uncommitted = true",
                        "PRAGMA synchronous=Normal"
                    };

                    if (CacheSize.HasValue)
                    {
                        queries.Add("PRAGMA cache_size=" + CacheSize.Value.ToString(CultureInfo.InvariantCulture));
                    }

                    if (EnableTempStoreMemory)
                    {
                        queries.Add("PRAGMA temp_store = memory");
                    }
                    else
                    {
                        queries.Add("PRAGMA temp_store = file");
                    }

                    //foreach (var query in queries)
                    //{
                    //    db.Execute(query);
                    //}
                    db.ExecuteAll(string.Join(";", queries.ToArray()));
                }
                catch
                {
                    using (db)
                    {

                    }

                    throw;
                }

                _connection = db;

                return db;
            }
        }

        protected static ReadOnlyMemory<byte> GetBytes(string sql)
        {
            return System.Text.Encoding.UTF8.GetBytes(sql).AsMemory();
        }

        public IStatement PrepareStatement(IDatabaseConnection connection, string sql)
        {
            return PrepareStatement(connection, sql.AsSpan());
        }

        public IStatement PrepareStatement(IDatabaseConnection connection, ReadOnlySpan<char> sql)
        {
            var encoding = System.Text.Encoding.UTF8;

#if NETCOREAPP
            var byteSpan = new Span<byte>(new byte[encoding.GetMaxByteCount(sql.Length)]);
            var length = encoding.GetBytes(sql, byteSpan);
            byteSpan = byteSpan.Slice(0, length);
            return connection.PrepareStatement(byteSpan);
#else
            return connection.PrepareStatement(encoding.GetBytes(sql.ToString()).AsSpan());
#endif
        }

        public IStatement PrepareStatement(IDatabaseConnection connection, ReadOnlySpan<byte> sqlUtf8)
        {
            return connection.PrepareStatement(sqlUtf8);
        }

        public IStatement[] PrepareAll(IDatabaseConnection connection, List<string> sql)
        {
            var list = new List<ReadOnlyMemory<byte>>();

            var length = sql.Count;
            for (var i = 0; i < length; i++)
            {
                list.Add(System.Text.Encoding.UTF8.GetBytes(sql[i]).AsMemory());
            }

            return PrepareAll(connection, list);
        }

        public IStatement[] PrepareAll(IDatabaseConnection connection, List<ReadOnlyMemory<byte>> sqlUtf8)
        {
            var length = sqlUtf8.Count;
            var result = new IStatement[length];

            for (var i = 0; i < length; i++)
            {
                result[i] = connection.PrepareStatement(sqlUtf8[i].Span);
            }

            return result;
        }

        public IStatement[] PrepareAll(IDatabaseConnection connection, ReadOnlyMemory<byte>[] sqlUtf8)
        {
            var length = sqlUtf8.Length;
            var result = new IStatement[length];

            for (var i = 0; i < length; i++)
            {
                result[i] = connection.PrepareStatement(sqlUtf8[i].Span);
            }

            return result;
        }

        protected bool TableExists(IDatabaseConnection db, string name)
        {
            db.BeginTransaction(ReadTransactionMode);

            try
            {
                var retval = false;

                using (var statement = PrepareStatement(db, "select DISTINCT tbl_name from sqlite_master".AsSpan()))
                {
                    foreach (var row in statement.ExecuteQuery())
                    {
                        if (string.Equals(name, row.GetString(0), StringComparison.OrdinalIgnoreCase))
                        {
                            retval = true;
                            break;
                        }
                    }
                }

                db.CommitTransaction();

                return retval;
            }
            catch (Exception)
            {
                db.RollbackTransaction();

                throw;
            }
        }

        protected void RunDefaultInitialization(IDatabaseConnection db)
        {
            var queries = new List<string>
            {
                "PRAGMA journal_mode=WAL",
                "PRAGMA page_size=4096",
                "PRAGMA synchronous=Normal"
            };

            if (EnableTempStoreMemory)
            {
                queries.AddRange(new List<string>
                {
                    "pragma default_temp_store = memory",
                    "pragma temp_store = memory"
                });
            }
            else
            {
                queries.AddRange(new List<string>
                {
                    "pragma temp_store = file"
                });
            }

            db.ExecuteAll(string.Join(";", queries.ToArray()));
        }

        protected virtual bool EnableTempStoreMemory
        {
            get
            {
                return false;
            }
        }

        protected virtual int? CacheSize
        {
            get
            {
                return null;
            }
        }

        internal static Exception CreateException(ErrorCode rc, string msg)
        {
            var exp = new Exception(msg);

            return exp;
        }

        private bool _disposed;
        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name + " has been disposed and cannot be accessed.");
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Dispose(true);
        }

        private readonly object _disposeLock = new object();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                DisposeConnection();
            }
        }

        private void DisposeConnection()
        {
            try
            {
                lock (_disposeLock)
                {
                    using (lock_manager.getLockItem().Write())
                    {
                        if (_connection != null)
                        {
                            using (_connection)
                            {
                                _connection.Close();
                            }
                            _connection = null;
                        }

                        CloseConnection();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error disposing database", ex);
            }
        }

        protected virtual void CloseConnection()
        {

        }

        protected List<string> GetColumnNames(IDatabaseConnection connection, string table)
        {
            var list = new List<string>();

            using (var statement = PrepareStatement(connection, "PRAGMA table_info(" + table + ")"))
            {
                foreach (var row in statement.ExecuteQuery())
                {
                    if (!row.IsDBNull(1))
                    {
                        var name = row.GetString(1);

                        list.Add(name);
                    }
                }
            }

            return list;
        }

        protected bool AddColumn(IDatabaseConnection connection, string table, string columnName, string type, List<string> existingColumnNames)
        {
            if (existingColumnNames.Contains(columnName, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            connection.Execute("alter table " + table + " add column " + columnName + " " + type + " NULL");
            return true;
        }
    }

    public static class ReaderWriterLockSlimExtensions
    {
        private sealed class ReadLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;
            public ReadLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterReadLock();
            }
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitReadLock();
                    _sync = null;
                }
            }
        }
        private sealed class WriteLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;
            public WriteLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterWriteLock();
            }
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitWriteLock();
                    _sync = null;
                }
            }
        }

        public class DummyToken : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public static IDisposable Read(this ReaderWriterLockSlim obj)
        {
            //if (BaseSqliteRepository.ThreadSafeMode > 0)
            //{
            //    return new DummyToken();
            //}
            return new WriteLockToken(obj);
        }
        public static IDisposable Write(this ReaderWriterLockSlim obj)
        {
            //if (BaseSqliteRepository.ThreadSafeMode > 0)
            //{
            //    return new DummyToken();
            //}
            return new WriteLockToken(obj);
        }
    }
}
