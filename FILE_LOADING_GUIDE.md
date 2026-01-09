# File Loading System - User Guide

## Overview
The application now supports loading clubs and users from text files stored in the root directory. Only Federation users can load these files.

## File Formats

### clubs.txt Format
**Location:** Root directory (`/Users/mohamedelafifi/ContactsRazor/clubs.txt`)

**Format:** Pipe-separated values, one per line
```
CLUBCODE|LONGNAME
```

**Example:**
```
CLB001|Golf Club Number One
CLB002|Golf Club Number Two
CLB003|Golf Club Number Three
...
FEDERE|Federation
```

**Rules:**
- ClubCode: Exactly 6 characters (e.g., "CLB001", "FEDERE")
- LongName: Up to 30 characters (will be truncated if longer)
- HM ID and Number of Players: Left blank initially (can be added later)
- Lines starting with # are treated as comments
- Empty lines are ignored

### users.txt Format
**Location:** Root directory (`/Users/mohamedelafifi/ContactsRazor/users.txt`)

**Format:** Pipe-separated values, one per line
```
USERNAME|PASSWORD|CLUBCODE
```

**Example:**
```
federe|Federation@2026|FEDERE
club001_captain|Password123|CLB001
club002_captain|Password456|CLB002
```

**Rules:**
- Username: 3-20 characters
- Password: Will be hashed automatically with BCrypt
- ClubCode: Exactly 6 characters, must match a club in the database (except "FEDERE")
- Federation user must have ClubCode = "FEDERE"
- Lines starting with # are treated as comments
- Empty lines are ignored

## How to Use

### Step 1: Prepare Files
1. Copy `clubs.txt.example` to `clubs.txt` in the root directory
2. Copy `users.txt.example` to `users.txt` in the root directory
3. Edit the files with your actual club and user data

### Step 2: Login as Federation
- Username: `federe`
- Password: `Federation@2026` (from seed data on first run)

### Step 3: Load Files
1. After login, you'll see the Dashboard
2. In the "Federation Configuration" section, you'll see two buttons:
   - **📁 Load Clubs** - Loads clubs from `clubs.txt`
   - **👥 Load Users** - Loads users from `users.txt`
3. Click each button to load the respective file
4. You'll see success/error messages after each load

### Step 4: Verify
- Go to "User Management" page to see loaded users
- Check that clubs are loaded in the database

## Loading Behavior

### Clubs Loading
- **If club exists:** Updates the LongName (preserves existing data)
- **If club is new:** Creates new club with LongName, HM ID and NumberOfPlayers set to null
- **Errors:** Invalid format, duplicate ClubCodes, etc. are reported

### Users Loading
- **If user exists:** Updates password hash and ClubCode
- **If user is new:** Creates new user with hashed password
- **Errors:** Invalid format, club not found, duplicate usernames, etc. are reported

## Important Notes

1. **Load Order:** Always load clubs first, then users (users reference clubs)
2. **Federation Access:** Users with ClubCode = "FEDERE" get access to all clubs
3. **File Location:** Files must be in the application root directory (same level as Program.cs)
4. **Password Security:** Passwords in users.txt are in plain text, but are immediately hashed when loaded
5. **File Updates:** You can reload files multiple times - existing records will be updated

## Example Files

Example files are provided:
- `clubs.txt.example` - Template for clubs
- `users.txt.example` - Template for users

Copy these to `clubs.txt` and `users.txt` and edit as needed.

## Troubleshooting

**File not found:**
- Make sure files are named exactly `clubs.txt` and `users.txt`
- Check they're in the root directory, not in a subfolder

**Club not found error when loading users:**
- Load clubs first before loading users
- Make sure ClubCode in users.txt matches exactly (case-sensitive, but converted to uppercase)

**Invalid format errors:**
- Check that you're using pipe (|) separator, not comma or other character
- Make sure each line has the correct number of fields

**Duplicate errors:**
- Existing clubs/users will be updated, not duplicated
- If you want to completely replace, delete from database first
