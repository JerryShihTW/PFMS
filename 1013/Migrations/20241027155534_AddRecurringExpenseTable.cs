using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _1013.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringExpenseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddPrimaryKey(
                name: "PK_RecurringExpense",
                table: "RecurringExpense",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RecurringExpense",
                table: "RecurringExpense");
        }
    }
}
