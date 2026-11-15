# User access master plan (authentication and authorization)

Plan only; nothing here is built yet. When a phase starts, copy its scope into `docs/roadmap.md`, and when it is done,
record it there as usual. The owner's answers are in "Owner decisions" at the end.

## Who does what

| Where | Who                               | Does                                                                                                                                                                                         |
| ----- | --------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Panel | SuperAdmin (`PanelUser`)          | Creates accounts and their admin users, decides which features each account and business unit gets, and controls every ERP user (edit, active / inactive, reset password, unlock, sign out). |
| Panel | Implementer (`PanelUser`)         | Sees ERP users (read only). Configures screens as today.                                                                                                                                     |
| ERP   | Account admin (made in the Panel) | Everything in every business unit of the account, including all users, roles and employees.                                                                                                  |
| ERP   | Other users (made in the ERP)     | What their one role allows, per business unit.                                                                                                                                               |
| Both  | Every user                        | Signs in with email or username, changes their own password, and resets a forgotten one with a code sent by email.                                                                           |

**No roles in the Panel.** The Panel gives features to accounts and business units; roles live in the ERP.

Effective access of an ERP user in a business unit:

```
features the account has (Panel)
  ∩ features the business unit has (Panel, new)
  ∩ permissions of the user's role in that business unit (ERP)
```

Account admins skip the last step: every action of every feature the account and the unit have, in every active unit.

## What exists today (phase 1)

- JWT with two audiences (`panel`, `erp`), login and `me` on both surfaces, ERP `select-unit`, `X-Business-Unit` header
  checked on every request against `UserBusinessUnit`.
- `ErpUser` (email unique across accounts, `IsPrimaryAdmin`, `IsActive`, `LastLoginAt`), `PanelUser` (`SuperAdmin`,
  `Implementer`), `Role` + `RolePermission` per account, `UserBusinessUnit` (user → unit → one role).
- Permission catalog from the ERP menu: `{module}.{feature}.{action}`, actions view, create, edit, delete, export,
  import (a feature may declare its own). `.RequirePermission(...)` on endpoints, `useFeatureAccess` in the ERP.
- Account setup creates the system roles Administrator and Viewer and the primary ERP admin.
- Login rate limit per IP, password hashing, same 401 for every login failure.

Gaps: the access token lives 8 hours with no refresh or logout; deactivation does not end tokens on endpoints without
a business unit; no username, lockout, change or forgotten password, or email sending; no user, role or employee
screens; Administrator role rows are written once and miss features added later; no business unit feature access; no
login history.

## Data model changes

| Table / column                                                                                        | Purpose                                                                                                                                  |
| ----------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `ErpUser.Username`                                                                                    | Required, unique across all accounts, lower-case, 3–50 of `a-z 0-9 . _ -`, no `@`. Existing users get one from their email at migration. |
| `ErpUser.Email`                                                                                       | Stays required and unique across all accounts.                                                                                           |
| `ErpUser.IsPrimaryAdmin` → `IsAccountAdmin`                                                           | Renamed; an account may have several admins. Claim `primary_admin` → `account_admin`.                                                    |
| `ErpUser.Phone`, `EmployeeId` (null, unique)                                                          | Contact; link to at most one employee.                                                                                                   |
| `MustChangePassword`, `FailedLoginCount`, `LockedUntil`, `SecurityStamp` on `ErpUser` and `PanelUser` | Must change at next sign-in; lockout; a new stamp ends every token of the user.                                                          |
| `UserSessions`                                                                                        | Refresh tokens: user, surface, token hash, expires, revoked, replaced by, IP, user agent, last used.                                     |
| `PasswordResetCodes`                                                                                  | User, surface, code hash, expires, attempts, used at.                                                                                    |
| `LoginEvents`                                                                                         | Every sign-in and reset attempt: surface, user (if known), login name, success, reason, IP, user agent, time.                            |
| `BusinessUnitFeatures`                                                                                | Features a business unit does **not** get (only exclusions; empty = all account features).                                               |
| `Employees` (account-owned)                                                                           | Typed columns for the General, Personal and Job tabs (see "Employee"), home business unit, active, `CustomData`.                         |
| `Roles` unique `(AccountId, Name)`                                                                    | Role names are unique in an account.                                                                                                     |
| `UserBusinessUnit.RoleId` → nullable, `+ IsActive`                                                    | A user is added to a unit first and gets the role there later; access can be switched off per unit.                                      |

