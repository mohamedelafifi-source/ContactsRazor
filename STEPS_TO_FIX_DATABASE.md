# Steps to Fix Database Schema Issue

## The Problem
You're getting `SQLite Error 1: No Such Column u.ClubCode` because:
- The database was created with the OLD schema (Role, ClubId columns)
- The code now uses NEW schema (ClubCode column only)
- SQLite doesn't automatically update the schema

## Solution: Delete and Recreate Database

### Step-by-Step Instructions

#### Step 1: Stop the Application
- If the app is running, stop it (Ctrl+C or close the terminal)

#### Step 2: Delete the Old Database File
The database file is located in your project root directory:
- **File name:** `golf.db`
- **Location:** `/Users/mohamedelafifi/ContactsRazor/golf.db`

**Option A: Using Terminal (Recommended)**
```bash
cd /Users/mohamedelafifi/ContactsRazor
rm golf.db
```

**Option B: Using File Explorer**
- Navigate to your project folder
- Find `golf.db` file
- Delete it

**Also check these locations:**
- `bin/Debug/net10.0/golf.db` (if exists, delete it too)

#### Step 3: Verify Database File is Deleted
```bash
ls -la golf.db
# Should show: No such file or directory
```

#### Step 4: Run the Application Again
```bash
dotnet run
```

#### Step 5: What Happens Next

When you run the application, this sequence happens automatically:

1. **Application starts** (`Program.cs` runs)

2. **Database Creation** (Line 51 in Program.cs):
   ```csharp
   context.Database.EnsureCreated();
   ```
   - Checks if database exists
   - If NOT exists → Creates new database with CURRENT schema
   - If exists → Does nothing (that's why we deleted it!)

3. **Seed Data** (Line 79-132 in Program.cs):
   ```csharp
   await SeedDataAsync(context, authService);
   ```
   - Checks if clubs exist
   - If clubs don't exist → Creates:
     - Federation club (FEDR)
     - 10 clubs (CLB1-CLB10)
     - Federation user (username: "federation", password: "Federation@2026")
     - 10 club captain users (club1_captain through club10_captain)

4. **Database is Ready!**
   - New schema with ClubCode column
   - All seed data created
   - Ready to use

#### Step 6: Test Login
1. Navigate to: `http://localhost:5139` (or the port shown)
2. Click "Login"
3. Enter:
   - Username: `federation`
   - Password: `Federation@2026`
4. Should login successfully!

## Summary of What Changed

### User Model - Simplified
**Before:**
- Id, Username, PasswordHash, **Role**, **ClubId**, **IsActive**, **CreatedAt**, **LastLoginAt**

**Now:**
- Id, Username, PasswordHash, **ClubCode** (that's it!)

### Database Schema
**New Users Table:**
```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    ClubCode TEXT NOT NULL  -- 4 characters: "FEDR", "CLB1"-"CLB10"
);
```

## Important Notes

1. **Deleting the database is safe** - All data will be recreated from seed
2. **The seed creates default users** - You can change passwords later through Users page
3. **If you have real data** - Export it first before deleting!
4. **For production** - Use migrations instead of EnsureCreated()

## Troubleshooting

**If you still get errors:**
1. Make sure the app is completely stopped
2. Delete ALL `golf.db` files:
   ```bash
   find . -name "golf.db" -type f -delete
   ```
3. Also delete any `.db-shm` and `.db-wal` files (SQLite temporary files)
4. Run `dotnet clean` then `dotnet build`
5. Run `dotnet run` again

**If seed data doesn't create:**
- Check console output for errors
- Make sure no clubs exist already
- The seed only runs if database is empty
