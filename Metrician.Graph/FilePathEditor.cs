// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.ComponentModel;
using System.Drawing.Design;

namespace Metrician.Graph
{
    /// <summary>
    /// PropertyGrid type-editor that opens an <see cref="OpenFileDialog"/> when
    /// the user clicks the property's "..." button. Wired up via the string
    /// form of <c>[Editor]</c> so plugin assemblies can opt in without taking
    /// a compile-time WinForms reference:
    /// <code>
    ///   [Editor("Metrician.Graph.FilePathEditor, Metrician.Graph",
    ///           typeof(System.Drawing.Design.UITypeEditor))]
    ///   public string FilePath { get; set; }
    /// </code>
    /// </summary>
    public sealed class FilePathEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) =>
            UITypeEditorEditStyle.Modal;

        public override object? EditValue(
            ITypeDescriptorContext? context,
            IServiceProvider provider,
            object? value)
        {
            using var dlg = new OpenFileDialog();
            if (value is string s && !string.IsNullOrEmpty(s))
                dlg.FileName = s;
            return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : value;
        }
    }
}
