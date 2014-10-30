using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuadTreeMap
{
    public class PushPinInfo
    {
        public GeoCoordinate Position { get; set; }
        public object Content { get; set; }


        public string LayerId
        {
            get
            {
                return Position.Latitude + Content.ToString() + Position.Longitude;
            }
        }
    }
}