Access changes (user created or edited, status, password reset by the Panel, role permissions, unit and role
assignment) go to the existing `ConfigurationChanges` table with new areas `user` and `role` through
`ConfigurationHistory.Record`, so Panel History shows them without a new table.

**Administrator is computed, not stored.** The system Administrator role always means "every permission the account
and unit have", so new features need no data fix. Viewer stays stored and is re-synced at startup.

## Authentication (both surfaces)

- **Sign-in name.** The ERP login field takes an email or a username (`@` means email). The Panel keeps email only.
- **Tokens.** Access token 15 minutes; refresh token 7 days, stored hashed in `UserSessions` and rotated on every use.
  Reusing an old refresh token revokes the whole chain.
- **Token check per request.** The token carries `SecurityStamp`; a short cached lookup per user rejects a token whose
  stamp changed or whose user or account is inactive. Deactivate, password change, reset and sign out everywhere take
  effect at once.
- **Lockout.** 3 wrong passwords in a row lock the user for 15 minutes (`Auth:LockoutAttempts`,
  `Auth:LockoutMinutes`); a correct password resets the count. This is on top of the IP rate limit. The answer stays the same
  401; the Panel shows "locked" and a SuperAdmin can unlock. A successful password reset also unlocks.
- **Password policy** in config (`Auth:PasswordMinLength`, letter and digit). One validator for setup, create user,
  Panel reset, change and forgotten password.
- **Change password** (signed in): current + new password; ends the other sessions.
- **Must change password.** `MustChangePassword = 1` is set when someone else sets the password (account setup, create
  user, Panel reset); a user who changes it themselves clears it. While it is 1:
  - sign-in works, but the token carries `must_change_password`, and the server answers **403** on every endpoint
    except `me`, `change-password` and `logout`; the unit picker and the menu are not reached;
  - the app shows only the change password page;
  - after a successful change the flag becomes 0, a new token is issued, and the user goes on to the unit picker.
- **Forgot password** (login page link, and **Reset password** in the profile menu, both the same flow):
  1. `POST forgot-password { login }` always answers 200. If the user exists and is active, a 6-digit code is emailed;
     it lasts 10 minutes, allows 5 tries, and a new code needs at least 1 minute.
  2. `POST reset-password { login, code, newPassword }` sets the password, clears `MustChangePassword` and the lockout,
     changes the security stamp and ends every session.
- **Email.** One `IEmailSender` with SMTP (`System.Net.Mail`, settings `Email:*`, no new package). In development and
  E2E, emails are written to a folder (`Email:PickupDirectory`) instead of being sent; E2E reads the code from there.
  The code email is in the language of the request (EN or বাংলা).
- **Endpoints** on `/api/panel/auth` and `/api/erp/auth`: `login` (returns refresh token and `mustChangePassword`),
  `refresh`, `logout`, `change-password`, `forgot-password`, `reset-password`, `me`. All except `me` and `logout` are
  rate limited.
- **Frontend.** `api-client` refreshes once on 401 (one refresh in flight, others wait) and signs out when it fails.
  Login page: **Forgot password?** → code + new password page. Top bar profile menu: **Change password**,
  **Reset password**, **Sign out**.
- **Login history** in `LoginEvents`; `LastLoginAt` stays.

## Panel

