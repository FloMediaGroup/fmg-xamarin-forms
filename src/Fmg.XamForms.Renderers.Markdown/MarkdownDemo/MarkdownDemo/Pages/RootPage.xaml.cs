using System;
using MarkdownDemo.ViewModels;
using Xamarin.Forms;

namespace MarkdownDemo.Pages
{
    public partial class RootPage : ContentPage
    {
        private readonly string _old;
        private const string _new = @"
Hello World";

        public RootPage()
        {
            InitializeComponent();

            ViewModel = new RootPageViewModel();
            BindingContext = ViewModel;

            _old = ViewModel.Markdown;

            ((Button) ClickMe).Clicked += OnClicked;
        }

        public RootPageViewModel ViewModel { get; set; }

        private void OnClicked(object sender, EventArgs eventArgs)
        {
            ViewModel.Markdown = ViewModel.Markdown == _old ? _new : _old;
        }
    }
}