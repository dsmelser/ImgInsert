using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Creates and initializes an instance of <see cref="ImageAdornmentManager"/> when view is created.
    /// </summary>
    [Export(typeof(ILineTransformSourceProvider))]
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class ImageInsertionFactory : ILineTransformSourceProvider, IWpfTextViewCreationListener
    {
        [Import]
        private IEditorFormatMapService EditorFormatMapService { get; set; }

        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider ServiceProvider { get; set; }

        ILineTransformSource ILineTransformSourceProvider.Create(IWpfTextView view)
        {
            return new LineTransformSource(GetOrCreateManager(view));
        }

        private ImageAdornmentManager GetOrCreateManager(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty<ImageAdornmentManager>(() =>
                new ImageAdornmentManager(ServiceProvider, view, this.EditorFormatMapService.GetEditorFormatMap(view))); 
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            GetOrCreateManager(textView);
        }
    }
}