using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor.DragDrop;

namespace Microsoft.VisualStudio.ImageInsertion
{
    [Export(typeof(IDropHandlerProvider))]
    [DropFormat(ImageInsertionDropHandlerProvider.FileDropDataFormat)]
    [DropFormat(ImageInsertionDropHandlerProvider.VSProjectItemDataFormat)]
    [Name("Image Insertion Drop Handler")]
    [Order(Before = "DefaultFileDropHandler")]
    internal class ImageInsertionDropHandlerProvider : IDropHandlerProvider
    {
        internal const string VSProjectItemDataFormat = "CF_VSSTGPROJECTITEMS";
        internal const string FileDropDataFormat = "FileDrop";

        public IDropHandler GetAssociatedDropHandler(IWpfTextView view)
        {
            ImageAdornmentManager imagesManager = view.Properties.GetProperty<ImageAdornmentManager>(typeof(ImageAdornmentManager));

            return view.Properties.GetOrCreateSingletonProperty<ImageInsertionDropHandler>(() => new ImageInsertionDropHandler(imagesManager));
        }
    }
}