using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Proximity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace NFC_Example
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Attribute
        private long messageId = -1;
        ProximityDevice proximityDevice;
        #endregion

        #region Constuctor & OnNavigatedTo
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

        }
        #endregion

        #region Event Click
        private void StartClick(object sender, RoutedEventArgs e)
        {
            // Bắt đầu bằng cách lấy thiết bị NFC.
            string id = ProximityDevice.GetDeviceSelector();
            proximityDevice = ProximityDevice.GetDefault();

            if (proximityDevice == null)
            {
                this.OutputString("Devices not Found");
            }
            else
            {
                // Thêm sự kiện DeviceArrived và DeviceDeparted. 
                // Hai sự kiện được kích hoạt khi nhận được thiết bị trong vùng hạot động hoặc thiết bị rời khỏi vùng hoạt động
                proximityDevice.DeviceArrived += DeviceArrived;
                proximityDevice.DeviceDeparted +=DeviceDeparted;
                this.OutputString("Tap to connect ready");

                // Liên tục gởi gói tin, vì NFC là kết nối gần nên không cần bảo mật gì mà chỉ cần gửi gói tin đi thuần tuý
                // Tham số thứ nhất messageType, có dang Windows.*, với * là chuỗi tự định nghĩa. Ngoài ra còn có các format khác.
                this.proximityDevice.PublishMessage("Windows.demo", "My name is Tung");
            }
        }

        private void ClearClick(object sender, RoutedEventArgs e)
        {
            this.OutputTextblock.Text = String.Empty;
        }
        #endregion

        #region Departed & Arrived Event
        private async void DeviceDeparted(ProximityDevice sender)
        {
            // Những sự kiện này được kích hoạt trên thread khác nên không thể gán bình thường mà phải dùng Dispatcher
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    this.OutputString("Our friend left us");
                });
        }

        private async void DeviceArrived(ProximityDevice sender)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    this.OutputString("Someone arrive!!!");
                    // Nếu muốn nhận được msg giống như trên thì messageType phải giống ở trên
                    messageId = this.proximityDevice.SubscribeForMessage("Windows.demo", messageReceivedHandler);
                });
           // throw new NotImplementedException();
        }

        private async void messageReceivedHandler(ProximityDevice sender, ProximityMessage message)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    this.OutputString("Message Received");
                    this.OutputString("Sender is: " + sender.DeviceId);
                    this.OutputString("He said that: " + message.DataAsString);
                });
        }
        #endregion

        #region Private Method
        // xuất ra màn hình câu msg
        private void OutputString(string msg)
        {
            this.OutputTextblock.Text += msg + "\n";
        }
        #endregion

    }
}