New Panel menu item **ERP users** (seeded by `PanelMenuSynchronizer`) and changes to the account pages. Every action
that changes an ERP user needs `PanelRole.SuperAdmin` (a `RequireSuperAdmin` endpoint filter); Implementers see the
pages read only and the buttons are hidden.

### ERP users (`/erp-users`)

- List of every ERP user of every account (`ListPageLayout`): name, username, email, phone, account, business units with
  role names, admin badge, status, locked, last login. Filters: account, business unit (after an account is picked),
  status, admins only. Search by name, username, email or phone. Export.
- Row actions: View and Edit inline; Reset password, Unlock (when locked), Activate / Deactivate login and Sign out
  everywhere in the row menu. Implementers see only View.
- **View** (`/erp-users/{id}`, `DetailsPageLayout`): profile card (avatar, name, Active / Locked tags, username, email,
  mobile, account, employee, last sign-in; actions Edit, Reset password, More › Unlock, Deactivate login, Sign out
  everywhere) and pill tabs: Units and roles (read only), Devices, Sign-in history (last 50), Access history.
- **Edit**: name, username, email, phone.
- **Reset password** (modal): generate (shown once with Copy) or type a password and confirm it; sets
  `MustChangePassword`, unlocks and ends every session.
- An account always keeps at least one active admin.

### Account pages

- **Account setup**: the ERP admin step gets a username field.
- **Users** tab on the account page (`/accounts/{id}`): an "Account admins" card with **Add admin** and an "Other users"
  card (read only, link to ERP users). **Add admin** is a `FormModal`: name, username, email, mobile, password and
  confirm password, with Create and "Create & add another". Users made in the Panel are always account admins; normal
  users are made in the ERP.
- **Feature access per business unit**: the existing menu access tree gets a business unit picker; unticking a
  feature for a unit stores a `BusinessUnitFeatures` row. The ERP menu, the role matrix and permissions follow it.

### Panel API (`/api/panel`)

| Method and path                                                             | Does                                       |
| --------------------------------------------------------------------------- | ------------------------------------------ |
| `GET /erp-users?accountId=&businessUnitId=&status=&search=&page=&pageSize=` | List                                       |
| `GET /erp-users/{id}`                                                       | Detail with units, roles, sessions         |
| `GET /erp-users/{id}/logins`                                                | Login events                               |
| `PUT /erp-users/{id}`                                                       | Name, username, email, phone               |
| `PUT /erp-users/{id}/status`                                                | Active / inactive                          |
| `POST /erp-users/{id}/reset-password`                                       | Generate or set, returns the password once |
| `POST /erp-users/{id}/unlock`                                               | Clears lockout                             |
| `POST /erp-users/{id}/sign-out`                                             | New security stamp, revoke sessions        |
| `POST /accounts/{id}/admins`                                                | Add an account admin                       |
| `GET` / `PUT /accounts/{id}/business-units/{unitId}/features`               | Feature access per unit                    |

## ERP: System Setup › User Management

New sub-module `user-management` in the `system-setup` module (`UserManagementMenu.cs`, `IErpMenuProvider`) with three
screens. Frontend folder `apps/erp-web/src/modules/system-setup/user-management/` with `user`, `employee` and
`user-role` feature folders, each with the standard files.

| Screen    | Feature code             |
| --------- | ------------------------ |
| User      | `system-setup.user`      |
| Employee  | `system-setup.employee`  |
| User Role | `system-setup.user-role` |

Like every screen, each has its own line in the role matrix (who may open and change it). An employee record itself
has no permissions and cannot sign in; only users have a role.

**Screens follow the existing templates** (the "UI and UX standard" in `.claude/skills/erp-feature/SKILL.md`): page
header with Export then New; Filters row, search, All / Active / Inactive views and pagination in lists; View and Edit
as inline row icons, everything else in the row menu; forms save only from the sticky header (Cancel, Save & new,
Save); details pages have the profile card on the left and pill tabs on the right; modals offer "Create & add
another". The approved demo is the design canvas "User Access Screens" (claude.ai artifact
`Jcd9YJ7HYw8yRdfs1g8rQK`).

