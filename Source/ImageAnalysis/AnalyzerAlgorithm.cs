using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Gwindalmir.ImageAnalysis
{
    public enum AnalyzerAlgorithm
    {
        [Description("Error-level Analysis")]
        ELA,
        [Description("Principle Component Analysis")]
        PCA,
        [Description("Luminance Gradient")]
        LG,
    }
}
