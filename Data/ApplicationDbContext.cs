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
        }
    }
}