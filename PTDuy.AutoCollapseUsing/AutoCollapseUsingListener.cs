using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;

namespace Duy.AutoCollapseUsing
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("CSharp")]
    internal sealed class AutoCollapseUsingListener : IWpfTextViewCreationListener
    {
        [Import]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        private IWpfTextView _textView;
        private bool _isEverCollapse;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (OutliningManagerService == null || textView == null)
            {
                return;
            }

            IOutliningManager outliningManager = OutliningManagerService.GetOutliningManager(textView);

            if (outliningManager == null)
            {
                return;
            }

            outliningManager.RegionsChanged += OnRegionsChanged;
            _textView = textView;
            _isEverCollapse = false;
        }

        private void OnRegionsChanged(object sender, RegionsChangedEventArgs regionsChangedEventArgs)
        {
            var outliningManager = sender as IOutliningManager;
            if (outliningManager != null && outliningManager.Enabled)
            {
                // Collapses all of the regions within the span where Match() returns true.
                outliningManager.CollapseAll(regionsChangedEventArgs.AffectedSpan, Match);
            }
        }

        // Returns true when the collapsible should be collapsed.
        private bool Match(ICollapsible collapsible)
        {
            try
            {
                if (_isEverCollapse)
                    return false;

                string regionText = collapsible?.Extent?.GetText(_textView.TextSnapshot);
                bool isUsingRegion = IsUsingRegion(regionText);

                if (isUsingRegion)
                {
                    //bool isCaretFound = IsCaretInRegion(collapsible);

                    //if (isCaretFound)
                    //    return false;

                    _isEverCollapse = true;

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("AutoCollapseUsing", ex.ToString());

                return false;
            }
        }

        private bool IsUsingRegion(string regionText)
        {
            if (string.IsNullOrEmpty(regionText))
                return false;

            string[] lines = Regex.Split(regionText, Environment.NewLine);
            bool isUsingRegion = lines != null && lines.Length > 1 && Regex.IsMatch(lines[1], @"(?<=\b)using\s+(?:static\s+|\S+\s*=\s*)?\S+;");

            return isUsingRegion;
        }

        private bool IsCaretInRegion(ICollapsible collapsible)
        {
            if (collapsible == null || collapsible.Extent == null)
                return false;

            ITextSnapshot textSnapshot = _textView.TextSnapshot;
            int startLine = collapsible.Extent.GetStartPoint(textSnapshot).GetContainingLine().LineNumber;
            int endLine = collapsible.Extent.GetEndPoint(textSnapshot).GetContainingLine().LineNumber;
            var cline = _textView.Caret.Position.BufferPosition.GetContainingLine();
            int caretLine = _textView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;

            return startLine <= caretLine && endLine + 1 >= caretLine;
        }
    }
}
