using AresTools.ViewModels;
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
	private bool isWorking;
	private static UsedMethodsF usedMethodsF;
	private static UsedMethodsI usedMethodsI;
	private static UsedMethodsT usedMethodsT;
	private static int usedSizesF, usedSizesT;
#if !DEBUG
	private DateTime compressionStart;
#endif

	private readonly string[] args;
	private readonly TcpListener tcpListener; //монитор подключений TCP клиентов
	private readonly Thread listenThread; //создание потока

	private readonly List<TcpClient> clients = []; //список клиентских подключений
	private readonly List<NetworkStream> netStream = []; //список потока данных
	private readonly int port = 11000;
	private Process executorF;
	private Process executorI;
	private Process executorT;
	private int executorIdF;
	private int executorIdI;
	private int executorIdT;
#if RELEASE
	private readonly Random random = new(1234567890);
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
		executorF = default!;
		executorI = default!;
		executorT = default!;
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
		//TabView.CurrentItem = TabItemText;
		ComboQuickSetupF.SelectedIndex = 1;
		filename = args.Length == 0 ? "" : args[0];
#if RELEASE
		port = random.Next(1024, 65536);
		StartExecutor();
#endif
		System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Normal;
		try
		{
			tcpListener = new(IPAddress.Loopback, port);
			listenThread = new(new ThreadStart(ListenThread)) { Name = "Ожидание подключения клиентов", IsBackground = true };
			listenThread.Start(); //старт потока
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
		var receiveLen = new byte[4];
		while (true)
		{
			try
			{
				netStream[client].ReadExactly(receiveLen);
				receive = new byte[BitConverter.ToInt32(receiveLen)];
				netStream[client].ReadExactly(receive);
				WorkUpReceiveMessage(client, receive);
			}
			catch
			{
				if (client >= clients.Length)
					client = clients.Length - 1;
				clients.RemoveAt(client);
				netStream.RemoveAt(client);
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
		if (executorF != null && !executorF.HasExited)
		{
			executorF.EnableRaisingEvents = false;
			executorF.Kill();
		}
		if (executorI != null && !executorI.HasExited)
		{
			executorI.EnableRaisingEvents = false;
			executorI.Kill();
		}
		if (executorT != null && !executorT.HasExited)
		{
			executorT.EnableRaisingEvents = false;
			executorT.Kill();
		}
		var tempFilename = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresF-" + executorIdF + ".tmp";
		try
		{
			if (File.Exists(tempFilename))
				File.Delete(tempFilename);
		}
		catch
		{
		}
		tempFilename = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresI-" + executorIdI + ".tmp";
		try
		{
			if (File.Exists(tempFilename))
				File.Delete(tempFilename);
		}
		catch
		{
		}
		tempFilename = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\AresT-" + executorIdT + ".tmp";
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
			.ShowAsync() == MsBox.Avalonia.Enums.ButtonResult.Ok);
		if (!button)
			Environment.Exit(0);
		SetProgressBarsFull();
		StartExecutor();
	}

	private void StartExecutor()
	{
		executorF = ExecFunction.Start(MainClassF.Main, [port.ToString(), Environment.ProcessId.ToString()]);
		executorIdF = executorF.Id;
		executorF.EnableRaisingEvents = true;
		executorF.Exited += ExecutorExited;
		executorI = ExecFunction.Start(MainClassI.Main, [port.ToString(), Environment.ProcessId.ToString()]);
		executorIdI = executorI.Id;
		executorI.EnableRaisingEvents = true;
		executorI.Exited += ExecutorExited;
		executorT = ExecFunction.Start(MainClassT.Main, [port.ToString(), Environment.ProcessId.ToString()]);
		executorIdT = executorT.Id;
		executorT.EnableRaisingEvents = true;
		executorT.Exited += ExecutorExited;
	}

	private void SendMessageToClient(int index, byte[] toSend)
	{
		var toSendLen = BitConverter.GetBytes(toSend.Length);
		netStream[index].Write(toSendLen);
		netStream[index].Write(toSend);
		netStream[index].Flush(); //удаление данных из потока
	}

	private static async void SetValue(ProgressBar pb, double new_value) => await Dispatcher.UIThread.InvokeAsync(() => SetValueInternal(pb, new_value));

	private static void SetValueInternal(ProgressBar pb, double new_value)
	{
		if (new_value < 0)
			pb.Value = 1;
		else
			pb.Value = new_value;
	}

	private async void WorkUpReceiveMessage(int client, byte[] message)
	{
		try
		{
			if (message.Length == 0)
				return;
			if (message[0] == 0 && message.Length == ProgressBarGroups * 24 + 17 && isWorking)
				UpdateProgressBars(message);
			else if (message[0] == 1 && client == 1 && message.Length == 2)
				await OpenFile(client, message[1]);
			else if (message[0] == 1 && client != 1 && message.Length == 1)
				await OpenFile(client, 0);
			else if (message[0] == 2)
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось " + (operation_type is OperationType.Compression or OperationType.Recompression ? "сжать" : "распаковать") + " файл.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
			else if (message[0] == 3)
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Файл сжат, но распаковка не удалась.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
			if (message[0] is not 0)
			{
				SetProgressBarsFull();
			}
		}
		catch
		{
			if (message.Length != 0 && message[0] is not 0)
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Произошла серьезная ошибка при попытке выполнить действие. Повторите попытку позже. Если проблема не исчезает, обратитесь к разработчикам приложения.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
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

	private async Task OpenFile(int client, byte transparency)
	{
		var timeString = "";
#if !DEBUG
				var elapsed = DateTime.Now - compressionStart;
				timeString += " (" + (elapsed.Days == 0 ? "" : $"{elapsed.Days:D}:") + (elapsed.Days == 0 && elapsed.Hours == 0 ? "" : $"{elapsed.Hours:D2}:") + $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})";
#endif
		if (operation_type == OperationType.Opening)
		{
			var path = (Environment.GetEnvironmentVariable("temp") ?? throw new IOException()) + @"\" + Path.GetFileNameWithoutExtension(filename) + (client != 1 ? "" : transparency != 0 ? ".tga" : ".bmp");
			using Process process = new();
			process.StartInfo.FileName = "explorer";
			process.StartInfo.Arguments = "\"" + path + "\"";
			process.Start();
			System.Threading.Thread.Sleep(MillisecondsPerSecond);
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Файл успешно распакован" + timeString + "!", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
			process.WaitForExit();
			try
			{
				File.Delete(path);
			}
			catch
			{
			}
		}
		else
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Файл успешно " + (operation_type is OperationType.Compression or OperationType.Recompression ? "сжат" : "распакован") + timeString + "!", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
	}

	private void ThreadF() => Thread(0, "F", false);

	private void ThreadI() => Thread(1, "I", false);

	private void ThreadT() => Thread(2, "T", false);

	private async void Thread(int client, string filter, bool startImmediate)
	{
		if (isWorking)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось запустить сжатие/распаковку, так как существует другой активный процесс.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
			return;
		}
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
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось распаковать файл.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
				break;
			}
			await StartProcess(client, true);
			return;
			case OperationType.Compression:
			if (ProcessStartup(client == 1 ? "Images" : "").Result is bool || !await ValidateImageAsync(client))
				break;
			if (client == 0 && usedMethodsF.HasFlag(UsedMethodsF.CS4) && usedSizesF > 4 && new FileInfo(filename).Length > 16000000)
			{
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Слишком большой размер фрагмента для PPM. Попробуйте уменьшить размер фрагмента, отключите PPM или возьмите меньший файл.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
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
			await TopLevel.GetTopLevel(this)?.StorageProvider.OpenFilePickerAsync(new() { Title = $"Select the *{string.Join(" or *", MainViewModel.AresFilesMasks[filter])} file", FileTypeFilter = [MainViewModel.GetFilesType(filter)] })!);
		filename = fileResult?.Count == 0 ? "" : fileResult?[0]?.TryGetLocalPath() ?? "";
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
		ButtonOpenT.IsEnabled = isEnabled;
		ButtonOpenForCompressionT.IsEnabled = isEnabled;
		ButtonOpenForUnpackingT.IsEnabled = isEnabled;
		ButtonOpenForRecompressionT.IsEnabled = isEnabled;
		ComboQuickSetupT.IsEnabled = isEnabled;
		GridSettingsT.IsEnabled = isEnabled;
		return 0;
	}

	private async Task StartProcess(int client, bool unpack)
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
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось " + (unpack ? "распаковать" : "сжать") + " файл.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
		}
	}

	private async Task<bool> ValidateImageAsync(int client)
	{
		if (client != 1)
			return true;
		try
		{
			using var image = SixLabors.ImageSharp.Image.Load<Bgra32>(filename);
			if (image == null)
			{
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось прочитать изображение.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
				return false;
			}
			if (image.Width < 16 || image.Height < 16 || image.Width * image.Height > 0x300000)
			{
				await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Обе стороны изображения должны быть от 16 пикселов, а площадь - не более 3 мегапикселов.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
				return false;
			}
#if RELEASE
			if (usedMethodsI.HasFlag(UsedMethodsI.CS2) && usedMethodsI.HasFlag(UsedMethodsI.LZ2) && !usedMethodsI.HasFlag(UsedMethodsI.AHF) && new Chain(image.Width * image.Height).Any((_, index) => image[index % image.Width, index / image.Width].A != ValuesInByte - 1))
			{
				if (await Dispatcher.UIThread.InvokeAsync(async () =>
					await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Сжатие этого изображения таким наборов методов в разработке. Переключиться на максимально близкий набор методов и сжать изображение?", MsBox.Avalonia.Enums.ButtonEnum.YesNo).ShowAsync())
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
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Не удалось прочитать изображение.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
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
		thread = new Thread(new ThreadStart(ThreadF)) { IsBackground = true, Name = "Поток сжатия" };
		thread.Start();
	}

	private void ButtonOpenForUnpackingF_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Unpacking;
		thread = new Thread(new ThreadStart(ThreadF)) { IsBackground = true, Name = "Поток распаковки" };
		thread.Start();
	}

	private void ButtonOpenForRecompressionF_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Recompression;
		thread = new Thread(new ThreadStart(ThreadF)) { IsBackground = true, Name = "Поток пересжатия" };
		thread.Start();
	}

	private void ButtonStop_Click(object? sender, RoutedEventArgs e)
	{
		executorF?.Kill();
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
		CheckBoxLZ1F.IsChecked = selectedIndex >= 1;
		CheckBoxHF1F.IsChecked = selectedIndex >= 0;
		CheckBoxCS2F.IsChecked = selectedIndex >= 2;
		CheckBoxAHF2F.IsChecked = selectedIndex >= 3;
		CheckBoxCS4F.IsChecked = selectedIndex >= 4;
		SendUsedMethods();
	}

	private void CheckBoxCS1F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsF ^= UsedMethodsF.CS1;
		SendUsedMethods();
	}

	private void CheckBoxCS2F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
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
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Этот метод находится в разработке.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
		}
