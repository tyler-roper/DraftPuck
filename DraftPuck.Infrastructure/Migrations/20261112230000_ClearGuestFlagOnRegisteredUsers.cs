using DraftPuck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <summary>
    /// Registering without a guest account inserted the user with IsGuest = false, but EF left the column out of the
    /// INSERT (false was its "not set" value) and the database default made the account a guest. The model now sends
    /// false explicitly; this corrects accounts created before that. Guests never have a password, so any user with
    /// one is a registered account.
    /// </summary>
    [DbContext(typeof(DraftPuckContext))]
    [Migration("20261112230000_ClearGuestFlagOnRegisteredUsers")]
    public partial class ClearGuestFlagOnRegisteredUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Users] SET [IsGuest] = 0 WHERE [IsGuest] = 1 AND [IsBot] = 0 AND [PasswordHash] IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: the affected rows can't be told apart from correct ones afterwards, and none should be guests.
        }
    }
}
