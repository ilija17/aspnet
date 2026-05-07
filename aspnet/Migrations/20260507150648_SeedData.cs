using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace aspnet.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Casinos",
                columns: new[] { "Id", "Address", "FoundedDate", "LicenseNumber", "Name" },
                values: new object[,]
                {
                    { 1, "Ilica 12, Zagreb", new DateTime(2005, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "HR-CAS-001", "Royal Vegas" },
                    { 2, "Vukovarska 55, Split", new DateTime(2010, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "HR-CAS-002", "Golden Palace" },
                    { 3, "Korzo 3, Rijeka", new DateTime(2018, 2, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "HR-CAS-003", "Diamond Club" }
                });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "Description", "MaxBet", "MinBet", "Name", "Type" },
                values: new object[,]
                {
                    { 1, "Classic card game", 500m, 10m, "Blackjack", 2 },
                    { 2, "Most popular poker variant", 1000m, 20m, "Texas Hold'em", 1 },
                    { 3, "European single-zero roulette", 300m, 5m, "Roulette", 3 },
                    { 4, "Progressive jackpot slots", 50m, 1m, "Lucky Slots", 0 }
                });

            migrationBuilder.InsertData(
                table: "Players",
                columns: new[] { "Id", "Balance", "DateOfBirth", "Email", "FirstName", "LastName" },
                values: new object[,]
                {
                    { 1, 1500m, new DateTime(1990, 5, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "marko@mail.com", "Marko", "Horvat" },
                    { 2, 800m, new DateTime(1995, 8, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "ana@mail.com", "Ana", "Kovač" },
                    { 3, 3200m, new DateTime(1988, 3, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "ivan@mail.com", "Ivan", "Babić" },
                    { 4, 600m, new DateTime(1993, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "petra@mail.com", "Petra", "Novak" },
                    { 5, 5000m, new DateTime(1985, 6, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "tomislav@mail.com", "Tomislav", "Jurić" }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "CasinoId", "FirstName", "LastName", "Position" },
                values: new object[,]
                {
                    { 1, 1, "Luka", "Perić", "Dealer" },
                    { 2, 1, "Sara", "Blažić", "Manager" },
                    { 3, 1, "Josip", "Matić", "Security" },
                    { 4, 2, "Maja", "Šimić", "Dealer" },
                    { 5, 2, "Darko", "Vukić", "Manager" },
                    { 6, 2, "Nikolina", "Čović", "Cashier" },
                    { 7, 3, "Bruno", "Knežević", "Dealer" },
                    { 8, 3, "Ivana", "Turić", "Manager" },
                    { 9, 3, "Ante", "Grgić", "Security" }
                });

            migrationBuilder.InsertData(
                table: "Tables",
                columns: new[] { "Id", "CasinoId", "GameId", "IsAvailable", "MaxBet", "MinBet", "TableNumber" },
                values: new object[,]
                {
                    { 1, 1, 1, true, 500m, 10m, 1 },
                    { 2, 1, 2, false, 1000m, 20m, 2 },
                    { 3, 1, 3, true, 300m, 5m, 3 },
                    { 4, 2, 2, true, 1000m, 20m, 1 },
                    { 5, 2, 3, true, 300m, 5m, 2 },
                    { 6, 2, 4, false, 50m, 1m, 3 },
                    { 7, 3, 1, true, 500m, 10m, 1 },
                    { 8, 3, 4, true, 50m, 1m, 2 },
                    { 9, 3, 2, false, 1000m, 20m, 3 }
                });

            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "Amount", "CreatedAt", "PlayerId", "Type" },
                values: new object[,]
                {
                    { 1, 500m, new DateTime(2024, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 0 },
                    { 2, 200m, new DateTime(2024, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 2 },
                    { 3, 450m, new DateTime(2024, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 3 },
                    { 4, 300m, new DateTime(2024, 2, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 0 },
                    { 5, 150m, new DateTime(2024, 2, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 2 },
                    { 6, 100m, new DateTime(2024, 2, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 1 },
                    { 7, 1000m, new DateTime(2024, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 0 },
                    { 8, 500m, new DateTime(2024, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 2 },
                    { 9, 800m, new DateTime(2024, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "Reservations",
                columns: new[] { "Id", "PlayerId", "ReservedAt", "TableId" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2024, 4, 10, 20, 0, 0, 0, DateTimeKind.Unspecified), 1 },
                    { 2, 2, new DateTime(2024, 4, 10, 21, 0, 0, 0, DateTimeKind.Unspecified), 4 },
                    { 3, 3, new DateTime(2024, 4, 11, 18, 0, 0, 0, DateTimeKind.Unspecified), 7 },
                    { 4, 1, new DateTime(2024, 4, 12, 19, 0, 0, 0, DateTimeKind.Unspecified), 5 },
                    { 5, 5, new DateTime(2024, 4, 12, 22, 0, 0, 0, DateTimeKind.Unspecified), 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Casinos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Casinos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Casinos",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