### User Role

- **List** (`ListPageLayout`): role (system or custom), description, permissions (bar and "24 of 64"), users in the
  selected unit, status. Filter: type. Row actions: Edit inline (View for Administrator); Activate / Deactivate and
  Delete (only custom roles with no users) in the row menu.
- **Form** (`FormPageLayout`): main column has a "Role" card (name, description) and the **Permissions** card; the
  aside has Status (switch) and "Users in <unit>".
- **Permissions tree** (inside the Permissions card):
  - Module › sub-module › screen rows with chevrons to expand and collapse, plus **Expand all** and **Collapse all**.
  - Columns View, Create, Edit, Delete, Export, Import; "—" where a screen has no such action.
  - Every row has a checkbox; module and sub-module checkboxes and their column checkboxes show checked, partly
    checked (dash) or empty, and tick or clear everything below them. Each module and sub-module shows a count
    ("18/36"); the card header shows the total ("24 of 64 selected").
  - A screen's own actions (for example Special discount, Reprint) show as toggle chips under its name.
  - **Search** by module, screen or action name: matches are highlighted and their branches open; no match shows an
    empty state with Clear search. **All screens / Selected only** switch.
  - Ticking any action ticks View; clearing View clears the whole screen. Only screens the account and the selected
    unit have are listed.
- Roles are made once per account and used in every unit; the role list is the same in every unit.
- **Users in <unit>** (aside card): the users that have this role **in the selected unit**, with remove, and
  **Assign users** (modal) that picks from the users of that unit. A user has one role per unit, so assigning replaces
  the user's role in that unit; the modal says which role will be replaced.
- Administrator is read only (all permissions); Viewer can be edited but not deleted.

### Employee

- A configurable screen (skill pattern E: manifest, list, form, view, custom fields), so the Panel can shape it like
  Item Profile. Every field is a typed column ("typed tables stay"). Only code, name and home business unit are
  locked; the rest are optional, and the less common ones are hidden by default so an account turns on what it uses.
