using SmartPert.Model;
using SmartPert.View.Controls;
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
    /// Interaction logic for WBS.xaml
    /// Created 3/2/2021 by Robert Nelson
    /// </summary>
    public partial class WBS : Page, IItemObserver
    {
        private Project project;
        private Line connector;
        private WBS_Task startConnector;
        private CtrlHitTest hitTest;


        #region Properties
        public Project Project { get => project;
            set
            {
                project = value;
                LoadProject(project);
            }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public WBS()
        {
            InitializeComponent();
            Project = Model.Model.Instance.GetProject();
            hitTest = new CtrlHitTest(typeof(WBS_Task), Canvas);
        }

        ~WBS()
        {
            if (project != null)
                project.UnSubscribe(this);
        }
        #endregion

        #region Private Methods
        private WBS_Task AddTaskControl(Task t, Point point, WBS_Task parent = null)
        {
            WBS_Task task = new WBS_Task() { Task = t, Canvas = Canvas };
            Canvas.SetLeft(task, point.X);
            Canvas.SetTop(task, point.Y);
            task.Init_Anchors(Canvas);
            return task;
        }

        private void LoadProject(Project project)
        {
            Canvas.Children.Clear();
            Point point = Mouse.GetPosition(Canvas);
            foreach (Task t in project.Tasks)
            { 
                AddTaskControl(t, point);
                point.X += 100;
            }
        }

        //private void Page_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if(isDrawing)
        //    {
        //        Canvas.ReleaseMouseCapture();
        //        // Find the WBS_Task control and attempt to connect to it
        //        WBS_Task found = null;
        //        List<DependencyObject> hits = hitTest.Run(Mouse.GetPosition(Canvas));
        //        foreach (var ctrl in hits)
        //        {
        //            found = (WBS_Task)ctrl;
        //            break;  // top one
        //        }
        //        startConnector.Connect(found, connector);
        //        if (found == null)
        //        {
        //            Console.WriteLine("Failed to find task");
        //            Canvas.Children.Remove(connector);
        //        }
        //        isDrawing = false;
        //    }
        //}

        //private void Page_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if(isDrawing)
        //    {
        //        Point p = Mouse.GetPosition(Canvas);
        //        connector.X2 = p.X;
        //        connector.Y2 = p.Y;
        //    }
        //}
        #endregion


        #region Public Methods
        public void OnUpdate(IDBItem item)
        {
            LoadProject((Project)item);
        }

        public void OnDeleted(IDBItem item)
        {
            project = null;
        }
        #endregion

    }
}
