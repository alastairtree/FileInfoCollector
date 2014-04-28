## What is this?

A simple console app to collect basic information about all files in a directory and write the results into a text file.
It collects: 

* File path/file name
* File exists (true/false)
* Size
* Created date
* MD5 checksum.

## Key features

* Collects checksums for all files in a folder.
* Designed to work on massive directories, used on a million large files no problems. 
* Speed, or at least "fast enough". It has some simple parallelism and was considerably faster than the solution it replaced.
* Works against network shares, just use path \\server\folder\
* Stop and resume for giant directoryies that can take hours to checksum every file. 
* Progress reporting to console out. Again useful if you are running for a long time.
* Error handling. If a file has some sort of issue then it is logged to the output file and the collector moves onto the next file.

## Example/Instructions

**Basic use**

    FileInfoCollector.exe "c:\folder to analyse\" 
	
Will output 2 files into folder "FileInfoCollectorWorkFolder_0000000"

*FileList.txt* will contain a list of all the files inside "c:\folder to analyse\", e.g.

    c:\folder to analyse\file.jpg
    c:\folder to analyse\folder2\file2.jpg
    c:\folder to analyse\folder2\folder3\file3.jpg

*FileInfo.txt* will contain a pipe delimited table with info on all files inside "c:\Some\folder to analyse\", e.g.

    File Path|File Exists?|Size|Created|Checksum|Error
    c:\folder to analyse\file.jpg|True|1574|08/04/2014 16:18:51|c60ea1a96ee41bc2c5cfef1808ee7a7e|
    c:\folder to analyse\folder2\file2.jpg|True|1574|08/04/2014 16:18:51|c60ea1a96ee41bc2c5cfef1808ee7a7e|
    c:\folder to analyse\folder2\folder3\file3.jpg|False|1574|15/04/2014 12:35:36|08f0ff05cc46f009a469aedfa205c3ef|

**Advanced use with stop/resume**

For stop resume to work (or just to force the output directory) you must tell FileInfoCollector where the working directory is to be located, so that the existing fileList.txt file cvan be read, if it exists

    FileInfoCollector.exe c:\folder_to_analyse\ c:\output_folder\ 

## How good is it?
It is a quick and dirty solution to a simple problem. However it has been proved to work reliably on lots of large files on a remote network share, so it is quite resilient. 

## Warranty
It works great on my machine, and nothing more! 
Should be considered untested. 
If it works for you, that's great, but it is entirely unsupported.
