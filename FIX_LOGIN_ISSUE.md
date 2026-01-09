# Fix Login Issue - Step by Step

## Problem
- Login page might show old version (browser cache)
- All logins are considered Federation
- Load buttons not visible

## Root Cause
The database (`golf.db`) likely has old schema/data from before we changed to 6-character ClubCode. The database needs to be deleted and recreated.

## Solution - Complete Reset

### Step 1: Stop the Application
- Press Ctrl+C in terminal if app is running

### Step 2: Delete Database File
```bash
cd /Users/mohamedelafifi/ContactsRazor
rm -f golf.db
rm -f bin/Debug/net10.0/golf.db
find . -name "golf.db*" -type f -delete
```

### Step 3: Clear Browser Cache
- Press Ctrl+Shift+Delete (or Cmd+Shift+Delete on Mac)
- Clear cached files
- Or use Incognito/Private mode

### Step 4: Run Application
```bash
dotnet run
```

### Step 5: What Happens
1. Database is created with NEW schema (ClubCode = 6 characters)
2. Seed data creates ONLY Federation user:
   - Username: `federe`
   - Password: `Federation@2026`
   - ClubCode: `FEDERE`

### Step 6: Login as Federation
- Go to: http://localhost:5139/Login (or port shown)
- Username: `federe`
- Password: `Federation@2026`
- Should login successfully

### Step 7: Create Configuration Files
Create these files in root directory:

**clubs.txt:**
```
CLB001|Golf Club Number One
CLB002|Golf Club Number Two
CLB003|Golf Club Number Three
CLB004|Golf Club Number Four
CLB005|Golf Club Number Five
CLB006|Golf Club Number Six
CLB007|Golf Club Number Seven
CLB008|Golf Club Number Eight
CLB009|Golf Club Number Nine
CLB010|Golf Club Number Ten
FEDERE|Federation
```

**users.txt:**
```
federe|Federation@2026|FEDERE
club001_captain|Password123|CLB001
club002_captain|Password123|CLB002
club003_captain|Password123|CLB003
club004_captain|Password123|CLB004
club005_captain|Password123|CLB005
club006_captain|Password123|CLB006
club007_captain|Password123|CLB007
club008_captain|Password123|CLB008
club009_captain|Password123|CLB009
club010_captain|Password123|CLB010
```

### Step 8: Load Files
1. After logging in as Federation, you should see Dashboard
2. Look for "Federation Configuration" section with two buttons:
   - **📁 Load Clubs** button
   - **👥 Load Users** button
3. Click "Load Clubs" first
4. Then click "Load Users"
5. You should see success messages

### Step 9: Test Club Captain Login
- Logout
- Login with: `club001_captain` / `Password123`
- Should login as Club Captain (not Federation)
- Load buttons should NOT be visible

## If Still Not Working

### Check 1: Verify Database Schema
The Users table should have:
- Id (integer)
- Username (text, 20 chars)
- PasswordHash (text)
- ClubCode (text, 6 chars) ← Must be 6 characters!

### Check 2: Verify User Data
After loading, check Users table:
- Federation user should have ClubCode = "FEDERE"
- Club captains should have ClubCode = "CLB001", "CLB002", etc.

### Check 3: Clear Browser Cache
- Hard refresh: Ctrl+F5 (or Cmd+Shift+R on Mac)
- Or use Incognito mode

### Check 4: Check Console Output
When you run `dotnet run`, look for any errors about:
- Database schema mismatches
- User creation errors
- File loading errors

## Quick Fix Script

Run these commands in sequence:

```bash
cd /Users/mohamedelafifi/ContactsRazor
rm -f golf.db
rm -f bin/Debug/net10.0/golf.db
dotnet clean
dotnet build
dotnet run
```

Then:
1. Login as `federe` / `Federation@2026`
2. Create clubs.txt and users.txt files
3. Click Load buttons on Dashboard
