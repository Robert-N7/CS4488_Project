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
        private Point maxPoint;

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
            maxPoint = new Point(0, 0);
            Project = Model.Model.Instance.GetProject();
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
            if (point.X > maxPoint.X)
                maxPoint.X = point.X;
            if (point.Y > maxPoint.Y)
                maxPoint.Y = point.Y;
            return task;
        }

        private void LoadProject(Project project)
        {
            Canvas.Children.Clear();
            Point point = Mouse.GetPosition(Canvas);
            foreach (Task t in project.Tasks)
            { 
                AddTaskControl(t, point);
                point.X += 150;
            }
        }

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
