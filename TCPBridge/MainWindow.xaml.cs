using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace TCPBridge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient? wmlClient = null;
        public MainWindow()
        {
            InitializeComponent();
            StartListener();
        }

        private async void StartListener()
        {
            TcpListener? server = new TcpListener(IPAddress.Parse(Constant.CONNECTION_ADDRESS), Constant.CONNECTION_PORT);
            server.Start();

            Console.Write("Waiting for a connection... ");

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (wmlClient == null)
                {
                    wmlClient = await server.AcceptTcpClientAsync();
                    Console.WriteLine("WMLClient Connected");
                }
                else
                {
                    Console.Write("Waiting for a connection... ");
                    var client = await server.AcceptTcpClientAsync();
                    Console.WriteLine("ASP Client Connected!");
                    await HandleAspClient(client);
                }

            }
            server.Stop();
        }

        private async Task HandleAspClient(TcpClient client)
        {
            NetworkStream tcpStream = client.GetStream();

            byte[] data = new byte[Constant.MAX_BUFFER_SIZE];
            int sz = await tcpStream.ReadAsync(data);
            Array.Resize(ref data, sz);
            if (wmlClient != null)
            {
                NetworkStream wmlStream = wmlClient.GetStream();
                await wmlStream.WriteAsync(data, 0, sz);

                int size = await wmlStream.ReadAsync(data);
                Console.WriteLine(size.ToString());
                await tcpStream.WriteAsync(data, 0, size);
            }
        }
    }
}
