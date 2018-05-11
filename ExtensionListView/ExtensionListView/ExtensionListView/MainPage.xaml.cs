using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ExtensionListView
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            Task.Run(() =>
            {
                var source = Enumerable.Range(0, 1000).Select(x => x.ToString()).ToArray();

                var templatedSource = new List<IGrouping<string, string>>(
                    source.OrderBy(x => x).GroupBy(x => x[0].ToString().ToUpper()));

                return templatedSource;
            }).ContinueWith(task =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    listView.ItemsSource = task.Result;
                });
            });
        }
    }
}
