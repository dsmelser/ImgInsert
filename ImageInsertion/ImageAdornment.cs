using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using System.Globalization;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Provides a visual element that shows an image in the editor.
    /// </summary>
    internal class ImageAdornment
    {
        private ImageAdornment()
        {
            // Generate an unique ID for the adornment
            this.Id = string.Format(CultureInfo.InvariantCulture, "[IMG:{0}]", Guid.NewGuid().ToString());
        }

        internal ImageAdornment(SnapshotSpan span, Image image)
            : this(span, image.Source.Clone())
        {
            this.VisualElement.Image.Tag = image.Tag;
            this.VisualElement.MoveTo(Canvas.GetLeft(image), Canvas.GetTop(image));
        }

        internal ImageAdornment(SnapshotSpan span, ImageSource source)
            : this()
        {
            UpdateTrackingSpan(span);

            this.VisualElement = new EditorImage(source);
            this.VisualElement.Tag = this;
        }

        internal ImageAdornment(ITextSnapshot textSnapshop, ImageAdornmentInfo info, ImageSource source)
            : this(new SnapshotSpan(textSnapshop, info.Span), source)
        {
            // Use the adornment info to setup the image parameters.
            this.Id = info.Id;
            this.TextViewLineDelta = new Point(info.TextViewLineDelta.X, info.TextViewLineDelta.Y);
            Canvas.SetLeft(this.VisualElement, info.Area.X);
            Canvas.SetTop(this.VisualElement, info.Area.Y);
            this.VisualElement.Width = info.Area.Width;
            this.VisualElement.Height = info.Area.Height;
            this.VisualElement.Image.Tag = info.Bitmap;
        }

        /// <summary>
        /// Gets the visual representation of the adornment.
        /// </summary>
        internal EditorImage VisualElement { get; private set; }

        /// <summary>
        /// Gets the associated span with the image.
        /// </summary>
        internal ITrackingSpan TrackingSpan { get; private set; }

        /// <summary>
        /// Updates the span the image adornment is tracking
        /// </summary>
        /// <param name="line"></param>
        internal void UpdateTrackingSpan(ITextViewLine line)
        {
            UpdateTrackingSpan(new SnapshotSpan(line.Start, line.Length));
        }

        /// <summary>
        /// Updates the span the image adornment is tracking
        /// </summary>
        internal void UpdateTrackingSpan(SnapshotSpan span)
        {
            string spanText = span.GetText();

            // Create a tracking span removing the spaces at the beginning and at the end.
            int emptySpacesAtTheBegining = spanText.Length - spanText.TrimStart().Length;
            this.TrackingSpan = span.Snapshot.CreateTrackingSpan(span.Start.Position + emptySpacesAtTheBegining, spanText.Trim().Length, SpanTrackingMode.EdgeExclusive);
        }

        /// <summary>
        /// Gets or sets the difference between the location of the image and the associated span.
        /// </summary>
        internal Point TextViewLineDelta { get; set; }

        /// <summary>
        /// Gets an unique id of the adornment.
        /// </summary>
        internal string Id { get; private set; }

        /// <summary>
        /// Gets a simpler representation of the adornment information. This can be serialized.
        /// </summary>
        internal ImageAdornmentInfo Info { get { return new ImageAdornmentInfo(this); } }

        /// <summary>
        /// Gets or sets the point where the image adornment is being rendered
        /// </summary>
        internal ITrackingPoint RenderTrackingPoint { get; set; }

        /// <summary>
        /// Returns tru if the line applies to the render target point.
        /// </summary>
        /// <param name="textSnapshot"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        internal bool ApplyRenderTrackingPoint(ITextSnapshot textSnapshot, ITextViewLine line)
        {
            if (RenderTrackingPoint != null)
            {
                int position = RenderTrackingPoint.GetPosition(textSnapshot);
                return line.Start.Position == position;
            }

            return false;
        }
    }
}