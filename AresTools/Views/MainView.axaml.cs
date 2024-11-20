using AresALib;
using AresTools.ViewModels;
using AresVLib;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tmds.Utils;

namespace AresTools.Views;

public partial class MainView : UserControl
{
	private bool emptyFileName;
	private enum ClientIndex
	{
		Files,
		Images,
		Text,
		Zipping,
	};
	private enum OperationType
	{
		Opening,
		Compression,
		Unpacking,
		Recompression,
	};

	private Thread thread = new(() => { });
	private string filename = "";
	private OperationType operation_type;
	private bool continue_;
	private bool isWorking, multiSelect;
	private static UsedMethodsF usedMethodsF = UsedMethodsF.CS1 | UsedMethodsF.AHF1;
	private static UsedMethodsI usedMethodsI = UsedMethodsI.CS2 | UsedMethodsI.LZ2 | UsedMethodsI.HF2 | UsedMethodsI.AHF;
	private static UsedMethodsT usedMethodsT = UsedMethodsT.CS1 | UsedMethodsT.LZ1;
	private static UsedMethodsZ usedMethodsZ = UsedMethodsZ.ArchiveThenCompress | UsedMethodsZ.ApplyF | UsedMethodsZ.ApplyI;
	private static int usedSizesF = 68, usedSizesT = 4;
#if !DEBUG
	private DateTime compressionStart;
#endif

	private readonly string[] args;
	private TcpListener tcpListener; //монитор подключений TCP клиентов
	private Thread listenThread; //создание потока

	private readonly List<TcpClient> clients = []; //список клиентских подключений
	private readonly List<NetworkStream> netStream = []; //список потока данных
	private readonly int port = 11000;
	private Process executor;
	private int executorId;
#if RELEASE
	private readonly Random random = new();
#endif

	public MainView()
	{
		ThreadsLayout = new Grid[ProgressBarGroups];
		TextBlockSubtotal = new TextBlock[ProgressBarGroups];
		TextBlockCurrent = new TextBlock[ProgressBarGroups];
		TextBlockStatus = new TextBlock[ProgressBarGroups];
		ContentViewSubtotal = new ContentControl[ProgressBarGroups];
		ContentViewCurrent = new ContentControl[ProgressBarGroups];
		ContentViewStatus = new ContentControl[ProgressBarGroups];
		ProgressBarSubtotal = new ProgressBar[ProgressBarGroups];
		ProgressBarCurrent = new ProgressBar[ProgressBarGroups];
		ProgressBarStatus = new ProgressBar[ProgressBarGroups];
		tcpListener = default!;
		listenThread = default!;
		executor = default!;
		args = Environment.GetCommandLineArgs();
		if (args.Length >= 1)
			args = args[1..];
		if (ExecFunction.IsExecFunctionCommand(args))
		{
			ExecFunction.Program.Main(args);
			Environment.Exit(0);
		}
		InitializeComponent();
		for (var i = 0; i < ProgressBarGroups; i++)
		{
			ThreadsLayout[i] = new();
			GridThreadsProgressBars.Children.Add(ThreadsLayout[i]);
			Grid.SetColumn(ThreadsLayout[i], i / ProgressBarVGroups);
			Grid.SetRow(ThreadsLayout[i], i % ProgressBarVGroups);
			ThreadsLayout[i].ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Auto)];
			ThreadsLayout[i].RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)];
			TextBlockSubtotal[i] = new();
			ThreadsLayout[i].Children.Add(TextBlockSubtotal[i]);
			Grid.SetColumn(TextBlockSubtotal[i], 0);
			Grid.SetRow(TextBlockSubtotal[i], 0);
			TextBlockSubtotal[i].FontSize = 12;
			TextBlockSubtotal[i].Foreground = new SolidColorBrush(new Color(255, 0, 0, 0));
			TextBlockSubtotal[i].Text = "Subtotal" + (i + 1).ToString();
			ContentViewSubtotal[i] = new();
			ThreadsLayout[i].Children.Add(ContentViewSubtotal[i]);
			Grid.SetColumn(ContentViewSubtotal[i], 1);
			Grid.SetRow(ContentViewSubtotal[i], 0);
			ContentViewSubtotal[i].MinHeight = 16;
			ProgressBarSubtotal[i] = new();
			ContentViewSubtotal[i].Content = ProgressBarSubtotal[i];
			ProgressBarSubtotal[i].Background = new SolidColorBrush(new Color(255, 255, 191, 223));
			ProgressBarSubtotal[i].Maximum = 1;
			ProgressBarSubtotal[i].MinHeight = 16;
			ProgressBarSubtotal[i].MinWidth = 180;
			ProgressBarSubtotal[i].Value = 0.25;
			ProgressBarSubtotal[i].Foreground = new SolidColorBrush(new Color(255, 191, 128, 128));
			TextBlockCurrent[i] = new();
			ThreadsLayout[i].Children.Add(TextBlockCurrent[i]);
			Grid.SetColumn(TextBlockCurrent[i], 0);
			Grid.SetRow(TextBlockCurrent[i], 1);
			TextBlockCurrent[i].FontSize = 12;
			TextBlockCurrent[i].Foreground = new SolidColorBrush(new Color(255, 0, 0, 0));
			TextBlockCurrent[i].Text = "Current" + (i + 1).ToString();
			ContentViewCurrent[i] = new();
			ThreadsLayout[i].Children.Add(ContentViewCurrent[i]);
			Grid.SetColumn(ContentViewCurrent[i], 1);
			Grid.SetRow(ContentViewCurrent[i], 1);
			ContentViewCurrent[i].MinHeight = 16;
			ProgressBarCurrent[i] = new();
			ContentViewCurrent[i].Content = ProgressBarCurrent[i];
			ProgressBarCurrent[i].Background = new SolidColorBrush(new Color(255, 128, 255, 191));
			ProgressBarCurrent[i].Maximum = 1;
			ProgressBarCurrent[i].MinHeight = 16;
			ProgressBarCurrent[i].MinWidth = 180;
			ProgressBarCurrent[i].Value = 0.5;
			ProgressBarCurrent[i].Foreground = new SolidColorBrush(new Color(255, 64, 128, 64));
			TextBlockStatus[i] = new();
			ThreadsLayout[i].Children.Add(TextBlockStatus[i]);
			Grid.SetColumn(TextBlockStatus[i], 0);
			Grid.SetRow(TextBlockStatus[i], 2);
			TextBlockStatus[i].FontSize = 12;
			TextBlockStatus[i].Foreground = new SolidColorBrush(new Color(255, 0, 0, 0));
			TextBlockStatus[i].Text = "Status" + (i + 1).ToString();
			ContentViewStatus[i] = new();
			ThreadsLayout[i].Children.Add(ContentViewStatus[i]);
			Grid.SetColumn(ContentViewStatus[i], 1);
			Grid.SetRow(ContentViewStatus[i], 2);
			ContentViewStatus[i].Background = new SolidColorBrush(new Color(255, 191, 191, 255));
			ContentViewStatus[i].MinHeight = 16;
			ProgressBarStatus[i] = new();
			ContentViewStatus[i].Content = ProgressBarStatus[i];
			ProgressBarStatus[i].Background = new SolidColorBrush(new Color(255, 191, 191, 255));
			ProgressBarStatus[i].Maximum = 1;
			ProgressBarStatus[i].MinHeight = 16;
			ProgressBarStatus[i].MinWidth = 180;
			ProgressBarStatus[i].Value = 0.75;
			ProgressBarStatus[i].Foreground = new SolidColorBrush(new Color(255, 128, 128, 191));
		}
