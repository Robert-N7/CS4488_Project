using SmartPert.Model;
using SmartPert.View.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartPert.View.WBS
{
    /// <summary>
    /// Interaction logic for WBS_Task.xaml
    /// Created 3/2/2021 by Robert Nelson
    /// </summary>
    public partial class WBS_Task : UserControl, IItemObserver, IConnectable
    {
        private Task task;
        private List<Anchor> anchors;
        protected bool isDragging;
        private Point clickPosition;
        private Point origin;   // The origin of tranformation
        private Canvas canvas;

        #region Properties
        /// <summary>
        /// Task this refers to
        /// </summary>
        public Task Task
        {
            get
            {
                if (task == null)
                    task = Task.GetUniqueTask();
                return task;
            }

            set
            {
                task = value;
                task.Subscribe(this);
                OnUpdate(task);
            }
        }

        /// <summary>
        /// Task name
        /// </summary>
        public string MyName
        {
            get => Task.Name; set
            {
                Task.Name = value;
            }
        }

        /// <summary>
        /// Likely duration estimate
        /// </summary>
        public int Estimate { get => Task.LikelyDuration; set => Task.LikelyDuration = value; }

        /// <summary>
        /// Connector Visibility
        /// </summary>
        public bool ConnectorVisible
        {
            get => TopConnector.IsVisible;
            set
            {
                var visible = value ? Visibility.Visible : Visibility.Hidden;
                TopConnector.Visibility = visible;
                LeftConnector.Visibility = visible;
                RightConnector.Visibility = visible;
                BottomConnector.Visibility = visible;
            }
        }

        public Canvas Canvas
        {
            get => canvas;
            set
            {
                canvas = value;
                canvas.Children.Add(this);
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public WBS_Task()
        {
            InitializeComponent();
            DataContext = this;
            EstimateEdit.IntegerChange = OnEstimateChange;
            ConnectorVisible = false;
            this.MouseLeftButtonDown += new MouseButtonEventHandler(Control_MouseLeftButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(Control_MouseLeftButtonUp);
            this.MouseMove += new MouseEventHandler(Control_MouseMove);
        }

        public void Init_Anchors(Canvas canvas)
        {
            anchors = new List<Anchor>();
            anchors.Add(RightConnector);
            anchors.Add(LeftConnector);
            anchors.Add(BottomConnector);
            anchors.Add(TopConnector);
            foreach(Anchor anchor in anchors)
            {
                anchor.Canvas = canvas;
                anchor.Connectable = this;
            }
        }

        ~WBS_Task()
        {
            if (task != null)
                task.UnSubscribe(this);
        }
        #endregion


        #region Private Methods
        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            ConnectorVisible = true;
        }

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            ConnectorVisible = false;
        }

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!TopConnector.IsMouseOver && !LeftConnector.IsMouseOver && !RightConnector.IsMouseOver && !BottomConnector.IsMouseOver)
            {
                isDragging = true;
                var draggableControl = sender as UserControl;
                clickPosition = e.GetPosition(this.Parent as UIElement);
                draggableControl.CaptureMouse();
            }
        }

        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                var draggable = sender as UserControl;
                draggable.ReleaseMouseCapture();
                var transform = draggable.RenderTransform as TranslateTransform;
                if (transform != null)  // Store the last transform
                {
                    origin.X = transform.X;
                    origin.Y = transform.Y;
                }
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as UserControl;

            if (isDragging && draggableControl != null)
            {
                Point currentPosition = e.GetPosition(this.Parent as UIElement);

                var transform = draggableControl.RenderTransform as TranslateTransform;
                if (transform == null)
                {
                    transform = new TranslateTransform();
                    draggableControl.RenderTransform = transform;
                }

                transform.X = currentPosition.X - clickPosition.X;
                transform.Y = currentPosition.Y - clickPosition.Y;
                if (origin != null)
                {
                    transform.X += origin.X;
                    transform.Y += origin.Y;
                }
                foreach (Anchor a in anchors)
                    a.OnMove(currentPosition);
            }
        }
        #endregion

        #region Public Methods
        ///// <summary>
        ///// Connects the task to this one, making it a subtask
        ///// </summary>
        ///// <param name="task">subtask</param>
        //public void Connect(WBS_Task task, Line connector)
        //{
        //    if(task != null)
        //    {
        //        // The start (x1, y1) should already be at one of our connectors
        //        // Hook connector to top connector of task
        //        Point point = task.TopConnector.TransformToAncestor(Parent as Visual).Transform(new Point(0, 0));
        //        connector.X2 = point.X;
        //        connector.Y2 = point.Y;
        //        // todo, add task as subtask

        //    }

        //}

        /// <summary>
        /// Estimate has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="i"></param>
        public void OnEstimateChange(object sender, int i)
        {
            Estimate = i;
        }

        public void OnUpdate(IDBItem item)
        {
            EstimateEdit.Value = task.LikelyDuration;
            NameBox.Text = task.Name;
        }

        public void OnDeleted(IDBItem item)
        {
            task = null;
        }

        // Override default hit test support in visual object.
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            Point pt = hitTestParameters.HitPoint;
            // Return hit on bounding rectangle of visual object.
            return new PointHitTestResult(this, pt);
        }

        public void OnConnect(Anchor sender, IConnectable target, bool isReceiver)
        {
            // todo, add as subtask/parent task
        }

        public void OnDisconnect(Anchor sender, IConnectable target, bool isReceiver)
        {
            // todo, remove as subtask/parent task
        }

        public List<Anchor> GetAnchors()
        {
            return anchors;
        }

        public bool CanConnect(IConnectable target)
        {
            foreach (Anchor anchor in anchors)
                if (anchor.IsConnectedTo(target))
                    return false;
            return true;
        }

        #endregion
    }
}
