# UC06 — Inloggen (implementatie & test)  

**Context.** De basis van de login zat al in het project (LoginView + VM, AuthService skeleton, ClientRepository, PasswordHelper).  
**Doel.** Inloggen met e‑mail + wachtwoord, inclusief gehashte wachtwoorden (PBKDF2) en nette feedback bij fout.

---

## 1. Scope & Randvoorwaarden
**In scope**
- `Client` model met properties
- `ClientRepository` lookup op e‑mail en id
- `AuthService.Login(email, password)` met PBKDF2‑validatie
- DI-registraties in `MauiProgram.cs`
- Startup naar `LoginView` in `App.xaml.cs`
- Route in `AppShell.xaml.cs`
- Minimale unit tests (Core/Data)

**Out of scope**
- Registreren, wachtwoord resetten, sessiebehoud

Randvoorwaarden:
- `PasswordHelper` (PBKDF2) is leidend. Geen plaintext-vergelijkingen.
- E‑mailvergelijkingen zijn case‑insensitive.

---

## 2. Implementatie (samenvatting)
### Client (Grocery.Core/Models/Client.cs)
Properties i.p.v. fields, zodat services/repositories er normaal mee werken:
```csharp
public string EmailAddress { get; set; }
public string Password { get; set; }
```

### ClientRepository (Grocery.Core.Data)
- `Get(string email)` → `FirstOrDefault(..., OrdinalIgnoreCase)`
- `Get(int id)` → op `Id`
- `GetAll()` → alles

### AuthService (Grocery.Core/Services/AuthService.cs)
```csharp
var client = _clientService.Get(email);
if (client == null) return null;
return PasswordHelper.VerifyPassword(password, client.Password) ? client : null;
```

### App startup & DI
- **MauiProgram.cs:**  
  `builder.Services.AddSingleton<IAuthService, AuthService>();`  
  `builder.Services.AddTransient<LoginView>().AddTransient<LoginViewModel>();`
- **App.xaml.cs:**  
  `MainPage = new LoginView(viewModel);` (en `AppShell` uitcommenten)
- **AppShell.xaml.cs:**  
  `Routing.RegisterRoute("Login", typeof(LoginView));`
- **Grocery.App.csproj:** zorg dat `LoginView.xaml` niet op *Remove* staat (includen in project).

---

## 3. Acceptatiecriteria (AC) & hoe ze behaald zijn
- **AC1**: juiste e‑mail + wachtwoord → `Client` terug en door naar AppShell  
  ✔️ `AuthService.Login` + `LoginViewModel` (zet `MainPage = new AppShell()`)
- **AC2**: fout e‑mail/wachtwoord → `null` + feedback  
  ✔️ `AuthService.Login` geeft `null`; VM toont “Ongeldige inloggegevens.”
- **AC3**: e‑mail case‑insensitive  
  ✔️ `ClientRepository.Get(string)` gebruikt case‑insensitive vergelijking
- **AC4**: wachtwoordcontrole via PBKDF2  
  ✔️ `PasswordHelper.VerifyPassword`

---

## 4. Teststrategie
### Unit (xUnit)
- **AuthServiceTests** (fake `IClientService`):  
  - geldige combinatie → client
  - onbekende e‑mail → null
  - verkeerd wachtwoord → null
  - casing op e‑mail → werkt
  - (extra) PasswordHelper: 2 hashes voor hetzelfde wachtwoord zijn verschillend (random salt), maar beide verifiëren `true`
- **ClientRepositoryTests** (echte repo):  
  - `Get("user1@mail.com")` → Id 1  
  - `Get("USER1@mail.com")` → Id 1 (case‑insensitive)  
  - `Get(2)` → e‑mail `user2@mail.com`  
  - onbekend e‑mail → `null`

### Handmatig (UI, low effort)
1. App start op Login.  
2. Goede combinatie → AppShell + welkomtekst.  
3. Foute combinatie → melding, blijf op Login.  
4. Herstart app → weer Login (geen sessiebehoud).

---

## 5. Bekende aandachtspunten
- Hashes in `ClientRepository` zijn PBKDF2 met salt; **platte** wachtwoorden staan dus niet in de code. Voor snel testen kun je tijdelijk zelf een hash genereren met `PasswordHelper.HashPassword("MijnTestWachtwoord")` en in de repo injecteren.
- Let op dat `LoginView.xaml` en code‑behind **included** zijn in het project (csproj).

---

## 6. Uitvoeren van de tests
```bash
# vanuit de solution-root
dotnet test
```
Zorg dat je testproject refereert naar **Grocery.Core** en **Grocery.Core.Data**.

---

## 7. Done‑checklist
- [x] Client properties
- [x] Repo lookups af
- [x] AuthService met PBKDF2‑check
- [x] DI + Startup + Route
- [x] Unit tests groen
- [x] UI handtest happy path
