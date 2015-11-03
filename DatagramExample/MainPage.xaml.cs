using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace DatagramExample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DatagramSocket datagramSocket;
        private const int port = 42413;
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private async void StartClick(object sender, RoutedEventArgs e)
        {
            try
            {
                datagramSocket = new DatagramSocket();
                datagramSocket.MessageReceived += MessageReceived;
                await datagramSocket.BindServiceNameAsync(port.ToString());
                this.OutputString("Done!");
            }
            catch (Exception ex)
            {
                datagramSocket = null;
                this.OutputString(ex.Message);
            }

        }

        private async void MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                          async () =>
                          {
                              String msg = await readMessage(args.GetDataStream());
                              this.OutputString("Received from" + args.RemoteAddress.RawName + ": " + msg);
                          }

                );


        }


        private async Task sendMessage(string message)
        {
            if (datagramSocket == null)
            {
                this.OutputTextblock.Text += "No connection found\n";
                return;
            }
            var outputstream = await datagramSocket.GetOutputStreamAsync(datagramSocket.Information.RemoteAddress, datagramSocket.Information.RemotePort);

            DataWriter datawritter = new DataWriter(outputstream);

            datawritter.WriteUInt32(datawritter.MeasureString(message));

            // Ghi chuỗi
            datawritter.WriteString(message);
            try
            {
                // Gửi Socket đi 
                await datawritter.StoreAsync();
                
                // Xuất thông tin ra màn hình bằng textbox
                this.OutputTextblock.Text += "Send from " + datagramSocket.Information.LocalAddress + ": " + message + "\n";
            }
            catch (Exception ex)
            {
                this.OutputTextblock.Text += ex.Message + "\n";
            }

        }

        private async Task<String> readMessage(IInputStream input)
        {
            DataReader datareader = new DataReader(input);

            while (true)
            {
                try
                {
                    uint size = await datareader.LoadAsync(sizeof(uint));
                    if (size != sizeof(uint))
                    {
                        return String.Empty;
                    }
                    uint lenght = datareader.ReadUInt32();
                    uint exactlylenght = await datareader.LoadAsync(lenght);
                    if (lenght != exactlylenght)
                    {
                        return String.Empty;
                    }
                    string msg = datareader.ReadString(exactlylenght);
                    return msg;
                }
                catch (Exception ex)
                {
                    this.OutputTextblock.Text += ex.Message + "\n";
                }
            }

        }

        // xuất ra màn hình câu msg
        private void OutputString(string msg)
        {
            this.OutputTextblock.Text += msg + "\n";
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            string serverip = this.TextBoxIPSERVER.Text;
            HostName hostname = new HostName(serverip);

            if (datagramSocket == null)
            {
                datagramSocket = new DatagramSocket();
            }
            try
            {
                await datagramSocket.ConnectAsync(hostname, port.ToString());
                this.OutputString("Connected");
            }
            catch (Exception ex)
            {
                this.OutputString(ex.Message);   
            }
        }

        private async void SendMesage_Click(object sender, RoutedEventArgs e)
        {
            string msg = textboxMessage.Text;
            await this.sendMessage(msg);
            this.textboxMessage.Text = String.Empty;
        }

    }
}
