using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ConsoleApp1.Models
{
    public partial class SADContext : DbContext
    {
        public virtual DbSet<OneCallTicket> OneCallTicket { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer(@"Server=eccsqldev;Database=SAD;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OneCallTicket>(entity =>
            {
                entity.HasKey(e => e.TicketNumber)
                    .ForSqlServerIsClustered(false);

                entity.ToTable("OneCallTicket", "dbo");

                entity.HasIndex(e => new { e.BeginWorkDate, e.City, e.OriginalCallDate, e.StreetAddress, e.TicketNumber, e.StateAbbreviation, e.Status })
                    .HasName("ClusteredColumnStoreIndex-20180131-150027");

                entity.Property(e => e.TicketNumber)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.BeginWorkDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.City)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.OriginalCallDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.StateAbbreviation)
                    .IsRequired()
                    .HasColumnType("char(2)");

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.StreetAddress)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });
        }
    }
}
