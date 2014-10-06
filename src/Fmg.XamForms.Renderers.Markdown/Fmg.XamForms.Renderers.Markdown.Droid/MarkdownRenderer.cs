using System;
using System.ComponentModel;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Telephony;
using Android.Views;
using Android.Webkit;
using Fmg.XamForms.Renderers.Markdown;
using Fmg.XamForms.Renderers.Markdown.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Uri = Android.Net.Uri;
using WebView = Android.Webkit.WebView;

[assembly: ExportRenderer(typeof (MarkdownView), typeof (MarkdownViewRenderer))]

namespace Fmg.XamForms.Renderers.Markdown.Droid
{
    public class MarkdownViewRenderer : ViewRenderer<MarkdownView, WebView>
    {
        private readonly MarkdownSharp _markdownSharp;
        private bool _needsRedraw;
        private int _oldHeight = -1;
        private ViewTreeObserver _viewTreeObserver;
        private WebView _webView;

        public MarkdownViewRenderer()
        {
            _markdownSharp = new MarkdownSharp();
        }

        protected override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            _needsRedraw = true;
            _webView.Reload();
        }

        protected override void OnElementChanged(ElementChangedEventArgs<MarkdownView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement == null)
            {
                _webView = new WebView(Context);
                _webView.Settings.JavaScriptEnabled = true;
                _webView.SetWebViewClient(new MarkdownViewClient());

                _viewTreeObserver = _webView.ViewTreeObserver;
                _viewTreeObserver.PreDraw += ResizeWebView;

                SetNativeControl(_webView);
            }

            RenderMarkdownAndBindContent(Element.Markdown, Element.StyleString);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == MarkdownView.MarkdownProperty.PropertyName || e.PropertyName == MarkdownView.StyleStringProperty.PropertyName)
            {
                RenderMarkdownAndBindContent(Element.Markdown, Element.StyleString);
            }
        }

        private void HandleHtmlContentChanged()
        {
            var handler = HtmlContentChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void RenderMarkdownAndBindContent(string markdown, string styleString)
        {
            if (string.IsNullOrEmpty(markdown)) return;
            var rendered = _markdownSharp.Transform(markdown);

            var html = new StringBuilder();
            html.Append("<html>");
            html.Append(string.Format("<style>{0}</style>", styleString));
            html.Append("<body>");
            html.Append(rendered);
            html.Append("</body>");
            html.Append("</html>");

            _webView.LoadData(html.ToString(), "text/html", "utf-8");
            Element.HeightRequest = 0;
            _webView.Reload();
            _needsRedraw = true;
            HandleHtmlContentChanged();
        }

        private async void ResizeWebView(object sender, EventArgs e)
        {
            if (!_needsRedraw || Element == null) return;

            var newContentHeight = _webView.ContentHeight;

            if (newContentHeight == _oldHeight || newContentHeight == 0) return;

            var bounds = new Rectangle(Element.Bounds.X, Element.Bounds.Y, Element.Bounds.Width, newContentHeight);
            await Element.LayoutTo(bounds, Element.TransitionSpeedMsec, Element.Easing);
            Element.HeightRequest = newContentHeight;

            // todo: FIX ME
            // not sure why there's the odd case where the height is 8.
            if (newContentHeight == 8)
            {
                _webView.Reload();
                _oldHeight = -1;
                return;
            }

            _oldHeight = newContentHeight;
            _needsRedraw = false;
        }

        private class MarkdownViewClient : WebViewClient
        {
            private static bool IsTelephonyEnabled
            {
                get
                {
                    var tm = Application.Context.GetSystemService(Context.TelephonyService) as TelephonyManager;
                    return tm != null && tm.SimState == SimState.Ready;
                }
            }

            public override bool ShouldOverrideUrlLoading(WebView webView, string url)
            {
                if (url.StartsWith("mailto:"))
                {
                    url = url.Replace("mailto:", "");

                    var mail = new Intent(Intent.ActionSend);
                    mail.SetType("application/octet-stream");
                    mail.PutExtra(Intent.ExtraEmail, new[] {url.Split('?')[0]});
                    webView.Context.StartActivity(mail);
                    return true;
                }

                if (url.StartsWith("http:") || url.StartsWith("https:"))
                {
                    var link = new Intent(Intent.ActionView);
                    link.SetData(Uri.Parse(url));
                    webView.Context.StartActivity(link);
                    return true;
                }

                if (url.StartsWith("tel:"))
                {
                    if (IsTelephonyEnabled)
                    {
                        var link = new Intent(Intent.ActionDial, Uri.Parse(url));
                        webView.Context.StartActivity(link);
                    }
                    return true;
                }

                return false;
            }
        }

        public event EventHandler HtmlContentChanged;
    }
}