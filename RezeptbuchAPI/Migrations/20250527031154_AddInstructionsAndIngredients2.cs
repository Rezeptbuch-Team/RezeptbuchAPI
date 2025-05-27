using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezeptbuchAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructionsAndIngredients2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Recipes\" ALTER COLUMN \"CookingTime\" TYPE integer USING \"CookingTime\"::integer;"
            );
            migrationBuilder.Sql(
                "ALTER TABLE \"Ingredients\" ALTER COLUMN \"Amount\" TYPE integer USING \"Amount\"::integer;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CookingTime",
                table: "Recipes",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "Ingredients",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
