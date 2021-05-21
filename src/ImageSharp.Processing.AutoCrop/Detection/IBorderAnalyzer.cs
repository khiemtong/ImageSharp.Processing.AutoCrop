﻿using ImageSharp.Processing.AutoCrop.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageSharp.Processing.AutoCrop.Detection
{
    public interface IBorderAnalyzer<TPixel> where TPixel : unmanaged, IPixel<TPixel>
    {
        IBorderAnalysis Analyze(Image<TPixel> image, Rectangle rectangle, int? colorThreshold, float? bucketThreshold);
    }
}
