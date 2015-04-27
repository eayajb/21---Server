using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ServerData;
using System.Net;
using System.Threading;

namespace _21___Server
{
    class Server
    {
        static DataHandler dataHandler;

        static Socket listenerSocket;
        static List<ClientData> clientsList;

        public Server(DataHandler inDataHandler)
        {
            dataHandler = inDataHandler;

            Console.WriteLine("*** Starting Server on " + Packet.GetIPforAddress());

            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientsList = new List<ClientData>();

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(Packet.GetIPforAddress()), 4242);

            listenerSocket.Bind(ip);

            Thread listenThread = new Thread(ListenThread);
            listenThread.SetApartmentState(ApartmentState.STA);
            listenThread.Start();
        }

        static void ListenThread()
        {
            for (; ; )
            {
                Console.WriteLine("LISTENING");

                listenerSocket.Listen(0);

                clientsList.Add(new ClientData(listenerSocket.Accept()));
            }
        }

        public static void DataIN(object cSocket)
        {
            Socket clientSocket = (Socket)cSocket;

            byte[] buffer;
            int readBytes;

            for (; ; )
            {
                try
                {
                    buffer = new byte[clientSocket.SendBufferSize];

                    readBytes = clientSocket.Receive(buffer);

                    if (readBytes > 0)
                    {
                        DataManager(new Packet(buffer));
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("A client disconnected");
                    SendCodeToClientList("exit");
                    throw (ex);
                }
            }
        }

        public static void DataManager(Packet packet)
        {
            switch (packet.packetType)
            {
                case PacketType.RegisterClient:
                    Console.WriteLine("REGISTERING " + packet.clientID);
                    dataHandler.SetRefernceFrameData(packet.clientID, packet.referenceFrameData);
                    Console.WriteLine();
                    break;

                case PacketType.Transfer:
                    if (packet.personList.Count() > 0)
                        dataHandler.PassData( packet.clientID, packet.personList );
                    break;

                case PacketType.InputCode:
                    break;
            }
        }

        public static void SendCodeToClientList(string inputCode)
        {
            Packet packet = new Packet(PacketType.InputCode, "server");
            packet.clientCode = inputCode;

            foreach (ClientData c in clientsList)
            {
                c.clientSocket.Send(packet.ToBytes());
            }
        }

        public static void RequestRegistration()
        {
            dataHandler.RegistrationRequired = true;
        }
    }

    class ClientData
    {
        public Socket clientSocket;
        public Thread clientThread;
        public string id;

        public ClientData()
        {
            id = Guid.NewGuid().ToString();
            clientThread = new Thread(Server.DataIN);
            clientThread.Start(clientSocket);
            SendRegistrationPacket();
        }

        public ClientData(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
            id = Guid.NewGuid().ToString();
            clientThread = new Thread(Server.DataIN);
            clientThread.Start(clientSocket);
            SendRegistrationPacket();
        }

        public void SendRegistrationPacket()
        {
            Packet packet = new Packet(PacketType.RegisterClient, "server");
            clientSocket.Send(packet.ToBytes());
        }
    }
}
