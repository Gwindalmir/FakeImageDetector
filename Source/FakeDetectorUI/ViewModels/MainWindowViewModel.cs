using Gwindalmir.FakeDetectorUI.Extensions;
using Gwindalmir.ImageAnalysis;
using Gwindalmir.NeuralNetwork;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Gwindalmir.FakeDetectorUI.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        const string _indeterminateClass = "indeterminant";
        NeuralNetworkModel[] m_networks = new NeuralNetworkModel[Enum.GetValues(typeof(AnalyzerAlgorithm)).Length];
        string m_tempPath;
        string m_folder;

        #region Properties
        private AnalyzerAlgorithm m_algorithm;
        public AnalyzerAlgorithm Algorithm
        {
            get => m_algorithm;
            set
            {
                if(m_algorithm != value)
                {
                    m_algorithm = value;
                    AnalyzeImage();

                    if (!string.IsNullOrEmpty(m_folder))
                        CalculateConfusionMatrix(m_folder);

                    OnPropertyChanged();
                    OnPropertyChanged("ConfusionMatrix");
                }
            }
        }

        private bool m_showTotal;
        public bool ShowTotal
        {
            get => m_showTotal;
            set
            {
                if (m_showTotal != value)
                {
                    m_showTotal = value;

                    if (!string.IsNullOrEmpty(m_folder))
                        CalculateConfusionMatrix(m_folder);

                    OnPropertyChanged();
                    OnPropertyChanged("ConfusionMatrix");
                }
            }
        }

        private string m_filename;
        public string Filename
        {
            get => m_filename;
            set
            {
                if (m_filename != value)
                {
                    m_filename = value;
                    LoadImage();
                    OnPropertyChanged();
                }
            }
        }

        private BitmapImage m_originalImage;
        public BitmapImage OriginalImage
        {
            get => m_originalImage;
            set
            {
                if (m_originalImage != value)
                {
                    m_originalImage = value;
                    OnPropertyChanged();
                    ClassLabels.Clear();
                    AnalyzeImage();
                }
            }
        }

        private BitmapImage m_analyzedImage;
        public BitmapImage AnalyzedImage
        {
            get => m_analyzedImage;
            set
            {
                if (m_analyzedImage != value)
                {
                    m_analyzedImage = value;
                    OnPropertyChanged();
                }
            }
        }

        private float m_fakeProbability;
        public float FakeProbability
        {
            get => m_fakeProbability;
            set
            {
                if (m_fakeProbability != value)
                {
                    m_fakeProbability = value;
                    OnPropertyChanged();
                }
            }
        }

        private string m_classLabel;
        public string ClassLabel
        {
            get => m_classLabel;
            set
            {
                if (m_classLabel != value)
                {
                    m_classLabel = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<Tuple<string, float>> m_classLabels = new ObservableCollection<Tuple<string, float>>();
        public ObservableCollection<Tuple<string, float>> ClassLabels
        {
            get => m_classLabels;
            set
            {
                if (m_classLabels != value)
                {
                    m_classLabels = value;
                    OnPropertyChanged();
                }
            }
        }

        private ConfusionMatrix m_confusionMatrixTotal;
        private ConfusionMatrix[] m_confusionMatrix = new ConfusionMatrix[Enum.GetValues(typeof(AnalyzerAlgorithm)).Length];

        private DataTable m_confusionMatrixTable;

        public DataView ConfusionMatrix
        {
            get
            {
                var table = m_confusionMatrix[(int)Algorithm];
                if (ShowTotal)
                    table = m_confusionMatrixTotal;

                table?.GetDataTable(ref m_confusionMatrixTable);
                return m_confusionMatrixTable?.DefaultView;
            }
        }
        #endregion Properties

        public MainWindowViewModel()
        {
            m_tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(m_tempPath);
        }

        ~MainWindowViewModel()
        {
            if (!string.IsNullOrEmpty(m_tempPath))
                Directory.Delete(m_tempPath, true);
            m_tempPath = null;
        }

        private void LoadImage()
        {
            OriginalImage = new BitmapImage(new Uri(m_filename));
        }

        private void AnalyzeImage()
        {
            AnalyzeImage(Algorithm);
        }

        private void AnalyzeImage(AnalyzerAlgorithm algorithm)
        {
            if (!string.IsNullOrEmpty(m_filename))
            {
                using (var result = LoadImage(m_filename, algorithm))
                using (var stream = new MemoryStream())
                {
                    result.WriteToStream(stream);
                    // Manually load the bitmap from the stream
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    AnalyzedImage = bitmap;
                }
            }
        }

        private Mat LoadImage(string filename, AnalyzerAlgorithm algorithm)
        {
            using (var algo = AnalyzerFactory.Create(algorithm))
            {
                algo.LoadImage(filename);
                algo.AnalyzeImage();
                return algo.GetResultImageSafe();
            }
        }

        private NeuralNetworkModel LoadNetwork(AnalyzerAlgorithm algorithm)
        {
            if (m_networks[(int)algorithm] == null)
            {
                var modelName = algorithm.ToString();
                var model = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), $"vgg16_{modelName}.h5");

                m_networks[(int)algorithm] = new NeuralNetworkModel(model, 256, 256, algorithm == AnalyzerAlgorithm.PCA ? 1 : 3);
            }
            return m_networks[(int)algorithm];
        }

        public string CalculateOverallClass()
        {
            if (string.IsNullOrEmpty(Filename))
                return null;

            var numberOfItems = Enum.GetValues(typeof(AnalyzerAlgorithm)).Length;
            float[] predictions = new float[NeuralNetworkModel.Labels.Length];
            ClassLabels.Clear();

            foreach (AnalyzerAlgorithm algorithm in Enum.GetValues(typeof(AnalyzerAlgorithm)))
            {
                LoadNetwork(algorithm);
                var prediction = GetPrediction(algorithm);
                predictions[0] += prediction.Item1;
                predictions[1] += prediction.Item2;
                ClassLabels.Add(Tuple.Create(prediction.Item3, Math.Max(prediction.Item1, prediction.Item2)));
                OnPropertyChanged("ClassLabels");
            }

            predictions[0] /= numberOfItems;
            predictions[1] /= numberOfItems;

            var max = predictions.Max();

            if (predictions[0] == max)
                return NeuralNetworkModel.Labels[0];
            else if (predictions[1] == max)
                return NeuralNetworkModel.Labels[1];
            else
                return _indeterminateClass;
        }

        private (float, float, string) GetPrediction(AnalyzerAlgorithm algorithm)
        {
            using (var result = LoadImage(m_filename, algorithm))
            using (var pyresult = result.ToNDarray())
            using (var resized = Numpy.np.resize(pyresult, new Numpy.Models.Shape(m_networks[(int)algorithm].Size.Item1, m_networks[(int)algorithm].Size.Item2, pyresult.shape.Dimensions[2])))
            {
                var (fake, real) = m_networks[(int)algorithm].Predict(resized);

                if (fake > real)
                    return (fake, real, NeuralNetworkModel.Labels[0]);
                else if (fake < real)
                    return (fake, real, NeuralNetworkModel.Labels[1]);
                else
                    return (fake, real, _indeterminateClass);
            }
        }

        public ConfusionMatrix CalculateSingleConfusionMatrix(string folder, AnalyzerAlgorithm algorithm)
        {
            return m_confusionMatrix[(int)algorithm] ??= LoadNetwork(algorithm).GenerateConfusionMatrix(Path.Combine(folder, algorithm.ToString()));
        }

        public void ClearConfusionMatrices()
        {
            Array.Clear(m_confusionMatrix, 0, m_confusionMatrix.Length);
        }

        public void CalculateConfusionMatrix(string folder)
        {
            var algoList = (AnalyzerAlgorithm[])Enum.GetValues(typeof(AnalyzerAlgorithm));
            
            // Check if selected folder is an already preprocessed dataset
            foreach (var dir in Directory.EnumerateDirectories(folder))
            {
                if (!algoList.Any(a => dir.EndsWith(a.ToString())))
                {
                    ImageAnalyzer.ProcessImages(folder, m_tempPath);
                    folder = m_tempPath;
                    break;
                }
            }

            m_folder = folder;

            if (ShowTotal)
                CalculateTotalConfusionMatrix(folder);
            else
                CalculateSingleConfusionMatrix(folder, Algorithm);
            OnPropertyChanged("ConfusionMatrix");
        }

        public void CalculateTotalConfusionMatrix(string folder)
        {
            m_confusionMatrixTable?.Clear();
            m_confusionMatrixTotal = null;

            foreach (AnalyzerAlgorithm algo in Enum.GetValues(typeof(AnalyzerAlgorithm)))
            {
                var cm = CalculateSingleConfusionMatrix(folder, algo);

                if (m_confusionMatrixTotal is null)
                    m_confusionMatrixTotal = new ConfusionMatrix(cm.ClassLabels);

                m_confusionMatrixTotal.AddMatrix(cm.ClassCounts);
            }
            OnPropertyChanged("ConfusionMatrix");
        }
    }
}
