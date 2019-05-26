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
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using SQLitePCL.pretty;
using System.IO;
using System.Text;

namespace playback_reporting.Data
{
    public static class SqliteExtensions
    {
        public static void RunQueries(this IDatabaseConnection db, string[] queries)
        {
            db.BeginTransaction(TransactionMode.Deferred);

            try
            {
                db.ExecuteAll(string.Join(";", queries));

                db.CommitTransaction();
            }
            catch (Exception)
            {
                db.RollbackTransaction();

                throw;
            }
        }

        private static string GetDateTimeKindFormat(
           DateTimeKind kind)
        {
            return (kind == DateTimeKind.Utc) ? _datetimeFormatUtc : _datetimeFormatLocal;
        }

        /// <summary>
        /// An array of ISO-8601 DateTime formats that we support parsing.
        /// </summary>
        private static string[] _datetimeFormats = new string[] {
      "THHmmssK",
      "THHmmK",
      "HH:mm:ss.FFFFFFFK",
      "HH:mm:ssK",
      "HH:mmK",
      "yyyy-MM-dd HH:mm:ss.FFFFFFFK", /* NOTE: UTC default (5). */
      "yyyy-MM-dd HH:mm:ssK",
      "yyyy-MM-dd HH:mmK",
      "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
      "yyyy-MM-ddTHH:mmK",
      "yyyy-MM-ddTHH:mm:ssK",
      "yyyyMMddHHmmssK",
      "yyyyMMddHHmmK",
      "yyyyMMddTHHmmssFFFFFFFK",
      "THHmmss",
      "THHmm",
      "HH:mm:ss.FFFFFFF",
      "HH:mm:ss",
      "HH:mm",
      "yyyy-MM-dd HH:mm:ss.FFFFFFF", /* NOTE: Non-UTC default (19). */
      "yyyy-MM-dd HH:mm:ss",
      "yyyy-MM-dd HH:mm",
      "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
      "yyyy-MM-ddTHH:mm",
      "yyyy-MM-ddTHH:mm:ss",
      "yyyyMMddHHmmss",
      "yyyyMMddHHmm",
      "yyyyMMddTHHmmssFFFFFFF",
      "yyyy-MM-dd",
      "yyyyMMdd",
      "yy-MM-dd"
    };

        private static string _datetimeFormatUtc = _datetimeFormats[5];
        private static string _datetimeFormatLocal = _datetimeFormats[19];

        public static DateTime ReadDateTime(this IResultSet result, int index)
        {
            var dateText = result.GetString(index);

            return DateTime.ParseExact(
                dateText, _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None).ToUniversalTime();
        }

        public static ReadOnlySpan<byte> ToGuidBlob(this ReadOnlySpan<char> str)
        {
#if NETCOREAPP
            return ToGuidBlob(Guid.Parse(str));
#else
            return ToGuidBlob(new Guid(str.ToString()));
#endif
        }

        public static byte[] ToGuidBlob(this Guid guid)
        {
            return guid.ToByteArray();
        }

        public static byte[] ToGuidBlob(this string str)
        {
            return ToGuidBlob(new Guid(str));
        }

        public static string ToDateTimeParamValue(this DateTime dateValue)
        {
            var kind = DateTimeKind.Utc;

            return (dateValue.Kind == DateTimeKind.Unspecified)
                ? DateTime.SpecifyKind(dateValue, kind).ToString(
                    GetDateTimeKindFormat(kind),
                    CultureInfo.InvariantCulture)
                : dateValue.ToString(
                    GetDateTimeKindFormat(dateValue.Kind),
                    CultureInfo.InvariantCulture);
        }

        public static DateTimeOffset ReadDateTimeOffset(this IResultSet result, int index)
        {
            return ReadDateTimeOffset(result, index, false);
        }

        public static DateTimeOffset ReadDateTimeOffset(this IResultSet result, int index, bool enableMsPrecision)
        {
            if (enableMsPrecision)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(result.GetInt64(index));
            }
            return DateTimeOffset.FromUnixTimeSeconds(result.GetInt64(index));
        }

        /// <summary>
        /// Serializes to bytes.
        /// </summary>
        /// <returns>System.Byte[][].</returns>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public static ReadOnlySpan<byte> SerializeToBytes(this IJsonSerializer json, object obj)
        {
            using (var stream = new MemoryStream())
            {
                json.SerializeToStream(obj, stream);
                return stream.ToArray().AsSpan();
            }
        }

        public static void Attach(IDatabaseConnection db, string path, string alias)
        {
            var commandText = string.Format("attach @path as {0};", alias);

            using (var statement = db.PrepareStatement(Encoding.UTF8.GetBytes(commandText).AsSpan()))
            {
                statement.TryBind("@path", path);
                statement.MoveNext();
            }
        }

