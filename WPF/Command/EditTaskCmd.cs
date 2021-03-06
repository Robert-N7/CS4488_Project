﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartPert.Model;

namespace SmartPert.Command
{
    /// <summary>
    /// Command to edit task
    /// Created 2/2/2021 by Robert Nelson
    /// </summary>
    public class EditTaskCmd : ICmd
    {
        private Task toEdit;
        private readonly string name;
        private readonly DateTime start;
        private readonly DateTime? end;
        private readonly int likelyDuration;
        private readonly int maxDuration;
        private readonly int minDuration;
        private readonly string description;
        private readonly Task oldTask;

        public EditTaskCmd(Task toEdit, string name, DateTime start, DateTime? end, int likelyDuration, int maxDuration, int minDuration, string description)
        {
            this.toEdit = toEdit;
            this.name = name;
            this.start = start;
            this.end = end;
            this.likelyDuration = likelyDuration;
            this.maxDuration = maxDuration;
            this.minDuration = minDuration;
            this.description = description;
            oldTask = new Task(toEdit.Name, toEdit.StartDate, toEdit.EndDate, toEdit.LikelyDuration, toEdit.MaxDuration,
                toEdit.MinDuration, toEdit.Description, toEdit.Project, toEdit.Id, false, false);
        }

        protected override bool Execute()
        {
            toEdit.Name = name;
            toEdit.StartDate = start;
            toEdit.EndDate = end;
            toEdit.MaxDuration = maxDuration;
            toEdit.LikelyDuration = likelyDuration;
            toEdit.MinDuration = minDuration;
            toEdit.Description = description;
            return true;
        }

        public override bool Undo()
        {
            toEdit.Name = oldTask.Name;
            toEdit.StartDate = oldTask.StartDate;
            toEdit.EndDate = oldTask.EndDate;
            toEdit.LikelyDuration = oldTask.LikelyDuration;
            toEdit.MaxDuration = oldTask.MaxDuration;
            toEdit.MinDuration = oldTask.MinDuration;
            toEdit.Description = oldTask.Description;
            return true;
        }

        public override void OnIdUpdate(TimedItem old, TimedItem newItem)
        {
            if(old == toEdit)
                toEdit = (Task) newItem;

        }

        public override void OnModelUpdate(Project p)
        {
            UpdateTask(ref toEdit);
        }
    }
}
