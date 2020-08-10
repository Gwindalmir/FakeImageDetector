using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Gwindalmir.ImageAnalysis
{
    /// <summary>
    /// Helper class for processing images.
    /// </summary>
    public class ImageAnalyzer
    {
        public ImageAnalyzer() { }

        /// <summary>
        /// Processes the specified image with each algorithm, then saves it.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="destination"></param>
        public void GenerateImage(string inputFile, string destination, AnalyzerAlgorithm algorithm)
        {
            var destinationPath = string.Format(destination, algorithm.ToString());
            var destinationFolder = Path.GetDirectoryName(destinationPath);

            if (File.Exists(destinationPath))
                return;
            else if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            using (var analyzer = AnalyzerFactory.Create(algorithm))
            {
                analyzer.LoadImage(inputFile);
                analyzer.AnalyzeAndSave(destinationPath);
            }
        }

        /// <summary>
        /// Processes the specified image with each algorithm, then saves it.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="destination"></param>
        public void GenerateImages(string inputFile, string destination)
        {
            foreach (AnalyzerAlgorithm algo in Enum.GetValues(typeof(AnalyzerAlgorithm)))
            {
                GenerateImage(inputFile, destination, algo);
            }
        }

        /// <summary>
        /// Processes the specified image with each algorithm.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="destination"></param>
        public Mat[] GenerateImages(string inputFile)
        {
            var result = new Mat[Enum.GetNames(typeof(AnalyzerAlgorithm)).Length];

            foreach (AnalyzerAlgorithm algo in Enum.GetValues(typeof(AnalyzerAlgorithm)))
            {
                using (var analyzer = AnalyzerFactory.Create(algo))
                {
                    analyzer.LoadImage(inputFile);
                    analyzer.AnalyzeImage();
                    result[(int)algo] = analyzer.GetResultImageSafe();
                }
            }

            return result;
        }

        /// <summary>
        /// Processes the specified image with each algorithm.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="destination"></param>
        public Mat GenerateImage(string inputFile, AnalyzerAlgorithm algorithm)
        {
            using (var analyzer = AnalyzerFactory.Create(algorithm))
            {
                analyzer.LoadImage(inputFile);
                analyzer.AnalyzeImage();
                return analyzer.GetResultImageSafe();
            }
        }

        /// <summary>
        /// Process a directory of images, recursively.
        /// </summary>
        /// <param name="sourceFolder">Location of source images.</param>
        /// <param name="destinationFolder">Location to place the processed images.</param>
        public static void ProcessImages(string sourceFolder, string destinationFolder)
        {
            Directory.CreateDirectory(destinationFolder);
            ProcessImagesRecursive(sourceFolder, destinationFolder, destinationFolder);
        }

        private static void ProcessImagesRecursive(string sourceFolder, string destinationFolder, string destinationRoot = null)
        {
            if (string.IsNullOrEmpty(destinationRoot))
                destinationRoot = destinationFolder;

            Debug.Assert(destinationFolder.StartsWith(destinationRoot));

            var destinationSuffix = destinationFolder.Remove(0, destinationRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var result = Parallel.ForEach(Directory.EnumerateFiles(sourceFolder), (s, pls) =>
            {
                var destinationPath = Path.Combine(destinationRoot, "{0}", destinationSuffix, Path.GetFileName(s));
                var analyzer = new ImageAnalyzer();
                analyzer.GenerateImages(Path.Combine(sourceFolder, Path.GetFileName(s)), destinationPath);
            });

            foreach (var entry in Directory.EnumerateDirectories(sourceFolder))
            {
                var entryName = Path.GetFileName(entry);
                var destPath = Path.Combine(destinationFolder, entryName);

                // Do post-order traversal processing
                ProcessImagesRecursive(entry, destPath, destinationRoot);
            }
        }

    }
}