#if RELEASE
		port = random.Next(1024, 65536);
		StartExecutor();
#endif
	}

	private void UserControl_Loaded(object? sender, RoutedEventArgs e)
	{
		//var temp = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + "/Ares-" + Environment.ProcessId + "-compressed.tmp";
		//var temp2 = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + "/Ares-" + Environment.ProcessId + "-unpacked.tmp";
		//MainClassV.MainThread(@"D:\User\Pictures\01-05-2024 155324.mp4", temp, MainClassV.Compress, false);
		//TabView.CurrentItem = TabItemText;
		ComboQuickSetupF.SelectedIndex = 1;
		filename = args.Length == 0 ? "" : args[0];
		System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Normal;
		try
		{
			tcpListener = new(IPAddress.Loopback, port);
			listenThread = new(ListenThread) { Name = "Ожидание подключения клиентов", IsBackground = true };
			listenThread.Start(); //старт потока
			if (args.Length != 0 && args[0] != "")
			{
				System.Threading.Thread.Sleep(MillisecondsPerSecond * 5);
				operation_type = OperationType.Opening;
				thread = new Thread(() => Thread(args[0][^1] switch { 'F' or 'f' => ClientIndex.Files, 'I' or 'i' =>
					ClientIndex.Images, 'T' or 't' => ClientIndex.Text, 'Z' or 'z' => ClientIndex.Zipping, _ => 0 },
					args[0][^1..].ToUpperInvariant(), true)) { Name = "Основной фоновый поток", IsBackground = true };
				thread.Start();
			}
		}
		catch
		{
			Disconnect();
		}
	}

	private void ListenThread()
	{
		tcpListener.Start();
		while (true)
		{
			clients.Add(tcpListener.AcceptTcpClient()); //подключение пользователя
			netStream.Add(clients[^1].GetStream()); //обьект для получения данных
			Thread clientThread = new(new ParameterizedThreadStart(ClientReceive)) { Name = "Соединение с клиентом #" + clients.Length.ToString() };
			clientThread.Start(clients.Length - 1);
			clientThread.IsBackground = true;
		}
	}

	private void ClientReceive(object? ID)
	{
		var client = (int?)ID ?? 0;
		byte[] receive;
		var receiveLen = GC.AllocateUninitializedArray<byte>(4);
		while (true)
		{
			try
			{
				netStream[client].ReadExactly(receiveLen);
				receive = GC.AllocateUninitializedArray<byte>(BitConverter.ToInt32(receiveLen));
				netStream[client].ReadExactly(receive);
				WorkUpReceiveMessage((ClientIndex)client, receive);
			}
			catch
			{
				clients.Clear();
				netStream.Clear();
				break;
			}
		}
	}

	private void Disconnect()
	{
		tcpListener.Stop(); //остановка чтения
		for (var i = 0; i < clients.Length; i++)
		{
			clients[i].Close(); //отключение клиента
			netStream[i].Close(); //отключение потока
		}
		Environment.Exit(0); //завершение процесса
	}

	private async void ExecutorExited(object? sender, EventArgs? e)
	{
		var tempFilename = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + "/Ares-" + executorId + ".tmp";
		try
		{
			if (File.Exists(tempFilename))
				File.Delete(tempFilename);
		}
		catch
		{
		}
		var button = await Dispatcher.UIThread.InvokeAsync(async () =>
			await MessageBoxManager.GetMessageBoxStandard("", "Произошла серьезная ошибка в рабочем модуле Ares и он аварийно завершился. Нажмите ОК, чтобы перезапустить его, или Отмена, чтобы выйти из приложения.", MsBox.Avalonia.Enums.ButtonEnum.OkCancel)
			.ShowAsPopupAsync(this) == MsBox.Avalonia.Enums.ButtonResult.Ok);
		if (!button)
			Environment.Exit(0);
		SetProgressBarsFull();
		StartExecutor();
	}

	private void StartExecutor()
	{
		executor = ExecFunction.Start(Executor.Main, [port.ToString(), Environment.ProcessId.ToString()]);
		executorId = executor.Id;
		executor.EnableRaisingEvents = true;
		executor.Exited += ExecutorExited;
	}

	private void SendMessageToClient(ClientIndex index, byte[] toSend)
	{
		var index2 = (int)index;
		var toSendLen = BitConverter.GetBytes(toSend.Length);
		netStream[index2].Write(toSendLen);
		netStream[index2].Flush(); //удаление данных из потока
		netStream[index2].Write(toSend);
		netStream[index2].Flush(); //удаление данных из потока
	}

	private static async void SetValue(ProgressBar pb, double new_value) => await Dispatcher.UIThread.InvokeAsync(() => SetValueInternal(pb, new_value));

	private static void SetValueInternal(ProgressBar pb, double new_value)
	{
		if (new_value < 0)
			pb.Value = 1;
		else
			pb.Value = new_value;
	}

	private async void WorkUpReceiveMessage(ClientIndex client, byte[] message)
	{
		try
		{
			if (message.Length == 0)
				return;
			if (message[0] == 0 && message.Length == ProgressBarGroups * 24 + 17 && isWorking)
				UpdateProgressBars(message);
			else if (message[0] == 1 && client == ClientIndex.Images && message.Length == 2)
				await OpenFile(client, message[1]);
			else if (message[0] == 1 && client != ClientIndex.Images && message.Length == 1)
				await OpenFile(client, 0);
			else if (message[0] == 2)
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось " + (operation_type is OperationType.Compression or OperationType.Recompression ? "сжать" : "распаковать") + " файл.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			else if (message[0] == 3)
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Файл сжат, но распаковка не удалась.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			if (message[0] is not 0)
			{
				SetProgressBarsFull();
			}
		}
		catch
		{
			if (message.Length != 0 && message[0] is not 0)
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Произошла серьезная ошибка при попытке выполнить действие. Повторите попытку позже. Если проблема не исчезает, обратитесь к разработчикам приложения.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
		}
	}

	private void UpdateProgressBars(byte[] message)
	{
		SetValue(ProgressBarSupertotal, (double)BitConverter.ToInt32(message.AsSpan(1, 4)) / BitConverter.ToInt32(message.AsSpan(5, 4)));
		SetValue(ProgressBarTotal, (double)BitConverter.ToInt32(message.AsSpan(9, 4)) / BitConverter.ToInt32(message.AsSpan(13, 4)));
		for (var i = 0; i < ProgressBarGroups; i++)
		{
			SetValue(ProgressBarSubtotal[i], (double)BitConverter.ToInt32(message.AsSpan(i * 24 + 17, 4)) / BitConverter.ToInt32(message.AsSpan(i * 24 + 21, 4)));
			SetValue(ProgressBarCurrent[i], (double)BitConverter.ToInt32(message.AsSpan(i * 24 + 25, 4)) / BitConverter.ToInt32(message.AsSpan(i * 24 + 29, 4)));
			SetValue(ProgressBarStatus[i], (double)BitConverter.ToInt32(message.AsSpan(i * 24 + 33, 4)) / BitConverter.ToInt32(message.AsSpan(i * 24 + 37, 4)));
		}
	}

	private async Task OpenFile(ClientIndex client, byte transparency)
	{
		var timeString = "";
#if !DEBUG
				var elapsed = DateTime.Now - compressionStart;
				timeString += " (" + (elapsed.Days == 0 ? "" : $"{elapsed.Days:D}:") + (elapsed.Days == 0 && elapsed.Hours == 0 ? "" : $"{elapsed.Hours:D2}:") + $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})";
