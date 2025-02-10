using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassNotes.API.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles_claims",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roles_claims_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "security",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    grading_period = table.Column<int>(type: "int", nullable: false),
                    max_score = table.Column<float>(type: "real", nullable: false),
                    qualification_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    course_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attendances",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    attended = table.Column<bool>(type: "bit", nullable: false),
                    registration_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    course_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    student_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "centers",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    abbreviation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    logo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    is_archived = table.Column<bool>(type: "bit", nullable: false),
                    teacher_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_centers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "course_notes",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    registration_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    use_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    course_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    section = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    code = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    center_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    setting_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.id);
                    table.ForeignKey(
                        name: "FK_courses_centers_center_id",
                        column: x => x.center_id,
                        principalSchema: "dbo",
                        principalTable: "centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "courses_settings",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    score_type = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    unit = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    minimum_grade = table.Column<float>(type: "real", nullable: false),
                    minimum_attendance_time = table.Column<int>(type: "int", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    resfesh_token = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    resfesh_token_expire = table.Column<DateTime>(type: "datetime2", nullable: false),
                    default_course_setting_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseSettingEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_courses_settings_CourseSettingEntityId",
                        column: x => x.CourseSettingEntityId,
                        principalSchema: "dbo",
                        principalTable: "courses_settings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_users_courses_settings_default_course_setting_id",
                        column: x => x.default_course_setting_id,
                        principalSchema: "dbo",
                        principalTable: "courses_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "students",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    teacher_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.id);
                    table.ForeignKey(
                        name: "FK_students_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_students_users_teacher_id",
                        column: x => x.teacher_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_students_users_updated_by",
                        column: x => x.updated_by,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users_claims",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_claims_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users_logins",
                schema: "security",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_users_logins_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users_roles",
                schema: "security",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_users_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "security",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_users_roles_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users_tokens",
                schema: "security",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_users_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "students_activities_notes",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    student_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    activity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    note = table.Column<float>(type: "real", nullable: false),
                    feedback = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students_activities_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_students_activities_notes_activities_activity_id",
                        column: x => x.activity_id,
                        principalSchema: "dbo",
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_students_activities_notes_students_student_id",
                        column: x => x.student_id,
                        principalSchema: "dbo",
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_students_activities_notes_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_students_activities_notes_users_updated_by",
                        column: x => x.updated_by,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "students_courses",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    course_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    student_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    final_note = table.Column<float>(type: "real", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students_courses", x => x.id);
                    table.ForeignKey(
                        name: "FK_students_courses_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "dbo",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_students_courses_students_student_id",
                        column: x => x.student_id,
                        principalSchema: "dbo",
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_students_courses_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_students_courses_users_updated_by",
                        column: x => x.updated_by,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activities_course_id",
                schema: "dbo",
                table: "activities",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_created_by",
                schema: "dbo",
                table: "activities",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_activities_updated_by",
                schema: "dbo",
                table: "activities",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_course_id",
                schema: "dbo",
                table: "attendances",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_created_by",
                schema: "dbo",
                table: "attendances",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_student_id",
                schema: "dbo",
                table: "attendances",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_updated_by",
                schema: "dbo",
                table: "attendances",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_centers_created_by",
                schema: "dbo",
                table: "centers",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_centers_teacher_id",
                schema: "dbo",
                table: "centers",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_centers_updated_by",
                schema: "dbo",
                table: "centers",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_course_notes_course_id",
                schema: "dbo",
                table: "course_notes",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_notes_created_by",
                schema: "dbo",
                table: "course_notes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_course_notes_updated_by",
                schema: "dbo",
                table: "course_notes",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_courses_center_id",
                schema: "dbo",
                table: "courses",
                column: "center_id");

            migrationBuilder.CreateIndex(
                name: "IX_courses_created_by",
                schema: "dbo",
                table: "courses",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_courses_setting_id",
                schema: "dbo",
                table: "courses",
                column: "setting_id");

            migrationBuilder.CreateIndex(
                name: "IX_courses_updated_by",
                schema: "dbo",
                table: "courses",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_courses_settings_created_by",
                schema: "dbo",
                table: "courses_settings",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_courses_settings_updated_by",
                schema: "dbo",
                table: "courses_settings",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "security",
                table: "roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_roles_claims_RoleId",
                schema: "security",
                table: "roles_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_students_created_by",
                schema: "dbo",
                table: "students",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_students_teacher_id",
                schema: "dbo",
                table: "students",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_updated_by",
                schema: "dbo",
                table: "students",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_students_activities_notes_activity_id",
                schema: "dbo",
                table: "students_activities_notes",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_activities_notes_created_by",
                schema: "dbo",
                table: "students_activities_notes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_students_activities_notes_student_id",
                schema: "dbo",
                table: "students_activities_notes",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_activities_notes_updated_by",
                schema: "dbo",
                table: "students_activities_notes",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_students_courses_course_id",
                schema: "dbo",
                table: "students_courses",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_courses_created_by",
                schema: "dbo",
                table: "students_courses",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_students_courses_student_id",
                schema: "dbo",
                table: "students_courses",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_courses_updated_by",
                schema: "dbo",
                table: "students_courses",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "security",
                table: "users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_users_CourseSettingEntityId",
                schema: "security",
                table: "users",
                column: "CourseSettingEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_users_default_course_setting_id",
                schema: "security",
                table: "users",
                column: "default_course_setting_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "security",
                table: "users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_claims_UserId",
                schema: "security",
                table: "users_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_logins_UserId",
                schema: "security",
                table: "users_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_roles_RoleId",
                schema: "security",
                table: "users_roles",
                column: "RoleId");

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
                name: "FK_activities_users_created_by",
                schema: "dbo",
                table: "activities",
                column: "created_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_activities_users_updated_by",
                schema: "dbo",
                table: "activities",
                column: "updated_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_attendances_courses_course_id",
                schema: "dbo",
                table: "attendances",
                column: "course_id",
                principalSchema: "dbo",
                principalTable: "courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_attendances_students_student_id",
                schema: "dbo",
                table: "attendances",
                column: "student_id",
                principalSchema: "dbo",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_attendances_users_created_by",
                schema: "dbo",
                table: "attendances",
                column: "created_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_attendances_users_updated_by",
                schema: "dbo",
                table: "attendances",
                column: "updated_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_centers_users_created_by",
                schema: "dbo",
                table: "centers",
                column: "created_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_centers_users_teacher_id",
                schema: "dbo",
                table: "centers",
                column: "teacher_id",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_centers_users_updated_by",
                schema: "dbo",
                table: "centers",
                column: "updated_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_course_notes_courses_course_id",
                schema: "dbo",
                table: "course_notes",
                column: "course_id",
                principalSchema: "dbo",
                principalTable: "courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_course_notes_users_created_by",
                schema: "dbo",
                table: "course_notes",
                column: "created_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_course_notes_users_updated_by",
                schema: "dbo",
                table: "course_notes",
                column: "updated_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_courses_users_created_by",
                schema: "dbo",
                table: "courses",
                column: "created_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_courses_users_updated_by",
                schema: "dbo",
                table: "courses",
                column: "updated_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_courses_settings_users_created_by",
                schema: "dbo",
                table: "courses_settings",
                column: "created_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_courses_settings_users_updated_by",
                schema: "dbo",
                table: "courses_settings",
                column: "updated_by",
                principalSchema: "security",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_courses_settings_users_created_by",
                schema: "dbo",
                table: "courses_settings");

            migrationBuilder.DropForeignKey(
                name: "FK_courses_settings_users_updated_by",
                schema: "dbo",
                table: "courses_settings");

            migrationBuilder.DropTable(
                name: "attendances",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "course_notes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "roles_claims",
                schema: "security");

            migrationBuilder.DropTable(
                name: "students_activities_notes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "students_courses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "users_claims",
                schema: "security");

            migrationBuilder.DropTable(
                name: "users_logins",
                schema: "security");

            migrationBuilder.DropTable(
                name: "users_roles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "users_tokens",
                schema: "security");

            migrationBuilder.DropTable(
                name: "activities",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "students",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "courses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "centers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "users",
                schema: "security");

            migrationBuilder.DropTable(
                name: "courses_settings",
                schema: "dbo");
        }
    }
}
