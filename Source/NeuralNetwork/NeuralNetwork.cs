using Keras;
using Keras.Applications.ResNet;
using Keras.Applications.VGG;
using Keras.Callbacks;
using Keras.Layers;
using Keras.Models;
using Keras.Optimizers;
using Keras.PreProcessing.Image;
using Numpy;
using Numpy.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;

namespace Gwindalmir.NeuralNetwork
{
    public class NeuralNetworkModel : IDisposable
    {
        static public string[] Labels { get; } = new[]
        {
            "fake",
            "real",
            //"fake-ELA",
            //"fake-LG",
            //"fake-PCA",
            //"real-LG",
            //"real-ELA",
            //"real-PCA",
        };

        static NeuralNetworkModel()
        {
            //Python.Runtime.PythonEngine.Initialize();
            //Python.Runtime.PythonEngine.ImportModule("plaidml.keras");
            //Python.Runtime.PythonEngine.RunSimpleString("plaidml.keras.install_backend()");
        }

        BaseModel m_model = null;
        string m_modelName = null;

        public Tuple<int, int> Size { get; }
        public int Channels { get; }

        public NeuralNetworkModel(string filename)
        {
            m_model = BaseModel.LoadModel(filename);
            m_model.Summary();
        }

        public NeuralNetworkModel(string filename, int width, int height, int channels = 3)
        {
            Size = new Tuple<int, int>(width, height);
            Channels = channels;
            m_model = BaseModel.LoadModel(filename);
            m_model.Summary();
        }

