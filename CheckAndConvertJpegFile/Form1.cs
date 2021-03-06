﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CheckAndConvertJpegFile
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            textBox1.Text = "Convert Progressive JPEG to Baseline JPEG\r\n for HackBGRT MULTI\r\nhttps://github.com/FREEWING-JP/HackBGRT";
            textBox1.Select(0, 0);
            label2.Text = "Drag and Drop JPEG file here";

            // JPEG Output Quality
            comboBox1.Items.Add("100");
            comboBox1.Items.Add("97");
            comboBox1.Items.Add("95");
            comboBox1.Items.Add("90");
            comboBox1.Items.Add("88");
            comboBox1.Items.Add("85");
            comboBox1.Items.Add("82");
            comboBox1.Items.Add("80");
            comboBox1.Items.Add("77");
            comboBox1.Items.Add("75");
            // JPEG Output Quality Default = 95
            comboBox1.Text = "95";

            // Output File Name = Prefix + Base File Name + Postfix + ext
            // File Name Prefix
            textBox2.Text = "out_";
            // File Name Postfix
            textBox3.Text = "";

            // Output Directory Path
            // var pathWithEnv = @"%USERPROFILE%\Pictures";
            // var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);
            var filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            textBox4.Text = filePath;
            if (!checkBox3.Checked)
            {
                checkBox3.Checked = true;
            }

            // Image Output Format
            comboBox2.Items.Add("JPEG Baseline");
            comboBox2.Items.Add("JPEG Progressive");
            comboBox2.Items.Add("PNG");
            comboBox2.Items.Add("BMP");
            // Default = JPEG Baseline
            comboBox2.SelectedIndex = 0;
        }

        private void CheckAndConvertJpegFile(string inputFilePath)
        {
            label2.Text = "";

            int qlty = 0;
            if (comboBox2.SelectedIndex <= 1)
            {
                string qltyStr = comboBox1.Text;
                if (!int.TryParse(qltyStr, out qlty))
                {
                    label2.Text = "Quality Error";
                    return;
                }
                if ((qlty > 100) || (qlty < 0))
                {
                    label2.Text = "Quality must be 0 - 100";
                    return;
                }
            }

            // Skip JPEG Progressive Check
            if (false && CheckFileExtensionIsJpeg(inputFilePath))
            {

                // [SOI 0xFFD8]-[SOF 0xFFCx]-[SOS 0xFFDA]
                // Search SOF0 Baseline Standard 0xFFC0 or Not
                // Search SOF2 Progressive 0xFFC2
                byte[] rbuff = ReadByteFile(inputFilePath);
                bool isBaseline = false;
                int seq = 0;
                int pos = 0;
                while (pos < rbuff.Length)
                {
                    byte b = rbuff[pos++];
                    if (b != (byte)0xFF) continue;

                    if (pos >= rbuff.Length)
                        break;

                    b = rbuff[pos++];
                    if (seq == 0)
                    {
                        // SOI ?
                        if (b != (byte)0xD8) continue;
                        ++seq;
                        continue;
                    }

                    if (seq == 1)
                    {
                        // SOF ?
                        if ((b & (byte)0xF0) != (byte)0xC0) continue;

                        // SOF0 ?
                        if (b == (byte)0xC0)
                        {
                            isBaseline = true;
                        }
                        break;
                    }

                    if (seq == 1)
                    {
                        // SOS ?
                        if (b != (byte)0xDA) continue;

                        label2.Text = "Invalid JPEG file";
                        return;
                    }
                }

                if (isBaseline)
                {
                    label2.Text = "Is Baseline JPEG";
                    return;
                }
            }

            // Image Format
            string fileExt = ".jpg";
            if (comboBox2.SelectedIndex == 2)
            {
                fileExt = ".png";
            }
            else if (comboBox2.SelectedIndex == 3)
            {
                fileExt = ".bmp";
            }

            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string filePath = Path.GetDirectoryName(inputFilePath);

            // Add Prefix and Postfix
            string prefix = textBox2.Text;
            string postfix = textBox3.Text;
            string outFileName = prefix + fileName + postfix + fileExt;
            // Output Directory Path
            if (!checkBox3.Checked)
            {
                filePath = textBox4.Text;
            }
            string outFilePath = Path.Combine(filePath, outFileName);
            if (File.Exists(outFilePath) && !checkBox2.Checked)
            {
                label2.Text = "Exists Same Filename";
                return;
            }

            try
            {
                using (Image source = Image.FromFile(inputFilePath))
                {
                    // https://developers.google.com/speed/docs/insights/OptimizeImages
                    // long GOOGLE_OPTIMIZE_JPEG_QUALUTY = 85L;
                    // long JPEG_QUALUTY = 95L;
                    if (comboBox2.SelectedIndex == 0)
                    {
                        // Baseline JPEG
                        var formatId = ImageFormat.Jpeg.Guid;
                        ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == formatId);

                        long JPEG_QUALUTY = qlty;

                        EncoderParameters parameters = new EncoderParameters(3);
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, JPEG_QUALUTY);
                        parameters.Param[1] = new EncoderParameter(Encoder.ScanMethod, (int)EncoderValue.ScanMethodNonInterlaced);
                        parameters.Param[2] = new EncoderParameter(Encoder.RenderMethod, (int)EncoderValue.RenderNonProgressive);
                        source.Save(outFilePath, codec, parameters);
                    }
                    else if (comboBox2.SelectedIndex == 1)
                    {
                        // TODO: Windows XP 32bit Not Work
                        // TODO: Windows 10 20H2 64bit Not Work
                        // Progressive JPEG
                        var formatId = ImageFormat.Jpeg.Guid;
                        ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == formatId);

                        long JPEG_QUALUTY = qlty;

                        EncoderParameters parameters = new EncoderParameters(3);
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, JPEG_QUALUTY);
                        parameters.Param[1] = new EncoderParameter(Encoder.ScanMethod, (int)EncoderValue.ScanMethodInterlaced);
                        parameters.Param[2] = new EncoderParameter(Encoder.RenderMethod, (int)EncoderValue.RenderProgressive);
                        source.Save(outFilePath, codec, parameters);
                    }
                    else if (comboBox2.SelectedIndex == 2)
                    {
                        // PNG
                        source.Save(outFilePath, ImageFormat.Png);
                    }
                    else if (comboBox2.SelectedIndex == 3)
                    {
                        // Bitmap
                        source.Save(outFilePath, ImageFormat.Bmp);
                    }

                    label2.Text = "Convert OK";
                }
            }
            catch (Exception ex)
            {
                label2.Text = "Convert ERROR";
            }
        }

        private byte[] ReadByteFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // long readLength = Math.Min(fs.Length, 2048L);
                long readLength = fs.Length;
                byte[] buffer = new byte[readLength];
                fs.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }
        private bool CheckFileExtensionIsJpeg(string fileName)
        {
            string ext = Path.GetExtension(fileName); // .ToLower(CultureInfo.InvariantCulture);

            string[] JPEG = new string[] { ".jpg", ".jpeg" };
            if (JPEG.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private bool CheckFileExtensionIsOthers(string fileName)
        {
            string ext = Path.GetExtension(fileName); // .ToLower(CultureInfo.InvariantCulture);

            string[] OTHER_IMAGES = new string[] { ".bmp", ".gif", ".tif", ".tiff", ".png" };
            if (OTHER_IMAGES.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] fileNames =
                (string[])e.Data.GetData(DataFormats.FileDrop, false);

            if (fileNames.Length != 1)
                return;

            string fileName = fileNames[0];
            if (CheckFileExtensionIsJpeg(fileName))
            {
            }
            else if (CheckFileExtensionIsOthers(fileName) && checkBox1.Checked)
            {
            }
            else
            {
                label2.Text = "Not Support";
                return;
            }

            e.Effect = DragDropEffects.Copy;
            textBox1.Text = fileName;
            label2.Text = "Drop Here !";
        }

        private void Form1_DragLeave(object sender, EventArgs e)
        {
            label2.Text = "";
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] fileNames =
                (string[])e.Data.GetData(DataFormats.FileDrop, false);

            string fileName = fileNames[0];

            CheckAndConvertJpegFile(fileName);
        }

        private void label1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.neko.ne.jp/~freewing/");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Select Output Directory Path
            string filePath = textBox4.Text;
            if (!Directory.Exists(filePath))
            {
                filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            // Select Folder UI type
            bool b = false;
            if (b)
            {
                FolderBrowserDialog foldereDialog = new FolderBrowserDialog();
                foldereDialog.Description = "Select Output Folder";
                foldereDialog.RootFolder = Environment.SpecialFolder.Desktop;
                foldereDialog.SelectedPath = filePath;
                foldereDialog.ShowNewFolderButton = true;
                if (foldereDialog.ShowDialog(this) == DialogResult.OK)
                {
                    filePath = foldereDialog.SelectedPath;
                    textBox4.Text = filePath;
                }
            }
            else
            {
                // Select Output Directory Path
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.InitialDirectory = filePath;
                fileDialog.ValidateNames = false;
                fileDialog.CheckFileExists = false;
                fileDialog.CheckPathExists = true;
                fileDialog.Title = "Select Output Folder";
                fileDialog.FileName = "(Select Output Folder)";
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = Path.GetDirectoryName(fileDialog.FileName);
                    textBox4.Text = filePath;
                }
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            button1.Enabled = !cb.Checked;
            textBox4.Enabled = !cb.Checked;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            var enabled = (comboBox2.SelectedIndex <= 1);
            label3.Enabled = enabled;
            comboBox1.Enabled = enabled;
        }
    }
}
