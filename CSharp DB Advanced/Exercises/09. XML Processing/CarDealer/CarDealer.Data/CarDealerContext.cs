﻿namespace CarDealer.Data
{
    using Microsoft.EntityFrameworkCore;
    using Models;

    public class CarDealerContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }

        public DbSet<Sale> Sales { get; set; }

        public DbSet<Car> Cars { get; set; }

        public DbSet<PartCar> PartCars { get; set; }

        public DbSet<Part> Parts { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Configuration.ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PartCar>()
                .HasKey(pc => new { pc.PartId, pc.CarId });
        }
    }
}