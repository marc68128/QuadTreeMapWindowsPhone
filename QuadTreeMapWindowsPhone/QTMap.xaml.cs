using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Shell;

namespace QuadTreeMap
{
    public partial class QTMap : UserControl
    {
        #region Internal

        private int _currentBucketLimit = 25;
        private bool _mapIsReady, _mapMoved;
        private bool _needToMoveToItems = true;
        private QTBounds _currentWindow;
        private QTBucket _mainBucket;
        private readonly List<QTBucket> _buckets;
        private IEnumerable<PushPinInfo> _items;
        private readonly Dictionary<QTBucket, MapLayer> _layerForBucket;
        private readonly Dictionary<MapLayer, QTBucket> _bucketForLayer;
        private GeoCoordinate _lastCenter;
        private double _lastZoomLevel;

        #endregion

        #region DependencyProperties

        private readonly DependencyProperty _pushPinTemplateProperty = DependencyProperty.Register("PushPinTemplate",
            typeof (ControlTemplate), typeof (QTMap), new PropertyMetadata(default(ControlTemplate)));

        private readonly DependencyProperty _bucketTemplateProperty = DependencyProperty.Register("BucketTemplate",
            typeof (ControlTemplate), typeof (QTMap), new PropertyMetadata(default(ControlTemplate)));

        #endregion

        #region Getter & Setter

        public ControlTemplate PushPinTemplate
        {
            get { return (ControlTemplate) GetValue(_pushPinTemplateProperty); }
            set { SetValue(_pushPinTemplateProperty, value); }
        }
        public ControlTemplate BucketTemplate
        {
            get { return (ControlTemplate) GetValue(_bucketTemplateProperty); }
            set { SetValue(_bucketTemplateProperty, value); }
        }

        #endregion


        public QTMap()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            _items = new List<PushPinInfo>();
            _buckets = new List<QTBucket>();
            _layerForBucket = new Dictionary<QTBucket, MapLayer>();
            _bucketForLayer = new Dictionary<MapLayer, QTBucket>();
            _mapIsReady = false;
        }
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {


            Observable.Interval(new TimeSpan(0, 0, 0, 0, 500))
                .Subscribe(_ =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (_mapMoved && _lastCenter == Map.Center && _lastZoomLevel == Map.ZoomLevel)
                        {
                            _mapMoved = false;
                            Dispatcher.BeginInvoke(MapMoved);
                        }
                        else if (_lastCenter != Map.Center && _lastZoomLevel != Map.ZoomLevel)
                        {
                            _mapMoved = true;
                        }
                        _lastCenter = Map.Center;
                        _lastZoomLevel = Map.ZoomLevel;
                    });
                });



            RefreshItems(_items);
            RefreshCurrentBucketLimit();
        }


        #region Public Methods

        public void MoveMapToItemsIfNeeded(bool force = false)
        {
            if (Map != null && ((force || _needToMoveToItems) && _items.Any()))
            {
                if (_items.Count() == 1)
                {
                    Map.SetView(_items.First().Position, 15);
                }
                else
                {
                    Map.SetView(LocationRectangle.CreateBoundingRectangle(_items.Select(l => l.Position)));
                }

                _needToMoveToItems = false;
            }
        }
        public void RefreshItems(IEnumerable<PushPinInfo> items)
        {
            _items = items;
            MoveMapToItemsIfNeeded();
            InitMainBucketIfNeeded();
            RefreshBucketsAndMarkers();
        }

        #endregion

        #region Internal Methods

        void InitMainBucketIfNeeded()
        {
            if (_mainBucket == null || _mainBucket.BucketLimit != _currentBucketLimit ||
                _mainBucket.Count != _items.Count())
            {
                if (_mainBucket != null)
                {
                    _mainBucket.Clear();
                }
                _mainBucket = new QTBucket(_currentBucketLimit, new QTBounds(90, 180, -90, -180));
                foreach (var item in _items)
                {
                    _mainBucket.AddPOI(item.Position);
                }
            }
        }
        void RefreshBucketsAndMarkers()
        {
            if (_mapIsReady)
            {
                var newBuckets = _mainBucket.BucketsForWindow(_currentWindow);

                var bucketsToAdd = newBuckets.Where(newBucket => !_buckets.Any(curB => curB.Equals(newBucket))).ToList();
                var bucketsToRemove =
                    _buckets.Where(currentBucket => !newBuckets.Any(newBucket => newBucket.Equals(currentBucket)))
                        .ToList();

                _buckets.Clear();
                _buckets.AddRange(newBuckets);

                bucketsToAdd.ToList().ForEach(b =>
                {
                    var layer = new MapLayer();
                    var overlay = new MapOverlay();
                    var pushpin = new Pushpin();

                    if (b.Count == 1)
                    {
                        pushpin.Content = b;
                        pushpin.Template = PushPinTemplate == default(ControlTemplate)
                            ? PushPinDefaultTemplate
                            : PushPinTemplate;
                    }
                    else
                    {
                        pushpin.DataContext = b;
                        pushpin.Template = BucketTemplate == default(ControlTemplate)
                            ? BucketDefaultTemplate
                            : BucketTemplate;
                    }

                    pushpin.GeoCoordinate = new GeoCoordinate(b.Latitude, b.Longitude);


                    overlay.GeoCoordinate = new GeoCoordinate(b.Latitude, b.Longitude);
                    overlay.Content = pushpin;

                    overlay.PositionOrigin = b.Count == 1 ? new Point(0.25, 1) : new Point(0.5, 0.5);
                    layer.Add(overlay);

                    Map.Layers.Add(layer);


                    _layerForBucket.Add(b, layer);
                    _bucketForLayer.Add(layer, b);
                });

                bucketsToRemove.ForEach(b =>
                {
                    if (_layerForBucket.ContainsKey(b))
                    {
                        var layer = _layerForBucket[b];
                        Map.Layers.Remove(layer);
                        _layerForBucket.Remove(b);
                        _bucketForLayer.Remove(layer);
                    }
                });
            }
        }
        void RefreshCurrentBucketLimit()
        {
            _currentBucketLimit = GetCurrentBucketLimit(_currentWindow);
        }
        int GetCurrentBucketLimit(QTBounds window)
        {
            var itemsCount = _items.Count(item => window.Contains(item.Position.Latitude, item.Position.Longitude));
            if (itemsCount > 1000)
                return 1000;
            int limit = itemsCount/13;
            return limit;
        }
        void MapMoved()
        {
            if (!_mapIsReady)
            {
                _mapIsReady = true;
                MoveMapToItemsIfNeeded();
            }
            var topLeft = Map.ConvertViewportPointToGeoCoordinate(new Point(0, 0));
            var bottomRight = Map.ConvertViewportPointToGeoCoordinate(new Point(ActualWidth, ActualHeight));
            if (topLeft != null && bottomRight != null)
            {
                _currentWindow = new QTBounds(topLeft.Latitude, bottomRight.Longitude, bottomRight.Latitude, topLeft.Longitude);
                RefreshCurrentBucketLimit();
                InitMainBucketIfNeeded();
                RefreshBucketsAndMarkers();
            }

        }

        #endregion


    }
}