        public NeuralNetworkModel(int width, int height, int channels = 3, string name = "complete")
        {
            // Configure the desired layer parameters
            var activation = "relu";
            var kernel_size = new Tuple<int, int>(3, 3);
            var padding = "same";
            var poolsize = new Tuple<int, int>(2, 2);
            var strides = new Tuple<int, int>(2, 2);
            var opt = new Adam(lr: 1e-5f);
            Size = new Tuple<int, int>(height, width);
            Channels = channels;

            // Create the network model, and add the layers
            var model = new Sequential();
            model.Add(new Conv2D(64, kernel_size, activation: activation, padding: padding, input_shape: (Size.Item1, Size.Item2, Channels)));
            model.Add(new Conv2D(64, kernel_size, activation: activation, padding: padding));
            model.Add(new MaxPooling2D(poolsize, strides));

            model.Add(new Conv2D(128, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(128, kernel_size, activation: activation, padding: padding));
            model.Add(new MaxPooling2D(poolsize, strides));

            model.Add(new Conv2D(256, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(256, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(256, kernel_size, activation: activation, padding: padding));
            model.Add(new MaxPooling2D(poolsize, strides));

            model.Add(new Conv2D(512, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(512, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(512, kernel_size, activation: activation, padding: padding));
            model.Add(new MaxPooling2D(poolsize, strides));

            model.Add(new Conv2D(512, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(512, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(512, kernel_size, activation: activation, padding: padding));
            model.Add(new MaxPooling2D(poolsize, strides));

            model.Add(new Conv2D(1024, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(1024, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(1024, kernel_size, activation: activation, padding: padding));
            model.Add(new MaxPooling2D(poolsize, strides));

            model.Add(new Conv2D(1024, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(1024, kernel_size, activation: activation, padding: padding));
            model.Add(new Conv2D(1024, kernel_size, activation: activation, padding: padding));
            model.Add(new MaxPooling2D(poolsize, strides));

            model.Add(new Flatten());

            model.Add(new Dense(4096, activation: activation));
            model.Add(new Dense(4096, activation: activation));

            //model.Add(new Dense(6, activation: "sigmoid"));                         // sigmoid for multi-label
            model.Add(new Dense(Labels.Length, activation: "softmax"));             // sigmoid for multi-class

            // Compile the model
            //model.Compile(opt, "binary_crossentropy", new[] { "accuracy" });        // For multi-label
            model.Compile(opt, "categorical_crossentropy", new[] { "accuracy" });   // For multi-class

            model.Summary();
            m_model = model;
            m_modelName = name;
        }

        public void Train(string trainingDirectory, string validationDirectory, string testDirectory = null, int steps = 64, int epochs = 10, int valSteps = 10, int patience = 20)
        {
            var monitor = "val_acc";
            //var monitor = "val_accuracy"; // CPU only
            Console.WriteLine($"Training model: {m_modelName}");

            // Create the image generators for loading the dataset from disk in batches
            using (var trdata = new ImageDataGenerator(rescale: 1f / 255, vertical_flip: true))
            using (var valdata = new ImageDataGenerator(rescale: 1f / 255, vertical_flip: true))
            using (var tsdata = new ImageDataGenerator(rescale: 1f / 255, vertical_flip: true))
            {
                var colormode = Channels == 1 ? "grayscale" : "rgb";
                var traindata = trdata.FlowFromDirectory(trainingDirectory, Size, color_mode: colormode, classes: Labels);
                var validata = valdata.FlowFromDirectory(validationDirectory, Size, color_mode: colormode, classes: Labels);

                var checkpoint = new ModelCheckpoint($"vgg16_{m_modelName}_ckp.h5", monitor, 1);
                var early = new EarlyStopping(monitor, patience: patience, verbose: 1, restore_best_weights: true);

                // Train the model; THIS WILL TAKE HOURS
                using (var hist = m_model.FitGenerator(traindata, steps, epochs, validation_steps: valSteps, validation_data: validata, callbacks: new Callback[] { checkpoint, early }))
                    m_model.Save($"vgg16_{m_modelName}.h5");

                // If the test directory was set, run a test
                if (!string.IsNullOrEmpty(testDirectory))
                {
                    var testdata = tsdata.FlowFromDirectory(testDirectory, Size, color_mode: colormode, classes: Labels);
                    var results = m_model.EvaluateGenerator(testdata);
                    for(var idx = 0; idx < results.Length; idx++)
                        Console.WriteLine($"Accuracy for class {idx}: {results[idx]}");
                }
            }
        }

        private NDarray LoadImage(string filename)
        {
            var img = ImageUtil.LoadImg(filename, target_size: new Keras.Shape(Size.Item1, Size.Item2));
            var array = NormalizeImage(ImageUtil.ImageToArray(img));
            return array;
        }

        private NDarray NormalizeImage(NDarray array)
        {
            array = array / 255f;
            array = array.reshape(1, array.shape[0], array.shape[1], array.shape[2]);
            return array;
        }

        /// <summary>
        /// Gets the probabilities for each class
        /// </summary>
        /// <param name="filename">Filename to predict.</param>
        /// <returns>Probabilities for each class.</returns>
        public (float, float) Predict(string filename)
        {
            var array = LoadImage(filename);
            return PredictInternal(array);
        }

        /// <summary>
        /// Gets the probabilities for each class
        /// </summary>
        /// <param name="array">A python NDarray of the already loaded image.</param>
        /// <returns>Probabilities for each class.</returns>
        public (float, float) Predict(Numpy.NDarray array)
        {
            array = NormalizeImage(array);
            return PredictInternal(array);
        }

        private (float, float) PredictInternal(NDarray array)
        {
            var predictions = m_model.Predict(array);
            var y = predictions.GetData<float>();
            return (y[0], y[1]);
        }

        /// <summary>
        /// Get the predicted class an image belongs to.
        /// </summary>
        /// <param name="filename">Filename to predict.</param>
        /// <returns>The class label of the prediction.</returns>
        public string PredictClass(string filename)
        {
            var array = LoadImage(filename);
            var index = PredictClassIndex(array);
            return Labels[index];
        }

        /// <summary>
        /// Get the predicted class an image belongs to.
        /// </summary>
        /// <param name="array">A python NDarray of the already loaded image.</param>
        /// <returns>The class label of the prediction.</returns>
        public string PredictClass(Numpy.NDarray array)
        {
            array = NormalizeImage(array);
            var index = PredictClassIndex(array);
            return Labels[index];
        }

        private int PredictClassIndex(NDarray array)
        {
            var predictions = m_model.Predict(array);
            var y = predictions.argmax(axis: -1);
            return y.asscalar<int>();
        }

        static ImageDataGenerator tsdata = new ImageDataGenerator(rescale: 1f / 255);
        private IList<int[]> PredictClassDirectory(string path)
        {
            var results = new List<int[]>();
            if (!string.IsNullOrEmpty(path))
            {
                var colormode = Channels == 1 ? "grayscale" : "rgb";

                var testdata = tsdata.FlowFromDirectory(path, Size, color_mode: colormode, classes: Labels, shuffle: false);
                using (var predictions = m_model.PredictGenerator(testdata))
                {
                    var class_indicies = new NDarray(testdata.PyObject.GetAttr("labels"));
                    var y = predictions.argmax(axis: -1).astype(np.uint32);
                    results.Add(class_indicies.GetData<int>());
                    results.Add(y.GetData<int>());
                }
            }
            return results;
        }

        /// <summary>
        /// Calculates the confusion matrix of a dataset.
        /// </summary>
        /// <param name="path">The location of the dataset to predict.</param>
        /// <returns>The confusion matrix for the dataset.</returns>
        public ConfusionMatrix GenerateConfusionMatrix(string path)
        {
            var cm = new ConfusionMatrix(Labels);
            GenerateConfusionMatrix(path, ref cm);
            return cm;
        }

        /// <summary>
        /// Calculates the confusion matrix of a dataset.
        /// </summary>
        /// <param name="path">The location of the dataset to predict.</param>
        /// <param name="cm">The confusion matrix to place the result into. Does not clear previous values.</param>
        /// <returns>The confusion matrix for the dataset.</returns>
        public ConfusionMatrix GenerateConfusionMatrix(string path, ref ConfusionMatrix cm)
        {
            var results = PredictClassDirectory(path);
            cm.AddData(results[0], results[1]);
            return cm;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_model.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
