using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace FaceDetectionWinForms
{
    public partial class Form1 : Form
    {
        private VideoCapture capture;
        private CascadeClassifier faceCascade;
        private Thread cameraThread;
        private bool running = false;

        public Form1()
        {
            InitializeComponent();

            // UI 元件
            var startButton = new Button
            {
                Text = "開始偵測",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(100, 30)  
            };
            startButton.Click += StartDetection;

            pictureBox = new PictureBox
            {
                Location = new System.Drawing.Point(10, 50),
                Size = new System.Drawing.Size(640, 480),
                BorderStyle = BorderStyle.FixedSingle
            };

            var exitButton = new Button
            {
              Text = "結束程式",
              Location = new System.Drawing.Point(100, 10),
              Size = new System.Drawing.Size(100, 30)
            };
            exitButton.Click += StopDetection;

            Controls.Add(startButton);
            Controls.Add(pictureBox);
            Controls.Add(exitButton);
        }

        private PictureBox pictureBox;

        private void StartDetection(object sender, EventArgs e)
        {
            if (running) return;

            faceCascade = new CascadeClassifier("cascades/haarcascade_frontalface_default.xml");
            capture = new VideoCapture(0);

            running = true;
            cameraThread = new Thread(CameraLoop);
            cameraThread.Start();
        }

        private void CameraLoop()
        {
            using var frame = new Mat();

            while (running)
            {
                capture.Read(frame);
                if (frame.Empty()) continue;

                // 灰階
                using var gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                // 偵測人臉
                var faces = faceCascade.DetectMultiScale(
                    gray,
                    scaleFactor: 1.1,
                    minNeighbors: 5,
                    minSize: new OpenCvSharp.Size(30, 30)
                );

                foreach (var face in faces)
                {
                    Cv2.Rectangle(frame, face, Scalar.Red, 2);
                }

                // OpenCvSharp Mat -> Bitmap 顯示到 PictureBox
                Bitmap bmp = BitmapConverter.ToBitmap(frame);
                pictureBox.Invoke((MethodInvoker)(() => pictureBox.Image = bmp));
            }
        }
        private void StopDetection(object sender, EventArgs e)
        {
         if (!running) return;

         running = false;               // 停止相機迴圈
         capture?.Release();            // 釋放攝影機資源
         capture = null;

        // 清空 PictureBox
        if (pictureBox.Image != null)
        {
         pictureBox.Image.Dispose();
         pictureBox.Image = null;
        }
        Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;
            capture?.Release();
            base.OnFormClosing(e); 
        }
    }
}
