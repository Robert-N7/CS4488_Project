﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SmartPert.Model
{
    /// <summary>
    /// A Task is a single item on the Pert chart that has a most likely duration, minimum duration, and maximum duration.
    /// Dependencies are Tasks that cannot be started until this one is finished.
    /// Created 1/29/2021 by Robert Nelson
    /// </summary>
    public class Task : TimedItem
    {
        private Project project;
        private HashSet<Task> dependencies;
        private Task parentTask;
        private int projectRow;    // The nth child of a parent

        #region Properties
        /// <summary>
        /// Gets the project the task is on
        /// Added 2/13/2021 by Robert Nelson
        /// </summary>
        public Project Project
        {
            get => project;
            private set
            {
                project = value;
                project.AddTask(this);
            }
        }
        public HashSet<Task> Dependencies { get => dependencies; }
        public Task ParentTask { get => parentTask; }

        public int ProjectRow { get => projectRow; 
            private set
            {
                projectRow = value;
                DB_UpdateRow();
                NotifyUpdate();
            } }
        #endregion

        #region Constructor
        /// <summary>
        /// Task constructor
        /// </summary>
        /// <param name="name">Task name (must be unique on project)</param>
        /// <param name="start">Start Date</param>
        /// <param name="end">EndDate (optional)</param>
        /// <param name="duration">Likely estimated duration</param>
        /// <param name="maxDuration">Maximum estimated duration (Optional)</param>
        /// <param name="minDuration">Minimum estimated duration (Optional)</param>
        /// <param name="description">Task Description (Optional)</param>
        /// <param name="project">Project associated</param>
        /// <param name="id">Task id</param>
        /// <param name="insert">flag to insert it into database</param>
        /// <param name="track">flag to track item</param>
        /// <param name="observer">observer for updates</param>
        public Task(string name, DateTime start, DateTime? end, int duration, int maxDuration = 0, int minDuration = 0, 
            string description = "", Project project=null, int id = -1, bool insert=true, bool track=true, IItemObserver observer=null) 
            : base(name, start, end, description, id, observer, duration, maxDuration, minDuration)
        {
            this.Project = project != null ? project : Model.Instance.GetProject();
            if (this.project == null)
                throw new ArgumentNullException("project");
            dependencies = new HashSet<Task>();
            projectRow = 0;
            PostInit(insert, track);
        }

        #endregion

        #region Task Methods
        /// <summary>
        /// Gets the first task after the task group, this being the parent task. 
        /// If there are none after returns null.
        /// </summary>
        /// <returns>The task after the task group ends</returns>
        public Task GetTaskAfterGroup()
        {
            List<Task> tasks = Project.SortedTasks;
            int i;
            for (i = tasks.IndexOf(this) + 1; i < tasks.Count; i++)
                if (!tasks[i].IsDescendentOf(this))
                    break;
            return i >= tasks.Count ? null : tasks[i];
        }

        // Determine if a task is a descendent of this
        private bool IsDescendentOf(Task t)
        {
            if (parentTask == t)
                return true;
            if (parentTask == null)
                return false;
            return parentTask.IsDescendentOf(t);
        }

        // Shifts the task range to the bottom of project
        private void ShiftToBottom(List<Task> tasks, int start, int end)
        {
            int maxRow = tasks.Max(x => x.ProjectRow);
            for (; start < tasks.Count && start < end; ++start)
                tasks[start].ProjectRow = ++maxRow;
        }

        private List<int> CacheProjectRows(List<Task> tasks, int start, int end)
        {
            List<int> cache = new List<int>();
            int max = end >= tasks.Count ? tasks[end - 1].projectRow + 1 : tasks[end].projectRow;
            for (int i = tasks[start].projectRow; i < max; i++)
                cache.Add(i);
            return cache;
        }

        /// <summary>
        /// Attempts to shift by the number of task rows
        /// </summary>
        /// <param name="numRows">Number of rows, specify negative to shift up</param>
        /// <returns>true if shift occurred</returns>
        public bool TryShiftRows(int numRows)
        {
            List<Task> tasks = Project.SortedTasks;
            int index = tasks.IndexOf(this) + numRows;
            if (index >= tasks.Count)    // Shift to end
                return TryShiftToRow(null);
            else
                return TryShiftToRow(index < 0 ? tasks[0] : tasks[index]);
        }

        /// <summary>
        /// A long *ss function that Shifts the task to the row in the project
        /// </summary>
        /// <param name="task">the target task row</param>
        /// <returns>True if successful</returns>
        public bool TryShiftToRow(Task task)
        {
            if (task == this)      // We're already done!
                return true;
            List<Task> tasks = Project.SortedTasks;
            int row = task == null ? tasks.Max(x => x.ProjectRow) + 1 : task.ProjectRow;
            // Constraint Cannot shift to row within task group (ie making a parent task a child to one of its subtasks)
            Task endGroupTask = GetTaskAfterGroup();
            if (row >= this.projectRow && (endGroupTask == null || row < endGroupTask.projectRow))
                return false;
            int projectRow = this.projectRow;
            Task taskAbove = null;
            int startGroup = tasks.IndexOf(this), i, j;
            int endGroup = endGroupTask == null ? tasks.Count : tasks.IndexOf(endGroupTask);
            List<int> availableRows = CacheProjectRows(tasks, startGroup, endGroup);
            // First move my range indices out of the way
            ShiftToBottom(tasks, startGroup, endGroup);

            // Perform the shift
            // Target row is further down, so shift up
            if (row > projectRow)
            {
                for (i = endGroup, j = 0; i < tasks.Count && tasks[i].projectRow <= row; i++)
                {
                    availableRows.Add(tasks[i].ProjectRow);
                    tasks[i].ProjectRow=availableRows[j++];
                }
                taskAbove = tasks[i - 1]; // last shifted becomes task directly above
                // And finally update my group
                for (i = startGroup; i < endGroup; i++)
                    tasks[i].ProjectRow = availableRows[j++];
            }
            else // Target row is up, so shift others down 
            {
                for (i = startGroup - 1, j = availableRows.Count; i >= 0 && tasks[i].projectRow >= row; i--)
                {
                    availableRows.Insert(0, tasks[i].ProjectRow);
                    tasks[i].ProjectRow = availableRows[j];      // Since we insert 1 no need to move j down
                }
                if (i >= 0)
                   taskAbove = tasks[i];   // The task just above shifted tasks is directly above
                // And finally update my group
                for (i = endGroup - 1; i >= startGroup; i--)
                    tasks[i].ProjectRow = availableRows[--j];
            }


            // But wait! what if the task above us is a parent? Add this as child
            if (taskAbove != null)
            {
                if (taskAbove.Tasks.Count > 0)
                    UpdateParentTask(taskAbove);
                // Great, but what if we're in the middle of a subtask group? Find the closest parent
                else
                    UpdateParentTask(taskAbove.parentTask);
            }
            else  // TaskAbove == null, you're at the root!
                UpdateParentTask(null);
            return true;
        }

        /// <summary>
        /// Updates a tasks parent by removing it from its current parent and adding it as subtask to the new one
        /// </summary>
        /// <param name="newParent">the new task parent</param>
        /// <returns>true if anything was updated</returns>
        public bool UpdateParentTask(Task newParent)
        {
            bool result = false;
            if(newParent != parentTask)
            {
                if (parentTask != null)
                    result |= parentTask.RemoveSubTask(this);
                if (newParent != null)
                    result |= newParent.AddSubTask(this);
            }
            return result;
        }

        /// <summary>
        /// Adds sub task to tasks
        /// </summary>
        /// <param name="t">Task</param>
        public bool AddSubTask(Task t)
        {
            if (t.parentTask != this)
            {
                if (t.parentTask != null)
                    t.parentTask.RemoveSubTask(t);
                if (t.TryShiftToRow(GetTaskAfterGroup()))
                {
                    t.parentTask = this;
                    tasks.Add(t);
                    t.Subscribe(this);
                    t.InsertSubTask();
                    NotifyUpdate();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes sub task from tasks
        /// </summary>
        /// <param name="t">task to remove</param>
        public bool RemoveSubTask(Task t)
        {
            if (tasks.Remove(t))
            {
                t.UnSubscribe(this);
                t.parentTask = null;
                t.DeleteSubTask();
                NotifyUpdate();
                return true;
            }
            return false;
        }
        #endregion


        #region Workers
        /// <summary>
        /// Adds worker to task
        /// </summary>
        /// <param name="worker">user</param>
        /// <returns>true if it was added</returns>
        public override bool AddWorker(User worker)
        {
            bool added = false;
            if (!workers.Contains(worker))
            {
                // Attempt to add...
                try
                {
                    Project.AddWorker(worker);  // Add to project too
                    SqlCommand command = OpenConnection("INSERT INTO dbo.UserTask (UserName, TaskId) Values(@username, @taskId);");
                    command.Parameters.AddWithValue("@username", worker.Username);
                    command.Parameters.AddWithValue("@taskId", this.Id);
                    command.ExecuteNonQuery();
                    CloseConnection();
                    workers.Add(worker);
                    added = true;
                    NotifyUpdate();
                }
                catch (SqlException) { }
            }
            return added;
        }

        /// <summary>
        /// Removes the worker from the task
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        public override bool RemoveWorker(User worker)
        {
            bool removed = false;
            if (workers.Contains(worker))
            {
                try
                {
                    SqlCommand command = OpenConnection("DELETE FROM UserTask WHERE UserTask.TaskId=@taskId AND UserTask.UserName=@username");
                    command.Parameters.AddWithValue("@taskId", this.Id);
                    command.Parameters.AddWithValue("@username", worker.Username);
                    command.ExecuteNonQuery();
                    CloseConnection();
                    workers.Remove(worker);
                    removed = true;
                    NotifyUpdate();
                } catch(SqlException) { }
            }
            return removed;
        }
        #endregion

        #region Dependencies
        public void UpdateDependencies()
        {

        }

        public void AddDependency(Task dependency)
        {

        }

        public void RemoveDependency(Task dependency)
        {

        }
        #endregion

        #region Database Methods
        private void DB_UpdateRow()
        {
            string query = "UPDATE dbo.[Task] SET ProjectRow=" + projectRow + " WHERE TaskId=" + Id + ";";
            ExecuteSql(query);
        }

        /// <summary>
        /// Updates the task data in the database
        /// </summary>
        protected override void PerformUpdate()
        {
            string query = "UPDATE dbo.[Task] SET Name=@Name, StartDate=@StartDate, EndDate=@EndDate" + 
                ", Description=@Description, MinEstDuration=" + MinDuration + ", MaxEstDuration=" + MaxDuration + ", MostLikelyEstDuration=" +
                LikelyDuration + ", ProjectRow=" + projectRow + " WHERE TaskId = " + Id + ";";                
            SqlCommand command = OpenConnection(query);
            command.Parameters.AddWithValue("@Name", Name);
            command.Parameters.AddWithValue("@Description", Description);
            var sd = command.Parameters.Add("@StartDate", System.Data.SqlDbType.DateTime);
            sd.Value = StartDate;
            var ed = command.Parameters.Add("@EndDate", System.Data.SqlDbType.DateTime);
            if (EndDate != null)
                ed.Value = EndDate;
            else
                ed.Value = DBNull.Value;
            command.ExecuteNonQuery();
            CloseConnection();
        }

        /// <summary>
        /// Inserts a task into the database
        /// 2/13/2021 by Robert Nelson
        /// </summary>
        /// <returns>Task id</returns>
        /// <throws>InsertionError on error (task name is already taken in project or project does not exist)</throws>
        protected override int PerformInsert()
        {
            string query = "EXEC dbo.CreateTask @Name, @Description, " + MinDuration + ", " + LikelyDuration + ", " + MaxDuration + 
                ", @StartDate, @EndDate, @ProjectId, @Creator, @CreationDate out, @Result out, @ResultId out, " +
                "@ProjectRow out, @HasParent, @ParentTaskId";
            SqlCommand command = OpenConnection(query);
            command.Parameters.AddWithValue("@Name", Name);
            command.Parameters.AddWithValue("@Description", Description);
            command.Parameters.AddWithValue("@ProjectId", Project.Id);
            if (creator == null)
                command.Parameters.AddWithValue("@Creator", DBNull.Value);
            else
                command.Parameters.AddWithValue("@Creator", creator.Username);
            var sd = command.Parameters.Add("@StartDate", System.Data.SqlDbType.DateTime);
            sd.Value = StartDate;
            var ed = command.Parameters.Add("@EndDate", System.Data.SqlDbType.DateTime);
            if (EndDate != null)
                ed.Value = EndDate;
            else
                ed.Value = DBNull.Value;
            var createDate = command.Parameters.Add("@CreationDate", System.Data.SqlDbType.DateTime);
            createDate.Direction = System.Data.ParameterDirection.Output;
            var result = command.Parameters.Add("@Result", System.Data.SqlDbType.Bit);
            result.Direction = System.Data.ParameterDirection.Output;
            var resultId = command.Parameters.Add("@ResultId", System.Data.SqlDbType.Int);
            resultId.Direction = System.Data.ParameterDirection.Output;
            var childRow = command.Parameters.Add("@ProjectRow", System.Data.SqlDbType.Int);
            childRow.Direction = System.Data.ParameterDirection.Output;
            if(parentTask != null)
            {
                command.Parameters.AddWithValue("@HasParent", 1);
                command.Parameters.AddWithValue("@ParentTaskId", parentTask.Id);
            } else
            {
                command.Parameters.AddWithValue("@HasParent", 0);
                command.Parameters.AddWithValue("@ParentTaskId", 0);
            }
            command.ExecuteNonQuery();
            if (!(bool)result.Value)
                throw new InsertionError("Failed to insert task " + Name);
            creationDate = (DateTime) createDate.Value;
            id = (int) resultId.Value;
            projectRow = (int)childRow.Value;
            CloseConnection();

            // If the parent task exists, be sure to move it next to it
            if(parentTask != null)
                TryShiftToRow(parentTask.GetTaskAfterGroup());
            return id;
        }

        private void InsertSubTask()
        {
            if (parentTask != null)
            {
                SqlCommand command = OpenConnection("INSERT INTO [dbo].[SubTask] (SubTaskId, ParentTaskId) Values (@SubId, @ParentId);");
                command.Parameters.AddWithValue("@SubId", Id);
                command.Parameters.AddWithValue("@ParentId", parentTask.Id);
                command.ExecuteNonQuery();
                CloseConnection();
            }
        }

        private void DeleteSubTask()
        {
            ExecuteSql("DELETE FROM [dbo].[SubTask] WHERE SubTaskId=" + Id);
        }

        /// <summary>
        /// Deletes a task
        /// </summary>
        protected override void PerformDelete()
        {
            string query = "EXEC dbo.TaskDelete " + Id + ";";
            ExecuteSql(query);
            if(parentTask != null)
                parentTask.RemoveSubTask(this);
            project.RemoveTask(this);
        }

        /// <summary>
        /// Parses the task from the sqldatareader
        /// </summary>
        /// <param name="reader">open SqlDataReader</param>
        /// <param name="users">list of all users</param>
        /// <param name="projects">projects</param>
        /// <returns>Task</returns>
        static public Task Parse(SqlDataReader reader, Dictionary<string, User> users, Dictionary<int, Project> projects)
        {
            string creator = DBFunctions.StringCast(reader, "CreatorUsername");
            Project proj = projects[(int)reader["ProjectId"]];
            User user = users != null && creator != "" ? users[creator] : null;
            int mostLikely = (int)reader["MostLikelyEstDuration"];
            Task t = new Task(
                (string)reader["Name"],
                (DateTime)reader["StartDate"],
                DBFunctions.DateCast(reader, "EndDate"),
                mostLikely,
                DBFunctions.IntCast(reader, "MaxEstDuration", mostLikely),
                DBFunctions.IntCast(reader, "MinEstDuration", mostLikely),
                DBFunctions.StringCast(reader, "Description"),
                project: proj,
                id: (int)reader["TaskId"],
                insert: false);

            t.creator = user;
            t.creationDate = (DateTime)DBFunctions.DateCast(reader, "CreationDate");
            t.projectRow = (int)reader["ProjectRow"];
            proj.AddTask(t);
            return t;
        }

        public override bool PerformParse(SqlDataReader reader)
        {
            bool result = base.PerformParse(reader);
            int projectRow = (int)reader["ProjectRow"];
            if(projectRow != this.projectRow)
            {
                this.projectRow = projectRow;
                this.isUpdated = true;
                return true;
            }
            return result;
        }

        public static List<Task> Set_Diff(HashSet<Task> set, IEnumerable<Task> other)
        {
            List<Task> ret = new List<Task>();
            foreach (Task t in set)
                if (!other.Contains(t))
                    ret.Add(t);
            return ret;
        } 

        /// <summary>
        /// Updates Subtasks (dbreader only)
        /// </summary>
        /// <param name="subtasks">updated</param>
        public void DB_UpdateSubtasks(HashSet<Task> subtasks)
        {
            // If tasks are gone, remove them 
            if(subtasks == null)
            {
                foreach (Task t in Tasks)
                {
                    t.parentTask = null;
                    tasks.Remove(t);
                    t.UnSubscribe(this);
                }
                isUpdated = true;
            } else if(!tasks.SetEquals(subtasks)) // If the subtasks have changed, update the ones that have changed
            {
                foreach (Task t in Set_Diff(subtasks, tasks))
                {
                    t.parentTask = this;
                    tasks.Add(t);
                    t.Subscribe(this);
                }
                foreach (Task t in Set_Diff(tasks, subtasks))
                {
                    t.parentTask = null;
                    tasks.Remove(t);
                    t.UnSubscribe(this);
                }
                isUpdated = true;
            }
        }

        /// <summary>
        /// Updates dependencies (dbreader only)
        /// </summary>
        /// <param name="updatedDependencies">updated</param>
        public void DB_UpdateDependencies(HashSet<Task> updatedDependencies)
        {
            if(updatedDependencies == null)
            {
                dependencies.Clear();
                isUpdated = true;
            }
            else if(!dependencies.SetEquals(updatedDependencies))
            {
                dependencies = updatedDependencies;
                isUpdated = true;
            }
        }

        #endregion

        /// <summary>
        /// Calculates the last date for the task if none exists
        /// Implemented by: Makayla Linnastruth and Robert Nelson
        /// </summary>
        /// <returns></returns>
        public DateTime CalculateLastTaskDate()
        {
            DateTime maxEstimate = MaxEstDate;
            return EndDate == null || maxEstimate > EndDate ? maxEstimate : (DateTime)EndDate;
        }
    }
}
