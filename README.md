# DriveEase - Car Rental Management System

## Overview

DriveEase is a web-based car rental management system developed using ASP.NET Core 8 MVC. The system helps manage vehicle rentals, customer records, user accounts, and rental transactions. It also includes role-based access and two-factor authentication for additional account security.

## Features

### Administrator
- Manage vehicle records
- Manage customer information
- Manage rental transactions
- Manage user accounts
- View system dashboard and statistics
- Approve, complete, or cancel rental bookings

### Customer
- View available vehicles
- Submit rental requests
- View rental history
- Enable or disable two-factor authentication

## Technologies Used

- ASP.NET Core 8 MVC
- Entity Framework Core
- SQLite Database
- ASP.NET Identity
- Bootstrap 5
- OtpNet
- QRCoder

## System Requirements

- Visual Studio 2022
- .NET 8 SDK

## Installation and Setup

1. Download or clone the project.
2. Open the project in Visual Studio 2022.
3. Restore the required NuGet packages.
4. Build the solution.
5. Run the application using F5 or the Run button.

The database will be created automatically during the first run.

## Default User Accounts

| Role | Username | Password |
|--------|----------|----------|
| Admin | admin | Admin@123 |

## Two-Factor Authentication

1. Log in to your account.
2. Open the Profile page.
3. Select the option to enable 2FA.
4. Scan the generated QR code using Google Authenticator or Microsoft Authenticator.
5. Enter the verification code to complete the setup.

## Project Structure

```
CarRentalSystem
│
├── Controllers
├── Models
├── Views
├── Data
├── Services
├── wwwroot
├── Program.cs
├── appsettings.json
└── carrental.db
```

## Database

The system uses SQLite as the database engine. A database file named `carrental.db` is automatically generated when the application is launched for the first time.

Main tables used in the system:

- AspNetUsers
- AspNetRoles
- Cars
- Customers
- RentalTransactions

## Packages

The project uses the following packages:

- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- OtpNet
- QRCoder

## Notes

- The application uses role-based authorization.
- Vehicle availability is updated based on rental status.
- Two-factor authentication is optional and can be enabled by users through their profile settings.
- Sample data is automatically added during the first application startup.

## Developer

Developed as part of an academic project using ASP.NET Core MVC and SQLite.