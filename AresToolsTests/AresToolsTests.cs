global using AresFLib;
global using AresILib;
global using AresTLib;
global using Corlib.NStar;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using System;
global using System.IO;
global using System.Text;
global using UnsafeFunctions;
global using G = System.Collections.Generic;
global using static AresFLib.Global;
global using static AresILib.Global;
global using static AresTLib.Global;
global using static Corlib.NStar.Extents;
global using static System.Math;
global using static UnsafeFunctions.Global;
using System.Text.RegularExpressions;

namespace AresTTests;

[TestClass]
public partial class DecompressionTests
{
	private readonly string[] files = Directory.GetFiles(ExcludeBinRegex().Replace(AppDomain.CurrentDomain.BaseDirectory, ""), "*.txt", SearchOption.TopDirectoryOnly);
	private readonly string[] images = Directory.GetFiles(ExcludeBinRegex().Replace(AppDomain.CurrentDomain.BaseDirectory, ""), "*.bmp", SearchOption.TopDirectoryOnly);

	[TestMethod]
	public void TestHF()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsF = UsedMethodsF.CS1 | UsedMethodsF.HF1;
		PresentMethodsI = UsedMethodsI.CS1 | UsedMethodsI.HF1;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			MainClassF.MainThread(file, temp, MainClassF.Compress, false);
			MainClassF.MainThread(temp, temp2, MainClassF.Decompress, false);
		}
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestLZ()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsF = UsedMethodsF.CS1 | UsedMethodsF.LZ1;
		PresentMethodsI = UsedMethodsI.CS1 | UsedMethodsI.LZ1;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			MainClassF.MainThread(file, temp, MainClassF.Compress, false);
			MainClassF.MainThread(temp, temp2, MainClassF.Decompress, false);
		}
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHF_LZ()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsF = UsedMethodsF.CS1 | UsedMethodsF.LZ1 | UsedMethodsF.HF1;
		PresentMethodsI = UsedMethodsI.CS1 | UsedMethodsI.HF1 | UsedMethodsI.LZ1;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			MainClassF.MainThread(file, temp, MainClassF.Compress, false);
			MainClassF.MainThread(temp, temp2, MainClassF.Decompress, false);
		}
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHF_Delta()
	{
		PresentMethodsI = UsedMethodsI.CS2 | UsedMethodsI.HF2;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestLZ_Delta()
	{
		PresentMethodsI = UsedMethodsI.CS2 | UsedMethodsI.LZ2;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHF_LZ_Delta()
	{
		PresentMethodsI = UsedMethodsI.CS2 | UsedMethodsI.HF2 | UsedMethodsI.LZ2;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestAHF()
	{
		PresentMethodsI = UsedMethodsI.CS1 | UsedMethodsI.HF1 | UsedMethodsI.AHF;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestAHF_LZ()
	{
		PresentMethodsI = UsedMethodsI.CS1 | UsedMethodsI.HF1 | UsedMethodsI.LZ1 | UsedMethodsI.AHF;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestAHF_Delta()
	{
		PresentMethodsI = UsedMethodsI.CS2 | UsedMethodsI.HF2 | UsedMethodsI.AHF;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestAHF_LZ_Delta()
	{
		PresentMethodsI = UsedMethodsI.CS2 | UsedMethodsI.HF2 | UsedMethodsI.LZ2 | UsedMethodsI.AHF;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var image in images)
		{
			MainClassI.MainThread(image, temp, MainClassI.Compress, false);
			MainClassI.MainThread(temp, temp2, MainClassI.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHFW()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsT = UsedMethodsT.CS1;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			try
			{
				MainClassT.MainThread(file, temp, MainClassT.Compress, false);
				MainClassT.MainThread(temp, temp2, MainClassT.Decompress, false);
			}
			catch (EncoderFallbackException)
			{ 
			}
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHFW_LZ()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsT = UsedMethodsT.CS1 | UsedMethodsT.LZ1;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			try
			{
				MainClassT.MainThread(file, temp, MainClassT.Compress, false);
				MainClassT.MainThread(temp, temp2, MainClassT.Decompress, false);
			}
			catch (EncoderFallbackException)
			{
			}
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHFW_COMB()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsT = UsedMethodsT.CS1 | UsedMethodsT.COMB1;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			try
			{
				MainClassT.MainThread(file, temp, MainClassT.Compress, false);
				MainClassT.MainThread(temp, temp2, MainClassT.Decompress, false);
			}
			catch (EncoderFallbackException)
			{
			}
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHFW_LZ_COMB()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsT = UsedMethodsT.CS1 | UsedMethodsT.LZ1 | UsedMethodsT.COMB1;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			try
			{
				MainClassT.MainThread(file, temp, MainClassT.Compress, false);
				MainClassT.MainThread(temp, temp2, MainClassT.Decompress, false);
			}
			catch (EncoderFallbackException)
			{
			}
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHF_BWT()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsF = UsedMethodsF.CS2;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			MainClassF.MainThread(file, temp, MainClassF.Compress, false);
			MainClassF.MainThread(temp, temp2, MainClassF.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestAHF_BWT()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsF = UsedMethodsF.CS2 | UsedMethodsF.AHF2;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			MainClassF.MainThread(file, temp, MainClassF.Compress, false);
			MainClassF.MainThread(temp, temp2, MainClassF.Decompress, false);
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHFW_BWT()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsT = UsedMethodsT.CS2;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			try
			{
				MainClassT.MainThread(file, temp, MainClassT.Compress, false);
				MainClassT.MainThread(temp, temp2, MainClassT.Decompress, false);
			}
			catch (EncoderFallbackException)
			{
			}
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[TestMethod]
	public void TestHFW_BWT_LZ()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		PresentMethodsT = UsedMethodsT.CS2 | UsedMethodsT.LZ2;
		var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-compressed.tmp";
		var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + Environment.ProcessId + "-unpacked.tmp";
		foreach (var file in files)
		{
			try
			{
				MainClassT.MainThread(file, temp, MainClassT.Compress, false);
				MainClassT.MainThread(temp, temp2, MainClassT.Decompress, false);
			}
			catch (EncoderFallbackException)
			{
			}
		}
		File.Delete(temp);
		File.Delete(temp2);
	}

	[GeneratedRegex(@"(?<=\\)bin\\.*")]
	private static partial Regex ExcludeBinRegex();
}
