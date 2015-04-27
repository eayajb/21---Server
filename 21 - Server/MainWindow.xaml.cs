using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _21___Server
{
    public partial class MainWindow : Window
    {
        static Models.ModelsHandler modelsHandler;
        static View viewController;

        static ConsoleHelper consoleHelper;
        static Server server;
        static DataHandler dataHandler;

        Timer drawTimer;

        public MainWindow()
        {
            modelsHandler = new Models.ModelsHandler();
            viewController = new View(this, modelsHandler);

            dataHandler = new DataHandler(modelsHandler);
            server = new Server(dataHandler);

            consoleHelper = new ConsoleHelper(this);
            consoleHelper.ConstructHelpList();

            Thread consoleThread = new Thread(ConsoleThread);
            consoleThread.SetApartmentState(ApartmentState.STA);
            consoleThread.Start();

            Loaded += MainWindow_Loaded;

            InitializeComponent();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            viewController.SetAllCanvas(this.bodyPointsCanvas1, this.bodyPointsCanvas2, this.mainBodyPointsCanvas);
            StartDrawCallback();
        }

        public void StartDrawCallback()
        {
            Console.WriteLine("DRAW CALLBACK STARTED");
            TimerCallback viewControllerCallback = new TimerCallback(Draw);
            if (drawTimer == null)
            {
                Console.WriteLine("TIMER NULL");
                drawTimer = new Timer(viewControllerCallback, null, 50, 20);
            }
            GC.KeepAlive(drawTimer);
        }

        static public void Draw(Object stateInfo)
        {
            Application.Current.Dispatcher.Invoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(
                    delegate()
                    {
                        viewController.DrawAllPersonData();
                    }
                ));
        }

        static void ConsoleThread()
        {
            for (; ; )
            {
                Console.WriteLine("Type 'input' COMMAND OR - 'help'");
                consoleHelper.ConsoleInput(Console.ReadLine().ToString());
                Console.WriteLine();
            }
        }
    }

    public class ConsoleHelper
    {
        MainWindow main;

        static List<string> helpFunctions = new List<string>() { "s", "t", "r", "z", "close", "exit" };
        public static Dictionary<string, int> helpList = new Dictionary<string, int>();
        public static Dictionary<string, string> helpDescriptions = new Dictionary<string, string>();

        public ConsoleHelper(MainWindow main)
        {
            this.main = main;
        }

        public void ConstructHelpList()
        {
            int counter = 0;
            foreach (string function in helpFunctions)
            {
                helpList.Add(function, counter);
                counter++;

                if (!helpDescriptions.ContainsKey(function))
                    helpDescriptions.Add(function, function);
            }

            helpDescriptions["s"] = "  --  start drawing callback";
            helpDescriptions["t"] = "  --  begin data transfer";
            helpDescriptions["r"] = "  --  begin registration";
            helpDescriptions["z"] = "  --  end data transfer";
            helpDescriptions["close"] = "  --  closes ALL CLIENTS";
            helpDescriptions["exit"] = "  --  EXIT the application";
        }

        internal void ConsoleInput(string input)
        {
            switch (input)
            {
                case ("s"):
                    main.StartDrawCallback();
                    break;

                case ("help"):
                    Console.WriteLine();
                    Console.WriteLine("'input' COMMANDS:");
                    foreach (string function in ConsoleHelper.helpList.Keys)
                    {
                        Console.WriteLine(function + ConsoleHelper.helpDescriptions[function]);
                    }
                    break;

                case ("t"):
                case ("z"):
                case ("close"):
                    Server.SendCodeToClientList(input);
                    break;

                case ("r"):
                    Server.RequestRegistration();
                    break;

                case ("exit"):
                    Server.SendCodeToClientList(input);
                    Environment.Exit(0);
                    break;
            }
        }
    }
}
