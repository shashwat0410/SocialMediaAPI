using SocialMediaAPI.Data.Interfaces;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Services
{
    // ════════════════════════════════════════════
    //  POST SERVICE
    // ════════════════════════════════════════════
    public interface IPostService
    {
        Task<ApiResponse<PagedResult<PostResponseDto>>> GetPostsAsync(PaginationParams p, string? currentUserId);
        Task<ApiResponse<PagedResult<PostResponseDto>>> GetFeedAsync(string userId, PaginationParams p);
        Task<ApiResponse<PagedResult<PostResponseDto>>> GetUserPostsAsync(string userId, PaginationParams p);
        Task<ApiResponse<PostResponseDto>> GetPostByIdAsync(int id, string? currentUserId);
        Task<ApiResponse<PostResponseDto>> CreatePostAsync(CreatePostDto dto, string userId);
        Task<ApiResponse<PostResponseDto>> UpdatePostAsync(int id, UpdatePostDto dto, string userId);
        Task<ApiResponse<string>> DeletePostAsync(int id, string userId, bool isAdmin = false);
        Task<ApiResponse<string>> ToggleLikeAsync(int postId, string userId);
    }

    public class PostService : IPostService
    {
        private readonly IUnitOfWork _uow;
        public PostService(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<PagedResult<PostResponseDto>>> GetPostsAsync(PaginationParams p, string? currentUserId)
        {
            var result = await _uow.Posts.GetPostsAsync(p, currentUserId);
            var dtos = await MapPostsAsync(result.Items, currentUserId);
            return ApiResponse<PagedResult<PostResponseDto>>.Ok(new PagedResult<PostResponseDto>
            {
                Items = dtos, TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
            });
        }

        public async Task<ApiResponse<PagedResult<PostResponseDto>>> GetFeedAsync(string userId, PaginationParams p)
        {
            var result = await _uow.Posts.GetFeedAsync(userId, p);
            var dtos = await MapPostsAsync(result.Items, userId);
            return ApiResponse<PagedResult<PostResponseDto>>.Ok(new PagedResult<PostResponseDto>
            {
                Items = dtos, TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
            });
        }

        public async Task<ApiResponse<PagedResult<PostResponseDto>>> GetUserPostsAsync(string userId, PaginationParams p)
        {
            var result = await _uow.Posts.GetUserPostsAsync(userId, p);
            var dtos = await MapPostsAsync(result.Items, userId);
            return ApiResponse<PagedResult<PostResponseDto>>.Ok(new PagedResult<PostResponseDto>
            {
                Items = dtos, TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
            });
        }

        public async Task<ApiResponse<PostResponseDto>> GetPostByIdAsync(int id, string? currentUserId)
        {
            var post = await _uow.Posts.GetPostWithDetailsAsync(id);
            if (post == null) return ApiResponse<PostResponseDto>.Fail("Post not found.");

            var isLiked = currentUserId != null && await _uow.Posts.IsLikedByUserAsync(id, currentUserId);
            return ApiResponse<PostResponseDto>.Ok(MapPost(post, isLiked));
        }

        public async Task<ApiResponse<PostResponseDto>> CreatePostAsync(CreatePostDto dto, string userId)
        {
            var post = new Post
            {
                Content   = dto.Content,
                ImageUrl  = dto.ImageUrl,
                UserId    = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Posts.AddAsync(post);
            await _uow.SaveChangesAsync();

            var created = await _uow.Posts.GetPostWithDetailsAsync(post.Id);
            return ApiResponse<PostResponseDto>.Ok(MapPost(created!, false), "Post created.");
        }

        public async Task<ApiResponse<PostResponseDto>> UpdatePostAsync(int id, UpdatePostDto dto, string userId)
        {
            var post = await _uow.Posts.GetByIdAsync(id);
            if (post == null) return ApiResponse<PostResponseDto>.Fail("Post not found.");
            if (post.UserId != userId) return ApiResponse<PostResponseDto>.Fail("Not authorized.");

            post.Content   = dto.Content;
            post.ImageUrl  = dto.ImageUrl;
            post.UpdatedAt = DateTime.UtcNow;

            _uow.Posts.Update(post);
            await _uow.SaveChangesAsync();

            var updated = await _uow.Posts.GetPostWithDetailsAsync(post.Id);
            var isLiked = await _uow.Posts.IsLikedByUserAsync(id, userId);
            return ApiResponse<PostResponseDto>.Ok(MapPost(updated!, isLiked), "Post updated.");
        }

        public async Task<ApiResponse<string>> DeletePostAsync(int id, string userId, bool isAdmin = false)
        {
            var post = await _uow.Posts.GetByIdAsync(id);
            if (post == null) return ApiResponse<string>.Fail("Post not found.");
            if (post.UserId != userId && !isAdmin) return ApiResponse<string>.Fail("Not authorized.");

            post.IsDeleted = true; // soft delete
            _uow.Posts.Update(post);
            await _uow.SaveChangesAsync();
            return ApiResponse<string>.Ok("Post deleted.", "Post deleted successfully.");
        }

        public async Task<ApiResponse<string>> ToggleLikeAsync(int postId, string userId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post == null) return ApiResponse<string>.Fail("Post not found.");

            var existing = await _uow.Likes.GetLikeAsync(postId, userId);
            if (existing != null)
            {
                _uow.Likes.Delete(existing);
                await _uow.SaveChangesAsync();
                return ApiResponse<string>.Ok("unliked", "Post unliked.");
            }

            await _uow.Likes.AddAsync(new Like { PostId = postId, UserId = userId });
            await _uow.SaveChangesAsync();
            return ApiResponse<string>.Ok("liked", "Post liked.");
        }

        private async Task<List<PostResponseDto>> MapPostsAsync(List<Post> posts, string? currentUserId)
        {
            var dtos = new List<PostResponseDto>();
            foreach (var p in posts)
            {
                var isLiked = currentUserId != null && await _uow.Posts.IsLikedByUserAsync(p.Id, currentUserId);
                dtos.Add(MapPost(p, isLiked));
            }
            return dtos;
        }

        private static PostResponseDto MapPost(Post p, bool isLiked) => new()
        {
            Id                   = p.Id,
            Content              = p.Content,
            ImageUrl             = p.ImageUrl,
            CreatedAt            = p.CreatedAt,
            UpdatedAt            = p.UpdatedAt,
            LikesCount           = p.Likes.Count,
            CommentsCount        = p.Comments.Count,
            IsLikedByCurrentUser = isLiked,
            Author = new UserSummaryDto
            {
                Id                = p.User.Id,
                UserName          = p.User.UserName!,
                FullName          = p.User.FullName,
                ProfilePictureUrl = p.User.ProfilePictureUrl
            }
        };
    }

    // ════════════════════════════════════════════
    //  COMMENT SERVICE
    // ════════════════════════════════════════════
    public interface ICommentService
    {
        Task<ApiResponse<PagedResult<CommentResponseDto>>> GetPostCommentsAsync(int postId, PaginationParams p);
        Task<ApiResponse<CommentResponseDto>> AddCommentAsync(int postId, CreateCommentDto dto, string userId);
        Task<ApiResponse<CommentResponseDto>> UpdateCommentAsync(int commentId, UpdateCommentDto dto, string userId);
        Task<ApiResponse<string>> DeleteCommentAsync(int commentId, string userId, bool isAdmin = false);
    }

    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _uow;
        public CommentService(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<PagedResult<CommentResponseDto>>> GetPostCommentsAsync(int postId, PaginationParams p)
        {
            var result = await _uow.Comments.GetPostCommentsAsync(postId, p);
            return ApiResponse<PagedResult<CommentResponseDto>>.Ok(new PagedResult<CommentResponseDto>
            {
                Items = result.Items.Select(MapComment).ToList(),
                TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
            });
        }

        public async Task<ApiResponse<CommentResponseDto>> AddCommentAsync(int postId, CreateCommentDto dto, string userId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post == null) return ApiResponse<CommentResponseDto>.Fail("Post not found.");

            var comment = new Comment
            {
                Content         = dto.Content,
                PostId          = postId,
                UserId          = userId,
                ParentCommentId = dto.ParentCommentId,
                CreatedAt       = DateTime.UtcNow
            };

            await _uow.Comments.AddAsync(comment);
            await _uow.SaveChangesAsync();

            var created = await _uow.Comments.GetCommentWithRepliesAsync(comment.Id);
            return ApiResponse<CommentResponseDto>.Ok(MapComment(created!), "Comment added.");
        }

        public async Task<ApiResponse<CommentResponseDto>> UpdateCommentAsync(int commentId, UpdateCommentDto dto, string userId)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment == null) return ApiResponse<CommentResponseDto>.Fail("Comment not found.");
            if (comment.UserId != userId) return ApiResponse<CommentResponseDto>.Fail("Not authorized.");

            comment.Content   = dto.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            _uow.Comments.Update(comment);
            await _uow.SaveChangesAsync();

            var updated = await _uow.Comments.GetCommentWithRepliesAsync(commentId);
            return ApiResponse<CommentResponseDto>.Ok(MapComment(updated!), "Comment updated.");
        }

        public async Task<ApiResponse<string>> DeleteCommentAsync(int commentId, string userId, bool isAdmin = false)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment == null) return ApiResponse<string>.Fail("Comment not found.");
            if (comment.UserId != userId && !isAdmin) return ApiResponse<string>.Fail("Not authorized.");

            comment.IsDeleted = true;
            _uow.Comments.Update(comment);
            await _uow.SaveChangesAsync();
            return ApiResponse<string>.Ok("Comment deleted.", "Comment deleted successfully.");
        }

        private static CommentResponseDto MapComment(Comment c) => new()
        {
            Id              = c.Id,
            Content         = c.Content,
            CreatedAt       = c.CreatedAt,
            UpdatedAt       = c.UpdatedAt,
            PostId          = c.PostId,
            ParentCommentId = c.ParentCommentId,
            Author = new UserSummaryDto
            {
                Id                = c.User.Id,
                UserName          = c.User.UserName!,
                FullName          = c.User.FullName,
                ProfilePictureUrl = c.User.ProfilePictureUrl
            },
            Replies = c.Replies.Where(r => !r.IsDeleted).Select(r => MapComment(r)).ToList()
        };
    }

    // ════════════════════════════════════════════
    //  USER SERVICE
    // ════════════════════════════════════════════
    public interface IUserService
    {
        Task<ApiResponse<UserProfileDto>> GetProfileAsync(string username, string? currentUserId);
        Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<ApiResponse<PagedResult<UserSummaryDto>>> SearchUsersAsync(PaginationParams p);
        Task<ApiResponse<string>> FollowUserAsync(string followerId, string followingId);
        Task<ApiResponse<PagedResult<UserSummaryDto>>> GetFollowersAsync(string userId, PaginationParams p);
        Task<ApiResponse<PagedResult<UserSummaryDto>>> GetFollowingAsync(string userId, PaginationParams p);
    }

    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        public UserService(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(string username, string? currentUserId)
        {
            var user = await _uow.Users.GetByUsernameAsync(username);
            if (user == null) return ApiResponse<UserProfileDto>.Fail("User not found.");

            var isFollowed = currentUserId != null && await _uow.Follows.IsFollowingAsync(currentUserId, user.Id);

            return ApiResponse<UserProfileDto>.Ok(new UserProfileDto
            {
                Id                       = user.Id,
                UserName                 = user.UserName!,
                FullName                 = user.FullName,
                Bio                      = user.Bio,
                ProfilePictureUrl        = user.ProfilePictureUrl,
                CreatedAt                = user.CreatedAt,
                PostsCount               = 0,
                FollowersCount           = await _uow.Follows.GetFollowersCountAsync(user.Id),
                FollowingCount           = await _uow.Follows.GetFollowingCountAsync(user.Id),
                IsFollowedByCurrentUser  = isFollowed
            });
        }

        public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return ApiResponse<UserProfileDto>.Fail("User not found.");

            if (dto.FullName != null) user.FullName = dto.FullName;
            if (dto.Bio != null) user.Bio = dto.Bio;
            if (dto.ProfilePictureUrl != null) user.ProfilePictureUrl = dto.ProfilePictureUrl;

            await _uow.Users.UpdateAsync(user);

            return ApiResponse<UserProfileDto>.Ok(new UserProfileDto
            {
                Id = user.Id, UserName = user.UserName!,
                FullName = user.FullName, Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl, CreatedAt = user.CreatedAt
            }, "Profile updated.");
        }

        public async Task<ApiResponse<PagedResult<UserSummaryDto>>> SearchUsersAsync(PaginationParams p)
        {
            var result = await _uow.Users.SearchUsersAsync(p);
            return ApiResponse<PagedResult<UserSummaryDto>>.Ok(new PagedResult<UserSummaryDto>
            {
                Items = result.Items.Select(u => new UserSummaryDto
                {
                    Id = u.Id, UserName = u.UserName!, FullName = u.FullName, ProfilePictureUrl = u.ProfilePictureUrl
                }).ToList(),
                TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
            });
        }

        public async Task<ApiResponse<string>> FollowUserAsync(string followerId, string followingId)
        {
            if (followerId == followingId) return ApiResponse<string>.Fail("You cannot follow yourself.");

            var target = await _uow.Users.GetByIdAsync(followingId);
            if (target == null) return ApiResponse<string>.Fail("User not found.");

            var existing = await _uow.Follows.GetFollowAsync(followerId, followingId);
            if (existing != null)
            {
                _uow.Follows.Delete(existing);
                await _uow.SaveChangesAsync();
                return ApiResponse<string>.Ok("unfollowed", "User unfollowed.");
            }

            await _uow.Follows.AddAsync(new Follow { FollowerId = followerId, FollowingId = followingId });
            await _uow.SaveChangesAsync();
            return ApiResponse<string>.Ok("followed", "User followed.");
        }

        public async Task<ApiResponse<PagedResult<UserSummaryDto>>> GetFollowersAsync(string userId, PaginationParams p)
        {
            var result = await _uow.Follows.GetFollowersAsync(userId, p);
            return ApiResponse<PagedResult<UserSummaryDto>>.Ok(new PagedResult<UserSummaryDto>
            {
                Items = result.Items.Select(MapUserSummary).ToList(),
                TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
            });
        }

        public async Task<ApiResponse<PagedResult<UserSummaryDto>>> GetFollowingAsync(string userId, PaginationParams p)
        {
            var result = await _uow.Follows.GetFollowingAsync(userId, p);
            return ApiResponse<PagedResult<UserSummaryDto>>.Ok(new PagedResult<UserSummaryDto>
            {
                Items = result.Items.Select(MapUserSummary).ToList(),
                TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
            });
        }

        private static UserSummaryDto MapUserSummary(ApplicationUser u) => new()
        {
            Id = u.Id, UserName = u.UserName!, FullName = u.FullName, ProfilePictureUrl = u.ProfilePictureUrl
        };
    }
}
