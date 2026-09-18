using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseholdBudget.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentName_Name",
                table: "Categories",
                columns: new[] { "ParentName", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentName_Name",
                table: "Categories");
        }
    }
}
