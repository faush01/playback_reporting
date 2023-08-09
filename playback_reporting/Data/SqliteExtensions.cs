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
using SQLitePCL.pretty;

namespace playback_reporting.Data
{
    public static class SqliteExtensions
    {
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

        private static string GetDateTimeKindFormat(
           DateTimeKind kind)
        {
            return (kind == DateTimeKind.Utc) ? _datetimeFormatUtc : _datetimeFormatLocal;
        }

        public static DateTime ReadDateTime(this IResultSet result, int index)
        {
            var dateText = result.GetString(index);

            return DateTime.ParseExact(
                dateText, _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None).ToUniversalTime();
        }

        public static string ToDateTimeParamValue(this DateTime dateValue)
        {
            var kind = DateTimeKind.Utc;
            if (dateValue.Kind == DateTimeKind.Unspecified) // if Unspecified force UTC
            {
                return DateTime.SpecifyKind(dateValue, kind).ToString(GetDateTimeKindFormat(kind), CultureInfo.InvariantCulture);
            }
            else
            {
                return dateValue.ToString(GetDateTimeKindFormat(dateValue.Kind), CultureInfo.InvariantCulture);
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
                    bindParam.Bind(value);
                }
            }
        }

        public static void TryBind(this IStatement statement, string name, int value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
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
