using AccountManagement;
using Checkbooks;
using Microsoft.EntityFrameworkCore;
using Request;
using Transactions;

namespace BankingData
{
    public class BankingContext : DbContext
    {
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
        public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();
        public DbSet<Checkbook> CheckbookRecords => Set<Checkbook>();
        public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
        public DbSet<Transaction> TransactionRecords => Set<Transaction>();

        public BankingContext() { }

        public BankingContext(DbContextOptions<BankingContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=BankingApp;Trusted_Connection=True;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TPH: one Accounts table. The PK must live on the root type, so accountId is a surrogate key;
            // accountNumber is the unique, app-assigned business identifier on UserAccount.
            modelBuilder.Entity<Account>(a =>
            {
                a.ToTable("Accounts");
                a.HasKey(x => x.accountId);
                a.Property(x => x.username).HasMaxLength(25).IsRequired();
                a.HasIndex(x => x.username).IsUnique();
                a.Property<string>("_password").HasColumnName("password").HasMaxLength(200).IsRequired();
                a.HasDiscriminator<string>("accountType")
                    .HasValue<UserAccount>("User")
                    .HasValue<AdminAccount>("Admin");
            });

            modelBuilder.Entity<UserAccount>(u =>
            {
                u.Property(x => x.accountNumber).ValueGeneratedNever();
                u.HasIndex(x => x.accountNumber).IsUnique();
                u.Property(x => x.accName).HasMaxLength(80);
                u.Property(x => x.email).HasMaxLength(40);
                u.Property(x => x.homeAddress).HasMaxLength(70);
                u.Property(x => x.accBalance).HasConversion<decimal>().HasColumnType("decimal(18,2)");
                u.Property(x => x.SSN).HasConversion(new SsnEncryptionConverter()).HasMaxLength(100);

                // one UserAccount -> many Checkbooks / ServiceRequests / Transactions
                u.HasMany(x => x.Checkbook).WithOne().HasForeignKey("userAccountId").IsRequired();
                u.HasMany(x => x.serviceRequests).WithOne().HasForeignKey("userAccountId").IsRequired();
                u.HasMany(x => x.transactions).WithOne().HasForeignKey("userAccountId").IsRequired();
            });

            modelBuilder.Entity<Checkbook>(c =>
            {
                c.HasKey(x => x.checkbookId);
                c.Property(x => x.Payee).HasMaxLength(80);
                c.Ignore(x => x.isPending);
            });

            modelBuilder.Entity<ServiceRequest>(s =>
            {
                s.HasKey(x => x.serviceRequestId);
                s.Property(x => x.requestor).HasMaxLength(25);
                s.Property(x => x.requestType).HasMaxLength(20);
                s.Property(x => x.description).HasMaxLength(200);
            });

            modelBuilder.Entity<Transaction>(t =>
            {
                t.HasKey(x => x.id);
                t.Property(x => x.accName).HasMaxLength(80);
                t.Property(x => x.description).HasMaxLength(200);
            });
        }
    }
}
