using Alturos.Yolo;
using Alturos.Yolo.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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
        YoloWrapper yoloWrapper;
        AutoResetEvent ev = new AutoResetEvent(true);
        

        class YoloEntry
        {
            public enum YoloSelection
            {
                T2,
                BUNNY
            }
            public readonly YoloSelection Selection;
            public readonly YoloConfiguration Conf;

            public YoloEntry(YoloSelection selection, YoloConfiguration conf)
            {
                Selection = selection;
                Conf = conf;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            SetupBackgroundWorker();
            List<YoloEntry> configs = new List<YoloEntry>
            {
                new YoloEntry(YoloEntry.YoloSelection.T2, new YoloConfiguration("yolov2-tiny.cfg", "yolov2-tiny.weights", "coco.names")),
                new YoloEntry(YoloEntry.YoloSelection.BUNNY, new YoloConfiguration("yolov3-tiny-tobii-bunny.cfg", "yolov3-tiny-tobii-bunny.weights", "bunny.names"))
            };

            SetupYolo(configs, YoloEntry.YoloSelection.T2);

            List<MenuItem> deviceMenuItems = new List<MenuItem>();

            MenuItem playbackMenuItem = new MenuItem("Start playback", new EventHandler(Playback));
            deviceMenuItems.Add(new MenuItem("-"));
            deviceMenuItems.Add(playbackMenuItem);
            MenuItem cocoMenuItem = new MenuItem("Coco", new EventHandler(Coco))
            {
                Checked = true
            };
            deviceMenuItems.Add(cocoMenuItem);
            MenuItem bunnyMenuItem = new MenuItem("Bunny", new EventHandler(Bunny));
            deviceMenuItems.Add(bunnyMenuItem);

            _pictureBox1.ContextMenu = new ContextMenu(deviceMenuItems.ToArray());
        }

        private void Playback(object sender, EventArgs e)
        {
            ((MenuItem)sender).Checked = !((MenuItem)sender).Checked;
            ((MenuItem)sender).Text = ((MenuItem)sender).Checked?"Stop playback": "Start playback";
            if (_bw.IsBusy)
            {
                _bw.CancelAsync();
            }
            else
            {
                _bw.RunWorkerAsync();
            }
        }

        private void Coco(object sender, EventArgs e)
        {
            ((MenuItem)sender).Checked = !((MenuItem)sender).Checked;
            if (_bw.IsBusy)
            {
                _bw.CancelAsync();
            }
        }

        private void Bunny(object sender, EventArgs e)
        {
            ((MenuItem)sender).Checked = !((MenuItem)sender).Checked;
        }

        private void EvalAndDraw()
        {
            ev.Reset();
            string _uripath = @"http://192.168.0.130:8080/shot.jpg";

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadDataCompleted += DownloadDataCompleted;
                    client.DownloadDataAsync(new Uri(_uripath));
                }
            }
            catch (WebException)
            {

            }

        }

        private void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            try
            {
                byte[] raw = e.Result;

                using (MemoryStream ms = new MemoryStream(raw))
                {
                    List<YoloItem> items = yoloWrapper.Detect(ms.ToArray()).ToList();
                    DrawImage(items, ms);
                    ev.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got exception: " + ex.Message);
            }

        }

        private void SetupYolo(List<YoloEntry> configs, YoloEntry.YoloSelection selection)
        {         
            if (configurationDetector == null)
            {
                var config = configs.Find(x => x.Selection == selection).Conf;
                if (config.IsValid)
                {
                    configurationDetector = new ConfigurationDetector(config);
                    config = configurationDetector.Detect();
                    yoloWrapper = new YoloWrapper(config);
                }
                else
                {
                    throw new InvalidDataException("Configuration files missing!");
                }
            }
        }

        public void DrawString()
        {
            Graphics formGraphics = this.CreateGraphics();
            string drawString = "Sample Text";
            Font drawFont = new Font("Arial", 16);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            float x = 150.0F;
            float y = 50.0F;
            StringFormat drawFormat = new StringFormat();
            formGraphics.DrawString(drawString, drawFont, drawBrush, x, y, drawFormat);
            drawFont.Dispose();
            drawBrush.Dispose();
            formGraphics.Dispose();
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

                        Font drawFont = new Font("Arial", 16);
                        SolidBrush drawBrush = new SolidBrush(Color.Green);
                        StringFormat drawFormat = new StringFormat();
                        canvas.DrawString(item.Type, drawFont, drawBrush, x-10, y-25, drawFormat);
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
            _bw.WorkerSupportsCancellation = true;
        }

        private void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            int i = 0;
            while (_bw.CancellationPending == false)
            {
                Stopwatch stopwatch = new Stopwatch();
                
                if (ev.WaitOne(1000 / _framerate) == true)
                {
                    stopwatch.Start();
                    EvalAndDraw();
                    stopwatch.Stop();
                    int delay = 1000 / _framerate - (int)stopwatch.ElapsedMilliseconds;
                    delay = (delay < 0) ? 0 : delay;
                    Task.Delay(delay).Wait();
                }

                _bw.ReportProgress(i); //Use for UI updates
                i++;
            }
        }




    }
}
