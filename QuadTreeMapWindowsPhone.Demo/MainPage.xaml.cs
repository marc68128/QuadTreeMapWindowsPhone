using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using QuadTreeMap;

namespace QuadTreeMapWindowsPhone.Demo
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
            TestMap();
        }

        private void TestMap()
        {
            var pinfos = new List<PushPinInfo>();
            for (int i = 0; i < 1000; i++)
            {
                var random = new Random();
                Thread.Sleep(10);
                var pinfo = new PushPinInfo
                {
                    Content = i.ToString(),
                    Position =
                        new GeoCoordinate(random.Next(-40, 40) + random.NextDouble(), random.Next(-40, 40) + random.NextDouble())
                };
                pinfos.Add(pinfo);
            }

            Map.RefreshItems(pinfos);
        }
    }
}