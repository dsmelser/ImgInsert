using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Provides a visual element that highlights a text line in the editor.
    /// </summary>
    internal class HighlightLineAdornment
    {
        private Rectangle visualElement;
        private IEditorFormatMap editorFormatMap;
        private readonly Color DefaultBackgroundColor = Color.FromRgb(51, 153, 255);
        private Color backgroundColor;

        internal HighlightLineAdornment(IWpfTextView view, IEditorFormatMap editorFormatMap)
        {
            this.View = view;
            this.editorFormatMap = editorFormatMap;
            
            CreateVisualElement();
        }

        private void CreateVisualElement()
        {
            UpdateBackgroundColor();

            visualElement = new Rectangle();
            LinearGradientBrush fillBrush = new LinearGradientBrush(
                Color.FromArgb(0x60, backgroundColor.R, backgroundColor.G, backgroundColor.B),
                Color.FromArgb(0x60, backgroundColor.R, backgroundColor.G, backgroundColor.B),
                90);

            fillBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x30, backgroundColor.R, backgroundColor.G, backgroundColor.B), 0.5));
            visualElement.Fill = fillBrush;

            visualElement.Stroke = new SolidColorBrush(Color.FromRgb(51, 153, 255));
            visualElement.StrokeThickness = 2;
            visualElement.Opacity = 0.3;
            visualElement.RadiusX = visualElement.RadiusY = 2;
            visualElement.Visibility = Visibility.Hidden;
        }

        private void UpdateBackgroundColor()
        {
            // Use the editor format map to get the configured background color of VS for selected text
            ResourceDictionary properties = this.editorFormatMap.GetProperties("Selected Text");
            if (properties != null && properties.Contains("BackgroundColor"))
            {
                this.backgroundColor = (Color)properties["BackgroundColor"];
            }
            else
            {
                // use the default one
                this.backgroundColor = DefaultBackgroundColor;
            }
        }

        /// <summary>
        /// Gets the visual representation of the adornment.
        /// </summary>
        internal UIElement VisualElement { get { return this.visualElement; } }
        
        /// <summary>
        /// Gets the view where the adornment is being shown
        /// </summary>
        internal IWpfTextView View { get; private set; }

        /// <summary>
        /// Highlighs the text line
        /// </summary>
        /// <param name="textLine">The text line</param>
        internal void Highlight(ITextViewLine textLine)
        {
            if (textLine != null)
            {
                this.visualElement.Visibility = Visibility.Visible;
                // Set the position of the visual element
                this.visualElement.Height = textLine.Height;
                this.visualElement.Width = this.View.ViewportWidth;
                Canvas.SetLeft(this.visualElement, this.View.ViewportLeft);
                Canvas.SetTop(this.visualElement, textLine.Top + textLine.LineTransform.TopSpace);
            }
            else
            {
                this.Clear();
            }
        }

        internal void Clear()
        {
            this.visualElement.Visibility = Visibility.Hidden;
        }
    }
}