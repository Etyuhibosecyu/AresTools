using System.Threading.Tasks;

// See https://aka.ms/new-console-template for more information
Task[] tasks = [Task.Factory.StartNew(() => AresFLib.MainClassF.Main(["11000"])),
Task.Factory.StartNew(() => AresILib.MainClassI.Main(["11000"])),
Task.Factory.StartNew(() => AresTLib.MainClassT.Main(["11000"]))];
Task.WaitAll(tasks);