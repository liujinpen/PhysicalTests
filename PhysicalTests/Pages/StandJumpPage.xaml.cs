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
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect;
using System.Windows.Threading;
using System.IO;

namespace PhysicalTests.Pages
{
    /// <summary>
    /// StandJumpPage.xaml 的交互逻辑
    /// </summary>
    public partial class StandJumpPage : Page
    {
        #region 各种变量
        //Width and height of output drawing
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;

        //连接关节线的宽度
        private const double JointThickness = 5;

        //人体中心圆形大小
        private const double BodyCenterThickness = 15;

        //边缘检测矩形宽度
        private const double ClipBoundsThickness = 5;

        //人体中心圆形颜色
        private readonly Brush centerPointBrush = Brushes.Blue;

        //当前追踪的关节点颜色
        //private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush trackedJointBrush = Brushes.Red;

        //骨骼图像的背景颜色
        //private readonly Brush skeletonImageBackground = new SolidColorBrush(Color.FromArgb(255, 55, 179, 195));

        //推测出的关节点颜色  
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        //追踪到的骨骼颜色
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        //推测出的骨骼颜色       
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        //Drawing group for skeleton rendering output
        private DrawingGroup skeletonDrawingGroup;

        //Drawing image that we will display
        private DrawingImage skeletonImageSource;

        //性能比较高的彩色图片写入
        private WriteableBitmap colorImageBitmap;
        private Int32Rect colorImageBitmapRect;
        private int colorImageStride;

        //图片大小和像素字节数
        private int colorImageFrameWidth;
        private int colorImageFrameHeight;
        private int colorImageFramePixel;

        //计时器
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        //开启Kinect
        private KinectSensor kinectSensor;

        #endregion

        public StandJumpPage()
        {
            InitializeComponent();

            LoadImg.Visibility = Visibility.Visible;    //显示加载界面

            //Loading页面的持续时间
            timer.Interval = 2000;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        //到达预设时间，关闭Loading页面
        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadImg.Visibility = Visibility.Collapsed;
            timer.Tick -= Timer_Tick;
        }

        //在页面载入时，在新线程中初始化Kinect
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(InitializeKinectSensor);
            thread.Start();
        }

        //Kinect初始化需要几秒钟，需要在新线程中进行，否则登录界面会假死
        //在操作完成后，通过向UI线程的Dispatcher列队注册工作项，来通知UI线程更新结果
        private void InitializeKinectSensor()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                foreach (var potentialSensor in KinectSensor.KinectSensors)
                {
                    if (potentialSensor.Status == KinectStatus.Connected)
                    {
                        this.kinectSensor = potentialSensor;
                        break;
                    }
                }
                //开启彩色数据流，骨骼数据流
                if (null != this.kinectSensor)
                {
                    kinectSensor.ColorStream.Enable();
                    kinectSensor.SkeletonStream.Enable();

                    colorImageFrameWidth = kinectSensor.ColorStream.FrameWidth;
                    colorImageFrameHeight = kinectSensor.ColorStream.FrameHeight;
                    colorImageFramePixel = kinectSensor.ColorStream.FrameBytesPerPixel;

                    //为ColorImage控件准备
                    //初始化WriteableBitmap以及绘制区域
                    colorImageBitmap = new WriteableBitmap(
                    colorImageFrameWidth,
                    colorImageFrameHeight,
                    96, 96,
                    PixelFormats.Bgr32, null);

                    colorImageBitmapRect = new Int32Rect(0, 0, colorImageFrameWidth, colorImageFrameHeight);
                    colorImageStride = colorImageFrameWidth * colorImageFramePixel;

                    //为SkeletonImage控件准备
                    //初始化骨骼追踪界面的Drawgroup，指定图像源，绑定到前台的SkeletonImage控件
                    this.skeletonDrawingGroup = new DrawingGroup();
                    this.skeletonImageSource = new DrawingImage(this.skeletonDrawingGroup);
                    SkeletonImage.Source = this.skeletonImageSource;

                    //订阅帧到达事件
                    kinectSensor.AllFramesReady += Kinect_AllFramesReady;

                    //开启Kinect
                    try
                    {
                        this.kinectSensor.Start();
                    }
                    catch (IOException)
                    {
                        this.kinectSensor = null;
                    }
                }
            });
        }

        #region 主要逻辑处理
        private void Kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            #region 彩色图像处理
            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                {
                    return;
                }

                byte[] pixels = new byte[colorImageFrame.PixelDataLength];
                colorImageFrame.CopyPixelDataTo(pixels);

                colorImageBitmap.WritePixels(colorImageBitmapRect, pixels, colorImageStride, 0);
                ColorImage.Source = colorImageBitmap;

            }
            #endregion

            #region 骨骼图像处理
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.skeletonDrawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                // 骨骼图像背景
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        // 是否超出范围
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc); //画出关节点和之间的连线
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),//以关节点坐标为中心画圆
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // 保证绘制在限定区域内
                this.skeletonDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
            #endregion


        }
        #endregion


        #region 骨骼图像绘制相关函数
        /// <summary>
        /// 当骨骼超出图像区域的时候，在周围绘制相应的边框提示
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            //超出了骨骼绘制范围
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Yellow,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Yellow,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Yellow,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Yellow,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }


        /// <summary>
        /// 画出骨骼和关节点
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            // 画关节点
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }

                /*//把左手单独画成图片（测试代码）
                if (drawBrush != null && joint.JointType == JointType.HandLeft)
                {
                    var X = this.SkeletonPointToScreen(joint.Position).X - 32.0;
                    var Y = this.SkeletonPointToScreen(joint.Position).Y - 32.0;
                    var bitmapImage = new BitmapImage(new System.Uri("Images/hand.png", System.UriKind.Relative));
                    drawingContext.DrawImage(bitmapImage, new Rect(X, Y, 64.0, 64.0));
                }*/
            }
        }

        /// <summary>
        /// 子函数，在DrawBonesAndJoints中调用，画出关节之间的连线
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            ColorImagePoint depthPoint = this.kinectSensor.CoordinateMapper.MapSkeletonPointToColorPoint(skelpoint, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }
        #endregion

        //关闭Kinect
        private void CloseKinet()
        {
            if (null != this.kinectSensor)
            {
                kinectSensor.AllFramesReady -= Kinect_AllFramesReady;
                kinectSensor.Stop();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            CloseKinet();
        }
    }
}