#endif
		if (operation_type == OperationType.Opening)
		{
			var path = Path.Combine(Environment.GetEnvironmentVariable("temp") ?? throw new IOException(), Path.GetFileNameWithoutExtension(filename) + (client != ClientIndex.Images ? "" : transparency != 0 ? ".tga" : ".bmp"));
			using Process process = new();
			process.StartInfo.FileName = "explorer";
			process.StartInfo.Arguments = "\"" + path + "\"";
			process.Start();
			System.Threading.Thread.Sleep(MillisecondsPerSecond);
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Файл успешно распакован" + timeString + "!", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			process.WaitForExit();
			try
			{
				if (client != ClientIndex.Zipping)
					File.Delete(path);
			}
			catch
			{
			}
		}
		else
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Файл успешно " + (operation_type is OperationType.Compression or OperationType.Recompression ? "сжат" : "распакован") + timeString + "!", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
	}

	private void ThreadF() => Thread(ClientIndex.Files, "F", false);

	private void ThreadI() => Thread(ClientIndex.Images, "I", false);

	private void ThreadT() => Thread(ClientIndex.Text, "T", false);

	private void ThreadZ() => Thread(ClientIndex.Zipping, "Z", false);

	private async void Thread(ClientIndex client, string filter, bool startImmediate)
	{
		if (isWorking)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось запустить сжатие/распаковку, так как существует другой активный процесс.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		multiSelect = false;
		switch (operation_type)
		{
			case OperationType.Opening:
			if (!startImmediate && ProcessStartup(filter).Result is bool)
				return;
			try
			{
				if (startImmediate)
					InitProgressBars();
			}
			catch
			{
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось распаковать файл.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
				break;
			}
			await StartProcess(client, true);
			return;
			case OperationType.Compression:
			if (client == ClientIndex.Zipping)
				multiSelect = true;
			if (ProcessStartup(client == ClientIndex.Images ? "Images" : "").Result is bool || !await ValidateImageAsync(client))
				break;
			if (client == ClientIndex.Files && usedMethodsF.HasFlag(UsedMethodsF.CS4) && (usedSizesF & 0xF) > 4 && new FileInfo(filename).Length > 16000000)
			{
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Слишком большой размер фрагмента для PPM. Попробуйте уменьшить размер фрагмента, отключите PPM или возьмите меньший файл.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
				break;
			}
			await StartProcess(client, false);
			return;
			case OperationType.Unpacking:
			if (ProcessStartup(filter).Result is bool)
				return;
			await StartProcess(client, true);
			return;
			case OperationType.Recompression:
			if (ProcessStartup(filter).Result is bool)
				return;
			await StartProcess(client, false);
			return;
		}
		SetButtonsEnabled(true);
	}

	private async Task<bool?> ProcessStartup(string filter)
	{
		if (!continue_)
		{
			await SetOFDPars(filter);
			if (emptyFileName == true)
				return true;
			InitProgressBars();
		}
		return null;
	}

	private void InitProgressBars()
	{
		SetButtonsEnabled(false);
		SetValue(ProgressBarSupertotal, 0);
		SetValue(ProgressBarTotal, 0);
		for (var i = 0; i < ProgressBarGroups; i++)
		{
			SetValue(ProgressBarSubtotal[i], 0);
			SetValue(ProgressBarCurrent[i], 0);
			SetValue(ProgressBarStatus[i], 0);
		}
	}

	private void SetProgressBarsFull()
	{
		SetValue(ProgressBarSupertotal, -1);
		SetValue(ProgressBarTotal, -1);
		for (var i = 0; i < ProgressBarGroups; i++)
		{
			SetValue(ProgressBarSubtotal[i], -1);
			SetValue(ProgressBarCurrent[i], -1);
			SetValue(ProgressBarStatus[i], -1);
		}
		SetButtonsEnabled(true);
		continue_ = false;
		isWorking = false;
	}

	private async Task SetOFDPars(string filter)
	{
		emptyFileName = false;
		await SetOFDParsInternal(filter);
	}

	private async Task<int> SetOFDParsInternal(string filter)
	{
		var fileResult = await Dispatcher.UIThread.InvokeAsync(async () =>
			await TopLevel.GetTopLevel(this)?.StorageProvider.OpenFilePickerAsync(new()
			{
				Title = $"Select the *{string.Join(" or *", MainViewModel.AresFilesMasks[filter])} file",
				FileTypeFilter = [MainViewModel.GetFilesType(filter)], AllowMultiple = multiSelect
			})!);
		if (fileResult?.Count == 0)
			filename = "";
		else if (multiSelect)
			filename = string.Join("<>", fileResult?.Take(ValuesInByte - 1).Convert(x => x?.TryGetLocalPath()).Filter(x => !string.IsNullOrEmpty(x)).ToArray() ?? []);
		else
			filename = fileResult?[0]?.TryGetLocalPath() ?? "";
		if (filename == "" || !MainViewModel.AresFilesMasks[filter].Any(x => filename.EndsWith(x)))
			emptyFileName = true;
		return 0;
	}

	private async void SetButtonsEnabled(bool Enabled)
	{
		emptyFileName = false;
		await Dispatcher.UIThread.InvokeAsync(() =>
				SetButtonsEnabledInternal(Enabled));
	}

	private int SetButtonsEnabledInternal(bool isEnabled)
	{
		ButtonOpenF.IsEnabled = isEnabled;
		ButtonOpenForCompressionF.IsEnabled = isEnabled;
		ButtonOpenForUnpackingF.IsEnabled = isEnabled;
		ButtonOpenForRecompressionF.IsEnabled = isEnabled;
		ComboQuickSetupF.IsEnabled = isEnabled;
		GridSettingsF.IsEnabled = isEnabled;
		ButtonOpenI.IsEnabled = isEnabled;
		ButtonOpenForCompressionI.IsEnabled = isEnabled;
		ButtonOpenForUnpackingI.IsEnabled = isEnabled;
		ButtonOpenForRecompressionI.IsEnabled = isEnabled;
		ComboQuickSetupI.IsEnabled = isEnabled;
		GridSettingsI.IsEnabled = isEnabled;
		ButtonOpenT.IsEnabled = isEnabled;
		ButtonOpenForCompressionT.IsEnabled = isEnabled;
		ButtonOpenForUnpackingT.IsEnabled = isEnabled;
		ButtonOpenForRecompressionT.IsEnabled = isEnabled;
		ComboQuickSetupT.IsEnabled = isEnabled;
		GridSettingsT.IsEnabled = isEnabled;
		ButtonOpenArchive.IsEnabled = isEnabled;
		ButtonCreateArchive.IsEnabled = isEnabled;
		ButtonUnpackArchive.IsEnabled = isEnabled;
		ButtonRepackArchive.IsEnabled = isEnabled;
		StackPanelArchiving.IsEnabled = isEnabled;
		return 0;
	}

	private async Task StartProcess(ClientIndex client, bool unpack)
	{
		try
		{
			isWorking = true;
			SendMessageToClient(client, [(byte)(operation_type + 2), .. Encoding.UTF8.GetBytes(filename)]);
#if !DEBUG
				compressionStart = DateTime.Now;
#endif
		}
		catch (OperationCanceledException)
		{
		}
		catch
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось " + (unpack ? "распаковать" : "сжать") + " файл.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
		}
	}

	private async Task<bool> ValidateImageAsync(ClientIndex client)
	{
		if (client != ClientIndex.Images)
			return true;
		try
		{
			using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filename);
			if (image == null)
			{
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось прочитать изображение.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
				return false;
			}
			if (image.Width < 16 || image.Height < 16 || image.Width * image.Height > 0x300000)
			{
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Обе стороны изображения должны быть от 16 пикселов, а площадь - не более 3 мегапикселов.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
				return false;
			}
#if RELEASE
			if (usedMethodsI.HasFlag(UsedMethodsI.CS2) && usedMethodsI.HasFlag(UsedMethodsI.LZ2) && !usedMethodsI.HasFlag(UsedMethodsI.AHF) && new Chain(image.Width * image.Height).Any((_, index) => image[index % image.Width, index / image.Width].A != ValuesInByte - 1))
			{
				if (await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Сжатие этого изображения таким наборов методов в разработке. Переключиться на максимально близкий набор методов и сжать изображение?", MsBox.Avalonia.Enums.ButtonEnum.YesNo).ShowAsPopupAsync(this))
					== MsBox.Avalonia.Enums.ButtonResult.Yes)
				{
					await Dispatcher.UIThread.InvokeAsync(() => RadioButtonAHFI.IsChecked = true);
					System.Threading.Thread.Sleep(MillisecondsPerSecond);
					return true;
				}
				return false;
			}
#endif
			return true;
		}
		catch
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось прочитать изображение.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return false;
		}
	}

	private void ButtonOpenF_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Opening;
		thread = new Thread(ThreadF) { IsBackground = true, Name = "Поток открытия" };
		thread.Start();
	}

	private void ButtonOpenForCompressionF_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Compression;
		thread = new Thread(ThreadF) { IsBackground = true, Name = "Поток сжатия" };
		thread.Start();
	}

	private void ButtonOpenForUnpackingF_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Unpacking;
		thread = new Thread(ThreadF) { IsBackground = true, Name = "Поток распаковки" };
		thread.Start();
	}

	private void ButtonOpenForRecompressionF_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Recompression;
		thread = new Thread(ThreadF) { IsBackground = true, Name = "Поток пересжатия" };
		thread.Start();
	}

	private void ButtonStop_Click(object? sender, RoutedEventArgs e)
	{
		executor?.Kill();
		SetValue(ProgressBarSupertotal, -1);
		SetValue(ProgressBarTotal, -1);
		for (var i = 0; i < ProgressBarGroups; i++)
		{
			SetValue(ProgressBarSubtotal[i], -1);
			SetValue(ProgressBarCurrent[i], -1);
			SetValue(ProgressBarStatus[i], -1);
		}
		SetButtonsEnabled(true);
		continue_ = false;
	}

	private void ComboQuickSetupF_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboQuickSetupF == null)
			return;
		var selectedIndex = ComboQuickSetupF.SelectedIndex;
		CheckBoxCS1F.IsChecked = selectedIndex >= 0;
		CheckBoxAHF1F.IsChecked = selectedIndex >= 1;
		CheckBoxCS2F.IsChecked = selectedIndex >= 2;
		CheckBoxLZ2F.IsChecked = selectedIndex >= 3;
		CheckBoxHF2F.IsChecked = selectedIndex >= 2;
		CheckBoxCS4F.IsChecked = selectedIndex >= 4;
		SendUsedMethods();
	}

	private void CheckBoxCS1F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS1F == null)
			return;
		usedMethodsF ^= UsedMethodsF.CS1;
		SendUsedMethods();
	}

	private void CheckBoxCS2F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS2F == null)
			return;
		usedMethodsF ^= UsedMethodsF.CS2;
		SendUsedMethods();
	}

	private void CheckBoxCS3F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
