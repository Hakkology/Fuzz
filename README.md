# FuzzSoft

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

**FuzzSoft** is an advanced AI Agent platform built with **.NET 10** and **Blazor Server** (MudBlazor). It provides a unified interface for interacting with multiple AI models and capabilities, featuring autonomous task management, visual analysis, and sound generation.

## Features

### Multi-Brain Architecture
- **Flexible Providers**: Switch seamlessly between **OpenAI** (GPT-4o), **Google Gemini**, **Local LLMs** (via Ollama), and **Replicate**.
- **Unified Interface**: A single chat interface that adapts to the selected model's capabilities.

### Agentic Capabilities
- **Task Agent**: An autonomous assistant that manages your To-Do list.
  - **Tool Use**: Can execute SQL queries against the local PostgreSQL database to Insert, Update, and List tasks.
  - **Self-Correction**: Includes guardrails to prevent unsafe operations while handling data intelligently.
- **Database Inspection**: The agent can inspect the database schema to understand the data structure dynamically.

### Multimodal Support
- **Visual Agents**: Analyze uploaded images using Vision-capable models (GPT-4o, Gemini, LLaVA).
- **Sound Agents**: Generate sound effects and music using **ElevenLabs** and **Replicate** (MusicGen) models.

### Modern Tech Stack
- **Framework**: .NET 10 (Preview/Latest)
- **UI Component Library**: MudBlazor
- **Database**: PostgreSQL (with EF Core)
- **Architecture**: Clean Architecture (Domain, Services, Web, Client)

## Getting Started

### Prerequisites
- **.NET 10 SDK** (or compatible version)
- **PostgreSQL** Database
- **Ollama** (Optional, for local models)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/FuzzSoft.git
   cd FuzzSoft
   ```

2. **Configure Database**
   Ensure PostgreSQL is running and update your connection string.

3. **Secure Configuration**
   The project uses `appsettings.json` for public defaults and `appsettings.Development.json` for local secrets.
   
   Create/Update `Fuzz.Web/appsettings.Development.json` with your real credentials:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=FuzzDb;Username=postgres;Password=YOUR_REAL_PASSWORD"
     }
   }
   ```
   > **Note**: `appsettings.Development.json` is gitignored to keep your secrets safe.

4. **Run the Application**
   ```bash
   cd Fuzz.Web
   dotnet run
   ```
   Access the app at `http://localhost:5073` (or the port shown in your terminal).

## License

Distributed under the MIT License. See `LICENSE` for more information.
