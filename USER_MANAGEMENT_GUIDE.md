# User Management & Password-to-Club Linking Guide

## 🔗 How Passwords Link to Clubs

### Database Structure

The relationship between users, passwords, and clubs is managed through the database:

```
Users Table:
├── Id (Primary Key)
├── Username (unique)
├── PasswordHash (BCrypt hashed password)
├── Role ("ClubCaptain" or "Federation")
└── ClubId (Foreign Key → Clubs.Id)
    ├── NULL = Federation user (can access all clubs)
    └── Integer = Specific club (can only access that club)
```

### How It Works

1. **User Creation:**
   - When creating a user, you specify:
     - Username
     - Password (which gets hashed with BCrypt)
     - Role (ClubCaptain or Federation)
     - Club Code (e.g., "CLB1", "CLB2", "FEDR")
   
2. **Club Linking:**
   - The system looks up the Club by its 4-character ClubCode
   - The User's `ClubId` is set to the Club's `Id` (integer)
   - For Federation users, `ClubId` is set to NULL

3. **Login Process:**
   - User enters username and password
   - System verifies password against stored hash
   - If valid, creates a session with claims:
     - Username
     - UserId
     - Role
     - ClubId (if Club Captain)

4. **Authorization:**
   - Club Captains: Can only access data where `ClubId` matches their assigned club
   - Federation: Can access all clubs (ClubId is NULL, so no restriction)

## 📝 User Management Options

### Option 1: Web Interface (Recommended)

**Location:** `/Users` page (Federation only)

**Features:**
- Create individual users through form
- Import multiple users from CSV or JSON file
- View all users in a table
- Delete users
- Download template files for import

**How to Use:**
1. Login as Federation user
2. Navigate to "Users" in the navigation menu
3. Use "Create New User" form or "Import Users from File"

### Option 2: CSV Import

**Format:**
```csv
Username,Password,ClubCode,Role
club1_captain,SecurePass123,CLB1,ClubCaptain
club2_captain,SecurePass123,CLB2,ClubCaptain
federation_user,FedPass123,FEDR,Federation
```

**Steps:**
1. Create CSV file with above format
2. Login as Federation
3. Go to Users page
4. Click "Download CSV Template" to get the exact format
5. Fill in your users
6. Upload and import

### Option 3: JSON Import

**Format:**
```json
{
  "users": [
    {
      "Username": "club1_captain",
      "Password": "SecurePass123",
      "ClubCode": "CLB1",
      "Role": "ClubCaptain"
    },
    {
      "Username": "federation_user",
      "Password": "FedPass123",
      "ClubCode": "FEDR",
      "Role": "Federation"
    }
  ]
}
```

**Steps:**
1. Create JSON file with above format
2. Login as Federation
3. Go to Users page
4. Click "Download JSON Template" to get the exact format
5. Fill in your users
6. Upload and import

## 🔐 Security Considerations

### Password Protection

1. **File Storage (If using file import):**
   - Store import files securely on the server
   - Use file system permissions to restrict access
   - Consider encrypting files containing passwords
   - Delete files after import

2. **Password Best Practices:**
   - Use strong passwords (min 8 characters, mix of letters, numbers, symbols)
   - Never share passwords in plain text
   - Change default passwords immediately
   - Consider password expiration policies

3. **Server Security:**
   - Restrict file upload directory permissions
   - Validate file types and sizes
   - Scan uploaded files for malware
   - Use HTTPS in production

## 📋 Example: Creating Users for 10 Clubs

### Using Web Interface:

1. Login as Federation
2. Go to Users page
3. For each club, fill in:
   - Username: `club1_captain`, `club2_captain`, etc.
   - Password: (choose secure password)
   - Role: `ClubCaptain`
   - Club Code: `CLB1`, `CLB2`, etc.
4. Click "Create User" for each

### Using CSV Import:

Create `users.csv`:
```csv
Username,Password,ClubCode,Role
club1_captain,MySecurePass1!,CLB1,ClubCaptain
club2_captain,MySecurePass2!,CLB2,ClubCaptain
club3_captain,MySecurePass3!,CLB3,ClubCaptain
club4_captain,MySecurePass4!,CLB4,ClubCaptain
club5_captain,MySecurePass5!,CLB5,ClubCaptain
club6_captain,MySecurePass6!,CLB6,ClubCaptain
club7_captain,MySecurePass7!,CLB7,ClubCaptain
club8_captain,MySecurePass8!,CLB8,ClubCaptain
club9_captain,MySecurePass9!,CLB9,ClubCaptain
club10_captain,MySecurePass10!,CLB10,ClubCaptain
```

Then import via Users page.

## 🎯 Key Points

1. **Club Code is the Link:**
   - Users are linked to clubs via the 4-character ClubCode
   - The system automatically resolves ClubCode → ClubId

2. **Federation Users:**
   - Use ClubCode: "FEDR" or leave ClubCode empty
   - ClubId will be NULL
   - Can access all clubs

3. **Club Captains:**
   - Must specify a valid ClubCode (CLB1-CLB10)
   - ClubId is set automatically
   - Can only access their assigned club

4. **Password Security:**
   - Passwords are hashed with BCrypt (never stored in plain text)
   - Original passwords cannot be recovered
   - To change a password, create a new user or implement password reset

## 🚀 Quick Start

1. **First Time Setup:**
   - Run the application (database and seed data created automatically)
   - Login as Federation: `federation` / `Federation@2024`
   - Go to Users page
   - Create or import your club captains

2. **Adding New Users:**
   - Federation can always add users via the Users page
   - Use CSV/JSON import for bulk operations
   - Individual users can be created via the form

3. **Managing Users:**
   - View all users in the Users table
   - Delete users (cannot delete yourself)
   - See last login times

## 💡 Recommendations

1. **For Production:**
   - Remove or change default passwords
   - Implement password complexity requirements
   - Add password reset functionality
   - Consider two-factor authentication
   - Log all user management activities

2. **File Import Workflow:**
   - Federation prepares CSV/JSON file offline
   - File can be password-protected (handled outside the app)
   - Upload file through secure connection
   - Delete file after successful import
   - Verify imported users

3. **Alternative: Protected File on Server**
   - Store import file in a protected directory
   - Use server file permissions (chmod 600)
   - Federation can access via secure file transfer
   - Import through web interface
   - Consider adding a "Import from Server File" option
