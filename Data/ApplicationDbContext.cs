using BookBoard.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Board> Boards { get; set; }

        public DbSet<BoardBook> BoardBooks { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<BoardTag> BoardTags { get; set; }

        public DbSet<BookTag> BookTags { get; set; }

        public DbSet<BoardVisualItem> BoardVisualItems { get; set; }

        public DbSet<SavedBoard> SavedBoards { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Tag>()
                .HasIndex(tag => tag.Slug)
                .IsUnique();

            builder.Entity<BoardTag>()
                .HasKey(boardTag => new { boardTag.BoardId, boardTag.TagId });

            builder.Entity<BoardTag>()
                .HasOne(boardTag => boardTag.Board)
                .WithMany(board => board.BoardTags)
                .HasForeignKey(boardTag => boardTag.BoardId);

            builder.Entity<BoardTag>()
                .HasOne(boardTag => boardTag.Tag)
                .WithMany(tag => tag.BoardTags)
                .HasForeignKey(boardTag => boardTag.TagId);

            builder.Entity<BookTag>()
                .HasKey(bookTag => new { bookTag.BoardBookId, bookTag.TagId });

            builder.Entity<BookTag>()
                .HasOne(bookTag => bookTag.BoardBook)
                .WithMany(book => book.BookTags)
                .HasForeignKey(bookTag => bookTag.BoardBookId);

            builder.Entity<BookTag>()
                .HasOne(bookTag => bookTag.Tag)
                .WithMany(tag => tag.BookTags)
                .HasForeignKey(bookTag => bookTag.TagId);

            builder.Entity<BoardVisualItem>()
                .HasOne(item => item.Board)
                .WithMany(board => board.VisualItems)
                .HasForeignKey(item => item.BoardId);

            builder.Entity<SavedBoard>()
                .HasKey(savedBoard => new { savedBoard.UserId, savedBoard.BoardId });

            builder.Entity<SavedBoard>()
                .HasOne(savedBoard => savedBoard.User)
                .WithMany(user => user.SavedBoards)
                .HasForeignKey(savedBoard => savedBoard.UserId);

            builder.Entity<SavedBoard>()
                .HasOne(savedBoard => savedBoard.Board)
                .WithMany(board => board.SavedByUsers)
                .HasForeignKey(savedBoard => savedBoard.BoardId);
        }
    }
}