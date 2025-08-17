using System;                       // 基本系統功能，例如例外處理
using System.Drawing;               // 提供 Point、Size、Bitmap 等圖形處理功能
using System.Threading;             // 提供 Thread 多執行緒功能
using System.Windows.Forms;         // 提供 WinForms UI 元件
using OpenCvSharp;                  // OpenCvSharp 核心功能 (Mat, VideoCapture, CascadeClassifier)
using OpenCvSharp.Extensions;       // 提供 Mat 與 Bitmap 的轉換工具

namespace FaceDetectionWinForms
{
    // Form1 類別繼承自 Form，表示這是一個 Windows 視窗
    public partial class Form1 : Form
    {
        private VideoCapture capture;           // 攝影機物件，用來抓影像
        private CascadeClassifier faceCascade;  // 人臉分類器
        private Thread cameraThread;            // 用來抓影像的執行緒
        private bool running = false;           // 控制相機是否在運行

        // 建構子，初始化 UI
        public Form1()
        {
            InitializeComponent();  // 初始化 Form 控制元件（WinForms 預設方法）

            // 建立「開始偵測」按鈕
            var startButton = new Button
            {
                Text = "開始偵測",                         // 按鈕文字
                Location = new System.Drawing.Point(10, 10), // 按鈕位置 (X=10, Y=10)
                Size = new System.Drawing.Size(100, 30)       // 按鈕大小 (寬100, 高30)
            };
            startButton.Click += StartDetection;  // 點擊按鈕時觸發 StartDetection 方法

            // 建立 PictureBox 用來顯示影像
            pictureBox = new PictureBox
            {
                Location = new System.Drawing.Point(10, 50),  // 位置
                Size = new System.Drawing.Size(640, 480),     // 大小
                BorderStyle = BorderStyle.FixedSingle        // 邊框樣式
            };

            // 建立「結束程式」按鈕
            var exitButton = new Button
            {
              Text = "結束程式",                            // 按鈕文字
              Location = new System.Drawing.Point(100, 10), // 位置
              Size = new System.Drawing.Size(100, 30)       // 大小
            };
            exitButton.Click += StopDetection;             // 點擊按鈕時觸發 StopDetection 方法

            // 將按鈕與 PictureBox 加到 Form 控制元件裡
            Controls.Add(startButton);
            Controls.Add(pictureBox);
            Controls.Add(exitButton);
        }

        private PictureBox pictureBox; // 用來顯示相機畫面

        // 按下「開始偵測」的事件
        private void StartDetection(object sender, EventArgs e)
        {
            if (running) return;  // 如果相機已經在運行，就直接跳過

            // 初始化人臉分類器
            faceCascade = new CascadeClassifier("cascades/haarcascade_frontalface_default.xml");

            // 打開攝影機 (索引 0 通常是內建攝影機)
            capture = new VideoCapture(0);

            running = true;                 // 標記相機正在運行
            cameraThread = new Thread(CameraLoop); // 建立抓影像的執行緒
            cameraThread.Start();            // 啟動執行緒
        }

        // 相機影像抓取的執行緒
        private void CameraLoop()
        {
            using var frame = new Mat();  // 建立 OpenCvSharp Mat 物件存影像

            while (running)               // 如果相機正在運行
            {
                capture.Read(frame);      // 從攝影機讀取影像
                if (frame.Empty()) continue; // 如果讀不到影像，跳過

                // 將影像轉成灰階
                using var gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                // 偵測人臉
                var faces = faceCascade.DetectMultiScale(
                    gray,
                    scaleFactor: 1.1,                       // 每次圖像縮小的比例
                    minNeighbors: 5,                         // 每個候選矩形需要至少被幾個鄰居確認
                    minSize: new OpenCvSharp.Size(30, 30)   // 偵測最小人臉大小
                );

                // 在偵測到的人臉上畫紅色矩形
                foreach (var face in faces)
                {
                    Cv2.Rectangle(frame, face, Scalar.Red, 2);
                }

                // 將 OpenCvSharp Mat 轉成 Bitmap 顯示到 PictureBox
                Bitmap bmp = BitmapConverter.ToBitmap(frame);
                pictureBox.Invoke((MethodInvoker)(() => pictureBox.Image = bmp)); // 使用 Invoke 保護 UI
            }
        }

        // 按下「停止/結束程式」按鈕
        private void StopDetection(object sender, EventArgs e)
        {
            if (!running) return;             // 如果相機沒有運行，直接返回

            running = false;                  // 停止相機迴圈
            capture?.Release();               // 釋放攝影機資源
            capture = null;

            // 清空 PictureBox 影像
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();   // 釋放 Bitmap 資源
                pictureBox.Image = null;      // 清空 PictureBox
            }

            Application.Exit();               // 關閉程式
        }

        // 當 Form 被關閉時
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;       // 停止相機迴圈
            capture?.Release();    // 釋放攝影機資源
            base.OnFormClosing(e); // 呼叫父類別方法
        }
    }
}
