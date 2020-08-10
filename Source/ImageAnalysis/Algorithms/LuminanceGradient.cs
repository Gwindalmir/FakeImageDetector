using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;

namespace Gwindalmir.ImageAnalysis.Algorithms
{
    public class LuminanceGradient : IAnalysisAlgorithm
    {
        public Mat SourceImage { get; private set; }
        public Mat ResultImage { get; private set; }

        /// <summary>
        /// Perform equalization of image, leveling out the image data.
        /// </summary>
        public bool Equalize { get; private set; }
        
        /// <summary>
        /// Run a normalization algorithm to detect hot points.
        /// </summary>
        public int Normalize { get; private set; } = 1;
        
        /// <summary>
        /// Threshold for the normalization.
        /// </summary>
        public int Threshold { get; private set; } = 100;

        public LuminanceGradient(string filename = null)
        {
            if (!string.IsNullOrEmpty(filename))
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
        // ~LuminanceGradient() {
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

        void IAnalysisAlgorithm.AnalyzeImage()
        {
            AnalyzeImage();
        }

        public void AnalyzeImage(bool equalize = true, int normalize = 0)
        {
            Normalize = normalize;
            Equalize = equalize;

            ResultImage = ProcessImage();
        }

        public void AnalyzeAndSave(string targetFilename)
        {
            AnalyzeImage();
            ResultImage.SaveImage(targetFilename);
        }

        public void AnalyzeAndDisplay()
        {
            using (var window = new Window("Luminance Gradient", WindowMode.AutoSize))
            {
                window.CreateTrackbar(nameof(Equalize), 0, 1, (i) =>
                {
                    if (i == 1)
                        Equalize = true;
                    else
                        Equalize = false;
                    window.ShowImage(ProcessImageForDisplay());
                });
                window.CreateTrackbar(nameof(Normalize), Normalize, 2, (i) =>
                {
                    Normalize = i;
                    window.ShowImage(ProcessImageForDisplay());
                });

                window.CreateTrackbar(nameof(Threshold), Threshold, 255, (i) =>
                {
                    Threshold = i;
                    window.ShowImage(ProcessImageForDisplay());
                });

                window.ShowImage(ProcessImageForDisplay());
                NativeMethods.highgui_waitKey(-1, out var retVal);
            }
        }

        public void LoadImage(string filename)
        {
            SourceImage = new Mat(filename, ImreadModes.Grayscale);

            if (SourceImage.Empty())
                throw new ArgumentException();
        }

        private Mat ProcessImageForDisplay()
        {
            return Resize(ProcessImage(), 1500, 800);
        }

        private Mat ProcessImage()
        {
            Mat gradX, gradY, grad = null;

            using (var sobelX = SourceImage.Sobel(MatType.CV_32FC1, 1, 0, 1))
            using (var sobelY = SourceImage.Sobel(MatType.CV_32FC1, 0, 1, 1))
            {
                gradX = sobelX.Normalize(0, 1, NormTypes.MinMax);
                gradY = sobelY.Normalize(0, 1, NormTypes.MinMax);

                if (Normalize > 0)
                    using (var temp = (gradX.Pow(2) + gradY.Pow(2)).ToMat())
                        grad = temp.Sqrt();
            }

            Cv2.Normalize(gradX, gradX, 0, 255, NormTypes.MinMax, MatType.CV_8UC1);
            Cv2.Normalize(gradY, gradY, 0, 255, NormTypes.MinMax, MatType.CV_8UC1);

            if (Normalize > 0)
            {
                Cv2.Normalize(grad, grad, 0, 255, NormTypes.MinMax, MatType.CV_8UC1);
                Cv2.Threshold(grad, grad, Threshold, 255, Normalize == 1 ? ThresholdTypes.BinaryInv : ThresholdTypes.Binary);
            }

            if (Normalize == 2)
            {
                //var gradNormXY = grad.Normalize(0, 1, NormTypes.MinMax, MatType.CV_8UC1);
                //var gradNormYX = grad.Normalize(0, 1, NormTypes.MinMax, MatType.CV_8UC1);
                //gradX = gradX.Mul(1 / gradNormXY);
                //gradY = gradY.Mul(1 / gradNormYX);
            }

            if (Equalize)
            {
                Cv2.EqualizeHist(gradX, gradX);
                Cv2.EqualizeHist(gradY, gradY);
            }

            // Order is BGR
            var result = new Mat(SourceImage.Rows, SourceImage.Cols, MatType.CV_8UC3);

            if (Normalize > 0)
                grad.InsertChannel(result, 0);          // Blue
            else
                Cv2.InsertChannel(Mat.Zeros(SourceImage.Rows, SourceImage.Cols, MatType.CV_8UC1), result, 0);

            gradY.InsertChannel(result, 1);             // Green
            gradX.InsertChannel(result, 2);             // Red

            gradX.Dispose();
            gradY.Dispose();
            grad?.Dispose();
            return result;
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
