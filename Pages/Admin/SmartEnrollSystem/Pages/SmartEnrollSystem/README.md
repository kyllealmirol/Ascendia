# SmartEnrollSystem

## Overview
SmartEnrollSystem is a web application designed to facilitate the management of student enrollments and registrar functionalities. This application provides a secure platform for registrars to log in, manage their accounts, and access various reports and masterlists related to student enrollments.

## Features
- **Registrar Authentication**: Secure login and logout functionality for registrars.
- **Change Credentials**: Ability for registrars to update their username and password.
- **Reports & Masterlist**: Generate and view student enrollment reports and system masterlists.
- **User-Friendly Interface**: Intuitive design for easy navigation and access to features.

## Project Structure
```
SmartEnrollSystem
├── Pages
│   ├── Account
│   │   ├── Login.cshtml
│   │   ├── Login.cshtml.cs
│   │   ├── ChangeCredentials.cshtml
│   │   ├── ChangeCredentials.cshtml.cs
│   │   ├── Logout.cshtml
│   │   └── Logout.cshtml.cs
│   ├── Registrar
│   │   └── Index.cshtml
│   └── _ViewImports.cshtml
├── Services
│   └── RegistrarAuthenticationService.cs
├── Data
│   └── RegistrarAccount.cs
├── Program.cs
├── appsettings.json
└── README.md
```

## Setup Instructions
1. **Clone the Repository**: 
   ```bash
   git clone <repository-url>
   cd SmartEnrollSystem
   ```

2. **Install Dependencies**: 
   Make sure you have the .NET SDK installed. Run the following command to restore the dependencies:
   ```bash
   dotnet restore
   ```

3. **Configure Database**: 
   Update the `appsettings.json` file with your database connection string and any other necessary configurations.

4. **Run the Application**: 
   Start the application using the following command:
   ```bash
   dotnet run
   ```

5. **Access the Application**: 
   Open your web browser and navigate to `http://localhost:5000` to access the SmartEnrollSystem.

## Usage Guidelines
- **Login**: Use the registrar credentials to log in to the system.
- **Change Credentials**: After logging in, navigate to the "Account Settings" to update your username or password.
- **Generate Reports**: Access the Reports & Masterlist section to generate and view reports.

## Contributing
Contributions are welcome! Please submit a pull request or open an issue for any enhancements or bug fixes.

## License
This project is licensed under the MIT License. See the LICENSE file for more details.