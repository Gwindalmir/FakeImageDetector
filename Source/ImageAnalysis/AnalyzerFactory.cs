using Gwindalmir.ImageAnalysis.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gwindalmir.ImageAnalysis
{
    /// <summary>
    /// Factory for creating various analyzer algorithms.
    /// </summary>
    public static class AnalyzerFactory
    {
        /// <summary>
        /// Create a new analyzer algorithm instance.
        /// </summary>
        /// <param name="algorithm">The algorithm to create.</param>
        /// <returns>The specific class instance as <see cref="IAnalysisAlgorithm"/>.</returns>
        public static IAnalysisAlgorithm Create(AnalyzerAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case AnalyzerAlgorithm.ELA:
                    return new ErrorLevelAnalysis();
                case AnalyzerAlgorithm.PCA:
                    return new PrincipleComponentAnalysis(1);
                case AnalyzerAlgorithm.LG:
                    return new LuminanceGradient();
                default:
                    throw new ArgumentOutOfRangeException($"Unknown algorithm: {algorithm}");
            }
        }
    }
}
