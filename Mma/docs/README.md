# Mma Project

A .NET 9.0 application that provides a robust backend with features for data management, validation, and machine learning.

## Features

*   **Database Integration:** Uses Entity Framework Core for data access with a SQL Server database. Includes features like audit trails and soft-delete.
*   **Custom Configuration:** Implements a custom configuration provider for SQL Server.
*   **Data Handling:** Provides a rich set of helpers for JSON serialization/deserialization, data conversion, and secure data handling.
*   **Validation:** Leverages FluentValidation for robust and easy-to-implement validation rules.
*   **Machine Learning:** Integrates with ML.NET for machine learning capabilities.
*   **REST Client:** Includes helpers for making REST API calls using RestSharp.
*   **Extensibility:** A wide range of extension methods for common types like `string`, `IEnumerable`, `DateTime`, etc.

## Technologies Used

*   .NET 9.0
*   ASP.NET Core
*   Entity Framework Core
*   FluentValidation
*   ML.NET
*   NodaTime
*   RestSharp
*   ULID

## Getting Started

### Prerequisites

*   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
*   SQL Server

### Installation

1.  Clone the repository:
    ```bash
    git clone <repository-url>
    ```
2.  Navigate to the project directory:
    ```bash
    cd Mma
    ```
3.  Restore the dependencies:
    ```bash
    dotnet restore
    ```
4.  Configure your database connection string in the appropriate configuration file.
5.  Run the application:
    ```bash
    dotnet run
    ```

## License

This project is licensed under the terms of the [LICENSE.txt](LICENSE.txt) file.
