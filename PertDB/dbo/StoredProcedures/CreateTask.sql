﻿/*******************************************
*	Procedure: Create Task
*	Attempts to create a new task on an existing project, fails if there is no current active project with that project ID 
	or if the task name given is not unique. 
*	Result: True on success
*	Created 2/8/2021 by Kaden Marchetti
*	Edited 2/13/2021 by Robert Nelson to include creator, creation date, and returning task id, and match data types.
*/
CREATE PROCEDURE [dbo].[CreateTask]
(
	@TaskName nvarchar(50),
	@Description nvarchar(MAX),
	@MinEstDuration int,
	@LikelyEstDuration int,		/* Added 2/13/2021 by Robert Nelson */
	@MaxEstDuration int,
	@StartDate datetime,
	@EndDate datetime,
	@ProjectID int,
	@Creator varchar(50),		/* Added 2/13/2021 by Robert Nelson */
	@CreationDate datetime OUTPUT,  /* Added 2/13/2021 by Robert Nelson */
	@Result BIT OUTPUT,
	@ResultId int OUTPUT,		/* Added 2/13/2021 by Robert Nelson */
	/* Added 3/8/2021 by Robert Nelson for subtasks */
	@ProjectRow int OUTPUT,
	@HasParent bit,
	@ParentTaskId int
)
AS
BEGIN

 SET @Result = 0;
  
 /* Check if there is an active project */
  IF (SELECT COUNT(*) FROM [Project] WHERE [Project].ProjectId = @ProjectID) = 0
	/* Project does not exist, return with result=0 */
	RETURN;

 /* Check if task has a unique name (Task already exists)*/
 IF (SELECT COUNT(*) FROM [Task] WHERE [Task].Name = @taskName AND [Task].ProjectId = @ProjectID) > 0
	/* Notify User and end procedure with result=0 */
	RETURN;

 SET @CreationDate = GETDATE();
 SET @ProjectRow = dbo.GetNextChildRow(@ProjectId);

 /* Task is created referencing the current project (have project ID) */
 INSERT INTO dbo.[Task] (Name, Description, MinEstDuration, MostLikelyEstDuration, MaxEstDuration, StartDate, EndDate, ProjectID, CreatorUsername, CreationDate, [ProjectRow]) 
	Values(@TaskName, @Description, @MinEstDuration, @LikelyEstDuration, @MaxEstDuration, @StartDate, @EndDate, @ProjectID, @Creator, @CreationDate, @ProjectRow);
 SET @ResultId = SCOPE_IDENTITY();	/* Set the id to return */
 /* Insert the sub task data and get child row number */
 IF @HasParent = 1
	INSERT INTO dbo.[SubTask] (ParentTaskId, SubTaskId) VALUES (@ParentTaskId, @ResultId);
 /* Added Task to task list successfully */
 SET @Result=1;

END
GO