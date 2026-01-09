# Quick Fix Instructions

## Issue: Login always shows as Federation / Load buttons not visible

## Solution:

### 1. Delete Database (Already Done ✅)
The database has been deleted. Now you need to:

### 2. Run Application
```bash
dotnet run
```

### 3. What Happens on First Run
- New database created with correct schema (ClubCode = 6 characters)
- Only ONE user is created automatically:
  - Username: `federe`
  - Password: `Federation@2026`
  - ClubCode: `FEDERE` (Federation)

### 4. Login as Federation
- Go to Login page
- Username: `federe`
- Password: `Federation@2026`
- You should see Dashboard with **"Load Clubs"** and **"Load Users"** buttons

### 5. Create Configuration Files

Create `clubs.txt` in root directory:
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

Create `users.txt` in root directory:
```
federe|Federation@2026|FEDERE
club001_captain|Password123|CLB001
club002_captain|Password456|CLB002
club003_captain|Password789|CLB003
club004_captain|Password012|CLB004
club005_captain|Password345|CLB005
club006_captain|Password678|CLB006
club007_captain|Password901|CLB007
club008_captain|Password234|CLB008
club009_captain|Password567|CLB009
club010_captain|Password890|CLB010
```

### 6. Load Files
1. Click **"📁 Load Clubs"** button
2. Wait for success message
3. Click **"👥 Load Users"** button
4. Wait for success message

### 7. Test Club Captain Login
1. Logout
2. Login with: `club001_captain` / `Password123`
3. Should login as **Club Captain** (not Federation)
4. Load buttons should **NOT** be visible

## Why It Was Always Federation

**Before:** Database had old schema or only Federation user existed.

**Now:** 
- Database is fresh with correct schema
- Only Federation user created on first run
- You must load clubs.txt and users.txt to create other users
- After loading, Club Captains will have different ClubCode values

## Important Notes

1. **Only Federation can load files** - Load buttons only appear for Federation users
2. **Load order matters** - Load clubs first, then users
3. **File location** - Files must be in root directory (same as Program.cs)
4. **File format** - Use pipe (|) separator, exactly as shown above

## If Still Not Working

### Clear Browser Cache
- Hard refresh: Ctrl+F5 (Windows) or Cmd+Shift+R (Mac)
- Or use Incognito/Private browsing mode

### Check File Names
- Must be exactly: `clubs.txt` and `users.txt` (not .example)
- Must be in root directory: `/Users/mohamedelafifi/ContactsRazor/`

### Verify Database
After loading, you should have:
- 11 clubs in database (CLB001-CLB010 + FEDERE)
- 11 users in database (federe + 10 club captains)
