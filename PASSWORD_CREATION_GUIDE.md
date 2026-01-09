# Password Creation Guide

## 🔐 How Passwords Are Created Initially

### Method 1: Automatic Seed Data (First Time Setup)

When you first run the application, the system automatically creates initial users with default passwords:

**Location:** `Program.cs` - SeedDataAsync method

**Default Passwords Created:**
- Federation: `federation` / `Federation@2024`
- Club 1: `club1_captain` / `Club1@2024`
- Club 2: `club2_captain` / `Club2@2024`
- ... and so on for clubs 1-10

**To Change These Before First Run:**
1. Open `Program.cs`
2. Find the `SeedDataAsync` method (around line 79)
3. Change the password values:
   ```csharp
   await authService.CreateUserAsync(
       username: "federation",
       password: "YourSecurePassword123!", // CHANGE THIS
       role: "Federation",
       clubId: null
   );
   ```

**⚠️ Important:** Once the database is created, these seed passwords are set. You'll need to:
- Delete the `golf.db` file to recreate, OR
- Change passwords through the User Management interface (see Method 2)

### Method 2: Web Interface (After Initial Setup)

**For Federation Users:**
1. Login as Federation (`federation` / `Federation@2024`)
2. Go to **Users** page
3. Use **"Create New User"** form:
   - Enter Username
   - Enter Password (in plain text - it will be hashed automatically)
   - Select Role
   - Enter Club Code
4. Click "Create User"
5. Password is automatically hashed with BCrypt and stored

**For CSV/JSON Import:**
1. Prepare file with passwords in plain text
2. Upload through Users page
3. System automatically hashes all passwords during import

### Method 3: Programmatic Creation

You can create users programmatically in code:

```csharp
var authService = serviceProvider.GetRequiredService<AuthService>();

await authService.CreateUserAsync(
    username: "newuser",
    password: "MyPassword123!",
    role: "ClubCaptain",
    clubId: 1
);
```

The password is automatically hashed using BCrypt.

## 🔑 Password Requirements

**Current Requirements:**
- Minimum length: 6 characters (set in UserInput validation)
- No maximum length limit
- Can contain any characters

**Recommended Best Practices:**
- Minimum 8-12 characters
- Mix of uppercase, lowercase, numbers, and symbols
- Avoid common words or patterns
- Unique password for each user

## 🛠️ Password Generation Tools

### Option 1: Online Password Generators
- Use secure password generators (e.g., LastPass, 1Password, Bitwarden)
- Generate strong, random passwords
- Store securely for distribution to users

### Option 2: Command Line (macOS/Linux)
```bash
# Generate a random 12-character password
openssl rand -base64 12

# Generate a password with specific requirements
openssl rand -base64 16 | tr -d "=+/" | cut -c1-12
```

### Option 3: PowerShell (Windows)
```powershell
# Generate random password
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 12 | % {[char]$_})
```

### Option 4: Use the Utility Script (See below)

## 📝 Creating Passwords for Multiple Users

### For CSV Import:

Create `users.csv`:
```csv
Username,Password,ClubCode,Role
club1_captain,SecurePass123!,CLB1,ClubCaptain
club2_captain,SecurePass456!,CLB2,ClubCaptain
club3_captain,SecurePass789!,CLB3,ClubCaptain
```

### For JSON Import:

Create `users.json`:
```json
{
  "users": [
    {
      "Username": "club1_captain",
      "Password": "SecurePass123!",
      "ClubCode": "CLB1",
      "Role": "ClubCaptain"
    },
    {
      "Username": "club2_captain",
      "Password": "SecurePass456!",
      "ClubCode": "CLB2",
      "Role": "ClubCaptain"
    }
  ]
}
```

**Important:** Passwords in these files are in **plain text**. They will be hashed when imported.

## 🔒 Password Security Best Practices

1. **Never Store Plain Text Passwords:**
   - Passwords are immediately hashed with BCrypt (work factor 12)
   - Original passwords cannot be recovered
   - Only the hash is stored in database

2. **Distribution:**
   - Send passwords through secure channels
   - Consider requiring password change on first login
   - Use temporary passwords that expire

3. **File Security (for imports):**
   - Delete CSV/JSON files after import
   - Use secure file transfer methods
   - Encrypt files if storing on server

4. **Password Policy:**
   - Enforce strong password requirements
   - Consider password expiration
   - Implement account lockout after failed attempts

## 🚀 Quick Start: Changing Default Passwords

### Step 1: Modify Seed Data (Before First Run)

Edit `Program.cs`, change lines 121 and 131:

```csharp
// Change Federation password
password: "YourNewSecurePassword123!", // Change this

// Change Club Captain passwords
password: $"YourClub{i}Password123!", // Change this pattern
```

### Step 2: Delete Existing Database (If Already Created)

```bash
# Delete the database file
rm golf.db

# Or delete from bin/Debug/net10.0/
rm bin/Debug/net10.0/golf.db
```

### Step 3: Run Application Again

The new passwords will be set when the database is recreated.

## 💡 Alternative: Reset Passwords After Creation

Since passwords are hashed, you cannot retrieve them. Options:

1. **Delete and Recreate User:**
   - Delete the user from Users page
   - Create new user with new password

2. **Add Password Reset Feature:**
   - Implement password reset functionality (future enhancement)
   - Send reset link via email
   - Allow temporary password generation

3. **Manual Database Update:**
   - Use a script to hash new password
   - Update database directly (advanced, not recommended)

## 📋 Example: Creating Initial Passwords

Here's a recommended approach for setting up your initial passwords:

1. **Generate Secure Passwords:**
   ```bash
   # Generate 11 unique passwords (1 Federation + 10 Clubs)
   for i in {1..11}; do
     openssl rand -base64 12 | tr -d "=+/" | cut -c1-12
   done
   ```

2. **Store Securely:**
   - Keep passwords in a secure password manager
   - Or create a secure file: `initial_passwords.txt` (protect with file permissions)

3. **Update Seed Data:**
   - Replace default passwords in `Program.cs`
   - Or use CSV import after first login

4. **Distribute to Users:**
   - Send passwords through secure channel
   - Request users change on first login
   - Consider implementing password change feature