#else
		usedMethodsF ^= UsedMethodsF.CS3;
		SendUsedMethods();
#endif
	}

	private void CheckBoxCS4F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
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
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Этот метод находится в разработке.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
		}
#else
		usedMethodsF ^= UsedMethodsF.CS5;
		SendUsedMethods();
#endif
	}

	private void CheckBoxLZ1F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsF ^= UsedMethodsF.LZ1;
		if (CheckBoxHF1F != null && !(CheckBoxHF1F.IsChecked ?? false) && CheckBoxLZ1F != null && !(CheckBoxLZ1F.IsChecked ?? false))
			CheckBoxHF1F.IsChecked = true;
		SendUsedMethods();
	}

	private void CheckBoxHF1F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsF ^= UsedMethodsF.HF1;
		if (CheckBoxHF1F != null && !(CheckBoxHF1F.IsChecked ?? false) && CheckBoxLZ1F != null && !(CheckBoxLZ1F.IsChecked ?? false))
			CheckBoxLZ1F.IsChecked = true;
		SendUsedMethods();
	}

	private void CheckBoxAHF2F_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsF ^= UsedMethodsF.AHF2;
		SendUsedMethods();
	}

	private void ComboFragmentLengthF_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboFragmentLengthF == null)
			return;
		usedSizesF = (usedSizesF & ~0xF) | ComboFragmentLengthF.SelectedIndex;
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
		thread = new Thread(new ThreadStart(ThreadI)) { IsBackground = true, Name = "Поток сжатия" };
		thread.Start();
	}

	private void ButtonOpenForUnpackingI_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Unpacking;
		thread = new Thread(new ThreadStart(ThreadI)) { IsBackground = true, Name = "Поток распаковки" };
		thread.Start();
	}

	private void ButtonOpenForRecompressionI_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Recompression;
		thread = new Thread(new ThreadStart(ThreadI)) { IsBackground = true, Name = "Поток пересжатия" };
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
		usedMethodsI ^= UsedMethodsI.CS1;
		SendUsedMethods();
	}

	private void CheckBoxCS2I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.CS2;
		SendUsedMethods();
	}

	private void CheckBoxCS3I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.CS3;
		SendUsedMethods();
	}

	private void CheckBoxCS4I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
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
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Этот метод находится в разработке.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
		}
