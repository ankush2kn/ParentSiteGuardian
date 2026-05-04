using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParentSiteGuardian.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_config",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_config", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "children",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_children", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    MachineFingerprint = table.Column<string>(type: "TEXT", nullable: true),
                    WindowsUserSid = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_devices_children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_groups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_groups_children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_limits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    SiteGroupId = table.Column<string>(type: "TEXT", nullable: false),
                    LimitSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_limits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_limits_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_limits_site_groups_SiteGroupId",
                        column: x => x.SiteGroupId,
                        principalTable: "site_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "overrides",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    SiteGroupId = table.Column<string>(type: "TEXT", nullable: false),
                    ExtraSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overrides_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_overrides_site_groups_SiteGroupId",
                        column: x => x.SiteGroupId,
                        principalTable: "site_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_rules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SiteGroupId = table.Column<string>(type: "TEXT", nullable: false),
                    Pattern = table.Column<string>(type: "TEXT", nullable: false),
                    PatternType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_rules_site_groups_SiteGroupId",
                        column: x => x.SiteGroupId,
                        principalTable: "site_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_events",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    SiteGroupId = table.Column<string>(type: "TEXT", nullable: false),
                    Browser = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usage_events_children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usage_events_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usage_events_site_groups_SiteGroupId",
                        column: x => x.SiteGroupId,
                        principalTable: "site_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_limits_DeviceId",
                table: "daily_limits",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_limits_SiteGroupId",
                table: "daily_limits",
                column: "SiteGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_devices_ChildId",
                table: "devices",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_overrides_DeviceId",
                table: "overrides",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_overrides_SiteGroupId",
                table: "overrides",
                column: "SiteGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_site_groups_ChildId",
                table: "site_groups",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_site_rules_SiteGroupId",
                table: "site_rules",
                column: "SiteGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_ChildId",
                table: "usage_events",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_DeviceId",
                table: "usage_events",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_SiteGroupId",
                table: "usage_events",
                column: "SiteGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_config");

            migrationBuilder.DropTable(
                name: "daily_limits");

            migrationBuilder.DropTable(
                name: "overrides");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "site_rules");

            migrationBuilder.DropTable(
                name: "usage_events");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "site_groups");

            migrationBuilder.DropTable(
                name: "children");
        }
    }
}
