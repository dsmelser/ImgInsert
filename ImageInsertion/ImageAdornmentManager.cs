using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System.Windows.Media;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using System.IO;
using EnvDTE;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Manages the image adornments in an instance of <see cref="IWpfTextView"/>
    /// </summary>
    internal class ImageAdornmentManager
    {
        private const string ImageAdornmentLayerName = "Intra Text Adornment";

        private IServiceProvider serviceProvider;
        private ImageAdornmentRepositoryService ImagesAdornmentsRepository { get; set; }
        internal IList<ImageAdornment> Images { get { return this.ImagesAdornmentsRepository.Images; } }

        internal HighlightLineAdornment HighlightLineAdornment { get; private set; }
        internal PreviewImageAdornment PreviewImageAdornment { get; private set; }
        internal IWpfTextView View { get; private set; }
        internal IAdornmentLayer AdornmentLayer { get; private set; }

        internal ImageAdornmentManager(IServiceProvider serviceProvider, IWpfTextView view, IEditorFormatMap editorFormatMap)
        {
            this.View = view;
            this.serviceProvider = serviceProvider;
            this.AdornmentLayer = this.View.GetAdornmentLayer(ImageAdornmentLayerName);

            this.ImagesAdornmentsRepository = new ImageAdornmentRepositoryService(view.TextBuffer);

            // Create the highlight line adornment
            this.HighlightLineAdornment = new HighlightLineAdornment(view, editorFormatMap);
            this.AdornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, HighlightLineAdornment, HighlightLineAdornment.VisualElement, null);

            // Create the preview image adornment
            this.PreviewImageAdornment = new PreviewImageAdornment();
            this.AdornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, this, this.PreviewImageAdornment.VisualElement, null);

            // Attach to the view events
            this.View.LayoutChanged += OnLayoutChanged;
            this.View.TextBuffer.Changed += OnBufferChanged;
            this.View.Closed += OnViewClosed;

            // Load and initialize the image adornments repository
            this.ImagesAdornmentsRepository.Load();
            this.ImagesAdornmentsRepository.Images.ToList().ForEach(image => InitializeImageAdornment(image));
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            // Save the image adornments
            this.ImagesAdornmentsRepository.Save();

            // Detach from the view events
            this.View.LayoutChanged -= OnLayoutChanged;
            this.View.TextBuffer.Changed -= OnBufferChanged;
            this.View.Closed -= OnViewClosed;
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // Remove the image adornments if the associated spans were deleted.
            List<ImageAdornment> imagesToBeRemoved = new List<ImageAdornment>();
            foreach (ImageAdornment imageAdornment in this.ImagesAdornmentsRepository.Images)
            {
                Span span = imageAdornment.TrackingSpan.GetSpan(e.After);
                if (span.Length == 0)
                {
                    imagesToBeRemoved.Add(imageAdornment);
                }
            }

            imagesToBeRemoved.ForEach(imageAdornment => RemoveImageAdornment(imageAdornment));
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            List<ImageAdornment> imageAdornmentsToBeShown = new List<ImageAdornment>();

            // Detect which images should be shown again based on the new or reformatted spans
            foreach (Span span in e.NewOrReformattedSpans)
            {
                imageAdornmentsToBeShown.AddRange(this.ImagesAdornmentsRepository.Images.Where(image => image.TrackingSpan.GetSpan(this.View.TextSnapshot).OverlapsWith(span)));
            }

            foreach (ImageAdornment imageAdornment in imageAdornmentsToBeShown)
            {
                SnapshotSpan imageSnaphotSpan = imageAdornment.TrackingSpan.GetSpan(this.View.TextSnapshot);
                // Get the text view line associated with the image span
                ITextViewLine newOrReformattedLine = e.NewOrReformattedLines.FirstOrDefault(line => 
                    line.ContainsBufferPosition(imageSnaphotSpan.Start) && line.ContainsBufferPosition(imageSnaphotSpan.End));
                if (newOrReformattedLine != null)
                {
                    // Use the top of the text view line to set image top location. And finally adjust the final location using the delta Y.
                    Canvas.SetTop(imageAdornment.VisualElement, newOrReformattedLine.Top + imageAdornment.TextViewLineDelta.Y);
                    Show(imageAdornment, newOrReformattedLine);
                }
            }
        }

        internal ITextViewLine GetTargetTextViewLine(UIElement uiElement)
        {
            if (this.View.TextViewLines == null)
                return null;

            return this.View.TextViewLines.GetTextViewLineContainingYCoordinate(Canvas.GetTop(uiElement));
        }

        internal ITextViewLine GetTargetTextViewLine(ImageAdornment imageAdornment)
        {
            if (this.View.TextViewLines == null)
                return null;

            return this.View.TextViewLines.GetTextViewLineContainingBufferPosition(imageAdornment.TrackingSpan.GetStartPoint(this.View.TextSnapshot));
        }

        /// <summary>
        /// Creates and adds an image adornment for the image.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        internal ImageAdornment AddImageAdornment(Image image)
        {
            ITextViewLine targetLine = GetTargetTextViewLine(image);

            if (targetLine != null && targetLine.Length > 0)
            {
                ImageAdornment imageAdornment = new ImageAdornment(
                    new SnapshotSpan(targetLine.Start, targetLine.Length),
                    image);

                // Initialize the image adornment
                InitializeImageAdornment(imageAdornment);

                // Add the image adornment to the repository
                ImagesAdornmentsRepository.Add(imageAdornment);
                ImagesAdornmentsRepository.EnsureRepositoryFileExists();

                // Add the repository file to the solution explorer
                AddFileToTheActiveDocument(this.ImagesAdornmentsRepository.RepositoryFilename);

                // Show the image
                Show(imageAdornment);

                DisplayTextViewLine(imageAdornment);

                return imageAdornment;
            }

            return null;
        }


        /// <summary>
        /// Adds the file as a child of the active document.
        /// </summary>
        /// <param name="filename"></param>
        internal void AddFileToTheActiveDocument(string filename)
        {
            if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
            {
                if (this.serviceProvider != null)
                {
                    DTE vs = this.serviceProvider.GetService(typeof(DTE)) as DTE;
                    if (vs != null && vs.ActiveDocument != null)
                    {
                        ProjectItem projectItem = vs.ActiveDocument.ProjectItem.ProjectItems.AddFromFile(filename);
                        if (projectItem != null)
                        {
                            Property buildActionProperty = projectItem.Properties.Item("BuildAction");
                            if (buildActionProperty != null)
                            {
                                buildActionProperty.Value = 0;
                            }
                        }
                    }
                }
            }
        }

        private void InitializeImageAdornment(ImageAdornment imageAdornment)
        {
            imageAdornment.VisualElement.MouseMove += new MouseEventHandler(OnAdornmentVisualElementMouseMove);
            imageAdornment.VisualElement.MouseLeftButtonDown += new MouseButtonEventHandler(OnAdornmentVisualElementMouseLeftButtonDown);
            imageAdornment.VisualElement.MouseLeftButtonUp += new MouseButtonEventHandler(OnAdornmentVisualElementMouseLeftButtonUp);
            imageAdornment.VisualElement.MouseLeave += new MouseEventHandler(OnAdornmentVisualElementMouseLeave);
            imageAdornment.VisualElement.Deleted += new EventHandler(OnAdornmentVisualElementDeleted);
            imageAdornment.VisualElement.Resizing += new EventHandler(OnImageAdornmentResizing);
        }

        private void OnAdornmentVisualElementDeleted(object sender, EventArgs e)
        {
            EditorImage visualElement = sender as EditorImage;
            ImageAdornment imageAdornment = visualElement.Tag as ImageAdornment;

            RemoveImageAdornment(imageAdornment);

        }

        private void RemoveImageAdornment(ImageAdornment imageAdornment)
        {
            this.ImagesAdornmentsRepository.Remove(imageAdornment);
            this.AdornmentLayer.RemoveAdornment(imageAdornment.VisualElement);
        }

        private void OnAdornmentVisualElementMouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement visualElement = sender as FrameworkElement;
            ImageAdornment imageAdornment = visualElement.Tag as ImageAdornment;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Move the image adornment
                if (!imageAdornment.VisualElement.IsResizing && !this.ImagesAdornmentsRepository.Images.ToList().Exists(image => image != imageAdornment && image.VisualElement.IsMoving))
                {
                    imageAdornment.VisualElement.IsMoving = true;

                    Point adjustedPosition = e.GetPosition(this.View.VisualElement);
                    adjustedPosition.X += this.View.ViewportLeft - (imageAdornment.VisualElement.Width / 2);
                    adjustedPosition.Y += this.View.ViewportTop - (imageAdornment.VisualElement.Height / 2);

                    this.AdornmentLayer.RemoveAdornmentsByTag(imageAdornment);

                    imageAdornment.VisualElement.Opacity = PreviewImageAdornment.PreviewOpacity;
                    imageAdornment.VisualElement.MoveTo(adjustedPosition);

                    Show(imageAdornment);

                    this.HighlightLineAdornment.Highlight(GetTargetTextViewLine(imageAdornment));
                }
            }

            e.Handled = true;
        }

        private void OnAdornmentVisualElementMouseLeave(object sender, MouseEventArgs e)
        {
            FrameworkElement visualElement = sender as FrameworkElement;
            ImageAdornment imageAdornment = visualElement.Tag as ImageAdornment;
            imageAdornment.VisualElement.Opacity = 1;
            imageAdornment.VisualElement.IsMoving = false;
            this.HighlightLineAdornment.Clear();
        }

        private void OnAdornmentVisualElementMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement visualElement = sender as FrameworkElement;
            ImageAdornment imageAdornment = visualElement.Tag as ImageAdornment;
            this.HighlightLineAdornment.Highlight(GetTargetTextViewLine(imageAdornment));
            e.Handled = true;
        }

        private void OnAdornmentVisualElementMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement visualElement = sender as FrameworkElement;
            ImageAdornment imageAdornment = visualElement.Tag as ImageAdornment;
            imageAdornment.VisualElement.Opacity = 1;
            this.HighlightLineAdornment.Clear();
            this.DisplayTextViewLine(imageAdornment);
        }

        private void Show(ImageAdornment imageAdornment)
        {
            Show(imageAdornment, GetTargetTextViewLine(imageAdornment));
        }

        private void Show(ImageAdornment imageAdornment, ITextViewLine targetTextViewLine)
        {
            if (targetTextViewLine != null)
            {
                // Update the line delta
                imageAdornment.TextViewLineDelta = new Point(imageAdornment.VisualElement.Left - targetTextViewLine.Left, imageAdornment.VisualElement.Top - targetTextViewLine.Top);
            }

            this.AdornmentLayer.RemoveAdornmentsByTag(imageAdornment);
            this.AdornmentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, imageAdornment.TrackingSpan.GetSpan(this.View.TextSnapshot), imageAdornment, imageAdornment.VisualElement, null);

            UpdateTargetLocation(imageAdornment);
        }

        private void OnImageAdornmentResizing(object sender, EventArgs e)
        {
            if (this.View.TextViewLines != null)
            {
                EditorImage editorImage = sender as EditorImage;
                ImageAdornment imageAdornment = editorImage.Tag as ImageAdornment;
                DisplayTextViewLine(imageAdornment);
            }
        }

        private void DisplayTextViewLine(ImageAdornment imageAdornment)
        {
            ITextViewLine textViewLine = this.View.TextViewLines.FirstOrDefault(line => imageAdornment.ApplyRenderTrackingPoint(this.View.TextSnapshot, line));

            if (textViewLine != null)
            {
                this.View.DisplayTextLineContainingBufferPosition(textViewLine.Start, textViewLine.Top, ViewRelativePosition.Top);
            }
            else
            {
				this.View.DisplayTextLineContainingBufferPosition(new SnapshotPoint(this.View.TextSnapshot, 0), 0.0, ViewRelativePosition.Top);
            }
        }

        private void UpdateTargetLocation(ImageAdornment imageAdornment)
        {
            imageAdornment.RenderTrackingPoint = null;

            foreach (ITextViewLine line in this.View.TextViewLines)
            {
                Rect lineArea = new Rect(line.Left, line.Top, line.Width, line.Height);
                Rect imageAdornmentArea = imageAdornment.VisualElement.Area;
                // Use the height half to be able to move the image up and down
                imageAdornmentArea.Height = imageAdornmentArea.Height / 2;

                if (line.Length > 0 && lineArea.IntersectsWith(imageAdornmentArea))
                {
                    imageAdornment.RenderTrackingPoint = this.View.TextSnapshot.CreateTrackingPoint(line.Start.Position, PointTrackingMode.Negative);
                    imageAdornment.UpdateTrackingSpan(line);

                    return;
                }
            }
        }
    }
}