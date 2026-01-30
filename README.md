# FuzzSoft

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

**FuzzSoft** is an AI Agent platform I built using **.NET 10** and **Blazor Server** (MudBlazor). The primary goal of this repository was for me to learn modern agentic workflows, experiment with toolchains, and create a maintainable codebase for exploring new models. It's a playground where I can test everything from local Ollama models to cloud-based vision and sound APIs.

## Key Features

### Multi-Brain Architecture
- **Flexible Providers**: You can seamlessly switch between **OpenAI**, **Google Gemini**, **Local LLMs** (via Ollama), and **Replicate**.
- **Playground Mode**: You can easily add your own API keys or toggle local models to test different behaviors across various interfaces (Chat, DB operations, Vision, MusicGen etc.).
- **Architecture**: I used a modular architecture to easily add new providers and tools. It's a work in progress, but it's a good start. 

### Agentic Capabilities
- **Task Agent**: An autonomous assistant that manages a To-Do list to test database interactions with **PostgreSQL**. It handles Create, Read, Update, and Delete operations with built-in guardrails for safety.
- **Database Inspection**: The agent can "look" at the database schema to dynamically understand how to query the tables.

### Multimodal Support
- **Visual Agents**: Analyze and describe images using vision-capable models. (Recognition is working, generation is on the roadmap).
- **Sound Agents**: Generate sound effects and music using **ElevenLabs** or **Replicate** (MusicGen). 
  - *Note:* Local models like `llamusic` work too, provided the output is formatted for the `abcjs` library used in the browser.
- **Voice Foundation**: Initial structure is there for voice interactions, waiting to be fully implemented. Maybe it won't be implemented, i don't know.

### User Experience
- **Multi-User Isolation**: I added configuration separation for user, taking advantage of basic Identity framework. Logs, settings, and tasks are bound to your user ID. If I ever host this on a server with Ollama, it's ready to scale.
- **MudBlazor UI**: I used **MudBlazor** for the interface because it's a framework I really like. It gives that nice "Glassmorphism" touch without much effort. Some of the UI features are vibe coded.
- **Live Parameters**: You can fine-tune **Temperature**, **TopP**, and **Max Tokens** on the fly from the settings page and see the effects immediately. This was a major point in my learning process.

## Tech Stack
- **Backend**: .NET 10 & ASP.NET Core with Identity
- **Frontend**: Blazor Server with MudBlazor
- **Data**: PostgreSQL with Entity Framework Core (Code First)

## Getting Started (The Short Version)
If you want to play around with this:
1. Ensure you have **.NET 10 SDK** and **PostgreSQL** installed.
2. Update the `DefaultConnection` in `appsettings.Development.json` with your DB credentials for local testing.
3. Run `dotnet ef database update` to set up the tables, although migrations should trigger automatically on startup.
4. Hit `dotnet run` and start exploring!

## Future Explorations
- [ ] Add more specialized tools for the agents.
- [ ] Implement image generation models.
- [ ] Add local model providers such as LM Studio, Groq, etc.
- [ ] Fully integrate voice-to-text and text-to-voice.
- [ ] Keep testing the "outer reaches" of new open-source models.
- [ ] Test multi table operations with schema tool.

## License
Feel free to use, modify, or learn from this project however you like. It is distributed under the **MIT License**. See [LICENSE](LICENSE) for more details.
