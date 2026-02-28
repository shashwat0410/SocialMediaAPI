using System.ComponentModel.DataAnnotations;

namespace SocialMediaAPI.DTOs
{
    // ════════════════════════════════════════
    //  COMMON
    // ════════════════════════════════════════
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }

    public class PaginationParams
    {
        private int _pageSize = 10;
        public int Page { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > 50 ? 50 : value; // max 50 per page
        }
        public string? Search { get; set; }
        public string SortBy { get; set; } = "createdAt";
        public string SortOrder { get; set; } = "desc"; // asc | desc
    }

    // ════════════════════════════════════════
    //  AUTH
    // ════════════════════════════════════════
    public class RegisterRequestDto
    {
        [Required] [StringLength(100)] public string FullName { get; set; } = string.Empty;
        [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] [StringLength(50, MinimumLength = 3)] public string UserName { get; set; } = string.Empty;
        [Required] [MinLength(6)] public string Password { get; set; } = string.Empty;
        [Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginRequestDto
    {
        [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequestDto
    {
        [Required] public string AccessToken { get; set; } = string.Empty;
        [Required] public string RefreshToken { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiry { get; set; }
        public UserSummaryDto User { get; set; } = null!;
    }

    // ════════════════════════════════════════
    //  USER
    // ════════════════════════════════════════
    public class UserSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }

    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public int PostsCount { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [StringLength(100)] public string? FullName { get; set; }
        [StringLength(300)] public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    // ════════════════════════════════════════
    //  POST
    // ════════════════════════════════════════
    public class CreatePostDto
    {
        [Required] [StringLength(2000, MinimumLength = 1)] public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class UpdatePostDto
    {
        [Required] [StringLength(2000, MinimumLength = 1)] public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class PostResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserSummaryDto Author { get; set; } = null!;
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }

    // ════════════════════════════════════════
    //  COMMENT
    // ════════════════════════════════════════
    public class CreateCommentDto
    {
        [Required] [StringLength(1000, MinimumLength = 1)] public string Content { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
    }

    public class UpdateCommentDto
    {
        [Required] [StringLength(1000, MinimumLength = 1)] public string Content { get; set; } = string.Empty;
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserSummaryDto Author { get; set; } = null!;
        public int PostId { get; set; }
        public int? ParentCommentId { get; set; }
        public List<CommentResponseDto> Replies { get; set; } = new();
    }
}
