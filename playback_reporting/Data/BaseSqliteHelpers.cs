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
        private static BaseSqliteLock instance = null;
        private static readonly object _padlock = new object();

        private BaseSqliteLock()
        {
        }

        public static BaseSqliteLock GetInstance(ILogger _logger)
        {
            lock (_padlock)
            {
                if (instance == null)
                {
                    instance = new BaseSqliteLock();
                }
                _logger.Debug("BaseSqliteLock Instance : {0}", instance.GetHashCode());
                return instance;
            }
        }
    }

    public static class BaseSqliteHelpers
    {
        public static IDatabaseConnection CreateConnection(ILogger _logger, string db_path)
        {
            ConnectionFlags connectionFlags;

            //Logger.Info("Opening write connection");
            connectionFlags = ConnectionFlags.Create;
            connectionFlags |= ConnectionFlags.ReadWrite;
            connectionFlags |= ConnectionFlags.PrivateCache;
            connectionFlags |= ConnectionFlags.NoMutex;

            var db = SQLite3.Open(db_path, connectionFlags, null, false);

            try
            {
                var queries = new List<string>
                {
                    //"PRAGMA cache size=-10000"
                    //"PRAGMA read_uncommitted = true",
                    "PRAGMA synchronous=Normal",
                    "PRAGMA temp_store = file"
                };
                    
                db.ExecuteAll(string.Join(";", queries.ToArray()));
            }
            catch
            {
                throw;
            }

            return db;
        }
    }
}
