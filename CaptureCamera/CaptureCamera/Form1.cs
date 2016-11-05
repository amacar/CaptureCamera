using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using AForge.Video;
using AForge.Video.DirectShow;

namespace CaptureCamera
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        }

        private VideoCaptureDevice videoSource;
        private Bitmap frameImage;
        private FilterInfoCollection videosources;
        private bool captureCanceled = true;

        private void Form1_Load(object sender, EventArgs e)
        {
            videosources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videosources != null)
            {
                if (videosources.Count > 0)
                {
                    foreach (FilterInfo VideoCaptureDevice in videosources)
                        comboBox1.Items.Add(VideoCaptureDevice.Name);

                    if (videosources.Count == 1)
                        comboBox1.SelectedIndex = 0;
                }
                else
                {
                    string text = "No camera detected!";
                    RectangleF rectf = new RectangleF(pictureBox1.Width / 2 - 130, pictureBox1.Height / 2, 300, 200);
                    frameImage = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                    Graphics g = Graphics.FromImage(frameImage);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.DrawString(text, new Font("Tahoma", 18), Brushes.Red, rectf);
                    g.Flush();

                    pictureBox1.Image = frameImage;
                }
            }

        }

        void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            if (frameImage != null)
                frameImage.Dispose();
            frameImage = null;
            try
            {
                frameImage = new Bitmap(eventArgs.Frame);
            }
            catch (Exception ex) { }

            if (pictureBox1.Image != null)
            {
                try
                {
                    this.BeginInvoke(new MethodInvoker(delegate () { pictureBox1.Image.Dispose(); pictureBox1.Image = frameImage; }));
                }
                catch (Exception ex) { }
            }

        }

        private void capture_Click(object sender, EventArgs e)
        {
            if (frameImage != null)
            {
                try
                {
                    string path = getSavePath();
                    if (path != null)
                    {
                        frameImage.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                        captureCanceled = false;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    RectangleF rectf = new RectangleF(pictureBox1.Width / 2 - 130, pictureBox1.Height / 2, 300, 200);
                    frameImage = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                    Graphics g = Graphics.FromImage(frameImage);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.DrawString("Error when saving captured frame!", new Font("Tahoma", 18), Brushes.Red, rectf);
                    g.Flush();

                    pictureBox1.Image = frameImage;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseVideoSource();
            if (captureCanceled && saveOnCaptureCanceled())
            {
                try
                {
                    string path = getSavePath();
                    if (path != null)
                    {
                        var image = new Bitmap(Properties.Resources.nophoto);
                        image.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }
                catch (Exception ex) { }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex < 0)
                return;

            CloseVideoSource();
            string text = "Initializing camera...";
            videoSource = new VideoCaptureDevice(videosources[comboBox1.SelectedIndex].MonikerString);

            try
            {
                if (videoSource.VideoCapabilities.Length > 0)
                {
                    string highestSolution = "0;0";
                    for (int i = 0; i < videoSource.VideoCapabilities.Length; i++)
                        if (videoSource.VideoCapabilities[i].FrameSize.Width > Convert.ToInt32(highestSolution.Split(';')[0]))
                            highestSolution = videoSource.VideoCapabilities[i].FrameSize.Width.ToString() + ";" + i.ToString();
                    //videoSource.VideoResolution = videoSource.VideoCapabilities[Convert.ToInt32(highestSolution.Split(';')[1])];
                }
            }
            catch { }

            videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(videoSource_NewFrame);

            RectangleF rectf = new RectangleF(pictureBox1.Width / 2 - 130, pictureBox1.Height / 2, 300, 200);
            frameImage = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics g = Graphics.FromImage(frameImage);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawString(text, new Font("Tahoma", 18), Brushes.Red, rectf);
            g.Flush();

            pictureBox1.Image = frameImage;

            videoSource.Start();
        }

        private void CloseVideoSource()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.NewFrame -= new NewFrameEventHandler(videoSource_NewFrame);
                videoSource = null;

            }
        }

        private string getSavePath()
        {
            try
            {
                string[] props = File.ReadAllLines("setup.ini");
                foreach (string prop in props)
                {
                    if (prop.StartsWith("path"))
                    {
                        string[] propSplitted = prop.Split('=');
                        return propSplitted[1].Trim();
                    }
                }
            }
            catch (Exception ex) { }

            SaveFileDialog savefile = new SaveFileDialog();
            savefile.FileName = "capture.jpg";
            savefile.Filter = "Jpg files (*.jpg)|*.jpg";
            if (savefile.ShowDialog() == DialogResult.OK)
                return savefile.FileName;

            return null;
        }

        private bool saveOnCaptureCanceled()
        {
            try
            {
                string[] props = File.ReadAllLines("setup.ini");
                foreach (string prop in props)
                {
                    if (prop.StartsWith("saveOnCancel"))
                    {
                        string[] propSplitted = prop.Split('=');
                        return propSplitted[1].Trim().ToLower() == "true";
                    }
                }
            }
            catch (Exception ex) { }

            return false;
        }
    }
}
