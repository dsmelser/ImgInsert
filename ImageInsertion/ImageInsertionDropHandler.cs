using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using Microsoft.VisualStudio.Text.Formatting;
using System.Globalization;
using Microsoft.VisualStudio.Text.Editor.DragDrop;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Handles a drag and drop of an image onto the editor.
    /// The image came from the file system (FileDrop) or from the VS Solution Explorer.
    /// </summary>
    internal class ImageInsertionDropHandler : IDropHandler
    {
        private ImageAdornmentManager manager;
        private readonly List<string> SupportedImageExtensions = new List<string> { ".jpg", ".jpeg", ".bmp", ".png", ".gif" };

        internal ImageInsertionDropHandler(ImageAdornmentManager manager)
        {
            this.manager = manager;
        }

        /// <summary>
        /// See <see cref="IDropHandler.HandleDragStarted"/> for more information.
        /// </summary>
        /// <param name="dragDropInfo"></param>
        /// <returns></returns>
        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            //drag started, so create a new Bitmap to be shown to the user as visual feedback
            string imageFilename = GetImageFilename(dragDropInfo);

            this.manager.PreviewImageAdornment.Show(imageFilename);

            //show the copy cursor to the user
            return DragDropPointerEffects.Copy;
        }

        /// <summary>
        /// See <see cref="IDropHandler.HandleDraggingOver"/> for more information.
        /// </summary>
        /// <param name="dragDropInfo"></param>
        /// <returns></returns>
        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            this.manager.PreviewImageAdornment.MoveTo(dragDropInfo.Location);

            ITextViewLine targetLine = this.manager.GetTargetTextViewLine(this.manager.PreviewImageAdornment.VisualElement);
            if (targetLine != null && targetLine.Length > 0)
            {
                this.manager.HighlightLineAdornment.Highlight(targetLine);

                return DragDropPointerEffects.Copy;
            }
            else
            {
                this.manager.HighlightLineAdornment.Clear();
                return DragDropPointerEffects.None;
            }
        }

        private void RemovePreviewImage()
        {
            this.manager.PreviewImageAdornment.Clear();
            this.manager.HighlightLineAdornment.Clear();
        }

        /// <summary>
        /// See <see cref="IDropHandler.HandleDataDropped"/> for more information.
        /// </summary>
        /// <param name="dragDropInfo"></param>
        /// <returns></returns>
        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            try
            {
                manager.AddImageAdornment(manager.PreviewImageAdornment.VisualElement);
                return DragDropPointerEffects.Copy;
            }
            finally
            {
                RemovePreviewImage();
            }
        }

        /// <summary>
        /// See <see cref="IDropHandler.IsDropEnabled"/> for more information.
        /// </summary>
        /// <param name="dragDropInfo"></param>
        /// <returns></returns>
        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            bool result = false;

            string imageFilename = GetImageFilename(dragDropInfo);

            if (!string.IsNullOrEmpty(imageFilename))
            {
                string imageFileExtension = Path.GetExtension(imageFilename).ToLowerInvariant();
                result = this.SupportedImageExtensions.Contains(imageFileExtension);
            }

            if (!result)
            {
                RemovePreviewImage();
            }

            return result;
        }

        private static string GetImageFilename(DragDropInfo info)
        {
            DataObject data = new DataObject(info.Data);
            
            if (info.Data.GetDataPresent(ImageInsertionDropHandlerProvider.FileDropDataFormat))
            {
                // The drag and drop operation came from the file system
                StringCollection files = data.GetFileDropList();

                if (files != null && files.Count == 1)
                {
                    return files[0];
                }
            }
            else if (info.Data.GetDataPresent(ImageInsertionDropHandlerProvider.VSProjectItemDataFormat))
            {            
                // The drag and drop operation came from the VS solution explorer
                return data.GetText();
            }

            return null;
        }

        /// <summary>
        /// See <see cref="IDropHandler.HandleDragCanceled"/> for more information.
        /// </summary>
        public void HandleDragCanceled()
        {
            this.manager.PreviewImageAdornment.Clear();
            this.manager.HighlightLineAdornment.Clear();
        }
    }
}