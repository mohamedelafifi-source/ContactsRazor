# Quick Start: Creating Initial Passwords

## 🚀 Three Ways to Create Passwords

### Option 1: Use Default Passwords (Development Only)

**Current Default Passwords:**
- Federation: `federation` / `Federation@2024`
- Club 1: `club1_captain` / `Club1@2024`
- Club 2-10: Similar pattern

**⚠️ These are for testing only! Change before production.**

### Option 2: Generate and Import via CSV/JSON

**Step 1: Generate Passwords**

**On macOS/Linux:**
```bash
cd Scripts
./generate-passwords.sh
```

**On Windows (PowerShell):**
```powershell
cd Scripts
.\generate-passwords.ps1
```

This will output:
- Passwords for all users
- CSV format ready for import

**Step 2: Save the CSV output to a file**

**Step 3: Login and Import**
1. Login as Federation (`federation` / `Federation@2024`)
2. Go to Users page
3. Upload the CSV file
4. Users are created with the generated passwords

### Option 3: Create Passwords Manually via Web Interface

1. Login as Federation
2. Go to Users page
3. Fill in "Create New User" form for each user:
   - Username
   - Password (type in plain text - it gets hashed automatically)
   - Role
   - Club Code
4. Click "Create User"

## 🔑 Key Points About Password Creation

### ✅ What Happens Automatically

1. **Password Hashing:** 
   - When you create a user, the password you enter is automatically hashed using BCrypt
   - The original password is never stored - only the hash
   - This happens in `AuthService.CreateUserAsync()`

2. **Hash Storage:**
   - The hashed password is stored in the `PasswordHash` field in the database
   - Original password cannot be recovered from the hash

3. **Password Verification:**
   - During login, the entered password is hashed and compared to the stored hash
   - If they match, login succeeds

### 📝 Example: Creating a Password

```csharp
// In code (Program.cs seed data):
await authService.CreateUserAsync(
    username: "club1_captain",
    password: "MySecurePassword123!",  // ← You type this
    role: "ClubCaptain",
    clubId: 1
);

// What gets stored in database:
// PasswordHash: "$2a$12$abc123xyz..."  // ← BCrypt hash (60+ chars)
// Original password is discarded
```

### 🔒 Security Best Practices

1. **Use Strong Passwords:**
   - Minimum 12-14 characters
   - Mix of uppercase, lowercase, numbers, symbols
   - Avoid dictionary words

2. **One-Time Setup:**
   - Generate passwords for initial setup
   - Provide to users securely
   - Require password change on first login (future feature)

3. **Distribution:**
   - Send passwords through secure channels
   - Never email passwords in plain text
   - Use encrypted communication

## 📋 Step-by-Step: First Time Setup

### Scenario: Setting up passwords for the first time

**Option A: Change Default Passwords Before First Run**

1. Open `Program.cs`
2. Find `SeedDataAsync` method
3. Change password values (lines 121, 131):
   ```csharp
   password: "YourNewSecurePassword123!", // Change this
   ```
4. Delete `golf.db` if it exists
5. Run application - new passwords are set

**Option B: Use Generated Passwords and Import**

1. Run password generator script
2. Copy the CSV output
3. Save to `users.csv` file
4. Run application (creates default users)
5. Login as Federation
6. Go to Users page
7. Delete default users (optional)
8. Import `users.csv`
9. New users created with generated passwords

**Option C: Manual Creation Through Web Interface**

1. Run application (creates default users)
2. Login as Federation
3. Go to Users page
4. For each user:
   - Enter username
   - Enter password (type it - it will be hashed)
   - Select role and club
   - Click Create
5. Delete default users (optional)

## 🛠️ Password Generator Usage

### Generate Passwords Now:

**macOS/Linux:**
```bash
cd /Users/mohamedelafifi/ContactsRazor
./Scripts/generate-passwords.sh > initial_passwords.csv
```

**Windows:**
```powershell
cd C:\path\to\ContactsRazor
.\Scripts\generate-passwords.ps1 | Out-File initial_passwords.csv
```

The output file will contain:
- Username, Password, ClubCode, Role format
- Ready to import through Users page

## ⚠️ Important Reminders

1. **Passwords are hashed automatically** - you don't need to hash them yourself
2. **Original passwords cannot be recovered** - keep a secure record
3. **Change default passwords** before going to production
4. **Store passwords securely** - use a password manager
5. **Delete password files** after importing

## 🔄 Changing Passwords Later

Currently, to change a password:
1. Delete the user from Users page
2. Create new user with new password

**Future Enhancement:** Password reset functionality can be added.
