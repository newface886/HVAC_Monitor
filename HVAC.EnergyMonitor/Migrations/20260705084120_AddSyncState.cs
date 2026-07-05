using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HVAC.EnergyMonitor.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProtocolType = table.Column<int>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    SerialPortName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BaudRate = table.Column<int>(type: "INTEGER", nullable: false),
                    SlaveAddress = table.Column<byte>(type: "INTEGER", nullable: false),
                    ScanIntervalMs = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSyncedRowId = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Points",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FunctionCode = table.Column<int>(type: "INTEGER", nullable: false),
                    RegisterAddress = table.Column<int>(type: "INTEGER", nullable: false),
                    DataType = table.Column<int>(type: "INTEGER", nullable: false),
                    ByteOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Scale = table.Column<double>(type: "REAL", nullable: false),
                    Offset = table.Column<double>(type: "REAL", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    HighLimit = table.Column<double>(type: "REAL", nullable: true),
                    LowLimit = table.Column<double>(type: "REAL", nullable: true),
                    StoreHistory = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Points", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Points_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlarmRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PointId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlarmType = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggerValue = table.Column<double>(type: "REAL", nullable: false),
                    LimitValue = table.Column<double>(type: "REAL", nullable: false),
                    TriggerTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Acknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                    AckTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlarmRecords_Points_PointId",
                        column: x => x.PointId,
                        principalTable: "Points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlarmRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PointId = table.Column<int>(type: "INTEGER", nullable: false),
                    HighLimit = table.Column<double>(type: "REAL", nullable: true),
                    LowLimit = table.Column<double>(type: "REAL", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlarmRules_Points_PointId",
                        column: x => x.PointId,
                        principalTable: "Points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PointValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PointId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quality = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointValues_Points_PointId",
                        column: x => x.PointId,
                        principalTable: "Points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmRecords_PointId",
                table: "AlarmRecords",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmRecords_TriggerTime",
                table: "AlarmRecords",
                column: "TriggerTime");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmRules_PointId",
                table: "AlarmRules",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_Points_DeviceId",
                table: "Points",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PointValues_PointId_Timestamp",
                table: "PointValues",
                columns: new[] { "PointId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncStates_TableName",
                table: "SyncStates",
                column: "TableName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmRecords");

            migrationBuilder.DropTable(
                name: "AlarmRules");

            migrationBuilder.DropTable(
                name: "PointValues");

            migrationBuilder.DropTable(
                name: "SyncStates");

            migrationBuilder.DropTable(
                name: "Points");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
