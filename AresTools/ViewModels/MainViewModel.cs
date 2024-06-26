using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AresTools.ViewModels;

public class MainViewModel : ViewModelBase
{
	public static ImmutableDictionary<string, string[]> AresFilesMasks { get; } =
		ImmutableDictionary.CreateRange([new KeyValuePair<string, string[]>("F", [".ares-f"]),
		new("I", [".ares-i"]),
		new("T", ["ares-t"]),
		new("Images", [".bmp", ".png", ".tga"]),
		new("", [""])]);
	public static ImmutableDictionary<string, FilePickerFileType> AresFilesTypes { get; } =
		ImmutableDictionary.CreateRange([new KeyValuePair<string, FilePickerFileType>("F", new("Ares F Files") { Patterns = ["*.ares-f"], AppleUniformTypeIdentifiers = ["UTType.Item"], MimeTypes = ["multipart/mixed"] }),
		new("I", new("Ares I Files") { Patterns = ["*.ares-i"], AppleUniformTypeIdentifiers = ["UTType.Item"], MimeTypes = ["multipart/mixed"] }),
		new("T", new("Ares T Files") { Patterns = ["*.ares-t"], AppleUniformTypeIdentifiers = ["UTType.Item"], MimeTypes = ["multipart/mixed"] }),
		new("Images", new("BMP, PNG and TGA images") { Patterns = ["*.bmp", "*.png", "*.tga"], AppleUniformTypeIdentifiers = ["public.bmp", "public.png", "public.tga"], MimeTypes = ["image/bmp", "image/png", "image/tga"] }),
		new("", FilePickerFileTypes.All)]);

	public static FilePickerFileType GetFilesType(string compressorType) => AresFilesTypes[compressorType];
}
