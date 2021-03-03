using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartPert.View.WBS
{
    /// <summary>
    /// Objects that can be connected
    /// </summary>
    public interface IConnectable
    {
        /// <summary>
        /// Determines if it can connect to target
        /// </summary>
        /// <param name="target">connectable target</param>
        /// <returns>true if it can</returns>
        bool CanConnect(IConnectable target);
        /// <summary>
        /// When a successful connection is made
        /// </summary>
        /// <param name="sender">The anchor sending signal</param>
        /// <param name="target">Connected item</param>
        /// <param name="isReceiver">True if it received the connection, false if it originated it</param>
        void OnConnect(Anchor sender, IConnectable target, bool isReceiver);

        /// <summary>
        /// When a disconnect is made
        /// </summary>
        /// <param name="sender">The anchor sending</param>
        /// <param name="target">Connected item that is disconnecting</param>
        /// <param name="isReceiver">Did not originate from here if true</param>
        void OnDisconnect(Anchor sender, IConnectable target, bool isReceiver);

        /// <summary>
        /// Get the anchors from a connectable item
        /// </summary>
        /// <returns>List of anchors</returns>
        List<Anchor> GetAnchors();
    }

    /// <summary>
    /// Anchor for connectors to connect to
    /// </summary>
    public class Anchor : Button
    {
        private List<Connector> connectors;
        private IConnectable connectable;
        private Canvas canvas;
        private bool initialized;

        #region Properties
        public Point Point { get => 
                TransformToAncestor(Canvas as Visual).Transform(new Point(ActualWidth / 2, ActualHeight / 2));
        }
        public Canvas Canvas { get => canvas; 
            set { 
                canvas = value;
            }
        }
        public IConnectable Connectable { get => connectable; set => connectable = value; }
        #endregion

        public Anchor()
        {
            Content = "o";
            Background = Brushes.Transparent;
            BorderThickness = new Thickness(0);
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            Click += StartConnect;
        }

        #region Public Methods
        public bool IsConnectedTo(IConnectable connectable)
        {
            if(connectors != null)
                foreach (Connector connector in connectors)
                    if (connector.GetConnected(this) == connectable)
                        return true;
            return false;
        }

        /// <summary>
        /// Connects the connector, (Does not check if it can!)
        /// </summary>
        /// <param name="connector">connector</param>
        /// <param name="connected">the target connectable</param>
        /// <param name="isReceiver">If this object received the connection (did not start it)</param>
        public void Connect(Connector connector, IConnectable connected, bool isReceiver)
        {
            if (connectors == null)
                connectors = new List<Connector>();
            connectors.Add(connector);
            Connectable.OnConnect(this, connected, isReceiver);
        }

        /// <summary>
        /// Disconnects the connection
        /// </summary>
        /// <param name="connector">connector</param>
        /// <param name="connected">connectable that's disconnecting</param>
        /// <param name="isReceiver">is receiver</param>
        public void Disconnect(Connector connector, IConnectable connected, bool isReceiver)
        {
            if (connectors != null)
                connectors.Remove(connector);
            Connectable.OnDisconnect(this, connected, isReceiver);
        }

        /// <summary>
        /// Determines if two anchors can be connected
        /// </summary>
        /// <param name="anchor">anchor</param>
        /// <returns>true if they can connect</returns>
        public bool CanConnect(Anchor anchor)
        {
            // Currently, this just specifies if a Connectable can connect to the other
            // In future, you may want to also distinguish which anchors are allowed to connect to which, (ie top only connects to bottom)
            return Connectable.CanConnect(anchor.Connectable) && anchor.Connectable.CanConnect(Connectable);
        }

        /// <summary>
        /// When moving, Update the connection position
        /// </summary>
        /// <param name="point">N/A</param>
        public void OnMove(Point point)
        {
            //Point = point;
            if (connectors != null)
                foreach (Connector c in connectors)
                    c.OnAnchorMove(this, Point);
        }
        #endregion

        #region Private Methods
        private void StartConnect(object sender, RoutedEventArgs e)
        {
            Connector con = new Connector(Canvas) { Anchor1 = this };
            con.StartConnecting(this);
        }
        #endregion

    }
}