- Tabs and fields (from the old system's `Employee` table, cleaned up):

| Tab      | Section | Fields                                                                                                                       |
| -------- | ------- | ---------------------------------------------------------------------------------------------------------------------------- |
| General  | Basic   | Code (empty = generated `EMP-00001`), name EN, name বাংলা, photo, designation, department, department section, employee type |
| General  | Contact | Mobile, email, official contact, emergency mobile                                                                            |
| Personal | Details | Father's name, mother's name, date of birth, gender, religion, blood group, NID                                              |
| Personal | Address | Present address, permanent address, signature image                                                                          |
| Job      | Posting | Home business unit, office, warehouse, location                                                                              |
| Job      | Service | Joining date, confirmation date, termination date, supervisor, line manager (both pick an employee)                          |
| Job      | Other   | Attendance ID, card number, sales representative (yes / no), service staff (yes / no)                                        |

- New account lookup lists (`EmployeeLookups.cs`, `ILookupProvider`, with default values): `designation`,
  `employee-department`, `department-section`, `employee-type`, `gender`, `religion`, `blood-group`.
- Office and warehouse come from the account structure, filtered to the home unit; supervisor and line manager from
  active employees.
- **List** (`ListPageLayout`): employee (avatar, name, code and Bangla name), designation, department, office, mobile,
  joining date, user (`@username` link or "—"), status. Filters: designation, department, office, user. Row actions:
  **View** and **Edit** inline; Make a user (or Open user) and Activate / Deactivate in the row menu.
- **View** (`DetailsPageLayout`): profile card (photo, name, Bangla name, status and designation tags, code, mobile,
  email, unit and office, joining date, linked user; actions Edit and More › Open user, Deactivate) and pill tabs
  General, Personal, Job with read-only cards.
- **Form** (`FormPageLayout`, config driven): pill tabs General, Personal, Job (and a disabled "Payroll & bank · Later"
  tab); aside cards Photo, Status (switch) and **User account**.
- **Is user** switch in the User account card. When on, it shows Username, Password and Confirm password, and Email
  becomes required. Save then creates the employee and its user in one transaction: the user is linked by
  `ErpUser.EmployeeId`, added to the employee's home unit **without a role**, and must change the password at first
  sign-in. When the employee already has a user, the card shows the user instead of the switch. Whether an employee is
  a user comes from `ErpUser.EmployeeId`, not from a flag on the employee.
- **Payroll and bank** (basic, house rent, medical, transport, mobile allowance, others, gross as their sum, cash or
  bank, bank, branch, account name and number, routing number, salary eligible, salary hold, payment hold, PF,
  overtime, leave policy) is a fourth tab **for later**, with the HR / payroll module. Its fields need their own
  action `system-setup.employee.view-salary`, because salary is private.

### User

- A user does **not** need an employee. The link is optional: admins from the Panel and outside people (auditor,
  consultant) have none, while staff are usually made from their employee record.
- **The ERP shows only the selected unit's users.** No "all units" filter. Switching the unit in the top bar
  switches the list.
- **List** (`ListPageLayout`): user (avatar, name, username), email, mobile, employee (link), role in this unit (or a
  "No role yet · Assign" tag that opens Assign role), status in this unit, locked, last sign-in. Filters: role in this
  unit (including "No role yet"), sign-in (locked, never signed in). Row actions: **Edit** inline; Assign role, Add to
  units, Activate / Deactivate (this unit) and Sign out everywhere in the row menu. Account admins show a lock icon and
  a note instead.
- **Create** (`FormPageLayout`): main cards Employee (optional, fills name, email, mobile) and Sign-in details (name,
  username, email, mobile, password, confirm password; the user must change it at first sign-in); aside cards
  **Business units** (checklist, the selected unit ticked, units without User permission disabled) and "After saving"
  (the three steps). Save makes one user and adds them to every chosen unit **without a role**.
- **Assign role** (row action, or the role's Users tab): the role of this user in the selected unit. Each unit gives
  its own role later, from inside that unit.
- **Add to units**: adds an existing user to more units, again without a role. The user then shows in those units'
  lists, waiting for a role.
- **Edit** changes the user's own details (name, username, email, phone, employee) only.
- **Activate / Deactivate** switches the user's access in the selected unit only (`UserBusinessUnit.IsActive`). Turning
  the whole login off is a Panel SuperAdmin action.
- Account admins show in every unit's list with a badge and are read only in the ERP; only a Panel SuperAdmin changes
  them.
- Nobody resets another user's password in the ERP. A user who forgot it uses **Forgot password**; the Panel
  SuperAdmin is the fallback.

### A user in several business units

- One user, one login. Access is one `UserBusinessUnit` row per unit: user → unit → role (empty until assigned) →
  active. Users are never copied per unit. Example: the user is created in Dhaka with units Dhaka and Chattogram; Dhaka
  gives "Store Manager", Chattogram later gives "Viewer"; there is no Sylhet row, so Sylhet cannot be opened.
- After sign-in the unit picker lists only units where the user is active **and has a role** (the last one is
  pre-selected); one unit skips the picker. No such unit: "You have no role in any business unit yet; ask your
  admin." The top bar unit switch changes `X-Business-Unit` without a new token, and the menu, buttons and API checks
  follow the role of that unit.
- Unit data (items, departments, employees) shows only the selected unit. Account data (UOM, roles, users) is shared,
  but what the user may do with it still comes from the role of the selected unit.
- A new business unit gives nobody access until a row is added; account admins get it at once.
- Deactivating the user in a unit, or deactivating the unit or the role, ends access to that unit on the next
  request. Rows are never deleted, so activating again restores the same role.
- Reports across several units are not planned; they belong to "data scope" under Later.

### Rules the server enforces

- A user cannot change their own access or status.
- Adding a user to a unit, or assigning a role there, needs the `system-setup.user` permission in **that** unit
  (account admins: every unit).
- No privilege escalation: a user who is not an account admin can only give a role whose permissions they have
  themselves in that unit, and can only edit roles within their own permissions.
- Unit and role must belong to the account and be active; a deactivated role keeps its users but grants nothing.
- Email and username are unique across all accounts.
- Every change is written to history (areas `user`, `role`).

### ERP API (`/api/erp/user-management`)

| Resource          | Endpoints                                                                                                                                           |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `roles`           | list, options, get, create, update, status, delete, `GET` / `PUT {id}/users`                                                                        |
| `permission-tree` | `GET`: the account's and unit's modules › sub-modules › features with their actions                                                                 |
| `users`           | list (selected unit), get, create (with units), update, `POST {id}/units`, `PUT {id}/role`, `PUT {id}/status` (selected unit), `POST {id}/sign-out` |
| `employees`       | list, options, get, create and update (optional `user`: username, password, confirm), status (plus `features/system-setup.employee/config`)         |

Roles are account level (one list in every unit). Users, role assignment and unit status work on the selected unit
(`X-Business-Unit`). The Employee list also shows only the selected unit's employees (by home unit), with no "all
units" filter.

## Ideas taken from the old system

The old system's `User`, `Employee`, `Roles` and `RolePermissions` tables were read for this plan. What is taken, and
what is done differently:

| Old system                                                                       | Here                                                                                                                       |
| -------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `UserName`, `Email`, `MobileNumber`, `EmployeeId`, `isActive`, `LastLoginTime`   | Same: `Username`, `Email`, `Phone`, `EmployeeId`, `IsActive`, `LastLoginAt`.                                               |
| `isMasterUser`                                                                   | `IsAccountAdmin` (several allowed, made in the Panel).                                                                     |
| `LoginFailedCount`, `IsBlock`                                                    | `FailedLoginCount`, `LockedUntil` (unlocks by itself; SuperAdmin can unlock).                                              |
| `IsForcefullyPasswordChange`                                                     | `MustChangePassword`.                                                                                                      |
| `Password`, `ConfirmPassword`, `OldPassword` (plain text)                        | Only `PasswordHash`. Confirm is a form field, never stored. No old passwords kept.                                         |
| `RoleId` on the user (one role everywhere)                                       | One role **per business unit** in `UserBusinessUnit`.                                                                      |
| About 25 yes / no columns on the user (`IsExchange`, `isRePrint`, `IsBackDate`…) | **Actions on a feature**, ticked per role in the matrix (see below). No permission columns on the user.                    |
| `RolePermissions`: one row per feature with `CanView`… `CanExport` columns       | One row per permission code (`{module}.{feature}.{action}`), so a feature's own actions need no new column.                |
| `Roles.RoleLevel`, `IsDeleted`                                                   | Not taken. The escalation rule (only give what you have) replaces levels; unused roles are deleted, used ones deactivated. |
| `Employee.PermissionGroupId`, `isUser`                                           | Not taken. Employees have no permissions; "is a user" comes from the user's link.                                          |
| Copied names (`AccountName`, `RoleName`, `Designation`, `ActionByName`…)         | Not taken. Ids only; names come from joins. Who and when come from `CreatedBy` / `UpdatedBy` and history.                  |
| `NOT NULL` with `'N/A'` or `0` defaults                                          | Optional fields are nullable.                                                                                              |
| `UserTypeId`, `isAppUser`, `IsEcomUser`, `IsDealRoomUser`                        | Later: sign-in channels, when a mobile app or online shop exists.                                                          |
| `isAllCustomerView`, `isEntityRestricted`                                        | Later: data scope per role.                                                                                                |
| `Employee.ReprintAllowDays`, `isReseller`, `ServicePoint`                        | Later, with the sales module (a sales setting and loyalty, not employee data).                                             |

**Special permissions become feature actions.** A screen declares its extra actions in its menu entry
(`MenuFeature.Actions`, already supported), and they show as extra columns in the role matrix. Actions always grant
("may see cost"), never hide ("cost hidden"), so no tick is the safe default. Example codes for the old flags, to be
fixed when each module is built:

| Old flag                                                    | Future action (example)                                                          |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `IsExchange`, `isReturn`, `IsWithoutRefRetExc`              | `sales.return.exchange`, `sales.return.create`, `sales.return.without-reference` |
| `isInvoiceEdit`, `isRePrint`, `IsBackDate`                  | `sales.invoice.edit`, `sales.invoice.reprint`, `sales.invoice.back-date`         |
| `IsItemDelete`, `isItemRowEdit`, `IsSpecialDiscount`        | `sales.pos.remove-line`, `sales.pos.edit-line`, `sales.pos.special-discount`     |
| `isHoldAuthorization`, `isRecallAuthorization`              | `sales.pos.hold`, `sales.pos.recall`                                             |
| `IsCreditSales`, `IsDirectPointRedeem`, `IsDeclarationSlip` | `sales.pos.credit-sale`, `sales.pos.redeem-points`, `sales.pos.declaration-slip` |
| `IsAllowBookingCancellation`                                | `sales.booking.cancel`                                                           |
| `isAudit`, `isAuditEdit`                                    | `inventory.stock-audit.view`, `inventory.stock-audit.edit`                       |
| `IsStockHideInSales`, `isCostHide`                          | `sales.pos.view-stock`, `{feature}.view-cost`                                    |
| `IsPosAdmin`                                                | A role ("POS Admin") with those actions, not a flag                              |

## Drafts on every create form

Every create form and create modal, in the ERP and in the Panel, can be saved as a **draft**: unfinished, with
required fields still empty, to finish later.

- **Save split button.** The primary button is **Save** (Ctrl+S); its arrow opens a menu with **Save & new**,
  **Save as draft** and **Cancel** (Esc). It lives once in the shared templates (`FormPageLayout` sticky header and
  `FormModal` footer), so every screen gets it. Edit forms of a saved record have no draft option.
- **One generic table, no change to business tables.** `Drafts`: id, surface (`erp` / `panel`), account (null for
  Panel drafts), business unit (the selected unit for ERP drafts, null in the Panel), feature code, title (for
  the list, e.g. the name typed so far), `Data` json (the form values as typed), created / updated by and at. Business
  tables never hold half-filled rows, so drafts never show in dropdowns, lookups, reports or counts.
- **Who sees them.** Everyone who may open the screen (`{feature}.view`) in that unit sees its drafts; saving,
  continuing or deleting one needs `{feature}.create` (owner, 2026-11-15). In the Panel any Panel user sees them.
- **In the list.** List pages get a **Draft** view (with count) beside All / Active / Inactive. It shows the drafts of
  that screen: title, saved by, saved at, and Continue / Delete. The same `DraftTable` (from `ListPageLayout`
  `drafts`) is used by every list.
- **Continue.** Opens the create page filled from the draft (`?draft={id}`) or the create modal (draft id kept in
  the list), with a Draft tag in the header. **Save as draft** again updates the same draft; **Save** runs every
  rule, creates the record and then deletes the draft.
- **API.** ERP `GET /api/erp/drafts?feature=` (list), `GET /drafts/{id}`, `POST /drafts`, `PUT /drafts/{id}`,
  `DELETE /drafts/{id}`; Panel the same under `/api/panel/drafts`. Create requests are unchanged; the form deletes
  the draft after the record is created. Password fields are never stored in a draft.
- **Server checks.** Draft data is stored as is (size limit in config, no field rules); the feature must exist and be
  one the account and unit have. Nothing reads draft data except the form that continues it.

## Phases

| Phase | Scope                                                                                                                                                                                                                                       |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 10    | Sessions and passwords: refresh tokens, logout, security stamp check, lockout, password policy, change password, must-change page, login events, api-client refresh, profile menu. Both surfaces.                                           |
| 11    | Username and email: `Username` (migration with backfill, setup wizard, login by email or username), `IEmailSender` with pickup folder, forgot and reset password by email code on both surfaces.                                            |
| 12    | Account admins and Panel ERP users: `IsAccountAdmin`, SuperAdmin filter, ERP users list and view, edit, status, Panel reset, unlock, sign out, Add admin, feature access per business unit.                                                 |
| 13    | ERP User Role: menu entry, permission tree, role list and form with the matrix, computed Administrator, escalation rules, history.                                                                                                          |
| 14    | ERP Employee (configurable screen with General, Personal and Job tabs, seven lookup lists, Create user) and User (unit list, create with units, add to units, assign role per unit from the user and from the role, unit status, sign out). |
| 15    | Drafts on every create form: `Drafts` table and API (ERP and Panel), Save split button in `FormPageLayout` and `FormModal`, Draft view and `DraftTable` in lists, continue a draft and delete it after the create.                          |
| 16    | Hardening: E2E (Panel adds an admin → admin makes a role and a user → the user signs in by username and sees only the allowed menu; forgot password by code; lockout and unlock), README and skill docs.                                    |

Each phase: backend, migration, API tests, `pnpm gen:api`, screens, i18n EN/বাংলা, browser check.

## Later (not planned; ask the owner)

- Two-step sign-in (authenticator app).
- Panel "sign in as" an ERP user for support, read only and logged.
- User limit and subscription end date per account (SaaS plan).
- Data scope per role (own records / business unit / all units).
- Password expiry, IP allow list, single sign-on, SMS codes.
- Panel users screen (manage the internal team; SuperAdmin only).
- Email settings per account (own sender name or SMTP).
- Employee payroll and bank tab, with the HR / payroll module.
- Importing users and employees from the old system (not for now). Old passwords would not be carried over;
  imported users would get `MustChangePassword` or use Forgot password.

## Owner decisions (2026-11-15)

1. Email is required for every ERP user. ERP sign-in is by email or username. Forgotten and reset passwords use a code
   sent by email.
2. User and Employee are two screens. Only users have permissions (a role); an employee can be turned into a user. A
   user does not have to be an employee first (recommended, see "User" above).
3. One role per user per business unit.
4. Users added in the Panel are always account admins, with control of every user and every business unit of the
   account.
5. Only a Panel SuperAdmin edits, deactivates, unlocks, signs out or resets the password of an ERP user. Users reset
   their own password through the email code.
6. Access token 15 minutes, refresh token 7 days, lockout after 3 wrong passwords for 15 minutes.
7. The ERP User screen shows only the selected unit's users, with no "all units" filter.
8. Creating a user (or adding them to more units) only puts them in those units; each unit assigns its role later,
   from inside that unit.
9. The Employee list shows only the selected unit's employees.
10. Payroll and bank fields come later, with the HR / payroll module.
11. No import from the old system for now.
12. While `MustChangePassword` is 1 the user can do nothing but change the password; after that, full access.
13. The Employee form has an **Is user** switch (username, password, confirm password); there is no separate
    "Save and create user" button. Employee rows have View and Edit, and the Employee screen has a view page.
14. Role permissions are a searchable tree with expand and collapse (see "User Role").
15. Every screen follows the existing templates, including Cancel, Save & new and Save in the sticky form header.
16. Passwords set by someone else are typed with a confirm field; only the Panel reset can also generate one.
17. Every create form and create modal (ERP and Panel) has **Save as draft** in a Save split button (Save & new,
    Save as draft, Cancel). Drafts of a screen are seen by everyone who may open that screen in that unit.