        public static Guid GetGuid(this IResultSet result, int index)
        {
#if NETCOREAPP
            return new Guid(result.GetBlob(index));
#else
            return new Guid(result.GetBlob(index).ToArray());
#endif
        }

        private static void CheckName(string name)
        {
#if DEBUG
            //if (!name.IndexOf("@", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new Exception("Invalid param name: " + name);
            }
#endif
        }

        public static void TryBind(this IStatement statement, int index, double value)
        {
            IBindParameter bindParam = statement.BindParameters[index];
            bindParam.Bind(value);
        }

        public static void TryBind(this IStatement statement, string name, double value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, string value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                if (value == null)
                {
                    bindParam.BindNull();
                }
                else
                {
                    bindParam.Bind(value.AsSpan());
                }
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, int index, string value)
        {
            IBindParameter bindParam = statement.BindParameters[index];
            if (value == null)
            {
                bindParam.BindNull();
            }
            else
            {
                bindParam.Bind(value.AsSpan());
            }
        }

        public static void TryBind(this IStatement statement, string name, ReadOnlySpan<char> value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                if (value.IsEmpty)
                {
                    bindParam.BindNull();
                }
                else
                {
                    bindParam.Bind(value);
                }
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, int index, bool value)
        {
            IBindParameter bindParam = statement.BindParameters[index];
            bindParam.Bind(value);
        }

        public static void TryBind(this IStatement statement, string name, bool value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, float value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, int index, int value)
        {
            IBindParameter bindParam = statement.BindParameters[index];
            bindParam.Bind(value);
        }

        public static void TryBind(this IStatement statement, string name, int value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, int index, Guid value)
        {
            IBindParameter bindParam = statement.BindParameters[index];
            bindParam.Bind(value.ToGuidBlob());
        }

        public static void TryBind(this IStatement statement, string name, Guid value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value.ToGuidBlob());
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, int index, DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, index, value.Value);
            }
            else
            {
                TryBindNull(statement, index);
            }
        }

        public static void TryBind(this IStatement statement, string name, DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, int index, DateTimeOffset value, bool enableMsPrecision)
        {
            IBindParameter bindParam = statement.BindParameters[index];

            if (enableMsPrecision)
            {
                bindParam.Bind(value.ToUnixTimeMilliseconds());
            }
            else
            {
                bindParam.Bind(value.ToUnixTimeSeconds());
            }
        }

        public static void TryBind(this IStatement statement, string name, DateTimeOffset value, bool enableMsPrecision)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                if (enableMsPrecision)
                {
                    bindParam.Bind(value.ToUnixTimeMilliseconds());
                }
                else
                {
                    bindParam.Bind(value.ToUnixTimeSeconds());
                }
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, int index, DateTimeOffset value)
        {
            TryBind(statement, index, value, false);
        }

        public static void TryBind(this IStatement statement, string name, DateTimeOffset value)
        {
            TryBind(statement, name, value, false);
        }

        public static void TryBind(this IStatement statement, int index, long value)
        {
            IBindParameter bindParam = statement.BindParameters[index];

            bindParam.Bind(value);
        }

        public static void TryBind(this IStatement statement, string name, long value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, int index, ReadOnlySpan<byte> value)
        {
            IBindParameter bindParam = statement.BindParameters[index];
            bindParam.Bind(value);
        }

        public static void TryBind(this IStatement statement, string name, ReadOnlySpan<byte> value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBindNull(this IStatement statement, int index)
        {
            IBindParameter bindParam = statement.BindParameters[index];

            bindParam.BindNull();
        }

        public static void TryBindNull(this IStatement statement, string name)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.BindNull();
            }
            else
            {
                CheckName(name);
            }
        }

        public static void TryBind(this IStatement statement, string name, Guid? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, int index, double? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, index, value.Value);
            }
            else
            {
                TryBindNull(statement, index);
            }
        }

        public static void TryBind(this IStatement statement, string name, double? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, int index, int? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, index, value.Value);
            }
            else
            {
                TryBindNull(statement, index);
            }
        }

        public static void TryBind(this IStatement statement, string name, int? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, string name, float? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static void TryBind(this IStatement statement, int index, bool? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, index, value.Value);
            }
            else
            {
                TryBindNull(statement, index);
            }
        }

        public static void TryBind(this IStatement statement, string name, bool? value)
        {
            if (value.HasValue)
            {
                TryBind(statement, name, value.Value);
            }
            else
            {
                TryBindNull(statement, name);
            }
        }

        public static IEnumerable<IResultSet> ExecuteQuery(
            this IStatement This)
        {
            while (This.MoveNext())
            {
                yield return This.Current;
            }
        }
    }
}
