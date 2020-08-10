using CommandLine;
using Gwindalmir.ImageAnalysis;
using Gwindalmir.ImageAnalysis.Algorithms;
using Gwindalmir.NeuralNetwork;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Gwindalmir.NeuralNetworkTraining
{
    public class Options
    {
        [Option("trainpathsrc", Default = @"C:\Capstone\DataSets\real-vs-fake\train", Required = false, HelpText = "The location of the training data")]
        public string TrainingSourcePath { get; set; }

        [Option("valpathsrc", Default = @"C:\Capstone\DataSets\real-vs-fake\valid", Required = false, HelpText = "The location of the validation data")]
        public string ValidationSourcePath { get; set; }

        [Option("testpathsrc", Default = @"C:\Capstone\DataSets\real-vs-fake\test", Required = false, HelpText = "The location of the test data")]
        public string TestSourcePath { get; set; }

        [Option("trainpathdst", Default = @"C:\Capstone\DataSets\real-vs-fake\processed_training", Required = false, HelpText = "The destination where processed training images are placed")]
        public string TrainingDestinationPath { get; set; }

        [Option("valpathdst", Default = @"C:\Capstone\DataSets\real-vs-fake\processed_validation", Required = false, HelpText = "The destination where processed validation images are placed")]
        public string ValidationDestinationPath { get; set; }

        [Option("testpathdst", Default = @"C:\Capstone\DataSets\real-vs-fake\processed_test", Required = false, HelpText = "The destination where processed test images are placed")]
        public string TestDestinationPath { get; set; }

        [Option('a', "algorithm", Required = false, HelpText = "The algorithm to select for testing or training")]
        public AnalyzerAlgorithm? Algorithm { get; set; }

        [Option('s', "steps", Default = 64, Required = false, HelpText = "The number of steps per epoch")]
        public int StepsPerEpoch { get; set; }

        [Option('e', "epochs", Default = 5, Required = false, HelpText = "The number of epochs to train for")]
        public int Epochs { get; set; }

        [Option('m', "matrix", Default = false, Required = false, HelpText = "Display the accuracy of the model against a dataset")]
        public bool ConfusionMatrix { get; set; }

        [Option('t', "test", Default = null, Required = false, HelpText = "Display a single single image, allows tweaking parameters")]
        public string TestImage { get; set; }
    }
    static class Program
    {
        const bool _alwaysPreprocess = false;
        static readonly Tuple<int, int> _imageSize = Tuple.Create(256, 256);
        static private AnalyzerAlgorithm? _algorithm = null;

        static string _SourceTrainingLocation = @"C:\Capstone\DataSets\real-vs-fake\train";
        static string _SourceValidationLocation = @"C:\Capstone\DataSets\real-vs-fake\valid";
        static string _SourceTestLocation = @"C:\Capstone\DataSets\real-vs-fake\test";

        static string _ProcessedTrainingLocation = @"C:\Capstone\DataSets\real-vs-fake\processed_training";
        static string _ProcessedValidationLocation = @"C:\Capstone\DataSets\real-vs-fake\processed_validation";
        static string _ProcessedTestLocation = @"C:\Capstone\DataSets\real-vs-fake\processed_test";

        static void Main(string[] args)
        {
            int steps = 0;
            int epochs = 0;
            bool generateConfusionMatrix = false;
            string testImage = null;

            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                if (!string.IsNullOrEmpty(o.TrainingSourcePath))
                    _SourceTrainingLocation = o.TrainingSourcePath;
                if (!string.IsNullOrEmpty(o.ValidationSourcePath))
                    _SourceValidationLocation ??= o.ValidationSourcePath;
                if (!string.IsNullOrEmpty(o.TestSourcePath))
                    _SourceTestLocation ??= o.TestSourcePath;

                if (!string.IsNullOrEmpty(o.TrainingDestinationPath))
                    _ProcessedTrainingLocation = o.TrainingDestinationPath;
                if (!string.IsNullOrEmpty(o.ValidationDestinationPath))
                    _ProcessedValidationLocation = o.ValidationDestinationPath;
                if (!string.IsNullOrEmpty(o.TestDestinationPath))
                    _ProcessedTestLocation = o.TestDestinationPath;

                if (o.Algorithm != null)
                    _algorithm = o.Algorithm;

                steps = o.StepsPerEpoch;
                epochs = o.Epochs;
                generateConfusionMatrix = o.ConfusionMatrix;
                testImage = o.TestImage;
            }).WithNotParsed(e =>
            {
                Environment.Exit(1);
            });
            
            IAnalysisAlgorithm data = null;
            if (!string.IsNullOrEmpty(testImage))
            {
                data = AnalyzerFactory.Create(_algorithm.Value);
                data.LoadImage(testImage);
            }

            if (data != null)
            {
                data.AnalyzeAndDisplay();
                return;
            }

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            // First, process images to generate the noise images
            if (_alwaysPreprocess || !Directory.Exists(_ProcessedTrainingLocation))
                ImageAnalyzer.ProcessImages(_SourceTrainingLocation, _ProcessedTrainingLocation);
            if (_alwaysPreprocess || !Directory.Exists(_ProcessedValidationLocation))
                ImageAnalyzer.ProcessImages(_SourceValidationLocation, _ProcessedValidationLocation);
            if (_alwaysPreprocess || !Directory.Exists(_ProcessedTestLocation))
                ImageAnalyzer.ProcessImages(_SourceTestLocation, _ProcessedTestLocation);

            if (_algorithm != null)
            {
                var modelName = _algorithm.ToString();
                var model = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"vgg16_{modelName}.h5");
                var channels = 3;
                if (_algorithm == AnalyzerAlgorithm.PCA)
                    channels = 1;

                if (!File.Exists(model))
                {
                    using (var nn = new NeuralNetworkModel(_imageSize.Item1, _imageSize.Item2, channels, modelName))
                    {
                        nn.Train(Path.Combine(_ProcessedTrainingLocation, modelName), 
                                Path.Combine(_ProcessedValidationLocation, modelName),
                                //Path.Combine(_ProcessedTestLocation, modelName),
                                steps: steps, epochs: epochs);
                    }
                }
                else
                {
                    if (generateConfusionMatrix)
                    {
                        // Load and predict with the model
                        using (var nn = new NeuralNetworkModel(model, _imageSize.Item1, _imageSize.Item2, channels: channels))
                        {
                            var cm = nn.GenerateConfusionMatrix(Path.Combine(_ProcessedTestLocation, modelName));
                            System.Console.Write($"Accuracy of model: {cm.Accuracy}");
                        }
                    }
                }
            }
        }
    }
}
