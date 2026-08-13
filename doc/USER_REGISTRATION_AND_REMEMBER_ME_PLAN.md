# 🔐 User Registration & Remember Me Authentication Flow Plan — InventoryAlert

> **Document Status**: Execution Plan  
> **Target Version**: 2.7  
> **Last Updated**: August 13, 2026  
> **Scope**: User Registration UI Page, Remember Me Persistent Session Flow, Backend DTO updates, Auto-fill Username, and Navigation Bar Actions.

---

## 🎯 Plan Objectives

1. **User Registration UI & Flow**:
   - Provide a visual, glassmorphic **Register Account Page** (`/register`) matching the application design system.
   - Update `UserNav.tsx` navigation header to display both **Sign In** and **Register** buttons when unauthenticated.
   - Upon registration success, redirect to `/login?registered=true&username=<registered_username>` with auto-filled username.

2. **Remember Me Authentication Flow**:
   - Add a **"Remember Me"** checkbox to `LoginForm` (`/login`).
   - Update backend `LoginRequest` DTO in `InventoryAlert.Domain/DTOs/AuthDTOs.cs` to include `bool RememberMe = false`.
   - Update `AuthService.cs` in `InventoryAlert.Api`:
     - If `RememberMe` is `true`, issue a long-lived **30-day httpOnly refresh token cookie**.
     - If `RememberMe` is `false`, issue a standard **7-day httpOnly refresh token cookie** (or session cookie).
   - In frontend, store `remembered_username` in `localStorage` when "Remember Me" is enabled, so returning users find their username pre-filled.

---

## 📑 Implementation Breakdown

### 🔹 Step 1: Backend DTO & AuthService Updates (`InventoryAlert.Domain` & `InventoryAlert.Api`)
- Update `LoginRequest` record in `AuthDTOs.cs`:
  ```csharp
  public record LoginRequest(string Username, string Password, bool RememberMe = false);
  ```
- Update `LoginAsync` in `AuthService.cs`:
  ```csharp
  var refreshExpiryDays = request.RememberMe ? 30 : (_settings.Jwt.RefreshExpiryDays > 0 ? _settings.Jwt.RefreshExpiryDays : 7);
  var refreshExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays);
  ```

### 🔹 Step 2: Register Page UI Enhancement (`InventoryAlert.UI/src/app/(auth)/register/page.tsx`)
- Enhance layout with glassmorphic cards (`bg-white/60 dark:bg-black/60 backdrop-blur-3xl border border-white/40 dark:border-white/10 rounded-[2.5rem]`).
- Add form validation for matching passwords.
- On success, navigate to `/login?registered=true&username=${encodeURIComponent(username)}`.

### 🔹 Step 3: Login Page & Remember Me UI (`InventoryAlert.UI/src/app/(auth)/login/page.tsx`)
- Add **Remember Me** custom checkbox.
- Check `localStorage.getItem('remembered_username')` on load to auto-populate the username input field if previously saved.
- Pass `{ username, password, rememberMe }` to `/api/v1/auth/login`.
- If `rememberMe` is checked, save `remembered_username` to `localStorage`; if unchecked, clear it.

### 🔹 Step 4: Navigation Bar Header (`InventoryAlert.UI/src/components/UserNav.tsx`)
- Update `UserNav.tsx` unauthenticated state to display both **Sign In** and **Register** actions side-by-side with crisp styling.

---

## 🧪 Verification & Acceptance Criteria

- [ ] Execute `dotnet test` to ensure unit test suite passes with updated `LoginRequest` signature.
- [ ] Verify `RegisterPage` submits `{ username, email, password }` and redirects to `/login` with success banner.
- [ ] Verify checking "Remember Me" persists `remembered_username` in `localStorage` and sets 30-day cookie.
