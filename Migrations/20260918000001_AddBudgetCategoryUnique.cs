using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseholdBudget.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetCategoryUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Budgets_CategoryParentName",
                table: "Budgets",
                column: "CategoryParentName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_CategoryParentName",
                table: "Budgets");
        }
    }
}
