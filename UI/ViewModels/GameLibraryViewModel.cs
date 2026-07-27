using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Mesen.Config;
using Mesen.Localization;
using Mesen.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mesen.ViewModels
{
	public partial class GameLibraryViewModel : DisposableViewModel
	{
		private CancellationTokenSource? _scanCancellation;
		private List<GameLibraryEntry> _allEntries = new();

		[ObservableProperty] public partial bool Visible { get; set; }
		[ObservableProperty] public partial bool IsScanning { get; private set; }
		[ObservableProperty] public partial bool RecursiveForNewFolder { get; set; } = true;
		[ObservableProperty] public partial string SearchText { get; set; } = "";
		[ObservableProperty] public partial string StatusText { get; private set; } = "";
		[ObservableProperty] public partial ObservableCollection<GameLibraryEntry> GameEntries { get; private set; } = new();

		public ObservableCollection<GameLibraryFolder> Folders { get; }
		public bool HasFolders => Folders.Count > 0;

		public GameLibraryViewModel()
		{
			Folders = new ObservableCollection<GameLibraryFolder>(ConfigManager.Config.GameLibrary.Folders);
		}

		public void Initialize()
		{
			Dispatcher.UIThread.Post(() => {
				Visible = true;
				_ = RefreshAsync();
			});
		}

		public void AddFolder(string path)
		{
			if(Folders.Any(folder => string.Equals(folder.Path, path, StringComparison.OrdinalIgnoreCase))) {
				return;
			}

			Folders.Add(new GameLibraryFolder() { Path = path, Recursive = RecursiveForNewFolder });
			SaveFolders();
			_ = RefreshAsync();
		}

		public void RemoveFolder(GameLibraryFolder folder)
		{
			Folders.Remove(folder);
			SaveFolders();
			_ = RefreshAsync();
		}

		public void UpdateFolder(GameLibraryFolder folder, bool recursive)
		{
			folder.Recursive = recursive;
			SaveFolders();
			_ = RefreshAsync();
		}

		public async Task RefreshAsync()
		{
			_scanCancellation?.Cancel();
			_scanCancellation?.Dispose();
			_scanCancellation = new CancellationTokenSource();
			CancellationToken cancellationToken = _scanCancellation.Token;

			IsScanning = true;
			StatusText = Folders.Count == 0 ? ResourceHelper.GetMessage("GameLibraryAddFolderHint") : ResourceHelper.GetMessage("GameLibraryScanning");
			if(Folders.Count == 0) {
				_allEntries = new();
				ApplyFilter();
				IsScanning = false;
				return;
			}

			List<GameLibraryFolder> folders = Folders.Select(folder => new GameLibraryFolder() {
				Path = folder.Path,
				Recursive = folder.Recursive
			}).ToList();

			try {
				GameLibraryScanResult result = await Task.Run(() => GameLibraryScanner.Scan(folders, cancellationToken), cancellationToken);
				if(cancellationToken.IsCancellationRequested) {
					return;
				}

				_allEntries = result.Entries;
				ApplyFilter();
				StatusText = result.ErrorCount == 0
					? ResourceHelper.GetMessage("GameLibraryScanComplete", result.Entries.Count)
					: ResourceHelper.GetMessage("GameLibraryScanCompleteWithErrors", result.Entries.Count, result.ErrorCount);
			} catch(OperationCanceledException) {
			} catch(Exception ex) {
				StatusText = ResourceHelper.GetMessage("GameLibraryScanFailed", ex.Message);
			} finally {
				if(_scanCancellation?.Token == cancellationToken) {
					IsScanning = false;
				}
			}
		}

		partial void OnSearchTextChanged(string value)
		{
			ApplyFilter();
		}

		private void ApplyFilter()
		{
			IEnumerable<GameLibraryEntry> entries = _allEntries;
			if(!string.IsNullOrWhiteSpace(SearchText)) {
				entries = entries.Where(entry => entry.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
					|| entry.Location.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
			}
			GameEntries = new ObservableCollection<GameLibraryEntry>(entries);
		}

		private void SaveFolders()
		{
			ConfigManager.Config.GameLibrary.Folders = Folders.ToList();
			ConfigManager.Config.Save();
			OnPropertyChanged(nameof(HasFolders));
		}

		protected override void DisposeView()
		{
			_scanCancellation?.Cancel();
			_scanCancellation?.Dispose();
		}
	}

	public sealed class GameLibraryEntry
	{
		public ResourcePath RomPath { get; }
		public string Name { get; }
		public string Location { get; }
		public string Format { get; }

		public GameLibraryEntry(ResourcePath romPath, string libraryFolder)
		{
			RomPath = romPath;
			string displayFile = romPath.Compressed && romPath.InnerFile.Contains('\uFFFD') ? romPath.Path : romPath.FileName;
			Name = Path.GetFileNameWithoutExtension(displayFile);
			Format = Path.GetExtension(romPath.FileName).TrimStart('.').ToUpperInvariant();
			Location = romPath.Compressed
				? $"{Path.GetFileName(romPath.Path)} / {romPath.InnerFile}"
				: Path.GetRelativePath(libraryFolder, romPath.Path);
		}

		public void Load()
		{
			LoadRomHelper.LoadRom(RomPath);
		}
	}
}
