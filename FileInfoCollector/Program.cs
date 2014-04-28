using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileInfoCollector
{
    internal class Program
    {
        private const int NotificationFrequency = 250;

        private static int Main(string[] args)
        {
            var arguments = new Arguments();
            var messageWriter = Console.Out;

            if (!arguments.TryParse(args, messageWriter))
            {
                return -1;
            }

            var exitCode = ExtractDirectoryListingToFile(arguments, messageWriter);

            if (exitCode != 0)
            {
                return exitCode;
            }

            var filesInfos = new ConcurrentBag<FileInfoRecord>();
            var progress = 0;
            var errors = 0;
            var startTime = DateTime.Now;
            var cts = new CancellationTokenSource();
            var filePaths = GetFilePaths(messageWriter, arguments);

            filePaths = RemoveFilesAlreadyProcessed(filePaths, arguments.OutputFileInfo, messageWriter);

            using (var streamWriter = new StreamWriter(arguments.OutputFileInfo.FullName, true))
            {
                var writerTask = StartWriterTask(cts.Token, filesInfos, streamWriter, arguments.ExistingOutputFile);

                Parallel.ForEach(filePaths,
                    path => CollectFileInfo(path, filesInfos, ref progress, ref errors, messageWriter, filePaths.Length, startTime));

                messageWriter.WriteLine("All files examined");

                StopWriterTask(cts, writerTask);
            }

            messageWriter.WriteLine("{0} files examined in {1:0.#} mins - details exported to '{2}'", filePaths.Length, (DateTime.Now - startTime).TotalMinutes, arguments.OutputFileInfo.FullName);

#if DEBUG
            Console.ReadLine();
#endif
            return 0;
        }

        private static void StopWriterTask(CancellationTokenSource cts, Task writerTask)
        {
            try
            {
                cts.Cancel();
                writerTask.Wait(cts.Token);
            }
            catch (OperationCanceledException)
            {
                //no-op
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => e.GetType() == typeof(OperationCanceledException));
            }
        }

        private static string[] RemoveFilesAlreadyProcessed(string[] filePaths, FileInfo outputFileInfo, TextWriter messageWriter)
        {
            if (outputFileInfo.Exists)
            {
                var startRecords = filePaths.Length;
                var existingRecords = File.ReadAllLines(outputFileInfo.FullName);
                var existingPaths = existingRecords
                    .Where(x=>x != "File Path|File Exists?|Size|Created|Checksum|Error")
                    .Where(x => x.Contains("|"))
                    .Select(x => x.Substring(0, x.IndexOf('|')));

                filePaths = filePaths.Except(existingPaths).ToArray();

                if (filePaths.Length < startRecords)
                {
                    messageWriter.WriteLine("Skipped {0} files already processed in file '{1}'", startRecords - filePaths.Length, outputFileInfo.FullName);
                }
            }
            return filePaths;
        }

        private static string[] GetFilePaths(TextWriter messageWriter, Arguments arguments)
        {
            messageWriter.WriteLine("Extracted directory listing for '{0}' to file '{1}'.", arguments.SourceDirectoryInfo.FullName, arguments.FileListFileInfo.FullName);

            var filePaths = File.ReadAllLines(arguments.FileListFileInfo.FullName);

            messageWriter.WriteLine("Found {0} files in the source directory", filePaths.Length);
            return filePaths;
        }

        private static Task StartWriterTask(CancellationToken token, ConcurrentBag<FileInfoRecord> filesInfos, StreamWriter streamWriter, bool existingOutputFile)
        {
            var writerTask = Task.Factory.StartNew(() =>
            {
                if (!existingOutputFile)
                    streamWriter.WriteLine("{0}|{1}|{2}|{3}|{4}|{5}", "File Path", "File Exists?", "Size", "Created", "Checksum", "Error");

                while (!token.IsCancellationRequested || !filesInfos.IsEmpty)
                {
                    FileInfoRecord item;
                    if (filesInfos.TryTake(out item))
                    {
                        string error = item.Error != null ? item.Error.ToString().Replace("\r\n", ",") : "";
                        streamWriter.WriteLine("{0}|{1}|{2}|{3}|{4}|{5}", item.FilePath, item.Exists, item.Size, item.Created, item.Checksum, error);
                        streamWriter.Flush();
                    }
                }
            }, token);
            return writerTask;
        }

        private static FileInfoRecord GetFileInfoRecord(string path)
        {
            var record = new FileInfoRecord { FilePath = path };

            try
            {
                var info = new FileInfo(record.FilePath);
                record.Exists = info.Exists;

                if (record.Exists)
                {
                    var hashBuilder = new StringBuilder();
                    using (var stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var ba = MD5.Create().ComputeHash(stream);
                        foreach (var b in ba)
                        {
                            hashBuilder.Append(b.ToString("x2"));
                        }

                        stream.Close();
                    }
                    record.Size = info.Length;
                    record.Checksum = hashBuilder.ToString();
                    record.Created = info.CreationTime;
                }
            }
            catch (Exception ex)
            {
                record.Error = ex;
            }

            return record;
        }

        private static int ExtractDirectoryListingToFile(Arguments arguments, TextWriter messageWriter)
        {
            var listOfFilePathsFile = arguments.FileListFileInfo.FullName;
            if (arguments.FileListFileInfo.Exists)
            {
                messageWriter.WriteLine("FileList.txt already found in output destination, skipping file listing.");
                return 0;
            }

            messageWriter.WriteLine("Extracting recursive file list for directory '{0}' to file '{1}'", arguments.SourceDirectoryInfo.FullName, listOfFilePathsFile);

            try
            {
                if (!Directory.Exists(arguments.DestinationWorkFolder.FullName))
                {
                    Directory.CreateDirectory(arguments.DestinationWorkFolder.FullName);
                }
            }
            catch (IOException)
            {
                messageWriter.WriteLine("Unable to create directory " + listOfFilePathsFile);
            }

            var dirCommand = String.Format("/C dir /S /B \"{0}\" /A:-D > \"{1}\"", arguments.SourceDirectoryInfo.FullName, listOfFilePathsFile);

            var procInfo = new ProcessStartInfo("CMD.exe", dirCommand) { UseShellExecute = false, RedirectStandardError = true };

            var proc = Process.Start(procInfo);

            if (proc != null)
            {
                proc.WaitForExit();
                return proc.ExitCode;
            }
            return -1;
        }

        private static void CollectFileInfo(string path, ConcurrentBag<FileInfoRecord> filesInfos, ref int progress, ref int errors, TextWriter messageWriter, int totalFiles, DateTime startTime)
        {
            var record = GetFileInfoRecord(path);

            if (record.Error != null)
            {
                Interlocked.Increment(ref errors);
                messageWriter.WriteLine("Error in file {0}: {1}", record.FilePath, record.Error);
            }

            filesInfos.Add(record);

            if (Interlocked.Increment(ref progress) % NotificationFrequency == 0)
            {
                messageWriter.WriteLine("{0} out of {1} files processed after {2} minutes ({3} errors so far)", progress, totalFiles, (int)(DateTime.Now - startTime).TotalMinutes, errors);
            }
        }
    }
}