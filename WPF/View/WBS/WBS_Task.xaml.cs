using SmartPert.Command;
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
    public partial class WBS_Task : Connectable, IItemObserver
    {
        private Task task;
        protected bool isDragging;
        private Point clickPosition;
        private Point origin;   // The origin of tranformation

        #region Properties
        /// <summary>
        /// Task this refers to
        /// </summary>
        public Task Task
        {
            get
            {
                if (task == null)
                    Task = Task.GetUniqueTask();
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
                new EditTaskCmd(Task, value, Task.StartDate, Task.EndDate, Task.LikelyDuration, Task.MaxDuration, Task.MinDuration, Task.Description).Run();
            }
        }

        /// <summary>
        /// Likely duration estimate
        /// </summary>
        public int Estimate { get => Task.LikelyDuration; set => Task.LikelyDuration = value; }

        /// <summary>
        /// If true, the canvas will automatically resize in the onMove event (doesn't work)
        /// </summary>
        protected bool ResizableCanvas
        {
            get; set;
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
            this.MouseLeftButtonDown += new MouseButtonEventHandler(Control_MouseLeftButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(Control_MouseLeftButtonUp);
            this.MouseMove += new MouseEventHandler(Control_MouseMove);
            Init_Anchors();
            ResizableCanvas = true;
        }

        private void Init_Anchors()
        {
            AddAnchor(RightConnector);
            AddAnchor(LeftConnector);
            AddAnchor(BottomConnector);
            AddAnchor(TopConnector);
            AnchorsVisible = false;
        }

        ~WBS_Task()
        {
            if (task != null)
                task.UnSubscribe(this);
        }
        #endregion

        #region Private Methods

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!TopConnector.IsMouseOver && !LeftConnector.IsMouseOver && !RightConnector.IsMouseOver && !BottomConnector.IsMouseOver)
            {
                isDragging = true;
                var draggableControl = sender as UserControl;
                clickPosition = e.GetPosition(this.Parent as UIElement);
                origin.X = Canvas.GetLeft(this);
                origin.Y = Canvas.GetTop(this);
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
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as UserControl;

            if (isDragging && draggableControl != null)
            {
                Point currentPosition = e.GetPosition(this.Parent as UIElement);

                double x = currentPosition.X - clickPosition.X + origin.X;
                double y = currentPosition.Y - clickPosition.Y + origin.Y;
                Canvas.SetLeft(this, x);
                Canvas.SetTop(this, y);
                OnMove(x, y);
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnMove(double x, double y)
        {
            base.OnMove(x, y);
            bool needsResize = false;
            // Handle too far right
            if (x + ActualWidth > Canvas.ActualWidth)
                if (!ResizableCanvas)
                    Canvas.SetLeft(this, Canvas.ActualWidth - Width);
                else
                {
                    needsResize = true;
                    Canvas.Width = x + ActualWidth * 2;
                }
            //handle too far left
            if (x < 0)
                Canvas.SetLeft(this, 0);
            //handle too far up
            if (y < 0)
                Canvas.SetTop(this, 0);
            //handle too far down
            if (y + ActualHeight > Canvas.ActualHeight)
                if (!ResizableCanvas)
                    Canvas.SetTop(this, Canvas.ActualHeight - ActualHeight);
                else
                {
                    Canvas.Height = y + ActualHeight * 2;
                    needsResize = true;
                }
            if(needsResize)
            {
                GrowCanvas(Canvas, Canvas.Width, Canvas.Height);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Grows the canvas to fit width and height, Does not shrink if it's smaller than actual size!
        /// </summary>
        /// <param name="canvas">canvas</param>
        /// <param name="width">width to adjust to</param>
        /// <param name="height">height to adjust to</param>
        public static void GrowCanvas(Canvas canvas, double width, double height)
        {
            width = Double.IsNaN(width) || canvas.ActualWidth > width ? canvas.ActualWidth : width;
            height = Double.IsNaN(height) || canvas.ActualHeight > height ? canvas.ActualHeight : height;
            canvas.Measure(new Size(width, height));
            canvas.Arrange(new Rect(new Point(0, 0), canvas.DesiredSize));
        }

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

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            Point pt = hitTestParameters.HitPoint;
            // Return hit on bounding rectangle of visual object.
            return new PointHitTestResult(this, pt);
        }

        public override void OnConnect(Anchor sender, Connectable target, bool isReceiver)
        {
            // todo, add as subtask/parent task
        }

        public override void OnDisconnect(Anchor sender, Connectable target, bool isReceiver)
        {
            // todo, remove as subtask/parent task
        }
        #endregion
    }
}
