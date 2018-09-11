using Alturos.Yolo;
using Alturos.Yolo.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DarknetSharpDemo
{
    public partial class MainWindow : Form
    {

        BackgroundWorker _bw = new BackgroundWorker();
        int _framerate = 10;
        ConfigurationDetector configurationDetector = null;
        YoloConfiguration config;
        YoloWrapper yoloWrapper;
        AutoResetEvent ev = new AutoResetEvent(true);
        

        class YoloEntry
        {
            public readonly string Name;
            public readonly YoloConfiguration Conf;

            public YoloEntry(string name, YoloConfiguration conf)
            {
                Name = name;
                Conf = conf;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            SetupBackgroundWorker();
            List<YoloEntry> configs = new List<YoloEntry>();
            configs.Add(new YoloEntry("t2", new YoloConfiguration("yolov2-tiny.cfg", "yolov2-tiny.weights", "coco.names")));
            configs.Add(new YoloEntry("bunny", new YoloConfiguration("yolov3-tiny-tobii-bunny.cfg", "yolov3-tiny-tobii-bunny.weights", "bunny.names")));

            SetupYolo(configs);
        }

        private void EvalAndDraw()
        {
            ev.Reset();
            string _uripath = @"http://192.168.0.130:8080/shot.jpg";

            try
            {
                List<YoloItem> items;
                using (WebClient client = new WebClient())
                {
                    using (MemoryStream ms = new MemoryStream(client.DownloadData(new Uri(_uripath))))
                    {
                        items = yoloWrapper.Detect(ms.ToArray()).ToList();
                        DrawImage(items, ms);
                        ev.Set();
                    }
                }
            }
            catch (WebException)
            {

            }

        }

        private void SetupYolo(List<YoloEntry> configs)
        {         
            if (configurationDetector == null)
            {
                var config = configs.Find(x => x.Name == "bunny").Conf;
                configurationDetector = new ConfigurationDetector(config);
                config = configurationDetector.Detect();
                yoloWrapper = new YoloWrapper(config);
            }
        }

        private void DrawImage(List<YoloItem> items, MemoryStream ms, YoloItem selectedItem = null)
        {

            Image image = Image.FromStream(ms);

            using (var canvas = Graphics.FromImage(image))
            {
                // Modify the image using g here... 
                // Create a brush with an alpha value and use the g.FillRectangle function
                foreach (var item in items.Where(x=>x.Confidence > 0.5))
                {
                    var x = item.X;
                    var y = item.Y;
                    var width = item.Width;
                    var height = item.Height;

                    Console.WriteLine("Drawing:" + item.Type);
                    using (var overlayBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 102)))
                    using (var pen = this.GetBrush(item.Confidence, image.Width))
                    {
                        if (item.Equals(selectedItem))
                        {
                            canvas.FillRectangle(overlayBrush, x, y, width, height);
                        }

                        canvas.DrawRectangle(pen, x, y, width, height);
                        canvas.Flush();
                    }
                }
            }

            var oldImage = this._pictureBox1.Image;
            if (_pictureBox1.InvokeRequired)
            {
                _pictureBox1.Invoke(new MethodInvoker(() => { this._pictureBox1.Image = image; }));
            }
            else
            {
                this._pictureBox1.Image = image;
            }
            
            oldImage?.Dispose();
            ev.Set();
        }

        private Pen GetBrush(double confidence, int width)
        {
            var size = width / 100;

            if (confidence > 0.5)
            {
                return new Pen(Brushes.GreenYellow, size);
            }
            else if (confidence > 0.2 && confidence <= 0.5)
            {
                return new Pen(Brushes.Orange, size);
            }

            return new Pen(Brushes.DarkRed, size);
        }

        private void SetupBackgroundWorker()
        {
            // Setup background worker
            _bw.DoWork += BackgroundWorkerDoWork;
            _bw.ProgressChanged += BackgroundWorkerProgressChanged;
            _bw.WorkerReportsProgress = true;
            _bw.WorkerSupportsCancellation = true;
        }
        private void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            int i = 0;
            while (_bw.CancellationPending == false)
            {
                Task.Delay(1000 / _framerate).Wait();
                _bw.ReportProgress(i); //Use for UI updates
                i++;
            }
        }

        private async void BackgroundWorkerProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (ev.WaitOne(100) == true)
            {
                await Task.Run(() => EvalAndDraw());
            }
        }

        private void _pictureBox1_Click(object sender, EventArgs e)
        {
            if (_bw.IsBusy)
            {
                _bw.CancelAsync();
            }
            else
            {
                _bw.RunWorkerAsync();
            }
        }
    }
}
