# Players Data Entry Implementation

## Summary
The Contacts page has been replaced with a Players data entry system. Players are linked to clubs and can be managed through a single-player view interface.

## What Changed

### 1. Model Changes
- **Deleted:** `Models/Contact.cs` (old contact model)
- **Created:** `Models/Player.cs` with fields:
  - `Code`: string(6) - exactly 6 digits, unique globally
  - `Name`: string(30) - max 30 characters, unique globally
  - `Index`: decimal - format nn.n (e.g., 9.4, 21.0)
  - `ClubCode`: string(6) - links to Club.ClubCode

### 2. Database Changes
- **Updated:** `Data/ContactsDbContext.cs`
  - Replaced `DbSet<Contact>` with `DbSet<Player>`
  - Added unique indexes on `Player.Code` and `Player.Name` (global uniqueness)
  - Added index on `Player.ClubCode` for faster queries
- **Updated:** `Models/Club.cs`
  - Added navigation property `ICollection<Player> Players`

### 3. New Service
- **Created:** `Services/PlayerLoaderService.cs`
  - `LoadPlayersFromFileAsync()` - imports players from text files
  - `GetAvailablePlayerFiles()` - lists available TeamList*.txt files
  - Parses format: `Code : value; Name : value; Index: value`
  - Handles case-insensitive field names (Code, CODE, Name, NAME, etc.)
  - Validates uniqueness (Code and Name must be unique globally)
  - Updates club's NumberOfPlayers after import

### 4. Pages Changes
- **Deleted:** `Pages/Contacts.cshtml` and `Pages/Contacts.cshtml.cs`
- **Created:** `Pages/Players.cshtml` and `Pages/Players.cshtml.cs`
  - Single-player view (not list view)
  - Displays club short name, long name, and number of players
  - Next/Previous navigation between players
  - Add/Update/Delete functionality
  - Import from server (selects TeamList*.txt files)
  - Exit button to Dashboard

### 5. Navigation Updates
- **Updated:** `Pages/Dashboard.cshtml` - link changed from `/Contacts` to `/Players`
- **Updated:** `Pages/Shared/_layout.cshtml` - navigation link changed from "Contacts" to "Players"

### 6. Service Registration
- **Updated:** `Program.cs` - registered `PlayerLoaderService`

## Features

### Player Data Entry
- **View:** Single player at a time
- **Navigation:** Previous/Next buttons to navigate between players
- **Add:** Create new players (form is empty when no player is selected)
- **Update:** Modify existing players (form is pre-filled)
- **Delete:** Delete players (with confirmation)
- **Import:** Import players from TeamList*.txt files on server

### Import Format
Files must be in format:
```
Code : 107031; Name : Mohamed Radwan; Index: 17 ;
Code : 123456; Name : Niazi Mostafa; Index: 24
Code : 110914; Name : Mohamed Abou El Atta; Index: 6.1
```

- Field names are case-insensitive (Code, CODE, Name, NAME, etc.)
- Spacing is flexible around colons and semicolons
- Code must be exactly 6 digits
- Name must be 30 characters or less (will be truncated)
- Index can be integer or decimal (e.g., 9 or 9.4)
- Missing values will result in import errors

### Validation
- **Code:** Must be exactly 6 digits, unique globally
- **Name:** Must be 30 characters or less, unique globally
- **Index:** Must be a valid decimal number
- **Uniqueness:** Code and Name must be unique across all clubs (not just within club)

### Access Control
- **Club Captains:** Can only access their own club's players
- **Federation:** Cannot access Players page (redirects to Dashboard)
- Club information is automatically displayed based on logged-in user's ClubCode

## Next Steps

### Database Migration
Since the app uses `EnsureCreated()`, you need to **delete the database file** for the new schema to be created:

1. Stop the application (if running)
2. Delete the database file:
   ```bash
   rm golf.db
   # Also delete contacts.db if it exists
   rm contacts.db
   ```
3. Run the application:
   ```bash
   dotnet clean
   dotnet run
   ```

The database will be automatically created with the new Player table.

### Testing
1. Login as a Club Captain (not Federation)
2. Navigate to Players page from Dashboard
3. Test:
   - Add a new player
   - Update an existing player
   - Delete a player
   - Navigate with Previous/Next buttons
   - Import from a TeamList*.txt file

### File Format Notes
The import parser is flexible but expects:
- Field name followed by colon (spacing optional)
- Value (can have spaces)
- Semicolon to end field (optional at end of line)
- Case-insensitive field names

Example valid lines:
```
Code : 107031; Name : Mohamed Radwan; Index: 17 ;
Code:123456;Name:Niazi Mostafa;Index:24
CODE : 110914; NAME : Mohamed Abou El Atta; INDEX: 6.1
```

## Files Created/Modified

### Created
- `Models/Player.cs`
- `Services/PlayerLoaderService.cs`
- `Pages/Players.cshtml`
- `Pages/Players.cshtml.cs`

### Modified
- `Data/ContactsDbContext.cs`
- `Models/Club.cs`
- `Program.cs`
- `Pages/Dashboard.cshtml`
- `Pages/Shared/_layout.cshtml`

### Deleted
- `Models/Contact.cs`
- `Pages/Contacts.cshtml`
- `Pages/Contacts.cshtml.cs`
