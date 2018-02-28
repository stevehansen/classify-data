using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            browser = new ChromiumWebBrowser("http://localhost:9000/")
            {
                Dock = DockStyle.Fill,
            };
            Controls.Add(browser);

            browser.TitleChanged += OnBrowserTitleChanged;
        }

        private void OnBrowserTitleChanged(object sender, TitleChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => Text = args.Title);
        }
    }
}
