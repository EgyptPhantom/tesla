using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace TeslaRentalPlatform
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Data Source=tesla_rental.db;Version=3;";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                InitializeDatabase(connection);

                Console.WriteLine("Tesla Rental Platform initialized.");
                // Add logic for interacting with the system here.
            }
        }

        static void InitializeDatabase(SQLiteConnection connection)
        {
            string createCarsTable = @"
                CREATE TABLE IF NOT EXISTS Cars (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Model TEXT NOT NULL,
                    HourlyRate REAL NOT NULL,
                    PerKmRate REAL NOT NULL
                );";

            string createClientsTable = @"
                CREATE TABLE IF NOT EXISTS Clients (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Email TEXT NOT NULL UNIQUE
                );";

            string createRentalsTable = @"
                CREATE TABLE IF NOT EXISTS Rentals (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CarId INTEGER NOT NULL,
                    ClientId INTEGER NOT NULL,
                    StartTime DATETIME NOT NULL,
                    EndTime DATETIME,
                    KilometersDriven REAL,
                    TotalAmount REAL,
                    FOREIGN KEY(CarId) REFERENCES Cars(Id),
                    FOREIGN KEY(ClientId) REFERENCES Clients(Id)
                );";

            using (var command = new SQLiteCommand(createCarsTable, connection))
                command.ExecuteNonQuery();

            using (var command = new SQLiteCommand(createClientsTable, connection))
                command.ExecuteNonQuery();

            using (var command = new SQLiteCommand(createRentalsTable, connection))
                command.ExecuteNonQuery();
        }

        static void AddCar(SQLiteConnection connection, string model, double hourlyRate, double perKmRate)
        {
            string insertCar = "INSERT INTO Cars (Model, HourlyRate, PerKmRate) VALUES (@Model, @HourlyRate, @PerKmRate);";
            using (var command = new SQLiteCommand(insertCar, connection))
            {
                command.Parameters.AddWithValue("@Model", model);
                command.Parameters.AddWithValue("@HourlyRate", hourlyRate);
                command.Parameters.AddWithValue("@PerKmRate", perKmRate);
                command.ExecuteNonQuery();
            }
        }

        static void RegisterClient(SQLiteConnection connection, string fullName, string email)
        {
            string insertClient = "INSERT INTO Clients (FullName, Email) VALUES (@FullName, @Email);";
            using (var command = new SQLiteCommand(insertClient, connection))
            {
                command.Parameters.AddWithValue("@FullName", fullName);
                command.Parameters.AddWithValue("@Email", email);
                command.ExecuteNonQuery();
            }
        }

        static void StartRental(SQLiteConnection connection, int carId, int clientId, DateTime startTime)
        {
            string insertRental = "INSERT INTO Rentals (CarId, ClientId, StartTime) VALUES (@CarId, @ClientId, @StartTime);";
            using (var command = new SQLiteCommand(insertRental, connection))
            {
                command.Parameters.AddWithValue("@CarId", carId);
                command.Parameters.AddWithValue("@ClientId", clientId);
                command.Parameters.AddWithValue("@StartTime", startTime);
                command.ExecuteNonQuery();
            }
        }

        static void EndRental(SQLiteConnection connection, int rentalId, DateTime endTime, double kilometersDriven)
        {
            string getRentalDetails = "SELECT CarId, StartTime FROM Rentals WHERE Id = @RentalId;";
            int carId;
            DateTime startTime;

            using (var command = new SQLiteCommand(getRentalDetails, connection))
            {
                command.Parameters.AddWithValue("@RentalId", rentalId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) throw new Exception("Rental not found.");
                    carId = reader.GetInt32(0);
                    startTime = reader.GetDateTime(1);
                }
            }

            string getCarRates = "SELECT HourlyRate, PerKmRate FROM Cars WHERE Id = @CarId;";
            double hourlyRate, perKmRate;
            using (var command = new SQLiteCommand(getCarRates, connection))
            {
                command.Parameters.AddWithValue("@CarId", carId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) throw new Exception("Car not found.");
                    hourlyRate = reader.GetDouble(0);
                    perKmRate = reader.GetDouble(1);
                }
            }

            double hours = (endTime - startTime).TotalHours;
            double totalAmount = (hours * hourlyRate) + (kilometersDriven * perKmRate);

            string updateRental = "UPDATE Rentals SET EndTime = @EndTime, KilometersDriven = @KilometersDriven, TotalAmount = @TotalAmount WHERE Id = @RentalId;";
            using (var command = new SQLiteCommand(updateRental, connection))
            {
                command.Parameters.AddWithValue("@EndTime", endTime);
                command.Parameters.AddWithValue("@KilometersDriven", kilometersDriven);
                command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                command.Parameters.AddWithValue("@RentalId", rentalId);
                command.ExecuteNonQuery();
            }
        }
    }
}
