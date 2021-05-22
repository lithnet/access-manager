using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Lithnet.AccessManager.Server.UI
{
    public class BindableRichTextBox : RichTextBox
    {
        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(FlowDocument), typeof(FlowDocument), typeof(BindableRichTextBox), new PropertyMetadata(OnDocumentChanged));

        public FlowDocument FlowDocument
        {
            get => (FlowDocument) this.GetValue(DocumentProperty);
            set => this.SetValue(DocumentProperty, value);
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableRichTextBox control = (BindableRichTextBox)d;

            if (!(e.NewValue is FlowDocument document))
            {
                control.Document = new FlowDocument();
            }
            else
            {
                control.Document = document;
            }
        }
    }
}