#else
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
				await MessageBoxManager.GetMessageBoxStandard("", "Ошибка! Этот метод находится в разработке.", MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsync());
		}
#else
		usedMethodsI ^= UsedMethodsI.CS6;
		SendUsedMethods();
#endif
	}

	private void CheckBoxHF1I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.HF1;
		SendUsedMethods();
	}

	private void CheckBoxLZ1I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.LZ1;
		SendUsedMethods();
	}

	private void CheckBoxHF2I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.HF2;
		SendUsedMethods();
	}

	private void CheckBoxLZ2I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.LZ2;
		SendUsedMethods();
	}

	private void CheckBoxHF3I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.HF3;
		SendUsedMethods();
	}

	private void CheckBoxLZ3I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.LZ3;
		SendUsedMethods();
	}

	private void CheckBoxHF4I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.HF4;
		SendUsedMethods();
	}

	private void CheckBoxLZ4I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.LZ4;
		SendUsedMethods();
	}

	private void CheckBoxHF5I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.HF5;
		SendUsedMethods();
	}

	private void CheckBoxLZ5I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.LZ5;
		SendUsedMethods();
	}

	private void CheckBoxLZ6I_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsI ^= UsedMethodsI.LZ6;
		SendUsedMethods();
	}

	private void RadioButtonAHFI_CheckedChanged(object? sender, RoutedEventArgs e)
	{
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
		thread = new Thread(new ThreadStart(ThreadT)) { IsBackground = true, Name = "Поток сжатия" };
		thread.Start();
	}

	private void ButtonOpenForUnpackingT_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Unpacking;
		thread = new Thread(new ThreadStart(ThreadT)) { IsBackground = true, Name = "Поток распаковки" };
		thread.Start();
	}

	private void ButtonOpenForRecompressionT_Click(object? sender, RoutedEventArgs e)
	{
		operation_type = OperationType.Recompression;
		thread = new Thread(new ThreadStart(ThreadT)) { IsBackground = true, Name = "Поток пересжатия" };
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
		CheckBoxLZ2T.IsChecked = selectedIndex >= 3;
		CheckBoxCS3T.IsChecked = selectedIndex >= 4;
		SendUsedMethods();
	}

	private void CheckBoxCS1T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsT ^= UsedMethodsT.CS1;
		SendUsedMethods();
	}

	private void CheckBoxCS2T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsT ^= UsedMethodsT.CS2;
		SendUsedMethods();
	}

	private void CheckBoxCS3T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsT ^= UsedMethodsT.CS3;
		SendUsedMethods();
	}

	private void CheckBoxLZ1T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsT ^= UsedMethodsT.LZ1;
		SendUsedMethods();
	}

	private void CheckBoxCOMB1T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsT ^= UsedMethodsT.COMB1;
		SendUsedMethods();
	}

	private void CheckBoxLZ2T_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		usedMethodsT ^= UsedMethodsT.LZ2;
		SendUsedMethods();
	}

	private void ComboFragmentLengthT_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboFragmentLengthT == null)
			return;
		usedSizesT = (usedSizesT & ~0xF) | ComboFragmentLengthT.SelectedIndex;
		SendUsedSizes();
	}

	private void SendUsedMethods()
	{
		if (netStream.Length < 3)
			return;
		SendMessageToClient(0, [0, .. BitConverter.GetBytes((int)usedMethodsF)]);
		SendMessageToClient(1, [0, .. BitConverter.GetBytes((int)usedMethodsI)]);
		SendMessageToClient(2, [0, .. BitConverter.GetBytes((int)usedMethodsT)]);
	}

	private void SendUsedSizes()
	{
		if (netStream.Length < 3)
			return;
		SendMessageToClient(0, [1, .. BitConverter.GetBytes(usedSizesF)]);
		SendMessageToClient(2, [1, .. BitConverter.GetBytes(usedSizesT)]);
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
