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
using System.IO;
using System.Text;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using playback_reporting.Data;

namespace playback_reporting
{
    class BackupManager
    {

        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public BackupManager(IServerConfigurationManager config, ILogger logger, IFileSystem fileSystem)
        {
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string SaveBackup()
        {
            ReportPlaybackOptions config = _config.GetReportPlaybackOptions();

            if (string.IsNullOrEmpty(config.BackupPath))
            {
                return "No backup path set";
            }

            DirectoryInfo fi = new DirectoryInfo(config.BackupPath);
            _logger.Info("Backup Path : " + config.BackupPath + " attributes : " + fi.Attributes + " exists : " + fi.Exists);
            if (fi.Exists == false || (fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
            {
                return "Backup path does not exist or is not a directory";
            }

            ActivityRepository repository = new ActivityRepository(_config.ApplicationPaths.DataPath);
            string raw_data = repository.ExportRawData();

            String fileName = "PlaybackReportingBackup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".tsv";
            string backup_file = Path.Combine(fi.FullName, fileName);
            _logger.Info("Backup Path Final : " + backup_file);

            try
            {
                System.IO.File.WriteAllText(backup_file, raw_data);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            FileInfo[] files = fi.GetFiles("PlaybackReportingBackup-*.tsv");
            int max_files = config.MaxBackupFiles;
            int files_to_delete = files.Length - max_files;

            _logger.Info("Backup Files Counts Current: " + files.Length + " Max:" + max_files + " ToDelete:" + files_to_delete);

            if (files_to_delete > 0)
            {
                List<string> file_paths = new List<string>();
                foreach (FileInfo file_info in files)
                {
                    file_paths.Add(file_info.FullName);
                    _logger.Info("Existing Backup Files Before: " + file_info.Name);
                }
                file_paths.Sort();

                for (int file_index = 0; file_index < files_to_delete; file_index++)
                {
                    FileInfo del_file = new FileInfo(file_paths[file_index]);
                    _logger.Info("Deleting backup file : " + del_file.FullName);
                    del_file.Delete();
                }
            }

            return "Backup saved : " + fileName;
        }

    }
}
