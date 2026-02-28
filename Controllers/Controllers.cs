using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Services;
using System.Security.Claims;

namespace SocialMediaAPI.Controllers
{
    // ════════════════════════════════════════════════════════
    //  BASE CONTROLLER
    // ════════════════════════════════════════════════════════
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected string? CurrentUserId => User.FindFirstValue("userId");
        protected string? CurrentUserName => User.FindFirstValue(ClaimTypes.Name);

        protected IActionResult ApiResult<T>(ApiResponse<T> response)
        {
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
    }

    // ════════════════════════════════════════════════════════
    //  AUTH CONTROLLER
    // ════════════════════════════════════════════════════════
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) => _authService = authService;

        /// <summary>Register a new user account</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Validation failed.",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            return ApiResult(await _authService.RegisterAsync(dto));
        }

        /// <summary>Login and receive JWT + Refresh tokens</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Validation failed.",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            return ApiResult(await _authService.LoginAsync(dto));
        }

        /// <summary>Refresh expired access token using a valid refresh token</summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
            => ApiResult(await _authService.RefreshTokenAsync(dto));

        /// <summary>Revoke all refresh tokens (logout)</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
            => ApiResult(await _authService.LogoutAsync(CurrentUserId!));
    }

    // ════════════════════════════════════════════════════════
    //  POSTS CONTROLLER
    // ════════════════════════════════════════════════════════
    [Route("api/posts")]
    [Authorize]
    public class PostsController : BaseApiController
    {
        private readonly IPostService _postService;
        public PostsController(IPostService postService) => _postService = postService;

        /// <summary>Get all posts with pagination, search and sorting</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PostResponseDto>>), 200)]
        public async Task<IActionResult> GetPosts([FromQuery] PaginationParams p)
            => ApiResult(await _postService.GetPostsAsync(p, CurrentUserId));

        /// <summary>Get personalized feed (posts from followed users)</summary>
        [HttpGet("feed")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PostResponseDto>>), 200)]
        public async Task<IActionResult> GetFeed([FromQuery] PaginationParams p)
            => ApiResult(await _postService.GetFeedAsync(CurrentUserId!, p));

        /// <summary>Get a single post by ID</summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PostResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPost(int id)
            => ApiResult(await _postService.GetPostByIdAsync(id, CurrentUserId));

        /// <summary>Create a new post</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PostResponseDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Validation failed.",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            return ApiResult(await _postService.CreatePostAsync(dto, CurrentUserId!));
        }

        /// <summary>Update your own post</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<PostResponseDto>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostDto dto)
            => ApiResult(await _postService.UpdatePostAsync(id, dto, CurrentUserId!));

        /// <summary>Delete a post (owner or admin)</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> DeletePost(int id)
        {
            var isAdmin = User.IsInRole("Admin");
            return ApiResult(await _postService.DeletePostAsync(id, CurrentUserId!, isAdmin));
        }

        /// <summary>Like or unlike a post (toggle)</summary>
        [HttpPost("{id:int}/like")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> ToggleLike(int id)
            => ApiResult(await _postService.ToggleLikeAsync(id, CurrentUserId!));

        /// <summary>Get comments for a post</summary>
        [HttpGet("{id:int}/comments")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CommentResponseDto>>), 200)]
        public async Task<IActionResult> GetComments(int id, [FromQuery] PaginationParams p,
            [FromServices] ICommentService commentService)
            => ApiResult(await commentService.GetPostCommentsAsync(id, p));

        /// <summary>Add a comment to a post</summary>
        [HttpPost("{id:int}/comments")]
        [ProducesResponseType(typeof(ApiResponse<CommentResponseDto>), 200)]
        public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentDto dto,
            [FromServices] ICommentService commentService)
            => ApiResult(await commentService.AddCommentAsync(id, dto, CurrentUserId!));
    }

    // ════════════════════════════════════════════════════════
    //  COMMENTS CONTROLLER
    // ════════════════════════════════════════════════════════
    [Route("api/comments")]
    [Authorize]
    public class CommentsController : BaseApiController
    {
        private readonly ICommentService _commentService;
        public CommentsController(ICommentService commentService) => _commentService = commentService;

        /// <summary>Update your own comment</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<CommentResponseDto>), 200)]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentDto dto)
            => ApiResult(await _commentService.UpdateCommentAsync(id, dto, CurrentUserId!));

        /// <summary>Delete a comment (owner or admin)</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var isAdmin = User.IsInRole("Admin");
            return ApiResult(await _commentService.DeleteCommentAsync(id, CurrentUserId!, isAdmin));
        }
    }

    // ════════════════════════════════════════════════════════
    //  USERS CONTROLLER
    // ════════════════════════════════════════════════════════
    [Route("api/users")]
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        public UsersController(IUserService userService) => _userService = userService;

        /// <summary>Search users by name or username</summary>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSummaryDto>>), 200)]
        public async Task<IActionResult> Search([FromQuery] PaginationParams p)
            => ApiResult(await _userService.SearchUsersAsync(p));

        /// <summary>Get user profile by username</summary>
        [HttpGet("{username}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetProfile(string username)
            => ApiResult(await _userService.GetProfileAsync(username, CurrentUserId));

        /// <summary>Get posts by a specific user</summary>
        [HttpGet("{userId}/posts")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PostResponseDto>>), 200)]
        public async Task<IActionResult> GetUserPosts(string userId, [FromQuery] PaginationParams p,
            [FromServices] IPostService postService)
            => ApiResult(await postService.GetUserPostsAsync(userId, p));

        /// <summary>Update current user profile</summary>
        [HttpPut("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
            => ApiResult(await _userService.UpdateProfileAsync(CurrentUserId!, dto));

        /// <summary>Follow or unfollow a user (toggle)</summary>
        [HttpPost("{userId}/follow")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> ToggleFollow(string userId)
            => ApiResult(await _userService.FollowUserAsync(CurrentUserId!, userId));

        /// <summary>Get a user's followers</summary>
        [HttpGet("{userId}/followers")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSummaryDto>>), 200)]
        public async Task<IActionResult> GetFollowers(string userId, [FromQuery] PaginationParams p)
            => ApiResult(await _userService.GetFollowersAsync(userId, p));

        /// <summary>Get users that a user is following</summary>
        [HttpGet("{userId}/following")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSummaryDto>>), 200)]
        public async Task<IActionResult> GetFollowing(string userId, [FromQuery] PaginationParams p)
            => ApiResult(await _userService.GetFollowingAsync(userId, p));
    }

    // ════════════════════════════════════════════════════════
    //  ADMIN CONTROLLER
    // ════════════════════════════════════════════════════════
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;

        public AdminController(IUserService userService, IPostService postService)
        {
            _userService = userService;
            _postService = postService;
        }

        /// <summary>[Admin] Get all users</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParams p)
            => ApiResult(await _userService.SearchUsersAsync(p));

        /// <summary>[Admin] Delete any post</summary>
        [HttpDelete("posts/{id:int}")]
        public async Task<IActionResult> DeletePost(int id)
            => ApiResult(await _postService.DeletePostAsync(id, CurrentUserId!, isAdmin: true));
    }
}
