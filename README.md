P0: Banking Application

## Developer: Pranav Madan

Summary:
This banking application will give users the ability to let users create accounts, withdraw, deposit, and transfer money, check their last 5 transactions, request a check book, and change the password.

## Tech Stack

Technology            | Purpose
----------------------|--------------
C#                    | programming language
.NET 10               | application framework
Entity Framework Core | ORM, code-first migrations
SQL Server            | relational database
xUnit                 | tests

## Project Structure

```
bankingapplication/
│
├── bankingapplication.Tests/   xUnit tests (run against a separate BankingAppTests database)
├── Migrations/                 EF Core migrations
├── Account.cs                  abstract base account (username + hashed password)
├── AdminAccount.cs
├── UserAccount.cs
├── AppConfig.cs
├── BankingContext.cs           EF Core DbContext and model configuration
├── Checkbook.cs
├── PasswordHasher.cs           PBKDF2 password hashing
├── Program.cs                  console menus
├── ServiceRequest.cs
├── SsnEncryptionConverter.cs   encrypts the SSN column (AES-GCM)
├── Tables.sql                  original hand-written schema, superseded by Migrations/
└── Transaction.cs
```

## Getting Started

Prerequisites:

- .NET 10 SDK
- SQL Server Express (the connection string in `BankingContext.cs` expects `localhost\SQLEXPRESS`)
- the EF Core tool: `dotnet tool install --global dotnet-ef`

Verify that .NET is installed:

```
dotnet --version
```

## Setup

1. Restore the dependencies:

   ```
   dotnet restore
   ```

2. Set the key used to encrypt SSNs, then reopen your terminal. Keep the value secret and do not change it once real data exists:

   ```
   setx BANKING_SSN_KEY "a-long-random-secret"
   ```

3. Create the database and tables:

   ```
   dotnet ef database update
   ```

4. Run the application:

   ```
   dotnet run
   ```

   A default admin is created on first start (username `admin`, password `Admin123!`).

## Tests

```
dotnet test bankingapplication.Tests
```

The tests need SQL Server Express running. They create and use a separate `BankingAppTests` database.

## Notes

- Passwords are stored as a salted PBKDF2 hash, never as plain text.
- SSNs are stored encrypted; the last 5 transactions come from the `TransactionRecords` table.

