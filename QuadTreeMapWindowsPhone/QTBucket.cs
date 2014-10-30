using System.Device.Location;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuadTreeMap
{
    public class QTBucket
    {
        public int BucketLimit { get; set; }
        public QTBounds Bounds { get; set; }
        public QTBucket TLBucket { get; set; }
        public QTBucket TRBucket { get; set; }
        public QTBucket BLBucket { get; set; }
        public QTBucket BRBucket { get; set; }
        public List<GeoCoordinate> Points { get; set; }
        public List<QTBucket> Buckets { get; set; }

        public int Count
        {
            get
            {
                return Points.Count + Buckets.Sum(b => b.Count);
                int count = 0;
                if (Points != null)
                {
                    count = Points.Count;
                    Buckets.ForEach(b => count += b.Count);
                }
                return count;
            }
        }

        public double Latitude
        {
            get
            {
                if (TLBucket == null)
                    return Points.Average(t => t.Latitude);
                return Buckets.Average(b => b.Latitude);
            }
        }

        public double Longitude
        {
            get
            {
                if (TLBucket == null)
                    return Points.Average(t => t.Longitude);
                return Buckets.Average(b => b.Longitude);
            }
        }

        public QTBucket(int bucketLimit, QTBounds bounds)
        {
            BucketLimit = bucketLimit;
            Bounds = bounds;
            Points = new List<GeoCoordinate>();
            Buckets = new List<QTBucket>();
        }

        public void Clear()
        {
            if (TLBucket != null)
            {
                Buckets.ForEach(b => b.Clear());
                TLBucket = null;
                TRBucket = null;
                BLBucket = null;
                BRBucket = null;
            }
            Points.Clear();
        }

        public void AddPOI(GeoCoordinate poi)
        {
            if (TLBucket == null)
                Points.Add(poi);
            else
                AddPointToSubBuckets(poi);

            if (Points.Count > BucketLimit)
            {
                if (Points.Any(p => p.Latitude != poi.Latitude) || Points.Any(p => p.Longitude != poi.Longitude))
                {
                    double midY = Bounds.MidY;
                    double midX = Bounds.MidX;
                    TLBucket = new QTBucket(BucketLimit, new QTBounds(Bounds.Top, midX, midY, Bounds.Left));
                    TRBucket = new QTBucket(BucketLimit, new QTBounds(Bounds.Top, Bounds.Right, midY, midX));
                    BLBucket = new QTBucket(BucketLimit, new QTBounds(midY, midX, Bounds.Bottom, Bounds.Left));
                    BRBucket = new QTBucket(BucketLimit, new QTBounds(midY, Bounds.Right, Bounds.Bottom, midX));
                    Buckets.Add(TLBucket);
                    Buckets.Add(TRBucket);
                    Buckets.Add(BLBucket);
                    Buckets.Add(BRBucket);
                    Points.ForEach(AddPointToSubBuckets);
                    Points.Clear();
                }
            }
        }

        public List<QTBucket> BucketsForWindow(QTBounds window)
        {
            var buckets = new List<QTBucket>();

            if (Buckets.Count == 0 && Points.Count > 0)
                buckets.Add(this);
            else
                Buckets.Where(b => window.Intersect(b.Bounds)).ToList().ForEach(b =>buckets.AddRange(b.BucketsForWindow(window)));
            
            return buckets;
        }

        private void AddPointToSubBuckets(GeoCoordinate p)
        {
            if (TLBucket.Bounds.Contains(p.Latitude, p.Longitude))
                TLBucket.AddPOI(p);
            else if (TRBucket.Bounds.Contains(p.Latitude, p.Longitude))
                TRBucket.AddPOI(p);
            else if (BLBucket.Bounds.Contains(p.Latitude, p.Longitude))
                BLBucket.AddPOI(p);
            else if (BRBucket.Bounds.Contains(p.Latitude, p.Longitude))
                BRBucket.AddPOI(p);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj as QTBucket == null)
                return false;
            var bucket = obj as QTBucket;

            return bucket.Count == Count && bucket.BucketLimit == BucketLimit && bucket.Buckets.Count == Buckets.Count && bucket.Bounds.Equals(Bounds);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
