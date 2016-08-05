using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Provides a simpler and serializable version of <see cref="ImageAdornment"/>
    /// </summary>
    [Serializable]
    public class ImageAdornmentInfo
    {
        /// <summary>
        /// Creates a new instance of <see cref="ImageAdornmentInfo"/>
        /// </summary>
        public ImageAdornmentInfo()
        { }

        /// <summary>
        /// Creates a new instance of <see cref="ImageAdornmentInfo"/> based on the image adornment.
        /// </summary>
        /// <param name="imageAdornment"></param>
        internal ImageAdornmentInfo(ImageAdornment imageAdornment)
        {
            this.Id = imageAdornment.Id;
            this.TextViewLineDelta = imageAdornment.TextViewLineDelta;
            this.Span = imageAdornment.TrackingSpan.GetSpan(imageAdornment.TrackingSpan.TextBuffer.CurrentSnapshot).Span;
            this.Bitmap = imageAdornment.VisualElement.Image.Tag as System.Drawing.Bitmap;

            this.Area = new Rect(
                imageAdornment.VisualElement.Left,
                imageAdornment.VisualElement.Top,
                imageAdornment.VisualElement.Width,
                imageAdornment.VisualElement.Height
                );
        }

        /// <summary>
        /// Gets or sets the left, top, width and height of the image adornment.
        /// </summary>
        public Rect Area { get; set; }

        /// <summary>
        /// Gets or sets the difference between the location of the image and the associated span.
        /// </summary>
        public Point TextViewLineDelta { get; set; }

        /// <summary>
        /// Gets or sets the start position of the span associated with the image.
        /// </summary>
        public int SpanStartPosition { get; set; }

        /// <summary>
        /// Gets or sets the lenght of the span associated with the image.
        /// </summary>
        public int SpanLength { get; set; }

        /// <summary>
        /// Gets the Span associated with the image
        /// </summary>
        internal Span Span 
        {
            get { return new Span(this.SpanStartPosition, this.SpanLength); } 
            private set
            {
                this.SpanStartPosition = value.Start;
                this.SpanLength = value.Length;
            }
        }

        /// <summary>
        /// Gets or sets the unique id of the image adornment
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the image as a bitmap
        /// </summary>
        public System.Drawing.Bitmap Bitmap { get; set; }
    }
}