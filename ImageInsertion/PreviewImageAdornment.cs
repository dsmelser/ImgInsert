using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Provides a visual element that shows a preview image in the editor.
    /// </summary>
    internal class PreviewImageAdornment
    {
        internal const double PreviewOpacity = .5;

        internal Image VisualElement { get; private set; }

        internal PreviewImageAdornment()            
        {
            CreateVisualElement();
        }

        private void CreateVisualElement()
        {
            this.VisualElement = new Image();
            this.VisualElement.Stretch = System.Windows.Media.Stretch.None;
            this.VisualElement.Opacity = PreviewOpacity;
            this.VisualElement.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Shows the preview of the image in the editor
        /// </summary>
        /// <param name="imageFilename"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000")]
        internal void Show(string imageFilename)
        {
            VisualElement.Source = BitmapFrame.Create(new Uri(imageFilename, UriKind.RelativeOrAbsolute));
            VisualElement.Visibility = Visibility.Visible;
            VisualElement.Tag = new System.Drawing.Bitmap(imageFilename);
        }

        internal void Clear()
        {
            VisualElement.Source = null;
            VisualElement.Visibility = Visibility.Hidden;
            VisualElement.Tag = null;
        }

        /// <summary>
        /// Moves the preview image to the target point
        /// </summary>
        /// <param name="targetPoint"></param>
        internal void MoveTo(Point targetPoint)
        {
            if (this.VisualElement != null)
            {
                Canvas.SetLeft(VisualElement, targetPoint.X - (VisualElement.Source.Width / 2));
                Canvas.SetTop(VisualElement, targetPoint.Y - (VisualElement.Source.Height / 2));
            }
        }
    }
}