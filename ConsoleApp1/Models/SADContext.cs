using Microsoft.EntityFrameworkCore;

namespace LocatesParser.Models
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
                entity.HasKey(e => new { e.TicketNumber, e.TicketKey })
                    .ForSqlServerIsClustered(false);

                entity.ToTable("OneCallTicket", "dbo");

                entity.HasIndex(e => new { e.BeginWorkDate, e.City, e.FinishWorkDate, e.OriginalCallDate, e.Remark, e.Status, e.StreetAddress, e.TicketKey, e.TicketNumber, e.TicketType, e.WorkExtent })
                    .HasName("ClusteredColumnStoreIndex-20180302-141020");

                entity.Property(e => e.TicketNumber)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.TicketKey)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.BeginWorkDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.City)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.FinishWorkDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.OriginalCallDate).HasColumnType("datetime2(2)");

                entity.Property(e => e.Remark)
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.StreetAddress)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TicketType)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.WorkExtent)
                    .HasMaxLength(1000)
                    .IsUnicode(false);
            });
        }
    }
}
