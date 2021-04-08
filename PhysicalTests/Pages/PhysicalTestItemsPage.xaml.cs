using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using PhysicalTests.Pages;
using System.Windows.Threading;

namespace PhysicalTests.Pages
{
    /// <summary>
    /// PhysicalTestItemsPage.xaml 的交互逻辑
    /// </summary>
    public partial class PhysicalTestItemsPage : Page
    {
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private KinectSensorChooser sensorChooser;


        public PhysicalTestItemsPage()
        {
            InitializeComponent();

            LoadImg.Visibility = Visibility.Visible;    //显示加载界面

            //Loading页面的持续时间
            timer.Interval = 2000;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        //在页面载入时，在新线程中初始化Kinect
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(InitializeKinectSensor);
            thread.Start();
        }

        //到达预设时间，关闭Loading页面
        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadImg.Visibility = Visibility.Collapsed;
            timer.Tick -= Timer_Tick;
        }

        //Kinect初始化需要几秒钟，需要在新线程中进行，否则登录界面会假死
        //在操作完成后，通过向UI线程的Dispatcher列队注册工作项，来通知UI线程更新结果
        private void InitializeKinectSensor()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                // initialize the sensor chooser and UI
                sensorChooser = new KinectSensorChooser();
                sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
                sensorChooser.Start();

                // Bind the sensor chooser's current sensor to the KinectRegion
                var regionSensorBinding = new System.Windows.Data.Binding("Kinect") { Source = this.sensorChooser };
                BindingOperations.SetBinding(this.kinectRegion, KinectRegion.KinectSensorProperty, regionSensorBinding);
            });
        }

        private static void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    args.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    args.NewSensor.SkeletonStream.Enable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        //关闭Kinect
        private void CloseKinet()
        {
            if (null != this.sensorChooser)
            {
                sensorChooser.KinectChanged -= SensorChooserOnKinectChanged;
                sensorChooser.Stop();
            }
        }

        //跳转到立定跳远测试界面
        private void StandJumpBtn_Click(object sender, RoutedEventArgs e)
        {
            //关闭Kinect
            CloseKinet();

            NavigationService.Navigate(new StandJumpPage());
        }

        //跳转到引体向上测试界面
        private void PullUpBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseKinet();
            NavigationService.Navigate(new PullUpPage());
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //关闭Kinect
            CloseKinet();
        }
    }
}