#if RELEASE
		if (CheckBoxCS3F != null && (CheckBoxCS3F.IsChecked ?? false))
		{
			CheckBoxCS3F.IsChecked = false;
			Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Этот метод находится в разработке.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
		}
#else
		if (CheckBoxCS3F == null)
			return;
		usedMethodsF ^= UsedMethodsF.CS3;
		SendUsedMethods();
#endif
	}

	private void CheckBoxCS4F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS4F == null)
			return;
		usedMethodsF ^= UsedMethodsF.CS4;
		SendUsedMethods();
	}

	private void CheckBoxCS5F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
#if RELEASE
		if (CheckBoxCS5F != null && (CheckBoxCS5F.IsChecked ?? false))
		{
			CheckBoxCS5F.IsChecked = false;
			Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Этот метод находится в разработке.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
		}
#else
		if (CheckBoxCS5F == null)
			return;
		usedMethodsF ^= UsedMethodsF.CS5;
		SendUsedMethods();
#endif
	}

	private void CheckBoxAHF1F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxAHF1F == null)
			return;
		usedMethodsF ^= UsedMethodsF.AHF1;
		SendUsedMethods();
	}

	private void CheckBoxLZ2F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxLZ2F == null)
			return;
		usedMethodsF ^= UsedMethodsF.LZ2;
		if (CheckBoxHF2F != null && !(CheckBoxHF2F.IsChecked ?? false) && CheckBoxLZ2F != null && !(CheckBoxLZ2F.IsChecked ?? false))
			CheckBoxHF2F.IsChecked = true;
		SendUsedMethods();
	}

	private void CheckBoxHF2F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxHF2F == null)
			return;
		usedMethodsF ^= UsedMethodsF.HF2;
		if (CheckBoxHF2F != null && !(CheckBoxHF2F.IsChecked ?? false) && CheckBoxLZ2F != null && !(CheckBoxLZ2F.IsChecked ?? false))
			CheckBoxLZ2F.IsChecked = true;
		SendUsedMethods();
	}

	private void ComboFragmentLengthF_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboFragmentLengthF == null)
			return;
		usedSizesF = (usedSizesF & ~0xF) | ComboFragmentLengthF.SelectedIndex;
		ComboFragmentLengthT.SelectedIndex = ComboFragmentLengthF.SelectedIndex;
		SendUsedSizes();
	}

	private void ComboBWTLengthF_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboBWTLengthF == null)
			return;
		usedSizesF = (usedSizesF & ~0x70) | ComboBWTLengthF.SelectedIndex << 4;
		ComboFragmentLengthF.SelectedIndex = ComboFragmentLengthT.SelectedIndex;
		SendUsedSizes();
	}

	private void ComboLZDictionaryF_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboLZDictionaryF == null)
			return;
		usedSizesF = (usedSizesF & ~0x380) | ComboLZDictionaryF.SelectedIndex << 7;
		SendUsedSizes();
	}

	private void ButtonOpenI_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Opening;
		thread = new Thread(ThreadI) { IsBackground = true, Name = "Поток открытия" };
		thread.Start();
	}

	private void ButtonOpenForCompressionI_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Compression;
		thread = new Thread(ThreadI) { IsBackground = true, Name = "Поток сжатия" };
		thread.Start();
	}

	private void ButtonOpenForUnpackingI_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Unpacking;
		thread = new Thread(ThreadI) { IsBackground = true, Name = "Поток распаковки" };
		thread.Start();
	}

	private void ButtonOpenForRecompressionI_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Recompression;
		thread = new Thread(ThreadI) { IsBackground = true, Name = "Поток пересжатия" };
		thread.Start();
	}

	private void ComboQuickSetupI_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboQuickSetupF == null)
			return;
		var selectedIndex = ComboQuickSetupI.SelectedIndex;
		CheckBoxCS1I.IsChecked = selectedIndex >= 2;
		CheckBoxHF1I.IsChecked = selectedIndex >= 2;
		CheckBoxLZ1I.IsChecked = selectedIndex >= 2;
		CheckBoxCS2I.IsChecked = selectedIndex >= 0;
		CheckBoxHF2I.IsChecked = selectedIndex >= 0;
		CheckBoxLZ2I.IsChecked = selectedIndex >= 1;
		CheckBoxCS3I.IsChecked = selectedIndex >= 4;
		CheckBoxCS4I.IsChecked = selectedIndex >= 3;
		RadioButtonAHFI.IsChecked = true;
		SendUsedMethods();
	}

	private void CheckBoxCS1I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS1I == null)
			return;
		usedMethodsI ^= UsedMethodsI.CS1;
		SendUsedMethods();
	}

	private void CheckBoxCS2I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS2I == null)
			return;
		usedMethodsI ^= UsedMethodsI.CS2;
		SendUsedMethods();
	}

	private void CheckBoxCS3I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS3I == null)
			return;
		usedMethodsI ^= UsedMethodsI.CS3;
		SendUsedMethods();
	}

	private void CheckBoxCS4I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS4I == null)
			return;
		usedMethodsI ^= UsedMethodsI.CS4;
		SendUsedMethods();
	}

	private void CheckBoxCS5I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
