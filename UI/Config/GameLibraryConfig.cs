using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Mesen.Config
{
	public class GameLibraryConfig
	{
		public List<GameLibraryFolder> Folders { get; set; } = new();
	}

	public partial class GameLibraryFolder : ObservableObject
	{
		[ObservableProperty] public partial string Path { get; set; } = "";
		[ObservableProperty] public partial bool Recursive { get; set; }
	}
}
