using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

//ref :https://msdn.microsoft.com/library/windows/apps/br241203?f=255&MSPPError=-2147217396
namespace ConnectBluetooth
{

    public sealed partial class MainPage : Page
    {
        PeerWatcher _peerWatcher;
        StreamSocket _streamsocket;
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            PeerFinder.AllowBluetooth = true;
            PeerFinder.AllowWiFiDirect = true;

            PeerFinder.ConnectionRequested += PeerFinder_ConnectionRequested;
            PeerFinder.TriggeredConnectionStateChanged += PeerFinder_TriggeredConnectionStateChanged;

            PeerFinder.Start();

            _peerWatcher = PeerFinder.CreateWatcher();
            _peerWatcher.Added += _peerWatcher_Added;
            _peerWatcher.Removed += _peerWatcher_Removed;
            _peerWatcher.EnumerationCompleted += _peerWatcher_EnumerationCompleted;
            _peerWatcher.Updated += _peerWatcher_Updated;
            _peerWatcher.Start();

            try
            {
                var allpeer = await PeerFinder.FindAllPeersAsync();
                this.listviewAllDevice.ItemsSource = allpeer;
            }
            catch (Exception ex)
            {
                this.textboxDebug.Text += ex.Message + "\n";
            }

        }
        private async Task connect(PeerInformation peerInformation)
        {
            _streamsocket = await PeerFinder.ConnectAsync(peerInformation);
            this.textboxDebug.Text += "Connected to: " + peerInformation.DisplayName + "\n";
            
        }
        private async void _peerWatcher_Updated(PeerWatcher sender, PeerInformation args)
        {
            await this.textboxDebug.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            () =>
            {
                this.textboxDebug.Text += "Updated\n";
            });
        }

        private async void _peerWatcher_EnumerationCompleted(PeerWatcher sender, object args)
        {
            await this.textboxDebug.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    this.textboxDebug.Text += "Enum Completed\n";
                });
        }

        private async void _peerWatcher_Removed(PeerWatcher sender, PeerInformation args)
        {
            await this.textboxDebug.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    this.textboxDebug.Text += "Removed\n";
                });
        }

        private async void _peerWatcher_Added(PeerWatcher sender, PeerInformation args)
        {
            await this.textboxDebug.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    this.textboxDebug.Text += "Added\n";
                });
        }

        private async void AllDevice_Click(object sender, ItemClickEventArgs e)
        {
            var item = (e.ClickedItem as PeerInformation);
            string displayname = "Displayname: " + item.DisplayName + "\n";
            string hostname = "HostName: " + item.HostName + "\n";
            string id = "Id: " + item.Id + "\n";
            await this.connect(item);
            //MessageDialog msgDialog = new MessageDialog(displayname + hostname + id);
            //await msgDialog.ShowAsync();

        }

        private async void PeerFinder_ConnectionRequested(object sender, ConnectionRequestedEventArgs args)
        {
            try
            {
                await this.textboxDebug.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        this.textboxDebug.Text += "Connection Requested\n";
                        await this.connect(args.PeerInformation);
                        if (_streamsocket != null)
                        {
                            while (true)
                            {
                                // nếu đã tồn tại một thể hiện của lớp DataReader thì đối tượng đó tự động được giải phóng.
                                DataReader datareader = new DataReader(_streamsocket.InputStream);
                                try
                                {
                                    uint size = await datareader.LoadAsync(sizeof(uint));
                                    if (size != sizeof(uint))
                                    {
                                        return;
                                    }
                                    uint lenght = datareader.ReadUInt32();
                                    uint exactlylenght = await datareader.LoadAsync(lenght);
                                    if (lenght != exactlylenght)
                                    {
                                        return;
                                    }
                                    string msg = datareader.ReadString(exactlylenght);
                                    this.textboxDebug.Text += "Receive from " + _streamsocket.Information.RemoteAddress + ": " + msg + "\n";
                                }
                                catch (Exception ex)
                                {
                                    this.textboxDebug.Text += ex.Message + "\n";
                                }
                            }

                        }
                    });
            }
            catch (Exception ex)
            {
                this.textboxDebug.Text += ex.Message + "\n";
            }


            //await PeerFinder.ConnectAsync(args.PeerInformation);
            // throw new NotImplementedException();
        }

        private async void PeerFinder_TriggeredConnectionStateChanged(object sender, TriggeredConnectionStateChangedEventArgs args)
        {
            await this.textboxDebug.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            () =>
            {
                this.textboxDebug.Text += "Connection State Changed\n";
            });

        }

        private async void SendMesage_Click(object sender, RoutedEventArgs e)
        {
            string msg = textboxMessage.Text;
            await this.sendMessage(msg);
            this.textboxMessage.Text = String.Empty;
        }
        private async Task sendMessage(string message)
        {
            if (_streamsocket == null)
            {
                this.textboxDebug.Text += "No connection found\n";
                return;
            }
            DataWriter datawritter = new DataWriter(_streamsocket.OutputStream);

            // cấu trúc dữ liệu gởi đi gồm 
            // [độ dài chuỗi]_[chuỗi]
            // có độ dài chuỗi đễ dễ đọc.   

            // Ghi độ dài chuỗi
            datawritter.WriteUInt32(datawritter.MeasureString(message));

            // Ghi chuỗi
            datawritter.WriteString(message);
            try
            {
                // Gửi Socket đi 
                await datawritter.StoreAsync();

                // Xuất thông tin ra màn hình bằng textbox
                this.textboxDebug.Text += "Send from " + _streamsocket.Information.LocalAddress + ": " + message + "\n";
            }
            catch (Exception ex)
            {
                this.textboxDebug.Text += ex.Message + "\n";
            }
        }
    }
}
