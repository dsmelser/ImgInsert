using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.ImageInsertion
{
    internal class LineTransformSource : ILineTransformSource
    {
        private const int ImageAdornmentSpacePadding = 20;

        ImageAdornmentManager manager;

        public LineTransformSource(ImageAdornmentManager manager)
        {
            this.manager = manager;
        }

        LineTransform ILineTransformSource.GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
        {
            IEnumerable<ImageAdornment> targetImages = this.manager.Images
                .Where(imageAdornment => imageAdornment.ApplyRenderTrackingPoint(this.manager.View.TextSnapshot, line));

            if (targetImages.Count() > 0)
            {
                ImageAdornment imageAdornmentWithMaxHeight = targetImages
                    .OrderByDescending(imageAdornment => imageAdornment.VisualElement.Height)
                    .FirstOrDefault();

                return new LineTransform(imageAdornmentWithMaxHeight.VisualElement.Height + ImageAdornmentSpacePadding, 0, 1.0);
            }

            return new LineTransform(0, 0, 1.0);
        }
    }
}
