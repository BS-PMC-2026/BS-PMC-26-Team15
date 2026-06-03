# SamiSpot

SamiSpot is a web-based emergency map system developed as part of a Software Engineering project at Sami Shamoon College of Engineering (SCE).

The goal of the system is to help civilians during emergency and war situations by providing nearby shelter locations, real-time risk levels, and emergency information through an interactive digital map.

## Live System

https://samispot-fde9ghhpduhybxcf.westus2-01.azurewebsites.net/

## GitHub Repository

https://github.com/BS-PMC-2026/BS-PMC-26-Team15

---

# Main Features

* Interactive shelter map
* Real-time risk level display
* Search by city or address
* Find closest shelters
* Route and navigation support
* Contributor shelter submissions
* Shelter approval system for admins
* AI emergency assistant
* Feedback and reporting system
* Emergency contacts and information pages

---

# Technologies

### Backend

* ASP.NET Core MVC
* C#
* Entity Framework Core
* SQL Server

### Frontend

* HTML
* CSS
* JavaScript
* Bootstrap

### APIs and External Services

* Google Maps API
* GovMap API
* Red Alert Data Integration
* OpenAI API

### Development Tools

* GitHub
* GitHub Actions
* Jira
* MSTest

---

# User Roles

| Role        | Capabilities                                                                         |
| ----------- | ------------------------------------------------------------------------------------ |
| User        | View shelters, search locations, view danger levels, send feedback, use AI assistant |
| Contributor | Add, edit, delete shelters, upload images, view and reply to user feedback           |
| Admin       | Approve/reject submissions, edit/delete shelters, manage contributors and users      |

---

# Risk Levels

| Color  | Meaning                                |
| ------ | -------------------------------------- |
| 🔴 Red | Alerts detected in the last 15 minutes |

---

# CI/CD

The project uses GitHub Actions for Continuous Integration and Continuous Deployment to Azure.

The workflow includes:

1. Restore dependencies
2. Build the solution
3. Run automated tests
4. Publish the project
5. Upload build artifacts
6. Deploy to Azure

---

# Testing

The project includes both unit tests and integration tests covering positive and negative scenarios.

Testing was performed using MSTest and GitHub Actions.

**Total tests across all sprints:**

* Sprint 1: 35 Tests
* Sprint 2: 46 Tests
* Sprint 3: 8 Tests

**Total: 89 Tests**

✅ All tests passed successfully.

---

# Installation (Local)

### Clone the repository

```bash
git clone https://github.com/BS-PMC-2026/BS-PMC-26-Team15.git
```

### Navigate to the project folder

```bash
cd SamiSpot
```

### Restore dependencies

```bash
dotnet restore
```

### Build the project

```bash
dotnet build
```

### Run the application

```bash
dotnet run
```

---

# Configuration

The project requires the following keys configured in `appsettings.json` or as environment variables:

### Google Maps

```json
GoogleMapsApiKey
```

Google Maps API key (requires a Google Cloud project with billing enabled).

### OpenAI

```json
OpenAIApiKey
```

Used for the AI emergency assistant.

### Database

```json
ConnectionStrings:DefaultConnection
```

SQL Server connection string.

---

# Project Structure

```text
SamiSpot/
│
├── Controllers/
├── Models/
├── Services/
├── Views/
├── Data/
├── wwwroot/
├── Program.cs
│
├── SamiSpot.sln
│
└── SamiSpot1.Tests/
```

---

# Team

* Rania Alafenish
* Doaa Alrabaiya
* Basmala Abu Hamid
* Rayan Matalka
