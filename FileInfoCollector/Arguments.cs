using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileInfoCollector
{
    public class Arguments
    {
        public DirectoryInfo SourceDirectoryInfo { get; set; }
        public DirectoryInfo DestinationWorkFolder { get; set; }
        public FileInfo FileListFileInfo { get; set; }
        public FileInfo OutputFileInfo { get; set; }
        public bool ExistingOutputFile { get; set; }

        public bool TryParse(string[] args, TextWriter errorWriter)
        {
            if (args == null || !args.Any() || args.Any(x => x == "?" || x.Equals("h", StringComparison.InvariantCultureIgnoreCase) || x.Equals("help", StringComparison.InvariantCultureIgnoreCase)))
            {
                errorWriter.WriteLine("Please specify at least 1 arguments - FileInfoCollector.exe <FileSourceDirectory> (<DestinationWorkFolder>)");
                return false;
            }

            if (!ParseSource(args)) return false;

            if (!ParseDestinationFolder(args)) return false;

            FileListFileInfo = new FileInfo(Path.Combine(DestinationWorkFolder.FullName, "FileList.txt"));
            OutputFileInfo = new FileInfo(Path.Combine(DestinationWorkFolder.FullName, "FileInfo.txt"));
            if (OutputFileInfo.Exists)
                this.ExistingOutputFile = true;
            return true;
        }


        private bool ParseDestinationFolder(string[] args)
        {
            if (args.Length > 1)
            {
                if (!Directory.Exists(args[1]))
                {
                    Console.Out.WriteLine("DestinationWorkFolder is not a valid or existing directory. Retry command in format - FileInfoCollector.exe <FileSourceDirectory> (<DestinationWorkFolder>)");
                    return false;
                }
                else
                {
                    DestinationWorkFolder = new DirectoryInfo(args[1]);
                }
            }
            else
            {
                DestinationWorkFolder = new DirectoryInfo("FileInfoCollectorWorkFolder_" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            return true;
        }

        private bool ParseSource(string[] args)
        {
            string sourcePath = args[0];
            if (!Directory.Exists(sourcePath))
            {
                Console.Out.WriteLine("FileSourceDiectory does not exists. Retry command in format - FileInfoCollector.exe <FileSourceDirectory> (<DestinationWorkFolder>)");
                return false;
            }
            SourceDirectoryInfo = new DirectoryInfo(sourcePath);
            return true;
        }
    }
}
