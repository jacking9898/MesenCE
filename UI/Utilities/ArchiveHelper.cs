using Mesen.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Mesen.GUI.Utilities
{
	public class ArchiveHelper
	{
		private static readonly Encoding _gb18030Encoding;

		static ArchiveHelper()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			_gb18030Encoding = Encoding.GetEncoding("GB18030", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
		}

		public unsafe static List<ArchiveRomEntry> GetArchiveRomList(string archivePath)
		{
			//Split the array on the [!|!] delimiter
			byte[] buffer = new byte[100000];
			fixed(byte* ptr = buffer) {
				EmuApi.GetArchiveRomList(archivePath, (IntPtr)ptr, 100000);
			}

			List<List<byte>> filenames = new List<List<byte>>();
			List<byte> filenameBytes = new List<byte>();
			for(int i = 0; i < buffer.Length - 5; i++) {
				if(buffer[i] == 0) {
					break;
				}

				if(buffer[i] == '[' && buffer[i + 1] == '!' && buffer[i + 2] == '|' && buffer[i + 3] == '!' && buffer[i + 4] == ']') {
					if(filenameBytes.Count > 0) {
						filenames.Add(filenameBytes);
					}
					filenameBytes = new List<byte>();
					i += 4;
				} else {
					filenameBytes.Add(buffer[i]);
				}
			}
			if(filenameBytes.Count > 0) {
				filenames.Add(filenameBytes);
			}

			List<ArchiveRomEntry> entries = new List<ArchiveRomEntry>();

			// ZIP entry names may be UTF-8 or an unmarked legacy encoding.
			for(int i = 0; i < filenames.Count; i++) {
				byte[] originalBytes = filenames[i].ToArray();
				string utf8Filename = Encoding.UTF8.GetString(originalBytes);
				byte[] convertedBytes = Encoding.UTF8.GetBytes(utf8Filename);
				bool equal = true;
				if(originalBytes.Length == convertedBytes.Length) {
					for(int j = 0; j < convertedBytes.Length; j++) {
						if(convertedBytes[j] != originalBytes[j]) {
							equal = false;
							break;
						}
					}
				} else {
					equal = false;
				}

				string? legacyFilename = DecodeLegacyFilename(originalBytes);
				if(!equal || (legacyFilename != null && ShouldPreferLegacyFilename(archivePath, utf8Filename, legacyFilename))) {
					entries.Add(new ArchiveRomEntry() { Filename = legacyFilename ?? Encoding.Default.GetString(originalBytes), IsUtf8 = false });
				} else {
					entries.Add(new ArchiveRomEntry() { Filename = utf8Filename, IsUtf8 = true });
				}
			}

			return entries;
		}

		private static string? DecodeLegacyFilename(byte[] originalBytes)
		{
			// A large number of older Chinese ROM archives store entry names as GBK/GB18030
			// without setting ZIP's UTF-8 flag. Encoding.Default is UTF-8 on macOS and Linux,
			// so it would replace those bytes with mojibake even though the ROM remains usable.
			try {
				return _gb18030Encoding.GetString(originalBytes);
			} catch(DecoderFallbackException) {
				return null;
			}
		}

		private static bool ShouldPreferLegacyFilename(string archivePath, string utf8Filename, string legacyFilename)
		{
			string archiveName = Path.GetFileNameWithoutExtension(archivePath);
			string utf8Name = Path.GetFileNameWithoutExtension(utf8Filename);
			string legacyName = Path.GetFileNameWithoutExtension(legacyFilename);

			if(string.Equals(archiveName, utf8Name, StringComparison.OrdinalIgnoreCase)) {
				return false;
			}
			if(string.Equals(archiveName, legacyName, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}

			return GetFilenameQuality(legacyName) > GetFilenameQuality(utf8Name);
		}

		private static int GetFilenameQuality(string filename)
		{
			int score = 0;
			foreach(char chr in filename) {
				UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(chr);
				if(chr >= '\u3400' && chr <= '\u9FFF' || chr >= '\u3040' && chr <= '\u30FF' || chr >= '\uAC00' && chr <= '\uD7AF') {
					score += 2;
				} else if(chr == '\uFFFD' || category == UnicodeCategory.Control || category == UnicodeCategory.PrivateUse) {
					score -= 10;
				} else if(category == UnicodeCategory.ModifierLetter || category == UnicodeCategory.ModifierSymbol || category == UnicodeCategory.NonSpacingMark) {
					score -= 2;
				}
			}
			return score;
		}
	}

	public class ArchiveRomEntry
	{
		public string Filename = "";
		public bool IsUtf8;

		public override string ToString()
		{
			return Filename;
		}
	}
}
