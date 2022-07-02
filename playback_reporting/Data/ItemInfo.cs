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

namespace playback_reporting.Data
{
    public class ItemInfo
    {
        public long Id { get; set; }
        public long ParentId { set; get; }
        public string ParentName { set; get; }
        public string Name { get; set; }
        public string ItemType { get; set; }
        public string Season { get; set; }
        public string Series { get; set; }

    }
}
