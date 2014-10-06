using System.IO;
using System.Reflection;
using Xamarin.Forms;

namespace Fmg.XamForms.Renderers.Markdown
{
    public class MarkdownView : View
    {
        public static readonly BindableProperty MarkdownProperty = BindableProperty.Create<MarkdownView, string>(p => p.Markdown, default(string));
        public static readonly BindableProperty StyleStringProperty = BindableProperty.Create<MarkdownView, string>(p => p.StyleString, default(string));

        public MarkdownView()
        {
            StyleString = GetDefaultStyle();
            TransitionSpeedMsec = 50;
            Easing = Easing.SinInOut;
        }

        public Easing Easing { get; set; }

        public string Markdown
        {
            get { return (string) GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }

        public string StyleString
        {
            get { return (string) GetValue(StyleStringProperty); }
            set { SetValue(StyleStringProperty, value); }
        }

        public uint TransitionSpeedMsec { get; set; }

        private static string GetDefaultStyle()
        {
            var assembly = typeof (MarkdownView).GetTypeInfo().Assembly;
            const string resourceName = "Fmg.XamForms.Renderers.Markdown.Styles.Default.css";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}