using Microsoft.VisualStudio.Shell;
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

                var usingText = collapsible?.Extent?.GetText(_textView.TextSnapshot);

                var isUsingRegion = !string.IsNullOrEmpty(usingText) && Regex.IsMatch(usingText, "using .*;") && Regex.Matches(usingText, "using ").Count >= 2;

                if (isUsingRegion)
                {
                    _isEverCollapse = true;

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("AutoCollapseUsing", ex.Message);

                return false;
            }
        }
    }
}
