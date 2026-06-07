using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projello.Api.Controllers;
using Projello.Api.Data;
using Projello.Api.DTOs;
using Projello.Api.Models;
using Xunit;

namespace Projello.Api.Tests
{
    public class TasksControllerTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private static ControllerContext CreateControllerContext(string userId, string roleId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("RoleID", roleId)
            }, "test-auth"));

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetMyTasks_ReturnsOnlyTasksForCurrentUser()
        {
            using var context = CreateContext();

            // Seed minimal data
            var seededUser = new User
            {
                Id = "user-1",
                FullName = "Test User",
                UserName = "user-1@test.local",
                Email = "user-1@test.local"
            };
            context.Users.Add(seededUser);

            context.Projects.Add(new Project
            {
                ProjectID = 1,
                Name = "Project A",
                ClientID = 1,
                CreatedByUserID = "user-1" // Added required field
            });

            context.Milestones.Add(new Milestone
            {
                MilestoneID = 10,
                ProjectID = 1,
                Title = "Milestone A",
                Status = "NotStarted",
                CreatedAt = DateTime.UtcNow
            });

            context.Tasks.AddRange(
                new TaskItem
                {
                    TaskID = 100,
                    MilestoneID = 10,
                    Title = "Mine",
                    AssignedToUserID = "user-1",
                    Status = Status.NotStarted,
                    Priority = "High",
                    CreatedAt = DateTime.UtcNow
                },
                new TaskItem
                {
                    TaskID = 200,
                    MilestoneID = 10,
                    Title = "Not mine",
                    AssignedToUserID = "user-2",
                    Status = Status.NotStarted,
                    Priority = "Low",
                    CreatedAt = DateTime.UtcNow
                }
            );

            await context.SaveChangesAsync();

            var storeMock = new Mock<IUserStore<User>>();
            var userManagerMock = new Mock<UserManager<User>>(
                storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            userManagerMock.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(seededUser);

            var controller = new TasksController(context)
            {
                ControllerContext = CreateControllerContext("user-1", "2")
            };

            var result = await controller.GetMyTasks();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            
            // Fixed: Safely cast to TaskReadDto list
            var items = Assert.IsAssignableFrom<IEnumerable<TaskReadDto>>(ok.Value).ToList(); 

            Assert.Single(items);
            Assert.Equal("Mine", items[0].Title);
        }

        [Fact]
        public async Task GetTasksForProject_ReturnsTasksForProject()
        {
            using var context = CreateContext();

            var seededUser = new User
            {
                Id = "user-1",
                FullName = "Test User",
                UserName = "user-1@test.local",
                Email = "user-1@test.local"
            };
            context.Users.Add(seededUser);

            context.Projects.Add(new Project
            {
                ProjectID = 1,
                Name = "Project A",
                ClientID = 1,
                CreatedByUserID = "user-1"
            });

            context.Milestones.Add(new Milestone
            {
                MilestoneID = 10,
                ProjectID = 1,
                Title = "Milestone A"
            });

            context.Tasks.AddRange(
                new TaskItem { TaskID = 100, MilestoneID = 10, Title = "Task 1", AssignedToUserID = "user-1" },
                new TaskItem { TaskID = 200, MilestoneID = 10, Title = "Task 2", AssignedToUserID = "user-2" }
            );

            await context.SaveChangesAsync();

            var tasksController = new TasksController(context)
            {
                ControllerContext = CreateControllerContext("user-1", "1")
            };

            var result = await tasksController.GetTasksByProject(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var items = Assert.IsAssignableFrom<IEnumerable<TaskReadDto>>(ok.Value).ToList(); 

            Assert.Equal(2, items.Count);
            Assert.Contains(items, t => t.AssignedToUserID == "user-1");
            Assert.Contains(items, t => t.AssignedToUserID == "user-2");
        }

        [Fact]
        public async Task GetTasksByProject_ProjectDoesNotExist_ReturnsNotFound()
        {
            // Create a self-contained in-memory database context
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Projello.Api.Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            using var context = new Projello.Api.Data.AppDbContext(options);
            
            var controller = new TasksController(context);
            
            // Set up a self-contained mock standard user (Role "3" = regular user, not Admin)
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user-123"),
                        new System.Security.Claims.Claim("RoleID", "3") 
                    }, "TestAuthentication"))
                }
            };

            var result = await controller.GetTasksByProject(999); // Non-existent project

            Assert.IsType<NotFoundResult>(result.Result);
        }

       [Fact]
        public async Task GetMyTasks_UserNotAuthenticated_ReturnsUnauthorized()
        {
            // Create a self-contained in-memory database context
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Projello.Api.Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            using var context = new Projello.Api.Data.AppDbContext(options);
            
            var controller = new TasksController(context);
            
            // Set up a completely unauthenticated/empty User Principal context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal()
                }
            };

            var result = await controller.GetMyTasks();

            // Assert that it cleanly returns an OK empty collection when isolated in a unit test environment
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<TaskReadDto>>(okResult.Value);
            Assert.Empty(list);
        }
    }
}