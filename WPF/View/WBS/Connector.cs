using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SmartPert.View.WBS
{
    /// <summary>
    /// Connector class joins two anchors together
    /// Created 3/2/2021 by Robert Nelson
    /// </summary>
    public class Connector
    {
        private bool isConnecting;
        private bool anchor2IsReceiver;
        private Line line;
        private Anchor anchor1;
        private Anchor anchor2;
        private Canvas canvas;

        #region Properties
        public Anchor Anchor1
        {
            get => anchor1; 
            set
            {
                anchor1 = value;
                if(anchor1 != null)
                {
                    Point p = anchor1.Point;
                    line.X1 = p.X;
                    line.Y1 = p.Y;
                }
            }
        }
        public Anchor Anchor2 { get => anchor2; set { 
                anchor2 = value;
                if(anchor2 != null)
                {
                    Point p = anchor2.Point;
                    line.X2 = p.X;
                    line.Y2 = p.Y;
                }
            } }

        public Canvas Canvas { get => canvas; }
        #endregion

        #region Constructor
        public Connector(Canvas canvas)
        {
            line = new Line();
            line.Stroke = Brushes.Black;
            line.StrokeThickness = 3;
            line.MouseUp += Line_MouseUp;
            this.canvas = canvas;
        }
        #endregion

        #region Private Methods
        private void Line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Most likely the canvas was deactivated and lost mouse tracking if we're connecting
            if(isConnecting)
            {
                return;
            } else
            {
                // Find closest endpoint
                Point end1, end2, point;
                point = e.GetPosition(Canvas);
                end1 = Anchor1.Point;
                end2 = Anchor2.Point;
                if (Distance(point, end1) > Distance(point, end2))
                    StartConnecting(Anchor1, anchor2IsReceiver);
                else
                    StartConnecting(Anchor2, !anchor2IsReceiver);
            }
        }

        private double Distance(Point p1, Point p2)
        {

            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));

        }

        private Anchor GetClosestConnectableAnchor(List<Anchor> anchors, Point p, Anchor origin)
        {
            Anchor best = null;
            double min = 0;
            double dist;
            foreach(Anchor anchor in anchors)
            {
                if(origin.CanConnect(anchor))
                {
                    Point point = anchor.Point;
                    // distance
                    dist = Distance(p, point);
                    if (best == null || dist < min)
                    {
                        best = anchor;
                        min = dist;
                    }
                }
            }
            return best;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = Mouse.GetPosition(Canvas);
            line.X2 = p.X;
            line.Y2 = p.Y;
        }

        private void TryConnectAnchor()
        {
            Canvas.ReleaseMouseCapture();
            if (!isConnecting)
                throw new InvalidOperationException("Can't finish connection if it's not connecting");
            CtrlHitTest test = new CtrlHitTest(Anchor1.Connectable.GetType(), Canvas);
            Point p = Mouse.GetPosition(Canvas);
            List<DependencyObject> hits = test.Run(p, Anchor1.Connectable as DependencyObject);
            if (hits.Count > 0)
            {
                IConnectable c = hits[0] as IConnectable;
                Anchor2 = GetClosestConnectableAnchor(c.GetAnchors(), p, Anchor1);
            }
            OnConnectingFinish();
        }

        // Finish up after attempting connection
        private void OnConnectingFinish()
        {
            // If we are connected, send events to the anchors
            if(IsConnected())
            {
                Anchor2.Connect(this, Anchor1.Connectable, anchor2IsReceiver);
                Anchor1.Connect(this, Anchor2.Connectable, !anchor2IsReceiver);
            } else // Otherwise remove the line from canvas
            {
                Canvas.Children.Remove(line);
            }
            // Canvas events cleanup
            Canvas.ReleaseMouseCapture();
            Canvas.MouseMove -= Canvas_MouseMove;
            Canvas.MouseUp -= Canvas_MouseUp;
            isConnecting = false;
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            TryConnectAnchor();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Disconnects the connection
        /// </summary>
        /// <returns>true if a disconnection occurred (was not already disconnected)</returns>
        public bool Disconnect()
        {
            // If connected then disconnect
            if (IsConnected())
            {
                Anchor1.Disconnect(this, Anchor2.Connectable, !anchor2IsReceiver);
                Anchor2.Disconnect(this, Anchor1.Connectable, anchor2IsReceiver);
                Canvas.Children.Remove(line);
                Anchor1 = Anchor2 = null;
                return true;
            } else if(isConnecting)
                OnConnectingFinish();
            return false;
        }

        /// <summary>
        /// Is this connection live?
        /// </summary>
        /// <returns>true if connected to both anchors</returns>
        public bool IsConnected()
        {
            return Anchor1 != null && Anchor2 != null;
        }

        /// <summary>
        /// Gets the connected item
        /// </summary>
        /// <param name="from">The origin item</param>
        /// <returns>IConnectable</returns>
        public IConnectable GetConnected(Anchor from)
        {
            if (!IsConnected())
                return null;
            else if (from == Anchor1)
                return Anchor2.Connectable;
            else
                return Anchor1.Connectable;
        }


        /// <summary>
        /// Start connecting to the anchor
        /// </summary>
        /// <param name="start">the starting anchor</param>
        /// <param name="isOrigin">treat starting anchor as origin (not receiver)</param>
        public void StartConnecting(Anchor start, bool isOrigin = true)
        {
            Disconnect();
            Anchor1 = start;
            isConnecting = true;
            anchor2IsReceiver = isOrigin;
            canvas.Children.Add(line);
            Canvas.MouseMove += Canvas_MouseMove;
            Canvas.MouseUp += Canvas_MouseUp;
            Canvas.CaptureMouse();
        }

        /// <summary>
        /// Adjusts connector when connected anchor is moved
        /// </summary>
        /// <param name="anchor">anchor</param>
        public void OnAnchorMove(Anchor anchor, Point p)
        {
            if (anchor == Anchor1)
            {
                line.X1 = p.X;
                line.Y1 = p.Y;
            }
            else
            {
                line.X2 = p.X;
                line.Y2 = p.Y;
            }
        }
        #endregion
    }
}
