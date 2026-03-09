# AgentLead Project Tree

```
AgentLeader/
├── .git/                          # Git repository
├── .gitignore                     # Git ignore rules
├── LICENSE                        # License file
├── Plan.md                        # Project planning document
├── TODOs.txt                      # Todo items
├── Prompts.txt                    # Prompts configuration
├── build.sh                       # Build script
├── run.sh                         # Production run script
├── run_local.sh                   # Local development script
│
├── AgentLead/                     # Main C# project
│   ├── AgentLead.csproj          # Project file
│   ├── Program.cs                # Entry point
│   │
│   ├── Models/
│   │   └── ApiModels.cs           # API data models
│   │
│   ├── Options/
│   │   └── AppConfiguration.cs   # Application configuration
│   │
│   ├── Services/
│   │   ├── OpenAIClient.cs        # OpenAI client service
│   │   ├── StreamingService.cs    # Streaming service
│   │   └── ConnectionManager.cs   # Connection management
│   │
│   ├── obj/                       # Build output (generated)
│   │   └── Debug/net10.0/
│   │
│   └── bin/                       # Compiled binaries (generated)
│       └── Debug/net10.0/
│
└── ProjectTree.md                # This file
```

## Project Summary

- **Type**: C# Console Application (.NET 10.0)
- **Purpose**: AI Agent Leadership/Coordination system
- **Key Dependencies**: OpenAI API integration
- **Language**: C#
