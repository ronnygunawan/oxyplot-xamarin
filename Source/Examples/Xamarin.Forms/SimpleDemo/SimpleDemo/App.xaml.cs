using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using OxyPlot.Xamarin.Forms;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace SimpleDemo
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var content = new ContentPage
            {
                Title = "SimpleDemo",
                Content = new PlotView
                {
                    Model = new PlotModel
                    {
                        Title = "OxyPlot in Xamarin Forms.",
                        Axes =
                        {
                            new CategoryAxis {Position = AxisPosition.Bottom},
                            new LinearAxis {Position = AxisPosition.Left, MinimumPadding = 0}
                        },
                        Series =
                        {
                            new BarSeries
                            {
                                Items =
                                {
                                    new BarItem {Value = 3},
                                    new BarItem {Value = 14},
                                    new BarItem {Value = 11},
                                    new BarItem {Value = 12},
                                    new BarItem {Value = 7}
                                }
                            }
                        }
                    },
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalOptions = LayoutOptions.Fill
                }
            };

            this.MainPage = new NavigationPage(content);

            // MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
