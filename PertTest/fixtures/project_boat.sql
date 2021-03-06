﻿Declare @username VarChar(3) = 'Bob';
INSERT INTO [dbo].[User] (UserName, [Password], Email, [Name])
VALUES (@username, 'Pass', 'Test@email.com', 'Bob Dillan');

INSERT INTO [dbo].[Project] ([Name], [StartDate], [EndDate], [Description], [CreationDate], [Creator], [MostLikelyEstDuration])
VALUES (N'Boat', N'2021-03-28 00:00:00', N'2021-09-28 00:00:00', N'A project to build a boat', '2021-03-28', @username, 30);

DECLARE @projectId int = (SELECT ProjectId FROM Project WHERE [Name] = 'Boat');
DECLARE @CreationDate DateTime, @Result BIT, @ResultId int, @ChildRowNum int, @ParentId int, @RootId int;
-- Tasks
EXEC [dbo].CreateTask 'Acquire Plans', 'Get example plans needed to build a boat', 1, 2, 3, '3/28/2021', null, @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 0, 0;
set @RootId = (SELECT @ResultId);

EXEC [dbo].CreateTask 'Get Materials', 'Buy the plywood, screws, paint, and adhesive', 2, 7, 14, '3/30/2021', null,  @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 0, 0;
EXEC [dbo].CreateDependency @RootId, @ResultId;
set @RootId = (SELECT @ResultId);

EXEC [dbo].CreateTask 'Build Frame', 'Get the boats frame together', 1, 20, 30, '4/1/2021', null, @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 0, 0;
EXEC [dbo].CreateDependency @RootId, @ResultId;
set @RootId = (SELECT @ResultId);
DECLARE @BuildFrameId int = (SELECT @ResultId);

EXEC [dbo].CreateTask 'Sketch Dimensions', 'sketch dimensions for the bottom, sides, transom, and bow.', 1, 3, 7, '4/1/2021', null, @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 1, @BuildFrameId;
set @RootId = (SELECT @ResultId);

EXEC [dbo].CreateTask 'Cut Plywood', 'Cut the plywood according to dimensions', 1, 3, 7, '4/7/2021', null, @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 1, @BuildFrameId;
EXEC [dbo].CreateDependency @RootId, @ResultId;
set @RootId = (SELECT @ResultId);

EXEC [dbo].CreateTask 'Secure with Screws', 'Secure all sides with screws', 1, 2, 7, '4/14/2021', null, @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 1, @BuildFrameId;
EXEC [dbo].CreateDependency @RootId, @ResultId;
set @RootId = (SELECT @ResultId);

EXEC [dbo].CreateTask 'Seal with Glue', 'Seal all joints with adhesive', 1, 14, 21, '4/21/2021', null, @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 0, 0;
EXEC [dbo].CreateDependency @RootId, @ResultId;

EXEC [dbo].CreateTask 'Paint', 'Paint the boat with waterproof paint so that it will withstand the elements', 1, 7, 14, '4/15/2021', null, @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 0, 0;
EXEC [dbo].CreateDependency @BuildFrameId, @ResultId;
set @RootId = (SELECT @ResultId);

EXEC [dbo].CreateTask 'Water Test', 'Test if the boat will hold water', 1, 1, 2, '5/1/2021', Null,  @projectId, @username, @CreationDate, @Result, @ResultId out, @ChildRowNum, 0, 0;
EXEC [dbo].CreateDependency @RootId, @ResultId;
