# SmartPark — Getting Started

> A step-by-step guide to set up and run the SmartPark project. No prior .NET experience required.

---

## 1. Install .NET 10 SDK

Download from: https://dotnet.microsoft.com/download/dotnet/10.0

Verify the installation:

```bash
dotnet --version
```

You should see `10.0.xxx`. If you get "command not found", restart your terminal.

---

## 2. Choose an IDE (Optional but Recommended)

You can use any of the following IDEs for a better development experience:

| IDE                    | Platform              | Download                           |
| ---------------------- | --------------------- | ---------------------------------- |
| **Visual Studio Code** | Windows, macOS, Linux | https://code.visualstudio.com      |
| **Visual Studio**      | Windows, macOS        | https://visualstudio.microsoft.com |
| **JetBrains Rider**    | Windows, macOS, Linux | https://www.jetbrains.com/rider    |

> **Tip:** For VS Code, install the **C# Dev Kit** extension for full .NET support including IntelliSense, debugging, and test exploration.

---

## 3. Build

```bash
dotnet build SmartPark.slnx
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

If you see package errors, run `dotnet restore SmartPark.slnx` first.

---

## 4. Run the Console App

```bash
dotnet run --project src/SmartPark.ConsoleApp
```

The app comes pre-loaded with demo vehicles. Try:

1. **Option 3** — View Active Tickets (see what's parked)
2. Copy a Ticket ID (e.g. `DEMO0001`)
3. **Option 2** — Check Out Vehicle (paste the ticket ID)
4. See the fee breakdown

Data is saved to `smartpark.db`. Delete this file to reset demo data.

> **Note:** Check-out will crash until you implement `ParkingFeeCalculator`. This is expected — it's your first TDD task.

---

## 5. Run the API

```bash
dotnet run --project src/SmartPark.Api
```

The API starts on `http://localhost:5000`. Try the endpoints with `curl` or your browser.

> **Note:** Check-out will return 502 unless a payment service is running on port 9091. This is expected behavior when exploring the API locally.

---

## 6. Run Tests

```bash
dotnet test SmartPark.slnx                                    # all tests
dotnet test --filter "ParkingFeeCalculatorTests"              # unit tests
dotnet test --filter "ParkingSessionManagerTests"             # test doubles
dotnet test --filter "ParkingFlowIntegrationTests"            # integration
```

> **First run:** Some example tests will fail with `NotImplementedException` because `ParkingFeeCalculator` is not yet implemented. This is by design — your first TDD cycle starts here.

---

## 7. Code Coverage

```bash
dotnet test SmartPark.slnx --collect:"XPlat Code Coverage"
```

Coverage XML appears under `tests/SmartPark.Tests/TestResults/`. To get an HTML report:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool      # one time
reportgenerator \
  -reports:"tests/SmartPark.Tests/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html
open coverage-report/index.html                               # macOS
```

---

## Project Structure

```
SmartPark.slnx                           ← solution file (open this in your IDE)

src/
  SmartPark.Core/                        ← business logic
    Models/                              ← Vehicle, ParkingTicket, enums, etc.
    Interfaces/                          ← contracts for external services
    Services/
      ParkingFeeCalculator.cs            ← STUB — implement via TDD
      ParkingSessionManager.cs           ← orchestration    — TEST DOUBLES TARGET
      InMemoryParkingRepository.cs       ← fake repository for tests
    Data/
      SmartParkDbContext.cs              ← EF Core context
      EfParkingRepository.cs             ← SQLite repository for console app

  SmartPark.Api/                         ← REST API with validation
    Controllers/ParkingController.cs     ← endpoints — API TEST TARGET
    DTOs/                                ← request/response models with validation
    HttpClients/                         ← HTTP-based payment & notification clients

  SmartPark.ConsoleApp/                  ← interactive demo (explore behavior)

tests/
  SmartPark.Tests/                       ← YOUR WORK GOES HERE
    ParkingFeeCalculatorTests.cs         ← unit tests + property-based tests
    ParkingSessionManagerTests.cs        ← tests with test doubles (Moq)
    IntegrationTests/
      ParkingFlowIntegrationTests.cs     ← end-to-end service tests

docs/
  getting-started.md                     ← this file
```

---

## Common Issues

| Problem                               | Solution                                            |
| ------------------------------------- | --------------------------------------------------- |
| `dotnet` not found                    | Install .NET SDK and restart your terminal          |
| Build fails — missing packages        | Run `dotnet restore SmartPark.slnx`                 |
| No tests available                    | Normal until you add `[Fact]`/`[Theory]` methods    |
| `NotImplementedException` on checkout | Implement `ParkingFeeCalculator` first (TDD Step 3) |
| Database locked                       | Close other instances of the console app            |

---

## Quick Reference

| Task               | Command                                                      |
| ------------------ | ------------------------------------------------------------ |
| Build              | `dotnet build SmartPark.slnx`                                |
| Run console app    | `dotnet run --project src/SmartPark.ConsoleApp`              |
| Run API            | `dotnet run --project src/SmartPark.Api`                     |
| Run all tests      | `dotnet test SmartPark.slnx`                                 |
| Run specific tests | `dotnet test --filter "ClassName"`                           |
| Code coverage      | `dotnet test SmartPark.slnx --collect:"XPlat Code Coverage"` |
| Reset demo data    | Delete `smartpark.db` and re-run the app                     |
| View TDD history   | `git log --oneline`                                          |
