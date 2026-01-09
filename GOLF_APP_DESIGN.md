# Golf Application - Authentication & Authorization Design

## Overview
Converting the Contacts application to a Golf Management System with role-based access control for 10 clubs and a Federation.

## Database Schema Recommendations

### 1. Club Table (`Clubs`)
```
- Id: int (Primary Key, Auto-increment)
- ClubCode: string(4) - Unique, 4 characters (e.g., "CLB1", "FEDR")
- ClubId: string(6) - Unique, 6 digits (e.g., "000001", "999999" for Federation)
- LongName: string(30) - Full club name
- NumberOfPlayers: int - Current player count
- CreatedAt: DateTime - Record creation timestamp
```

**Suggestions:**
- Federation could have ClubCode: "FEDR" and ClubId: "000000" or "999999" (special code)
- ClubId as string(6) allows leading zeros and easier formatting
- Consider adding `IsActive: bool` for soft deletion

### 2. User Table (`Users`)
```
- Id: int (Primary Key, Auto-increment)
- Username: string(20) - Unique username for login
- PasswordHash: string - BCrypt hashed password
- Role: string - "ClubCaptain" or "Federation"
- ClubId: int? - Foreign Key to Clubs (nullable for Federation)
- IsActive: bool - Enable/disable user access
- CreatedAt: DateTime
- LastLoginAt: DateTime?
```

**Suggestions:**
- ClubId is nullable: NULL = Federation user, int = Specific club
- Role field allows future expansion (e.g., "ClubSecretary", "TournamentOrganizer")
- Consider adding `Email: string` for notifications
- Consider adding `FullName: string` for display purposes

### 3. Additional Considerations

**Password Hashing:**
- Use BCrypt.Net-Next (NuGet package: `BCrypt.Net-Next`)
- Work factor of 12-13 is recommended (good balance of security/performance)
- Store only the hash, never plain passwords

**Authentication Flow:**
1. User submits username/password on Login page
2. System looks up user by username
3. Verify password using BCrypt.Verify()
4. Create authentication cookie with claims:
   - Username
   - UserId
   - Role (ClubCaptain/Federation)
   - ClubId (or null for Federation)
5. Redirect to appropriate dashboard

**Authorization Logic:**
- **Club Captain:** Can only access data where ClubId matches their assigned ClubId
- **Federation:** Can access all clubs' data (ClubId is null/ignored)
- Use Authorization attributes and custom authorization handlers

## Implementation Approach

### Phase 1: Database Models & Setup
- Create Club and User models
- Update DbContext
- Create migration
- Seed initial data (10 clubs + Federation, default users)

### Phase 2: Authentication Infrastructure
- Install BCrypt.Net-Next
- Configure Cookie Authentication in Program.cs
- Create authentication service/helper
- Create login/logout pages

### Phase 3: Authorization
- Create custom authorization policies
- Implement Club-based authorization
- Protect pages/routes
- Create authorization helpers

### Phase 4: UI Updates
- Create Login page
- Update navigation (show user info, logout button)
- Create protected dashboard pages
- Add access denied page

## Security Best Practices

1. **Password Storage:**
   - Always hash passwords (never store plain text)
   - Use BCrypt with appropriate work factor
   - Validate password strength (min length, complexity)

2. **Session Management:**
   - Use secure, HttpOnly cookies
   - Implement session timeout
   - Consider sliding expiration

3. **Authorization:**
   - Verify authorization on every request
   - Never trust client-side data for authorization
   - Use server-side validation

4. **SQL Injection:**
   - Use Entity Framework (parameterized queries)
   - Never concatenate user input into SQL

5. **XSS Protection:**
   - Always encode user input in views
   - Use Razor's automatic encoding

## Recommended Changes to Current Structure

1. Rename `Contact` model → `Player` or `GolfPlayer` (future)
2. Update database name from `contacts.db` → `golf.db`
3. Create separate service layer for authentication logic
4. Consider creating ViewModels separate from Models

## Sample Data Structure

### Federation User
```
Username: "federation"
Password: (hashed)
Role: "Federation"
ClubId: null
```

### Club Captain Example
```
Username: "club001_captain"
Password: (hashed)
Role: "ClubCaptain"
ClubId: 1 (references Club with ClubCode "CLB1")
```

## Next Steps
1. Review and approve this design
2. Implement models and database
3. Set up authentication
4. Create login functionality
5. Implement authorization
6. Test with sample data
