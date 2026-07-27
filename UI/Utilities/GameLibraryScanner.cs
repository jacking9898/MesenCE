using Mesen.Config;
using Mesen.GUI.Utilities;
using Mesen.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Mesen.Utilities
{
	public static class GameLibraryScanner
	{
		private static readonly HashSet<string> _nesExtensions = new(StringComparer.OrdinalIgnoreCase) {
			".nes", ".fds", ".qd", ".unif", ".unf", ".nsf", ".nsfe", ".studybox"
		};

		public static GameLibraryScanResult Scan(IEnumerable<GameLibraryFolder> folders, CancellationToken cancellationToken)
		{
			List<GameLibraryEntry> entries = new();
			HashSet<string> knownPaths = new(StringComparer.Ordinal);
			int errorCount = 0;

			foreach(GameLibraryFolder folder in folders) {
				cancellationToken.ThrowIfCancellationRequested();
				if(!Directory.Exists(folder.Path)) {
					errorCount++;
					continue;
				}

				try {
					EnumerationOptions options = new() {
						RecurseSubdirectories = folder.Recursive,
						IgnoreInaccessible = true,
						ReturnSpecialDirectories = false,
						AttributesToSkip = FileAttributes.ReparsePoint
					};

					foreach(string file in Directory.EnumerateFiles(folder.Path, "*", options)) {
						cancellationToken.ThrowIfCancellationRequested();
						string extension = System.IO.Path.GetExtension(file);
						if(_nesExtensions.Contains(extension)) {
							AddEntry(entries, knownPaths, new ResourcePath() { Path = file }, folder.Path);
						} else if(extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
							try {
								List<ArchiveRomEntry> archiveEntries = ArchiveHelper.GetArchiveRomList(file);
								for(int i = 0; i < archiveEntries.Count; i++) {
									cancellationToken.ThrowIfCancellationRequested();
									ArchiveRomEntry archiveEntry = archiveEntries[i];
									if(_nesExtensions.Contains(System.IO.Path.GetExtension(archiveEntry.Filename))) {
										ResourcePath path = new() {
											Path = file,
											InnerFile = archiveEntry.Filename,
											InnerFileIndex = archiveEntry.IsUtf8 ? 0 : i + 1
										};
										AddEntry(entries, knownPaths, path, folder.Path);
									}
								}
							} catch(OperationCanceledException) {
								throw;
							} catch {
								errorCount++;
							}
						}
					}
				} catch(OperationCanceledException) {
					throw;
				} catch {
					errorCount++;
				}
			}

			entries.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name));
			return new GameLibraryScanResult(entries, errorCount);
		}

		private static void AddEntry(List<GameLibraryEntry> entries, HashSet<string> knownPaths, ResourcePath path, string libraryFolder)
		{
			if(knownPaths.Add(path.ToString())) {
				entries.Add(new GameLibraryEntry(path, libraryFolder));
			}
		}
	}

	public sealed class GameLibraryScanResult
	{
		public List<GameLibraryEntry> Entries { get; }
		public int ErrorCount { get; }

		public GameLibraryScanResult(List<GameLibraryEntry> entries, int errorCount)
		{
			Entries = entries;
			ErrorCount = errorCount;
		}
	}
}