#if RELEASE
		if (CheckBoxCS5I != null && (CheckBoxCS5I.IsChecked ?? false))
		{
			CheckBoxCS5I.IsChecked = false;
			Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Этот метод находится в разработке.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
		}
#else
		if (CheckBoxCS5I == null)
			return;
		usedMethodsI ^= UsedMethodsI.CS5;
		SendUsedMethods();
#endif
	}

	private void CheckBoxCS6I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
#if RELEASE
		if (CheckBoxCS6I != null && (CheckBoxCS6I.IsChecked ?? false))
		{
			CheckBoxCS6I.IsChecked = false;
			Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Этот метод находится в разработке.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
		}
#else
		if (CheckBoxCS6I == null)
			return;
		usedMethodsI ^= UsedMethodsI.CS6;
		SendUsedMethods();
#endif
	}

	private void CheckBoxHF1I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxHF1I == null)
			return;
		usedMethodsI ^= UsedMethodsI.HF1;
		SendUsedMethods();
	}

	private void CheckBoxLZ1I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxLZ1I == null)
			return;
		usedMethodsI ^= UsedMethodsI.LZ1;
		SendUsedMethods();
	}

	private void CheckBoxHF2I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxHF2I == null)
			return;
		usedMethodsI ^= UsedMethodsI.HF2;
		SendUsedMethods();
	}

	private void CheckBoxLZ2I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxLZ2I == null)
			return;
		usedMethodsI ^= UsedMethodsI.LZ2;
		SendUsedMethods();
	}

	private void CheckBoxHF5I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxHF5I == null)
			return;
		usedMethodsI ^= UsedMethodsI.HF5;
		SendUsedMethods();
	}

	private void CheckBoxLZ5I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxLZ5I == null)
			return;
		usedMethodsI ^= UsedMethodsI.LZ5;
		SendUsedMethods();
	}

	private void CheckBoxLZ6I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxLZ6I == null)
			return;
		usedMethodsI ^= UsedMethodsI.LZ6;
		SendUsedMethods();
	}

	private void RadioButtonAHFI_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (RadioButtonAHFI == null)
			return;
		usedMethodsI ^= UsedMethodsI.AHF;
		SendUsedMethods();
	}

	private void ButtonOpenT_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Opening;
		thread = new Thread(ThreadT) { IsBackground = true, Name = "Поток открытия" };
		thread.Start();
	}

	private void ButtonOpenForCompressionT_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Compression;
		thread = new Thread(ThreadT) { IsBackground = true, Name = "Поток сжатия" };
		thread.Start();
	}

	private void ButtonOpenForUnpackingT_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Unpacking;
		thread = new Thread(ThreadT) { IsBackground = true, Name = "Поток распаковки" };
		thread.Start();
	}

	private void ButtonOpenForRecompressionT_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Recompression;
		thread = new Thread(ThreadT) { IsBackground = true, Name = "Поток пересжатия" };
		thread.Start();
	}

	private void ComboQuickSetupT_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboQuickSetupT == null)
			return;
		var selectedIndex = ComboQuickSetupT.SelectedIndex;
		CheckBoxCS1T.IsChecked = selectedIndex >= 0;
		CheckBoxLZ1T.IsChecked = selectedIndex >= 1;
		CheckBoxCOMB1T.IsChecked = false;
		CheckBoxCS2T.IsChecked = selectedIndex >= 2;
		CheckBoxCOMB2T.IsChecked = selectedIndex >= 3;
		CheckBoxCS3T.IsChecked = selectedIndex >= 4;
		SendUsedMethods();
	}

	private void CheckBoxCS1T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS1T == null)
			return;
		usedMethodsT ^= UsedMethodsT.CS1;
		SendUsedMethods();
	}

	private void CheckBoxCS2T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS2T == null)
			return;
		usedMethodsT ^= UsedMethodsT.CS2;
		SendUsedMethods();
	}

	private void CheckBoxCS3T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCS3T == null)
			return;
		usedMethodsT ^= UsedMethodsT.CS3;
		SendUsedMethods();
	}

	private void CheckBoxLZ1T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxLZ1T == null)
			return;
		usedMethodsT ^= UsedMethodsT.LZ1;
		SendUsedMethods();
	}

	private void CheckBoxCOMB1T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCOMB1T == null)
			return;
		usedMethodsT ^= UsedMethodsT.COMB1;
		SendUsedMethods();
	}

	private void CheckBoxCOMB2T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxCOMB2T == null)
			return;
		usedMethodsT ^= UsedMethodsT.COMB2;
		SendUsedMethods();
	}

	private void ComboFragmentLengthT_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboFragmentLengthT == null)
			return;
		usedSizesT = (usedSizesT & ~0xF) | ComboFragmentLengthT.SelectedIndex;
		ComboFragmentLengthF.SelectedIndex = ComboFragmentLengthT.SelectedIndex;
		SendUsedSizes();
	}

	private void ButtonOpenArchive_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Opening;
		thread = new Thread(ThreadZ) { IsBackground = true, Name = "Поток открытия" };
		thread.Start();
	}

	private void ButtonCreateArchive_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Compression;
		thread = new Thread(ThreadZ) { IsBackground = true, Name = "Поток сжатия" };
		thread.Start();
	}

	private void ButtonUnpackArchive_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Unpacking;
		thread = new Thread(ThreadZ) { IsBackground = true, Name = "Поток распаковки" };
		thread.Start();
	}

	private void ButtonRepackArchive_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Recompression;
		thread = new Thread(ThreadZ) { IsBackground = true, Name = "Поток пересжатия" };
		thread.Start();
	}

	private void ComboArchivingOrder_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboArchivingOrder == null)
			return;
		StackPanelCompressorSelection.IsVisible = ComboArchivingOrder.SelectedIndex == 1;
		usedMethodsZ = (UsedMethodsZ)(((int)usedMethodsZ & ~3) | ComboArchivingOrder.SelectedIndex);
		SendUsedMethods();
	}

	private void ComboOtherArchives_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboOtherArchives == null)
			return;
		usedMethodsZ = (UsedMethodsZ)(((int)usedMethodsZ & ~(3 << 2)) | ComboOtherArchives.SelectedIndex << 2);
		SendUsedMethods();
	}

	private void ApplyingCompressorsChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxApplyF == null || CheckBoxApplyI == null || CheckBoxApplyT == null || CheckBoxApplyA == null || CheckBoxApplyV == null)
			return;
		if (sender == CheckBoxApplyF)
			usedMethodsZ ^= UsedMethodsZ.ApplyF;
		if (sender == CheckBoxApplyI)
			usedMethodsZ ^= UsedMethodsZ.ApplyI;
		if (sender == CheckBoxApplyT)
			usedMethodsZ ^= UsedMethodsZ.ApplyT;
		if (sender == CheckBoxApplyA)
			usedMethodsZ |= UsedMethodsZ.ApplyA;
		if (sender == CheckBoxApplyV)
			usedMethodsZ &= UsedMethodsZ.ApplyV;
		if (CheckBoxApplyF.IsChecked == false && CheckBoxApplyI.IsChecked == false && CheckBoxApplyT.IsChecked == false)
			CheckBoxApplyF.IsChecked = true;
		else
			SendUsedMethods();
	}

	private void SendUsedMethods()
	{
		if (netStream.Length < 4)
			try
			{
				Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Рабочий модуль Ares еще не загрузился. Подождите несколько секунд, после чего нажмите на любой флажок, чтобы изменить состояние параметров (нажмите второй раз на него же, чтобы вернуть состояние, которое вы попытались установить сейчас).", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
				return;
			}
			catch
			{
			}
		SendMessageToClient(ClientIndex.Files, [0, .. BitConverter.GetBytes((int)usedMethodsF)]);
		SendMessageToClient(ClientIndex.Images, [0, .. BitConverter.GetBytes((int)usedMethodsI)]);
		SendMessageToClient(ClientIndex.Text, [0, .. BitConverter.GetBytes((int)usedMethodsT)]);
		SendMessageToClient(ClientIndex.Zipping, [0, .. BitConverter.GetBytes((int)usedMethodsZ)]);
	}

	private void SendUsedSizes()
	{
		if (netStream.Length < 3)
			try
			{
				Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Рабочий модуль Ares еще не загрузился. Подождите несколько секунд, после чего выберите любой размер фрагмента, кроме текущего, чтобы изменить состояние параметров (снова выберите текущий, чтобы вернуть состояние, которое вы попытались установить сейчас).", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
				return;
			}
			catch
			{
			}
		SendMessageToClient(ClientIndex.Files, [1, .. BitConverter.GetBytes(usedSizesF)]);
		SendMessageToClient(ClientIndex.Text, [1, .. BitConverter.GetBytes(usedSizesT)]);
	}

	private readonly Grid[] ThreadsLayout;
	private readonly TextBlock[] TextBlockSubtotal;
	private readonly TextBlock[] TextBlockCurrent;
	private readonly TextBlock[] TextBlockStatus;
	private readonly ContentControl[] ContentViewSubtotal;
	private readonly ContentControl[] ContentViewCurrent;
	private readonly ContentControl[] ContentViewStatus;
	private readonly ProgressBar[] ProgressBarSubtotal;
	private readonly ProgressBar[] ProgressBarCurrent;
	private readonly ProgressBar[] ProgressBarStatus;
}

public class OpacityConverter : IValueConverter
{
	public static readonly OpacityConverter Instance = new();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not bool b)
			b = false;
		return b ? 1 : 0.5;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => new();
}
