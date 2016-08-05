using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Provide the visual element for the <see cref="ImageAdornment"/>
    /// </summary>
    public partial class EditorImage : UserControl
    {
        /// <summary>
        /// The event is raised when the image is deleted by the user
        /// </summary>
        internal event EventHandler Deleted;

        /// <summary>
        /// The event is raised when the image is being resized
        /// </summary>
        internal event EventHandler Resizing;

        /// <summary>
        /// Gets or sets if the image is being moved
        /// </summary>
        internal bool IsMoving { get; set; }

        /// <summary>
        /// Gets true if the image is selected
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal bool IsSelected { get; private set; }

        /// <summary>
        /// Gets true if the image is being resized
        /// </summary>
        internal bool IsResizing { get; private set; }

        internal EditorImage(ImageSource imageSource)
        {
            InitializeComponent();
            this.image.Source = imageSource;
            canvasStretch.Width = this.image.Source.Width;
            canvasStretch.Height = this.image.Source.Height;
        }

        /// <summary>
        /// See <see cref="UIElement"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            Mouse.OverrideCursor = Cursors.Arrow;
            this.IsSelected = true;
            this.horizontalAndVerticalResizeThumb.Visibility =
                this.horizontalResizeThumb.Visibility =
                    this.verticalResizeThumb.Visibility =
                        this.CloseButtonGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// See <see cref="UIElement"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            Mouse.OverrideCursor = null;
            this.IsSelected = false;
            this.horizontalAndVerticalResizeThumb.Visibility =
                this.horizontalResizeThumb.Visibility =
                    this.verticalResizeThumb.Visibility =
                        this.CloseButtonGrid.Visibility = Visibility.Hidden;
        }

        internal Image Image
        {
            get { return this.image; }
        }

        private void AdjustVerticalChange(object sender, DragDeltaEventArgs e)
        {
            double heightAdjust = canvasStretch.Height + e.VerticalChange;
            if (heightAdjust >= canvasStretch.MinHeight)
            {
                canvasStretch.Height = heightAdjust;
            }
            OnResizing(new EventArgs());
        }

        private void AdjustHorizontalChange(object sender, DragDeltaEventArgs e)
        {
            double widthAdjust = canvasStretch.Width + e.HorizontalChange;
            if (widthAdjust >= canvasStretch.MinWidth)
            {
                canvasStretch.Width = widthAdjust;
            }
            OnResizing(new EventArgs());
        }

        private void AdjustHorizontalAndVerticalChange(object sender, DragDeltaEventArgs e)
        {
            AdjustHorizontalChange(sender, e);
            AdjustVerticalChange(sender, e);
        }

        private void OnResizing(EventArgs e)
        {
            if (Resizing != null)
            {
                Resizing(this, e);
            }
        }

        private void ResizeThumb_MouseEnter(object sender, MouseEventArgs e)
        {
            IsResizing = true;
            FrameworkElement element = sender as FrameworkElement;
            Mouse.OverrideCursor = element.Cursor;
        }

        private void ResizeThumb_MouseLeave(object sender, MouseEventArgs e)
        {
            IsResizing = false;
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void CloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OnDeleted(new EventArgs());
        }

        private void OnDeleted(EventArgs e)
        {
            if (Deleted != null)
            {
                Deleted(this, e);
            }
        }

        internal double Top
        {
            get { return Canvas.GetTop(this); }
            set { Canvas.SetTop(this, value); }
        }

        internal double Left
        {
            get { return Canvas.GetLeft(this); }
            set { Canvas.SetLeft(this, value); }
        }

        internal Rect Area
        {
            get { return new Rect(this.Left, this.Top, this.Width, this.Height); }
        }

        /// <summary>
        /// Moves the element to the target x,y location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        internal void MoveTo(double x, double y)
        {
            this.Left = x;
            this.Top = y;
        }

        /// <summary>
        /// Moves the element to the target point.
        /// </summary>
        /// <param name="targetPoint">The target point</param>
        internal void MoveTo(Point targetPoint)
        {
            this.MoveTo(targetPoint.X, targetPoint.Y);
        }
    }
}