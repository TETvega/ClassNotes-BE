using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassNotes.API.Migrations
{
    /// <inheritdoc />
    public partial class _20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activities_courses_course_id",
                schema: "dbo",
                table: "activities");

            migrationBuilder.DropForeignKey(
                name: "FK_courses_centers_center_id",
                schema: "dbo",
                table: "courses");

            migrationBuilder.DropForeignKey(
                name: "FK_courses_courses_settings_setting_id",
                schema: "dbo",
                table: "courses");

            migrationBuilder.DropForeignKey(
                name: "FK_users_courses_settings_CourseSettingEntityId",
                schema: "security",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_CourseSettingEntityId",
                schema: "security",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CourseSettingEntityId",
                schema: "security",
                table: "users");

            migrationBuilder.AddForeignKey(
                name: "FK_activities_courses_course_id",
                schema: "dbo",
                table: "activities",
                column: "course_id",
                principalSchema: "dbo",
                principalTable: "courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_courses_centers_center_id",
                schema: "dbo",
                table: "courses",
                column: "center_id",
                principalSchema: "dbo",
                principalTable: "centers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_courses_courses_settings_setting_id",
                schema: "dbo",
                table: "courses",
                column: "setting_id",
                principalSchema: "dbo",
                principalTable: "courses_settings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activities_courses_course_id",
                schema: "dbo",
                table: "activities");

            migrationBuilder.DropForeignKey(
                name: "FK_courses_centers_center_id",
                schema: "dbo",
                table: "courses");

            migrationBuilder.DropForeignKey(
                name: "FK_courses_courses_settings_setting_id",
                schema: "dbo",
                table: "courses");

            migrationBuilder.AddColumn<Guid>(
                name: "CourseSettingEntityId",
                schema: "security",
                table: "users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_CourseSettingEntityId",
                schema: "security",
                table: "users",
                column: "CourseSettingEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_activities_courses_course_id",
                schema: "dbo",
                table: "activities",
                column: "course_id",
                principalSchema: "dbo",
                principalTable: "courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_courses_centers_center_id",
                schema: "dbo",
                table: "courses",
                column: "center_id",
                principalSchema: "dbo",
                principalTable: "centers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_courses_courses_settings_setting_id",
                schema: "dbo",
                table: "courses",
                column: "setting_id",
                principalSchema: "dbo",
                principalTable: "courses_settings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_users_courses_settings_CourseSettingEntityId",
                schema: "security",
                table: "users",
                column: "CourseSettingEntityId",
                principalSchema: "dbo",
                principalTable: "courses_settings",
                principalColumn: "id");
        }
    }
}
