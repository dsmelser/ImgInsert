using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Resources;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Text;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;

namespace Microsoft.VisualStudio.ImageInsertion
{
    /// <summary>
    /// Saves and retrieves image adornements from a repository.
    /// </summary>
    internal class ImageAdornmentRepositoryService
    {
        private ITextBuffer textBuffer;
        private List<ImageAdornment> images = new List<ImageAdornment>();

        internal ImageAdornmentRepositoryService(ITextBuffer textBuffer)
        {
            this.textBuffer = textBuffer;

            ITextDocument textDocument = textBuffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));
            this.RepositoryFilename = Path.ChangeExtension(textDocument.FilePath, ".Images.resx");
        }

        internal IList<ImageAdornment> Images { get { return this.images; } }

        internal void EnsureRepositoryFileExists()
        {
            if (!File.Exists(this.RepositoryFilename))
            {
                using (IResourceWriter writer = new ResourceWriter(this.RepositoryFilename))
                {
                    writer.Generate();
                }
            }
        }

        internal void Save()
        {
            if (!File.Exists(this.RepositoryFilename) && this.images.Count == 0)
            {
                // nothing to do
                return;
            }

            // Write the images to the repository file
            using (IResourceWriter writer = new ResourceWriter(this.RepositoryFilename))
            {
                foreach (ImageAdornmentInfo info in this.images.Select(image => image.Info))
                {
                    writer.AddResource(info.Id, info);
                }

                writer.Generate();
            }
        }

        internal void Load()
        {
            if (File.Exists(this.RepositoryFilename))
            {
                using (IResourceReader reader = new ResourceReader(this.RepositoryFilename))
                {
                    try
                    {
                        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnCurrentAppDomainAssemblyResolve);

                        foreach (DictionaryEntry entry in reader)
                        {
                            ImageAdornmentInfo info = entry.Value as ImageAdornmentInfo;
                            // Convert the bitmap
                            ImageSource imageSource = GetImageSourceFromBitmap(info.Bitmap);

                            ImageAdornment imageAdornment = new ImageAdornment(
                                this.textBuffer.CurrentSnapshot, info, imageSource);

                            Add(imageAdornment);
                        }
                    }
                    finally
                    {
                        AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(OnCurrentAppDomainAssemblyResolve);
                    }
                }
            }
        }

        private Assembly OnCurrentAppDomainAssemblyResolve(object sender, ResolveEventArgs e)
        {
            if (e.Name == this.GetType().Assembly.FullName)
            {
                return this.GetType().Assembly;
            }

            return null;
        }

        /// <summary>
        ///  Adds an image adornment to the repository
        /// </summary>
        /// <param name="imageAdornment"></param>
        internal void Add(ImageAdornment imageAdornment)
        {
            if (imageAdornment != null && !this.images.Contains(imageAdornment))
            {
                this.images.Add(imageAdornment);
            }
        }

        /// <summary>
        /// Removes an image adornement form the repository
        /// </summary>
        /// <param name="imageAdornment"></param>
        internal void Remove(ImageAdornment imageAdornment)
        {
            if (imageAdornment != null && this.images.Contains(imageAdornment))
            {
                this.images.Remove(imageAdornment);
            }
        }

        private static ImageSource GetImageSourceFromBitmap(System.Drawing.Bitmap source)
        {
            // converts the bitmap to image source
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                source.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        /// <summary>
        /// Gets the repository filename
        /// </summary>
        internal string RepositoryFilename { get; private set; }
    }
}