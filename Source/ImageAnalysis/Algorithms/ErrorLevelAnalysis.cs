using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gwindalmir.ImageAnalysis.Algorithms
{
    /// <summary>
    /// Performs an error-level analysis of an image.
    /// </summary>
    public class ErrorLevelAnalysis : IAnalysisAlgorithm
    {
        public Mat SourceImage { get; private set; }
        public Mat ResultImage { get; private set; }
        
        /// <summary>
        /// The amount to scale the data by (in effect, changes the brightness of the result).
        /// </summary>
        public int Scale { get; set; } = 15;    // 15

        /// <summary>
        /// The quality to save the intermediate image for calculating the difference.
        /// </summary>
        public int Quality { get; set; } = 95;
        
        /// <summary>
        /// A high-pass filter for trimming low data.
        /// </summary>
        public int Squelch { get; set; } = 0;   // 20
        private string m_filename;

        public ErrorLevelAnalysis()
        { }

        public ErrorLevelAnalysis(string filename)
        {
            m_filename = filename;
            LoadImage(filename);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SourceImage?.Dispose();
                    ResultImage?.Dispose();
                    SourceImage = null;
                    ResultImage = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ErrorLevelAnalysis() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public void LoadImage(string filename)
        {
            m_filename = filename;

            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException();

            //SourceImage = Binding.cv2.imread(filename, IMREAD_COLOR.IMREAD_COLOR);
            SourceImage = new Mat(filename, ImreadModes.Color);

            if (SourceImage.Empty())
                throw new ArgumentException();
        }

        public void AnalyzeAndDisplay()
        {
            using (var window = new Window("Error Level Analysis", WindowMode.AutoSize))
            {
                window.CreateTrackbar("Scale", Scale, 100, (i) =>
                {
                    Scale = i;
                    window.ShowImage(ProcessImageForDisplay());
                });

                window.CreateTrackbar("Quality", Quality, 100, (i) =>
                {
                    Quality = i;
                    window.ShowImage(ProcessImageForDisplay());
                });
                window.CreateTrackbar("Squelch", Squelch, 100, (i) =>
                {
                    Squelch = i;
                    window.ShowImage(ProcessImageForDisplay());
                });

                window.ShowImage(ProcessImageForDisplay());
                NativeMethods.highgui_waitKey(-1, out var retVal);
            }
        }

        public void AnalyzeImage()
        {
            try
            {
                ResultImage = ProcessImage();
            }
            catch
            {
                Console.WriteLine($"Error processing file: {m_filename}");
                throw;
            }
        }

        public void AnalyzeAndSave(string targetFilename)
        {
            AnalyzeImage();
            ResultImage.SaveImage(targetFilename);
        }

        private Mat ProcessImageForDisplay()
        {
            return Resize(ProcessImage(), 1500, 800);
        }

        private Mat ProcessImage()
        {
            var tempStream = new MemoryStream();

            // Write out compressed image to temporary buffer
            SourceImage.WriteToStream(tempStream, ".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, Quality));
            tempStream.Seek(0, SeekOrigin.Begin);

            // Re-read the image, so we can compare
            var destinationImage = Mat.FromStream(tempStream, ImreadModes.Unchanged);

            // Calculate the difference
            var elaResult = (Mat)((SourceImage - destinationImage));
            byte extrema = 0;
            byte squelch = (byte)(Squelch * 255 / 100);

            // Determine the global maximum, to automatically adjust Scale.
            unsafe
            {
                elaResult.ForEachAsVec3b((v, p) =>
                {
                    if ((*v)[1] > extrema)
                        extrema = (*v)[1];
                });
            }

            if (extrema == 0)
                extrema = 1;

            // Adjust the scale automatically
            Scale = 255 / extrema;
            elaResult *= Scale;

            if (squelch > 0)
            {
                unsafe
                {
                    elaResult.ForEachAsVec3b((v, p) =>
                    {
                        // Apply scale, and check against squelch
                        var v0 = v->Item0 * Scale;
                        var v1 = v->Item1 * Scale;
                        var v2 = v->Item2 * Scale;

                        if (v0 > byte.MaxValue)
                            v0 = byte.MaxValue;
                        else if (v0 < squelch)
                            v0 = 0;

                        if (v1 > byte.MaxValue)
                            v1 = byte.MaxValue;
                        else if (v1 < squelch)
                            v1 = 0;

                        if (v2 > byte.MaxValue)
                            v2 = byte.MaxValue;
                        else if (v2 < squelch)
                            v2 = 0;

                        (*v)[0] = (byte)v0;
                        (*v)[1] = (byte)v1;
                        (*v)[2] = (byte)v2;
                    });
                }
            }
            return elaResult;
        }

        private Mat Resize(Mat image, int width, int height)
        {
            var size = image.Size();
            var max = new Size(1500, 800);

            if (size.Width > max.Width || size.Height > max.Height)
            {
                var scale = Math.Min((float)max.Width / size.Width, (float)max.Height / size.Height);

                image = image.Resize(new Size(size.Width * scale, size.Height * scale));
            }
            return image;
        }

        public Mat GetResultImageSafe()
        {
            return ResultImage.Clone();
        }
    }
}
