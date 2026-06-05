using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projello.Api.Controllers;
using Projello.Api.Data;
using Projello.Api.DTOs;
using Projello.Api.Models;
using System.Security.Claims;
using Xunit;

namespace Projello.Api.Tests.Controllers
{
    public class SiteUpdatesControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SiteUpdatesController _controller;
        private readonly Cloudinary _dummyCloudinary;

        public SiteUpdatesControllerTests()
        {
            // 1. Setup In-Memory Database (Unique name per instance ensures test isolation)
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 2. Setup a Dummy Cloudinary Instance
            // We pass dummy credentials so it instantiates successfully without throwing.
            var account = new Account("dummy_cloud", "dummy_key", "dummy_secret");
            _dummyCloudinary = new Cloudinary(account);

            // 3. Instantiate the Controller
            _controller = new SiteUpdatesController(_context, _dummyCloudinary);
        }

        // Helper to mock the logged-in user
        private void SetCurrentUser(string userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetUpdates_ReturnsMappedUpdatesWithReactionsAndComments()
        {
            // Arrange
            var projectId = 1;
            var userId = "user-123";

            // Seed User
            _context.Users.Add(new User { Id = userId, FullName = "John Doe" });

            // Seed Update
            var update = new ProjectUpdate
            {
                Id = 10,
                ProjectId = projectId,
                UserId = userId,
                Caption = "Site poured today",
                CreatedAt = DateTime.UtcNow
            };
            _context.ProjectUpdates.Add(update);

            // Seed Reaction & Comment
            _context.UpdateReactions.Add(new UpdateReaction { Id = 1, UpdateId = 10, UserId = userId, Emoji = "👍" });
            _context.UpdateComments.Add(new UpdateComment { Id = 1, UpdateId = 10, UserId = userId, CommentText = "Looks good!" });

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUpdates(projectId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsAssignableFrom<IEnumerable<ProjectDiscussionPostDto>>(okResult.Value);
            
            var posts = payload.ToList();
            Assert.Single(posts);
            
            var post = posts.First();
            Assert.Equal("John Doe", post.UserFullName);
            Assert.Equal("Site poured today", post.Caption);
            
            Assert.Single(post.Reactions);
            Assert.Equal("👍", post.Reactions.First().Emoji);
            Assert.Equal("John Doe", post.Reactions.First().UserFullName);

            Assert.Single(post.Comments);
            Assert.Equal("Looks good!", post.Comments.First().CommentText);
        }

        [Fact]
        public async Task CreateUpdate_WithoutImage_CreatesAndReturnsUpdate()
        {
            // Arrange
            var userId = "user-123";
            SetCurrentUser(userId);

            var dto = new CreateUpdateDto
            {
                Caption = "Progress check",
                Image = null // Testing null image to avoid triggering Cloudinary API call
            };

            // Act
            var result = await _controller.CreateUpdate(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdUpdate = Assert.IsType<ProjectUpdate>(okResult.Value);

            Assert.Equal("Progress check", createdUpdate.Caption);
            Assert.Equal(string.Empty, createdUpdate.ImageUrl);
            Assert.Equal(userId, createdUpdate.UserId);
            Assert.Equal(1, _context.ProjectUpdates.Count());
        }

        [Fact]
        public async Task React_TogglesReaction_AddsThenRemoves()
        {
            // Arrange
            var userId = "user-123";
            SetCurrentUser(userId);
            var updateId = 10;
            var emoji = "🔥";

            var dto = new ReactDto { Emoji = emoji };

            // Act 1: Add Reaction
            var addResult = await _controller.React(updateId, dto);
            
            // Assert 1: Check it was added
            var addOkResult = Assert.IsType<OkObjectResult>(addResult);
            var addResponse = addOkResult.Value?.GetType().GetProperty("removed")?.GetValue(addOkResult.Value, null);
            Assert.Equal(false, addResponse);
            Assert.Single(_context.UpdateReactions);

            // Act 2: Toggle Reaction (Remove)
            var removeResult = await _controller.React(updateId, dto);

            // Assert 2: Check it was removed
            var removeOkResult = Assert.IsType<OkObjectResult>(removeResult);
            var removeResponse = removeOkResult.Value?.GetType().GetProperty("removed")?.GetValue(removeOkResult.Value, null);
            Assert.Equal(true, removeResponse);
            Assert.Empty(_context.UpdateReactions);
        }

        [Fact]
        public async Task AddComment_ValidData_AddsCommentToDatabase()
        {
            // Arrange
            var userId = "user-123";
            SetCurrentUser(userId);
            var updateId = 10;

            var dto = new CommentDto { CommentText = "Great progress!" };

            // Act
            var result = await _controller.AddComment(updateId, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var comment = Assert.IsType<UpdateComment>(okResult.Value);

            Assert.Equal("Great progress!", comment.CommentText);
            Assert.Equal(userId, comment.UserId);
            Assert.Equal(updateId, comment.UpdateId);
            
            var dbComment = await _context.UpdateComments.FirstOrDefaultAsync();
            Assert.NotNull(dbComment);
            Assert.Equal("Great progress!", dbComment.CommentText);
        }

        [Fact]
        public async Task UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            // Explicitly clearing the context to simulate no logged-in user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var createResult = await _controller.CreateUpdate(1, new CreateUpdateDto());
            var reactResult = await _controller.React(1, new ReactDto());
            var commentResult = await _controller.AddComment(1, new CommentDto());

            // Assert
            Assert.IsType<UnauthorizedResult>(createResult);
            Assert.IsType<UnauthorizedResult>(reactResult);
            Assert.IsType<UnauthorizedResult>(commentResult);
        }

        public void Dispose()
        {
            // Clean up the in-memory database to prevent test bleed
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}