using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gwindalmir.ImageAnalysis.Algorithms
{
    public class PrincipleComponentAnalysis : IAnalysisAlgorithm
    {
        public Mat SourceImage { get; private set; }
        public Mat ResultImage { get; private set; }

        /// <summary>
        /// The Principle Component to generate.
        /// </summary>
        public int PC { get; private set; } = 0;

        /// <summary>
        /// Invert the image.
        /// </summary>
        public bool Invert{ get; private set; } = false;

        private string m_filename;

        public PrincipleComponentAnalysis(int pc = 0)
        {
            PC = pc;
        }

        public PrincipleComponentAnalysis(string filename)
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
        // ~PrincipleComponentAnalysis() {
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
            SourceImage = new Mat(filename, ImreadModes.Color);

            if (SourceImage.Empty())
                throw new ArgumentException();
        }

        void IAnalysisAlgorithm.AnalyzeImage()
        {
            AnalyzeImage();
        }

        public void AnalyzeImage(int pc = 1)
        {
            PC = pc;
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

        public void AnalyzeAndDisplay()
        {
            if (SourceImage.Empty())
                throw new ArgumentException();

            using (var window = new Window("Principle Component Analysis", WindowMode.AutoSize))
            {
                window.CreateTrackbar("PC", PC, 2, (i) =>
                {
                    PC = i;
                    window.ShowImage(ProcessImageForDisplay());
                });
                window.CreateTrackbar("Invert", 0, 1, (i) =>
                {
                    if (i == 1)
                        Invert = true;
                    else
                        Invert = false;
                    window.ShowImage(ProcessImageForDisplay());
                });
                window.ShowImage(ProcessImageForDisplay());
                NativeMethods.highgui_waitKey(-1, out var retVal);
            }
        }

        private Mat ProcessImageForDisplay()
        {
            return Resize(ProcessImage(), 1500, 800);
        }

        private Mat ProcessImage()
        {
            var tempImage = SourceImage;

            // Convert image to linear array, for PCA processing
            var linear = tempImage.Reshape(1, tempImage.Rows * tempImage.Cols);
            linear.ConvertTo(linear, MatType.CV_32FC1);

            // Perform PCA operation
            var pca = new PCA(linear, new Mat(), PCA.Flags.DataAsRow);

            // Method 1
            var points = pca.Project(linear);

            points = points.Reshape(tempImage.Channels(), tempImage.Rows);
            var pcChannels = points.Split();

            for (var ch = 0; ch < pcChannels.Length; ch++)
                pcChannels[ch] = pcChannels[ch].Normalize(0, 255, NormTypes.MinMax, MatType.CV_8UC1);

            var result = new Mat();
            Cv2.Merge(pcChannels, result);
            result = pcChannels[PC];

            // Process result
            if (Invert)
                Cv2.BitwiseNot(result, result);

            return result;
        }

        private Mat Resize(Mat image, int width, int height)
        {
            var size = image.Size();
            var max = new Size(width, height);

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
