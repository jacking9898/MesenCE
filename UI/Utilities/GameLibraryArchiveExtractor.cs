using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace Mesen.Utilities
{
	internal static class GameLibraryArchiveExtractor
	{
		private static readonly Encoding LegacyFilenameEncoding;
		private static readonly byte[] INesHeader = { (byte)'N', (byte)'E', (byte)'S', 0x1A };

		static GameLibraryArchiveExtractor()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			LegacyFilenameEncoding = Encoding.GetEncoding("GB18030", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
		}

		public static List<string> ExtractRoms(string archivePath, IReadOnlySet<string> romExtensions, CancellationToken cancellationToken)
		{
			string? archiveFolder = Path.GetDirectoryName(archivePath);
			if(string.IsNullOrWhiteSpace(archiveFolder)) {
				throw new IOException("The ZIP archive does not have a parent folder.");
			}

			string extractionFolder = Path.Combine(archiveFolder, Path.GetFileNameWithoutExtension(archivePath));

			List<string> extractedRoms = new();
			using FileStream archiveStream = new(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
			using ZipArchive archive = new(archiveStream, ZipArchiveMode.Read, false, LegacyFilenameEncoding);
			foreach(ZipArchiveEntry entry in archive.Entries) {
				cancellationToken.ThrowIfCancellationRequested();
				if(!TryGetRomEntryName(entry, romExtensions, extractionFolder, out string romEntryName)) {
					continue;
				}

				string targetPath = GetSafeTargetPath(extractionFolder, romEntryName);
				string? targetFolder = Path.GetDirectoryName(targetPath);
				if(string.IsNullOrWhiteSpace(targetFolder)) {
					throw new IOException("The ZIP entry does not have a valid destination folder.");
				}
				EnsureDirectoryIsSafe(extractionFolder, targetFolder);

				if(File.Exists(targetPath)) {
					if((File.GetAttributes(targetPath) & FileAttributes.ReparsePoint) != 0) {
						throw new IOException($"Refusing to use a symbolic link as an extracted ROM: {targetPath}");
					}
				} else {
					ExtractEntry(entry, targetPath);
				}
				extractedRoms.Add(targetPath);
			}

			return extractedRoms;
		}

		private static bool TryGetRomEntryName(ZipArchiveEntry entry, IReadOnlySet<string> romExtensions, string extractionFolder, out string romEntryName)
		{
			romEntryName = entry.FullName;
			if(string.IsNullOrWhiteSpace(entry.Name)) {
				return false;
			}
			if(romExtensions.Contains(Path.GetExtension(entry.Name))) {
				return true;
			}

			// Some older ROM collections contain a valid iNES file without a usable extension
			// (and version numbers in the filename can look like one). Detect only this
			// unambiguous signature; executables and other files stay ignored.
			if(entry.Length >= 4) {
				string candidateEntryName = entry.FullName + ".nes";
				string candidatePath = GetSafeTargetPath(extractionFolder, candidateEntryName);
				if(File.Exists(candidatePath)
					&& (File.GetAttributes(candidatePath) & FileAttributes.ReparsePoint) == 0
					&& HasINesHeader(candidatePath)) {
					romEntryName = candidateEntryName;
					return true;
				}

				using Stream input = entry.Open();
				if(HasINesHeader(input)) {
					romEntryName = candidateEntryName;
					return true;
				}
			}

			return false;
		}

		private static bool HasINesHeader(string path)
		{
			using FileStream input = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return HasINesHeader(input);
		}

		private static bool HasINesHeader(Stream input)
		{
			Span<byte> header = stackalloc byte[4];
			return input.Read(header) == header.Length && header.SequenceEqual(INesHeader);
		}

		private static string GetSafeTargetPath(string extractionFolder, string entryName)
		{
			string normalizedEntryName = entryName.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
			string extractionRoot = Path.GetFullPath(extractionFolder) + Path.DirectorySeparatorChar;
			string targetPath = Path.GetFullPath(Path.Combine(extractionFolder, normalizedEntryName));
			StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if(!targetPath.StartsWith(extractionRoot, comparison)) {
				throw new InvalidDataException($"The ZIP entry points outside its extraction folder: {entryName}");
			}
			return targetPath;
		}

		private static void EnsureDirectoryIsSafe(string extractionFolder, string directory)
		{
			string extractionRoot = Path.GetFullPath(extractionFolder);
			string targetDirectory = Path.GetFullPath(directory);
			string relativePath = Path.GetRelativePath(extractionRoot, targetDirectory);
			string currentDirectory = extractionRoot;

			CheckOrCreateDirectory(currentDirectory);
			if(relativePath == ".") {
				return;
			}

			foreach(string part in relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)) {
				if(part == "..") {
					throw new InvalidDataException("The ZIP entry points outside its extraction folder.");
				}
				currentDirectory = Path.Combine(currentDirectory, part);
				CheckOrCreateDirectory(currentDirectory);
			}
		}

		private static void CheckOrCreateDirectory(string directory)
		{
			if(Directory.Exists(directory)) {
				if((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) {
					throw new IOException($"Refusing to extract through a symbolic link: {directory}");
				}
			} else {
				Directory.CreateDirectory(directory);
			}
		}

		private static void ExtractEntry(ZipArchiveEntry entry, string targetPath)
		{
			string temporaryPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try {
				{
					using Stream input = entry.Open();
					using FileStream output = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.SequentialScan);
					input.CopyTo(output);
				}
				try {
					File.Move(temporaryPath, targetPath, false);
				} catch(IOException) when(File.Exists(targetPath)) {
					// A second scan may have finished extracting the same ROM first.
				}
			} finally {
				if(File.Exists(temporaryPath)) {
					File.Delete(temporaryPath);
				}
			}
		}
	}
}
