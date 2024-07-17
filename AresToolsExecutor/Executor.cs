using System.Threading.Tasks;

namespace AresTools.Views;

public class Executor
{
	public static void Main(string[] args)
	{
		Task[] tasks = [Task.Factory.StartNew(() => AresFLib.MainClassF.Main(args)),
			Task.Factory.StartNew(() => AresILib.MainClassI.Main(args)),
			Task.Factory.StartNew(() => AresTLib.MainClassT.Main(args))];
		Task.WaitAll(tasks);
	}
}