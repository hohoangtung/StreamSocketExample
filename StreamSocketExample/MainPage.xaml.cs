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
using Windows.Networking.Connectivity;
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

/*
 * Author: Hồ Hoàng Tùng
 * Email: hohoangtung12a3@gmail.com
 * 
 * Demo kết nối giữa 2 thiết bị qua wifi sử dụng mạng cục bộ, cho phép gửi tin nhắn chat
 * Sử dụng stream socket để tạo kết nối và truyền dữ liệu.
 * ref: https://code.msdn.microsoft.com/windowsapps/StreamSocket-Sample-8c573931#content 
 * Hoạt động tốt trên Windows Phone 8.1 và Windows App 8.1
 * 
 */
namespace testwinphone
{
    public sealed partial class MainPage : Page
    {
        StreamSocket _streamsocket;

        // Ứng dụng nào sử dụn listner sẽ đóng vai trò là server. Lắng nghe yêu cầu kết nối từ client
        StreamSocketListener _streamSocketListener;

        // Một thiết bị có thể thuộc nhiều mạng con, nên ta có danh sách các IP có thể dùng để kết nối trên thiết bị.
        // tuỳ vào mục đích ứng dụng ta có thể sử dụng tất cả hoặc chỉ một IP
        private List<HostName> _availableHostName;  

        // sử dụng một IP để demo
        private HostName _hostname;                     

        // cổng dịch vụ sử dụng cho chương trình, mỗi port chỉ sử dụng cho một chương trình.
        // ref: https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers
        // tránh sử dụng các port Well-known hoặc các port được sử dụng chính thức bởi các ứng dụng khác
        private const int _port = 23513;                // hardcode

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            _availableHostName = new List<HostName>();
            _streamSocketListener = new StreamSocketListener();

        }

        // lấy danh sách các ip của các mạng mà thiết bị này đang truy cập
        private IReadOnlyList<HostName> getHostNames()
        {
            // một thiết bị có thể kết nốt nhiều mạng cùng lúc (ví dụ như tham gia vào mạng cha, và phát mạng con)
            // gethostname sẽ lấy tất cả mạng tham gia, bao gồm cả kết nối bluetooth
            var list = NetworkInformation.GetHostNames();
            List<HostName> rtvalue = new List<HostName>();

            // host name có thuộc tính Type, nhận 4 giá trị Domain, IPv4, IPv6, Bluetooth.
            // nếu là domain thì sẽ không có IPInformation
            // (chúng ta không biết IP của doamin - tên miền. tên miền phải được phân giải thông qua DNS dùng cho trường hợp kết nối ngoại tuyến)
            // ở đây chỉ xét mạng cục bộ
            foreach (var item in list)
            {
                if (item.IPInformation != null)
                {
                    // work with blue tooth
                    //if (item.Type != HostNameType.Bluetooth)
                    //{
                    //    continue;
                    //}
                    rtvalue.Add(item);
                }
            }
            return rtvalue;
        }
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _availableHostName = this.getHostNames().ToList();

            if (_availableHostName.Any())
            {
                // để đơn giản ở đây sử dụng IP đầu tiên tìm thấy để tạo kết nối
                _hostname = _availableHostName.First();     // hardcode. 
            }
            else
            {
                this.textboxDebug.Text += "No network connected\n";
            }
        }

        private async Task startListener(HostName hostname)
        {
            try
            {
                // thêm sự kiện received.
                // sự kiện sẽ được kích hoạt khi nhận được yêu cầu kết nối từ client. hoặc khi client gửi thông điệp đến server
                _streamSocketListener.ConnectionReceived += ConnectionReceived;

                // Server bắt đầu lắng nghe kết nối
                await this._streamSocketListener.BindEndpointAsync(hostname, _port.ToString());

                // xuất ra màn hình bằng text box
                this.textboxDebug.Text += "Server started listen in IP:" + hostname.RawName +" and prefix: " + hostname.IPInformation.PrefixLength.ToString() +"\n";
            }catch (Exception ex)
            {
                this.textboxDebug.Text += ex.Message + "\n";
            }
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

        private async void ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // sự kiện được kích hoạt trên một threat khác, không có quyền truy cập trực tiếp đến biến cục bộ của thread hiện tại.
            // nên thay vì gán như thông thường ta cần dùng dispatcher để gán bằng threadS

            //this.textboxDebug.Text += "Server Received\n"; // => crash

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
              async  () =>
                {
                    textboxDebug.Text += "Server received message from: " +  args.Socket.Information.RemoteAddress + "\n";

                    if (_streamsocket == null)
                    {
                        _streamsocket = new StreamSocket();
                        await _streamsocket.ConnectAsync(args.Socket.Information.RemoteAddress, _port.ToString());
                    }
                    while (true)
                    {
                        // nếu đã tồn tại một thể hiện của lớp DataReader thì đối tượng đó tự động được giải phóng.
                        DataReader datareader = new DataReader(args.Socket.InputStream);
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
                            this.textboxDebug.Text += "Receive from " + args.Socket.Information.RemoteAddress + ": " + msg + "\n";
                        }
                        catch (Exception ex)
                        {
                            this.textboxDebug.Text += ex.Message + "\n";
                        }
                    }
                }
                );

        }

        private async void StartServer_Click(object sender, RoutedEventArgs e)
        {
            if (this._hostname != null)
            {
                await this.startListener(_hostname);
            }
        }

        private async void SendMesage_Click(object sender, RoutedEventArgs e)
        {
            string msg = textboxMessage.Text;
            await this.sendMessage(msg);
            this.textboxMessage.Text = String.Empty;
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            // nếu đóng vai trò là client.
            // ứng dụng cần kết nối đến servẻ thông qua IP người dùng nhập vào.
            string serverip = this.TextBoxIPSERVER.Text;
            HostName hostname = new HostName(serverip);

            // khởi tạo socket
            _streamsocket = new StreamSocket();
            _streamsocket.Control.KeepAlive = true;
            try
            {
                // kết nối đến server.
                // sử dụng phương thức connectasync mà không chỉ ra IP trên máy khách thì hệ thống tự động chọn IP phù hợp
                await _streamsocket.ConnectAsync(hostname, _port.ToString());

                // sau khi kết nối đến server, client cũng khởi tạo một listner để lắng nghe kết nối từ server.
                // nếu không. chỉ có thể truyền được dữ liệu từ client đến server. (Muốn nhận được dữ liệu cần có listener)
                if (this._streamSocketListener == null)
                {
                    // Vì mỗi thiết bị có thể thuộc nhiều mạng con, nên ta sử dụng stream socket để lấy ra IP cần sử dụng.
                    // Các bạn có thể sử dụng cách khác để lấy ra IP thích hợp. 

                    this._streamSocketListener = new StreamSocketListener();
                    await this.startListener(_streamsocket.Information.LocalAddress);       // => badcode
                }

                // Xuất ra màn hình bằng TextBox
                this.textboxDebug.Text += "Connected to " + hostname.RawName + "\n";
            }
            catch (Exception ex)
            {

                this.textboxDebug.Text += ex.Message + "\n";
            }

        }

        private void CloseConnection_Click(object sender, RoutedEventArgs e)
        {
            if (_streamSocketListener != null)
            {
                _streamSocketListener.ConnectionReceived -= ConnectionReceived;
                _streamSocketListener.Dispose();
                _streamSocketListener = null;
                this.textboxDebug.Text += "Connection Closed\n";

            }
            if (_streamsocket != null)
            {
                _streamsocket.Dispose();
                _streamsocket = null;
            }
        }
    }
}
