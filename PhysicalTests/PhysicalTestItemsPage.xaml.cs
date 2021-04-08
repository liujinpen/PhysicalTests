using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


namespace PhysicalTests.Pages
{
    /// <summary>
    /// PhysicalTestItemsPage.xaml 的交互逻辑
    /// </summary>
    public partial class PhysicalTestItemsPage : Page
    {
        private readonly Timer timer = new Timer();
        private readonly KinectSensorChooser sensorChooser;


        public PhysicalTestItemsPage()
        {
            InitializeComponent();

            // initialize the sensor chooser and UI
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            this.sensorChooser.Start();

            // Bind the sensor chooser's current sensor to the KinectRegion
            var regionSensorBinding = new System.Windows.Data.Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.kinectRegion, KinectRegion.KinectSensorProperty, regionSensorBinding);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //设置右上角手的图片
            var handImg = new BitmapImage(new Uri("Images/hand.png", UriKind.Relative));
            HandImg.Fill = new ImageBrush(handImg);

            //设置项目图片
            var standJumpImg = new BitmapImage(new Uri("Images/standjump.png", UriKind.Relative));
            standJumpBtn.Background = new ImageBrush(standJumpImg);

            var pullUpImg = new BitmapImage(new Uri("Images/pullup.png", UriKind.Relative));
            pullUpBtn.Background = new ImageBrush(pullUpImg);

            //设置加载页面的图片
            var image = new BitmapImage(new Uri("Images/Loading.png", UriKind.Relative));
            LoadImg.Fill = new ImageBrush(image);
            LoadImg.Visibility = Visibility.Collapsed;    //显示加载界面

            //Loading页面的持续时间(测试的时候先不开)
            //timer.Interval = 2000;
            //timer.Tick += Timer_Tick;
            //timer.Start();
        }

        //到达预设时间，关闭加载页面
        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadImg.Visibility = Visibility.Collapsed;
            timer.Tick -= Timer_Tick;
        }

        private static void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.DepthStream.Range = DepthRange.Default;
                    args.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
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

                    try
                    {
                        args.NewSensor.DepthStream.Range = DepthRange.Near;  //Near为近景模式
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        //跳转到立定跳远测试界面
        private void StandJumpBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new StandJumpPage());
        }

        //跳转到引体向上测试界面
        private void PullUpBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PullUpPage());
        }
    }
}
