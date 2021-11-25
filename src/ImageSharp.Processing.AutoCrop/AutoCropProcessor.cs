﻿using ImageSharp.Processing.AutoCrop.Extensions;
using ImageSharp.Processing.AutoCrop.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;
using System;

namespace ImageSharp.Processing.AutoCrop
{
    public class AutoCropProcessor : CloningImageProcessor
    {
        private readonly IAutoCropSettings _settings;

        public ICropAnalysis Analysis { get; set; }

        public AutoCropProcessor(IAutoCropSettings settings)
        {
            _settings = settings;
        }

        public override ICloningImageProcessor<TPixel> CreatePixelSpecificCloningProcessor<TPixel>(Configuration configuration, Image<TPixel> source, Rectangle sourceRectangle)
        {
            if (source is Image<Rgb24> rgbSource)
            {
                var processor = new RgbAutoCropProcessor(configuration, _settings, rgbSource);
                Analysis = processor.Analysis;

                return (ICloningImageProcessor<TPixel>)processor;
            }
            else if (source is Image<Rgba32> rgbaSource)
            {
                var processor = new RgbaAutoCropProcessor(configuration, _settings, rgbaSource);
                Analysis = processor.Analysis;

                return (ICloningImageProcessor<TPixel>)processor;
            }

            throw new NotSupportedException("Unsupported pixel type");
        }
    }

    public abstract class AutoCropProcessor<TPixel> : ICloningImageProcessor<TPixel> where TPixel : unmanaged, IPixel<TPixel>
    {
        protected readonly Configuration Configuration;
        protected readonly IAutoCropSettings Settings;
        protected readonly Image<TPixel> Source;
        
        public ICropAnalysis Analysis { get; set; }

        protected AutoCropProcessor(Configuration configuration, IAutoCropSettings settings, Image<TPixel> source)
        {
            Configuration = configuration;
            Source = source;
            Settings = settings;
        }

        public Image<TPixel> CloneAndExecute()
        {
            if (!Analysis.Success)
                return null;

            try
            {
                var target = CreateTarget();
                ApplySource(target);

                return target;
            }
            catch (Exception innerException)
            {
                throw new ImageProcessingException("An error occurred when processing the image using " + GetType().Name + ". See the inner exception for more detail.", innerException);
            }
        }

        public void Execute()
        {
            Image<TPixel> image = null;
            try
            {
                image = CloneAndExecute();
                
                if (image != null)
                    Source.SwapPixelBuffersFrom(image);
            }
            finally
            {
                image?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        protected virtual void ApplySource(Image<TPixel> image)
        {
            var paddedSource = GetTargetRectangle();
            var targetBox = paddedSource.Bounds();

            // Copy as much of the source image as possible.
            // Mainly to avoid jagged edges
            var constrainedSource = paddedSource.Constrain(Source.Bounds());
            var offset = GetOffset(constrainedSource, targetBox);

            image.CopyRect(Source, constrainedSource, offset);
        }

        protected virtual Size GetDestinationSize()
        {
            var paddedSource = GetTargetRectangle();
            return paddedSource.Size();
        }

        protected virtual Rectangle GetTargetRectangle()
        {
            var paddingConstraint = (Rectangle?)null;

            if (Settings.PadMode == PadMode.Contain)
                paddingConstraint = Source.Bounds();

            return GetPaddedRectangle(Analysis.BoundingBox, paddingConstraint);
        }

        protected virtual Point GetOffset(Rectangle source, Rectangle target)
        {
            var x = target.Left - source.Left;
            var y = target.Top - source.Top;

            return new Point(x, y);
        }

        protected virtual Rectangle GetPaddedRectangle(Rectangle rectangle, Rectangle? constraint = null)
        {
            var padding = GetPadSize(rectangle);
            var expanded = rectangle.Expand(padding.Width, padding.Height);

            if (constraint.HasValue)
                expanded = expanded.Constrain(constraint.Value);

            return expanded;
        }

        protected virtual Size GetPadSize(Rectangle rectangle)
        {
            var dimension = (rectangle.Width + rectangle.Height) / 2;

            var px = Settings.PadX / 100.0;
            var py = Settings.PadY / 100.0;

            return new Size((int)(dimension * px), (int)(dimension * py));
        }

        private Image<TPixel> CreateTarget()
        {
            var destinationSize = GetDestinationSize();
            var background = Analysis.Background.ToPixel<TPixel>();

            return new Image<TPixel>(Configuration, destinationSize.Width, destinationSize.Height, background);
        }
    }
}
