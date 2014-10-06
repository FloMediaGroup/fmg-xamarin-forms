using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using Fmg.XamForms.Renderers.Markdown;
using Fmg.XamForms.Renderers.Markdown.Touch;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Rectangle = Xamarin.Forms.Rectangle;

[assembly: ExportRenderer(typeof (MarkdownView), typeof (MarkdownViewRenderer))]

namespace Fmg.XamForms.Renderers.Markdown.Touch
{
    public class MarkdownViewRenderer : ViewRenderer<MarkdownView, UIWebView>
    {
        private readonly MarkdownSharp _markdownSharp;
        private string _styleString;
        private UIWebView _webView;

        public MarkdownViewRenderer()
        {
            _markdownSharp = new MarkdownSharp();
        }

        protected override void OnElementChanged(ElementChangedEventArgs<MarkdownView> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement != null) return;

            if (_webView == null)
            {
                _webView = new UIWebView {AutoresizingMask = UIViewAutoresizing.FlexibleHeight};
                _webView.ShouldStartLoad += LoadHook;
                _webView.LoadFinished += ResizeWebView;
                _webView.BackgroundColor = UIColor.White;
                _webView.ScrollView.ScrollEnabled = false;
                _styleString = Element.StyleString;
                SetNativeControl(_webView);
            }
            RenderMarkdownAndBindContent(Element.Markdown);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == MarkdownView.MarkdownProperty.PropertyName)
            {
                RenderMarkdownAndBindContent(Element.Markdown);
            }

            if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
            {
                _webView.BackgroundColor = Element.BackgroundColor.ToUIColor();
            }

            if (e.PropertyName == MarkdownView.StyleStringProperty.PropertyName)
            {
                _styleString = Element.StyleString;
                RenderMarkdownAndBindContent(Element.Markdown);
            }
        }

        private void HandleHtmlContentChanged()
        {
            var handler = HtmlContentChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private static bool LoadHook(UIWebView webview, NSUrlRequest request, UIWebViewNavigationType navigationtype)
        {
            if (navigationtype == UIWebViewNavigationType.LinkClicked)
            {
                var url = request.Url.AbsoluteString;
                if (url.StartsWith("http:") || url.StartsWith("https:"))
                {
                    UIApplication.SharedApplication.OpenUrl(new NSUrl(url));
                    return false;
                }
            }

            return true;
        }

        private void RenderMarkdownAndBindContent(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown)) return;

            var rendered = _markdownSharp.Transform(markdown);

            var html = new StringBuilder();
            html.Append("<html>");
            html.Append(string.Format("<style>{0}</style>", _styleString));
            html.Append("<body>");
            html.Append(rendered);
            html.Append("</body>");
            html.Append("</html>");

            var contentDirectoryPath = Path.Combine(NSBundle.MainBundle.BundlePath, "Content/");
            _webView.LoadHtmlString(html.ToString(), new NSUrl(contentDirectoryPath, true));

            HandleHtmlContentChanged();
        }

        private async void ResizeWebView(object sender, EventArgs eventArgs)
        {
            var isActuallyReallyFinishedLoadingForRealz = false;
            while (!isActuallyReallyFinishedLoadingForRealz)
            {
                var result = _webView.EvaluateJavascript("(function (){return true})();");
                bool.TryParse(result, out isActuallyReallyFinishedLoadingForRealz);
            }

            var frame = _webView.Frame;
            frame.Height = 10;

            _webView.Frame = frame;

            var fittingSize = _webView.SizeThatFits(SizeF.Empty);
            frame.Size = fittingSize;

            _webView.Frame = frame;
            _webView.ScrollView.Frame = frame;

            var bounds = new Rectangle(Element.Bounds.X, Element.Bounds.Y, Element.Bounds.Width, frame.Height);
            await Element.LayoutTo(bounds, Element.TransitionSpeedMsec, Element.Easing);
            Element.HeightRequest = bounds.Height;
        }

        public event EventHandler HtmlContentChanged;
    }
}