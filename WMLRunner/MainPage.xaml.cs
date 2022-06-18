using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;

#if USE_WINML_NUGET
using Microsoft.AI.MachineLearning;
#else
using Windows.AI.MachineLearning;
#endif
using Windows.Media;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Net;
using System.Net.Sockets;
using BlingFire;


namespace WMLRunner
{
    public sealed partial class MainPage : Page
    {
        private MrcViBaseModel _modelGen;

        public MainPage()
        {
            InitializeComponent();
            LoadModel();
            ConnectToTCPBridge();
        }

        private async Task LoadModel()
        {
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Constant.MODEL_PATH));
            _modelGen = await MrcViBaseModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
            QuestionTextBlock.Text = "Model Loaded!";
        }   

        private async void ConnectToTCPBridge()
        {
            TcpClient client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(Constant.CONNECTION_ADDRESS), Constant.CONNECTION_PORT);

            NetworkStream stream = client.GetStream();
            while (true)
            {
                byte[] data = new byte[Constant.MAX_BUFFER_SIZE];
                int bytes = await stream.ReadAsync(data, 0, data.Length);
                Array.Resize(ref data, bytes);
                string answer = await _modelGen.GetAnswer(System.Text.Encoding.UTF8.GetString(data));

                byte[] answerInBytes = System.Text.Encoding.UTF8.GetBytes(answer);
                await stream.WriteAsync(answerInBytes, 0, answerInBytes.Length);
            }
        }
    }
}
