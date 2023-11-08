using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DataAccess.Models
{
    public partial class PRN221_ProjectContext : DbContext
    {
        public PRN221_ProjectContext()
        {
        }

        public PRN221_ProjectContext(DbContextOptions<PRN221_ProjectContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Bid> Bids { get; set; } = null!;
        public virtual DbSet<Item> Items { get; set; } = null!;
        public virtual DbSet<ItemType> ItemTypes { get; set; } = null!;
        public virtual DbSet<Member> Members { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("server =(local); database = PRN221_Project; uid=sa;pwd=123456;Trusted_Connection=True;Encrypt=False");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bid>(entity =>
            {
                entity.Property(e => e.BidId).HasColumnName("BidID");

                entity.Property(e => e.BidDateTime).HasColumnType("datetime");

                entity.Property(e => e.BidPrice).HasColumnType("money");

                entity.Property(e => e.BidderId).HasColumnName("BidderID");

                entity.Property(e => e.ItemId).HasColumnName("ItemID");

                entity.HasOne(d => d.Bidder)
                    .WithMany(p => p.Bids)
                    .HasForeignKey(d => d.BidderId)
                    .HasConstraintName("FK__Bids__BidderID__3D5E1FD2");

                entity.HasOne(d => d.Item)
                    .WithMany(p => p.Bids)
                    .HasForeignKey(d => d.ItemId)
                    .HasConstraintName("FK__Bids__ItemID__3E52440B");
            });

            modelBuilder.Entity<Item>(entity =>
            {
                entity.Property(e => e.ItemId).HasColumnName("ItemID");

                entity.Property(e => e.CurrentPrice).HasColumnType("money");

                entity.Property(e => e.EndDateTime).HasColumnType("datetime");

                entity.Property(e => e.ItemName).HasMaxLength(100);

                entity.Property(e => e.ItemTypeId).HasColumnName("ItemTypeID");

                entity.Property(e => e.SellerId).HasColumnName("SellerID");

                entity.HasOne(d => d.ItemType)
                    .WithMany(p => p.Items)
                    .HasForeignKey(d => d.ItemTypeId)
                    .HasConstraintName("FK__Items__ItemTypeI__3F466844");

                entity.HasOne(d => d.Seller)
                    .WithMany(p => p.Items)
                    .HasForeignKey(d => d.SellerId)
                    .HasConstraintName("FK__Items__SellerID__403A8C7D");
            });

            modelBuilder.Entity<ItemType>(entity =>
            {
                entity.Property(e => e.ItemTypeId).HasColumnName("ItemTypeID");

                entity.Property(e => e.ItemTypeName).HasMaxLength(100);
            });

            modelBuilder.Entity<Member>(entity =>
            {
                entity.Property(e => e.Address).HasMaxLength(200);

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Expirationdate).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.Property(e => e.Password)
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
