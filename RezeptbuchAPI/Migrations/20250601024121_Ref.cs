using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezeptbuchAPI.Migrations
{
    /// <inheritdoc />
    public partial class Ref : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categories",
                table: "Recipes");

            migrationBuilder.AddColumn<string>(
                name: "RecipeHash",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_RecipeHash",
                table: "Categories",
                column: "RecipeHash");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Recipes_RecipeHash",
                table: "Categories",
                column: "RecipeHash",
                principalTable: "Recipes",
                principalColumn: "Hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Recipes_RecipeHash",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_RecipeHash",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "RecipeHash",
                table: "Categories");

            migrationBuilder.AddColumn<List<string>>(
                name: "Categories",
                table: "Recipes",
                type: "text[]",
                nullable: false);
        }
    }
}
