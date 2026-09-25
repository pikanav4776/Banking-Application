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
├── Migrations/                 EF Core migrations
├── Account.cs                  abstract base account (username + hashed password)
├── AdminAccount.cs
├── UserAccount.cs
├── AppConfig.cs
├── Checkbook.cs
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

## Functionality
Users have the option to deposit/withdraw money from accounts, and transfer them to other user accounts.
They can also request a checkbook, change their password, view their account details, and last 5 transactions.

Admins can create, view, and delete accounts, reset customer passwords, approve checkbook requests.

## Tables
Accounts
   Admin Accounts
   User Accounts
Checkbook
Service Requests
Transactions

## Notes

- Passwords are stored as a salted PBKDF2 hash, never as plain text.
- SSNs are stored encrypted; the last 5 transactions come from the `TransactionRecords` table.

