using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fauzora.Migrations
{
    /// <inheritdoc />
    public partial class AdminAccountManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "users",
                type: "boolean",
                nullable: false,
                                defaultValue: true);

                        migrationBuilder.Sql("""
                                INSERT INTO "user_roles" ("UserId", "RoleId")
                                SELECT legacy_membership."UserId", admin_role."Id"
                                FROM "user_roles" AS legacy_membership
                                INNER JOIN "roles" AS legacy_role ON legacy_role."Id" = legacy_membership."RoleId"
                                INNER JOIN "roles" AS admin_role ON admin_role."Name" = 'Admin'
                                WHERE legacy_role."Name" = 'SuperAdmin'
                                ON CONFLICT ("UserId", "RoleId") DO NOTHING;

                                DELETE FROM "user_roles" AS membership
                                USING "roles" AS role
                                WHERE membership."RoleId" = role."Id"
                                    AND role."Name" = 'SuperAdmin';

                                DELETE FROM "user_roles" AS membership
                                USING "roles" AS role
                                WHERE membership."RoleId" = role."Id"
                                    AND role."Name" IN ('Customer', 'Seller')
                                    AND EXISTS (
                                            SELECT 1
                                            FROM "user_roles" AS admin_membership
                                            INNER JOIN "roles" AS admin_role ON admin_role."Id" = admin_membership."RoleId"
                                            WHERE admin_membership."UserId" = membership."UserId"
                                                AND admin_role."Name" = 'Admin'
                                    );

                                DELETE FROM "roles" WHERE "Name" = 'SuperAdmin';
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
                        migrationBuilder.Sql("""
                                INSERT INTO "roles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
                                SELECT 'legacy-super-admin-role', 'SuperAdmin', 'SUPERADMIN', 'admin-account-migration-rollback'
                                WHERE NOT EXISTS (SELECT 1 FROM "roles" WHERE "Name" = 'SuperAdmin');

                                INSERT INTO "user_roles" ("UserId", "RoleId")
                                SELECT admin_membership."UserId", legacy_role."Id"
                                FROM "user_roles" AS admin_membership
                                INNER JOIN "roles" AS admin_role ON admin_role."Id" = admin_membership."RoleId"
                                CROSS JOIN "roles" AS legacy_role
                                WHERE admin_role."Name" = 'Admin'
                                    AND legacy_role."Name" = 'SuperAdmin'
                                ON CONFLICT ("UserId", "RoleId") DO NOTHING;
                                """);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "users");
        }
    }
}
