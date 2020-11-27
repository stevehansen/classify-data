using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using CefSharp.WinForms.Internals;

namespace ClassifyData.App
{
    public partial class AppForm : Form
    {
        private readonly ChromiumWebBrowser browser;

        public AppForm()
        {
            InitializeComponent();

            WindowState = FormWindowState.Maximized;

            browser = new ChromiumWebBrowser(Program.BaseAddress)
            {
                MenuHandler = new AppMenuHandler()
            };
            Controls.Add(browser);

            browser.TitleChanged += OnBrowserTitleChanged;
        }

        private void OnBrowserTitleChanged(object sender, TitleChangedEventArgs args)
        {
            this.BeginInvoke(new Action(() => Text = args.Title));
        }
    }
}