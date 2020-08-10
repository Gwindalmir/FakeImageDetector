# Fake Image Detector

This is an image classification tool that uses a convolutional neural network to determine if an image is real, or has been altered or generated.

Preprocessing algorithms:
  - Error-Level Analysis (ELA)
  - Luminence Gradient (LG)
  - Principle Component Analysis (PCA)

Neural Network:
  - Very-deep convolutional neural network based on VGG19.

### Tech

Fake Image Detector uses a number of open source projects to work properly:

* [opencvsharp] - .NET bindings for OpenCV
* [Keras.NET] - .NET wrapper around the Python Keras library
* [.NET Core] - Cross-platform runtime
* [Python] - Needed for ML libraries for current implementation
* [TensorFlow] - ML framework
* [plaidML] - Wrapper around keras for AMD GPU acceleration
* [Keras] - Deep learning API

### Installation

This project requires [Python] 3.7 to run.

Install the dependencies:
- [.NET Core] 3.1 (Optional)
- Latest version of [Python] 3.7 (3.78)

Once python is installed, install the required modules:
```sh
$ pip install keras==2.2.4 tensorflow tensorflow-cp
```
For AMD hardware:
```sh
pip install keras==2.2.4 tensorflow tensorflow-cpu plaidml-keras 
plaidml-setup
```

### Building from source
For production release:
```sh
$ dotnet build
```
Generating release for distribution (.NET Core runtime included):
```sh
$ dotnet publish -p:PublishProfile=Full FakeDetectorUI
$ dotnet publish -p:PublishProfile=Full NeuralNetworkTraining
```

For a cross-platform distribution:
```sh
$ dotnet publish -p:PublishProfile=Minimal FakeDetectorUI
$ dotnet publish -p:PublishProfile=Minimal NeuralNetworkTraining
```

### Todos

 - Write more Tests
 - Convert to [ML.NET] framework

License
----

LGPL v2.1

[//]: # (These are reference links used in the body of this note and get stripped out when the markdown processor does its job. There is no need to format nicely because it shouldn't be seen. Thanks SO - http://stackoverflow.com/questions/4823468/store-comments-in-markdown-syntax)

   [Keras.NET]: <https://github.com/SciSharp/Keras.NET>
   [opencvsharp]: <https://github.com/shimat/opencvsharp>
   [ML.NET]: <https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet>
   [Python]: <https://www.python.org/downloads/release/python-378/>
   [.NET Core]: <https://dotnet.microsoft.com/download/dotnet-core/3.1>
   [TensorFlow]: <https://www.tensorflow.org/>
   [plaidML]: <https://github.com/plaidml/plaidml>
   [Keras]: <https://keras.io/>
