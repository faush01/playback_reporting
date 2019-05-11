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
using System.Diagnostics;
using System.Text;

namespace playback_reporting.Data
{
    public class ProcessDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long Memory { get; set; }
        public double TotalMilliseconds_last { get; set; }
        public DateTime LastSampleTime { get; set; }
        public double CpuUsage { set; get; }
        public string Error { get; set; }
        public ProcessDetails(Process proc)
        {
            Name = proc.ProcessName;
            Id = proc.Id;
            Memory = proc.WorkingSet64;
            TotalMilliseconds_last = 0;
            LastSampleTime = DateTime.MinValue;
        }
        public ProcessDetails()
        {
        }

        override
        public string ToString()
        {
            return string.Format("{0} | {1} | {2} | {3} | {4}", Id, Name, CpuUsage, Memory, Error);
        }
    }
}
