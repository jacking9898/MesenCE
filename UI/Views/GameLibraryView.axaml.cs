using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Mesen.Config;
using Mesen.Interop;
using Mesen.Utilities;
using Mesen.ViewModels;
using System;

namespace Mesen.Views
{
	public class GameLibraryView : UserControl
	{
		public GameLibraryView()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private async void BtnAddFolder_OnClick(object sender, RoutedEventArgs e)
		{
			if(DataContext is GameLibraryViewModel model) {
				string? folder = await FileDialogHelper.OpenFolder(this.GetWindow());
				if(!string.IsNullOrWhiteSpace(folder)) {
					model.AddFolder(folder);
				}
			}
		}

		private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
		{
			if(DataContext is GameLibraryViewModel model) {
				_ = model.RefreshAsync();
			}
		}

		private void SearchBox_OnGotFocus(object? sender, RoutedEventArgs e)
		{
			InputApi.DisableAllKeys(true);
			InputApi.ResetKeyState();
		}

		private void SearchBox_OnLostFocus(object? sender, RoutedEventArgs e)
		{
			InputApi.DisableAllKeys(false);
		}

		private void BtnRemoveFolder_OnClick(object sender, RoutedEventArgs e)
		{
			if(DataContext is GameLibraryViewModel model && sender is Button { DataContext: GameLibraryFolder folder }) {
				model.RemoveFolder(folder);
			}
		}

		private void FolderRecursive_OnClick(object sender, RoutedEventArgs e)
		{
			if(DataContext is GameLibraryViewModel model && sender is CheckBox { DataContext: GameLibraryFolder folder } checkBox) {
				model.UpdateFolder(folder, checkBox.IsChecked == true);
			}
		}

		private void BtnPlay_OnClick(object sender, RoutedEventArgs e)
		{
			if(sender is Button { DataContext: GameLibraryEntry entry }) {
				entry.Load();
			}
		}

		private void GameList_OnDoubleTapped(object sender, TappedEventArgs e)
		{
			if(sender is ListBox { SelectedItem: GameLibraryEntry entry }) {
				entry.Load();
			}
		}
	}
}
