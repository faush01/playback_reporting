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
using SQLitePCL.pretty;

namespace Jellyfin.Plugin.PlaybackReporting.Data
{
    public class ManagedConnection : IDisposable
    {
        private SQLiteDatabaseConnection db;
        private readonly bool _closeOnDispose;

        public ManagedConnection(SQLiteDatabaseConnection db, bool closeOnDispose)
        {
            this.db = db;
            _closeOnDispose = closeOnDispose;
        }

        public IStatement PrepareStatement(string sql)
        {
            return db.PrepareStatement(sql);
        }

        public IEnumerable<IStatement> PrepareAll(string sql)
        {
            return db.PrepareAll(sql);
        }

        public void ExecuteAll(string sql)
        {
            db.ExecuteAll(sql);
        }

        public void Execute(string sql, params object[] values)
        {
            db.Execute(sql, values);
        }

        public int GetChangeCount()
        {
            return db.TotalChanges;
        }

        public void RunQueries(string[] sql)
        {
            db.RunQueries(sql);
        }

        public void RunInTransaction(Action<IDatabaseConnection> action, TransactionMode mode)
        {
            db.RunInTransaction(action, mode);
        }

        public T RunInTransaction<T>(Func<IDatabaseConnection, T> action, TransactionMode mode)
        {
            return db.RunInTransaction<T>(action, mode);
        }

        public IEnumerable<IReadOnlyList<IResultSetValue>> Query(string sql)
        {
            return db.Query(sql);
        }

        public IEnumerable<IReadOnlyList<IResultSetValue>> Query(string sql, params object[] values)
        {
            return db.Query(sql, values);
        }

        public void Close()
        {
            using (db)
            {

            }
        }

        public void Dispose()
        {
            if (_closeOnDispose)
            {
                Close();
            }
            GC.SuppressFinalize(this);
        }

    }

}
