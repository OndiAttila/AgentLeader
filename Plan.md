# AgentLead - C# CLI Project Plan

## Project Overview
- **Project Name**: AgentLead
- **Type**: Command Line Interface (CLI) Application
- **Purpose**: Send queries to an LLM via OpenAI API and display streaming responses

---

## 1. Project Setup

### 1.1 Initialize .NET Project
- Create new console application using `dotnet new console -n AgentLead`
- Target .NET 8.0 or later for modern HTTP client support
- Add required NuGet packages:
  - `System.Net.Http` (built-in)
  - `System.Text.Json` (for JSON parsing)

### 1.2 Project Structure
```
AgentLead/
├── Program.cs          # Entry point and main loop
├── Options/
│   └── CommandLineOptions.cs    # Command line argument parsing
├── Services/
│   ├── OpenAIClient.cs           # HTTP client for OpenAI API
│   └── StreamingService.cs       # Handle streaming responses
├── Models/
│   └── ApiModels.cs              # Request/response DTOs
└── Utils/
    └── ConnectionManager.cs      # Connection lifecycle & reconnection
```

---

## 2. Command Line Arguments & Configuration

### 2.1 Define Arguments
| Argument | Short | Description | Default |
|----------|-------|-------------|---------|
| `--base-url` | `-u` | OpenAI API base URL | `https://api.openai.com/v1` |
| `--model` | `-m` | LLM model name | `gpt-4` |
| `--help` | `-h` | Show help information | N/A |

### 2.2 Configuration Flow
1. Parse command line arguments
2. If argument not provided, use default values
3. Store in `AppConfiguration` class

---

## 3. Core Components

### 3.1 AppConfiguration
- Properties: `BaseUrl`, `ModelName`
- Method: `LoadFromArgs(args)` - parse command line
- Method: `setBaseUrl` - set the `BaseUrl` property to the specified one
- Method: `setModelName` - set the `ModelName` property to the specified value

### 3.2 OpenAIClient
- Uses `HttpClient` with `HttpCompletionOption.ResponseHeadersRead`
- Manages streaming POST requests to `/chat/completions` endpoint
- Sends proper headers:
  - `Authorization: Bearer {API_KEY}`
  - `Content-Type: application/json`

### 3.3 StreamingService
- Reads response stream asynchronously
- Parses SSE (Server-Sent Events) format
- Extracts `delta.content` from each chunk
- Outputs content to console in real-time
- Handles:
  - `[DONE]` message (stream complete)
  - Error responses in stream

### 3.4 ConnectionManager
- Implements ping/keep-alive mechanism
- Uses `System.Timers.Timer` or `CancellationToken` with timeout
- Sends ping every 60 seconds
- Implements reconnection logic:
  - Track connection attempts (max 5)
  - Exponential backoff between retries
  - Notify user on final failure

---

## 4. Main Program Flow

```
1. Parse command line arguments
2. If --base-url or --model missing:
   - Use default values
3. Store configuration
4. Loop (allow multiple queries):
   a. Display prompt indicator
   b. Read user input (query)
   c. If user exits (quit/exit), break loop
   d. Establish streaming connection
   e. Send POST request with query
   f. Start ping timer (60s interval)
   g. Read and display streaming response
   h. On connection close:
      - If unexpected, try reconnect (up to 5 times)
      - If all retries fail, notify user and allow retry
5. Exit program
```

---

## 5. Streaming HTTP Implementation

### 5.1 Request Format
```json
{
  "model": "gpt-4",
  "messages": [
    { "role": "user", "content": "{user_query}" }
  ],
  "stream": true
}
```

### 5.2 Response Parsing (SSE)
- Each chunk starts with `data: `
- Parse JSON: `{"choices":[{"delta":{"content":"..."}}]}`
- Extract and display content
- Stop on `data: [DONE]`

### 5.3 Keep-Alive (Ping) Mechanism
- Use `HttpClient` with `Timeout` set to > 60 seconds
- Alternatively, send HTTP PING/OPTIONS requests
- Timer triggers every 60 seconds to keep connection alive
- Cancel ping on stream end or disconnection

### 5.4 Reconnection Logic
```
attempt = 0
maxAttempts = 5

while attempt < maxAttempts:
   attempt++
   try:
       reconnect()
       break
   except Exception:
       if attempt < maxAttempts:
           wait(2^attempt seconds)  # exponential backoff
       else:
           print("Connection failed after 5 attempts")
           ask user to retry or exit
```

---

## 6. User Interaction

### 6.1 Command Line Usage
```bash
AgentLead --base-url https://api.openai.com/v1 --model gpt-4
AgentLead -u https://api.openai.com/v1 -m gpt-4
AgentLead  # uses default for missing values
```

### 6.2 Interactive Prompts
- Query prompt: `> ` (after each response)

### 6.3 Exit Commands
- `/quit`, `/exit`, `/q` - terminate program

### 6.4 Configuration
- `/base-url <string>` or `/u <string>` - set base URL to specified string value
- `/model <string>` or `/m <string>` - set model to specified string value

---

## 7. Error Handling

### 7.1 Connection Errors
- Network failures
- Timeout errors
- HTTP errors (4xx, 5xx)

### 7.2 Reconnection Strategy
- Maximum 5 retry attempts
- Exponential backoff: 2s, 4s, 8s, 16s, 32s
- After 5 failures, prompt user to:
  - Retry connection
  - Modify settings
  - Exit program

### 7.3 User Notifications
- Display connection status messages
- Show retry attempts
- Clear error messages

---

## 8. Security Considerations

- API Key should be provided via environment variable or user input
- Do not hardcode API keys in source code
- Use secure input for API key (password-style)

---

## 9. Testing Checklist

- [ ] Command line arguments parsed correctly
- [ ] Missing arguments trigger prompts
- [ ] Streaming responses display in real-time
- [ ] Multiple queries work in sequence
- [ ] Ping mechanism keeps connection alive
- [ ] Reconnection triggers after connection drop
- [ ] Maximum 5 reconnection attempts enforced
- [ ] User can exit with quit/exit command
- [ ] Default values applied when not specified

---

## 10. Implementation Priority

1. **Phase 1**: Basic project setup + configuration handling
2. **Phase 2**: OpenAI API client + streaming response
3. **Phase 3**: Interactive query loop
4. **Phase 4**: Keep-alive ping mechanism
5. **Phase 5**: Reconnection logic + error handling
6. **Phase 6**: Testing and refinement
