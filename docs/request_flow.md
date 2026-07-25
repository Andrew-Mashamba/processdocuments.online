---
  ZIMA Prompt/Request Flow Diagram

  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │                              USER INPUT ENTRY                                    │
  └─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                      ┌──────────────────┴──────────────────┐
                      ▼                                      ▼
           ┌──────────────────┐                   ┌──────────────────┐
           │  INTERACTIVE TUI  │                   │ NON-INTERACTIVE  │
           │    (default)      │                   │    (-p flag)     │
           └────────┬─────────┘                   └────────┬─────────┘
                    │                                       │
                    ▼                                       ▼

  ---
  PHASE 1: Entry Point & Initialization

  main.go:8-14

  func main() {
      cmd.Execute()  // Entry point
  }

  cmd/root.go:49-183 - Command Execution

  ┌─────────────────────────────────────────────────────────────────┐
  │  rootCmd.RunE()                                                  │
  │  ├── config.Load()           → Load configuration               │
  │  ├── db.Connect()            → SQLite DB + migrations           │
  │  ├── app.New(ctx, conn)      → Create App with all services     │
  │  └── initMCPTools()          → Initialize MCP tools async       │
  └─────────────────────────────────────────────────────────────────┘

  internal/app/app.go:42-81 - App Initialization

  func New(ctx, conn) (*App, error) {
      // Create services
      sessions := session.NewService(q)
      messages := message.NewService(q)
      files := history.NewService(q, conn)

      app := &App{
          Sessions:    sessions,
          Messages:    messages,
          History:     files,
          Permissions: permission.NewPermissionService(),
          LSPClients:  make(map[string]*lsp.Client),
      }

      // Create CoderAgent with tools
      app.CoderAgent = agent.NewAgent(
          config.AgentCoder,
          sessions, messages,
          CoderAgentTools(permissions, sessions, messages, history, lspClients),
      )
      return app, nil
  }

  ---
  PHASE 2: User Input Capture

  Interactive Mode: internal/tui/tui.go:901-965

  ┌─────────────────────────────────────────────────────────────────┐
  │  tui.New(app)                                                    │
  │  └── Creates appModel with:                                      │
  │      ├── ChatPage (page.NewChatPage)                            │
  │      ├── LogsPage                                                │
  │      ├── Various dialogs (permissions, help, models, etc.)      │
  │      └── Bubble Tea program                                      │
  └─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
  ┌─────────────────────────────────────────────────────────────────┐
  │  Event Loop: cmd/root.go:140-153                                 │
  │  for {                                                           │
  │      msg := <-subscriptionChannel   // Receives pubsub events    │
  │      program.Send(msg)              // Sends to TUI              │
  │  }                                                               │
  └─────────────────────────────────────────────────────────────────┘

  Editor Component: internal/tui/components/chat/editor.go:143-217

  func (m *editorCmp) Update(msg tea.Msg) {
      switch msg := msg.(type) {
      case tea.KeyMsg:
          if key.Matches(msg, editorMaps.Send) {  // Enter or Ctrl+S
              return m, m.send()
          }
      }
  }

  func (m *editorCmp) send() tea.Cmd {
      value := m.textarea.Value()  // Get user input
      return util.CmdHandler(SendMsg{
          Text:        value,
          Attachments: attachments,
      })
  }

  ---
  PHASE 3: Message Dispatch

  Chat Page: internal/tui/page/chat.go:61-141

  func (p *chatPage) Update(msg tea.Msg) {
      switch msg := msg.(type) {
      case chat.SendMsg:
          cmd := p.sendMessage(msg.Text, msg.Attachments)
          return p, cmd
      }
  }

  func (p *chatPage) sendMessage(text string, attachments) tea.Cmd {
      // Create session if needed
      if p.session.ID == "" {
          session, _ := p.app.Sessions.Create(ctx, "New Session")
          p.session = session
      }

      // CRITICAL: Start agent processing
      p.app.CoderAgent.Run(ctx, p.session.ID, text, attachments...)
      return nil
  }

  ---
  PHASE 4: Agent Processing

  internal/llm/agent/agent.go:198-231 - Run Method

  func (a *agent) Run(ctx, sessionID, content string, attachments...) (<-chan AgentEvent, error) {
      events := make(chan AgentEvent)

      genCtx, cancel := context.WithCancel(ctx)
      a.activeRequests.Store(sessionID, cancel)

      go func() {
          result := a.processGeneration(genCtx, sessionID, content, attachmentParts)

          a.Publish(pubsub.CreatedEvent, result)  // Notify subscribers
          events <- result
          close(events)
      }()

      return events, nil
  }

  agent.go:233-311 - processGeneration (Main Loop)

  ┌─────────────────────────────────────────────────────────────────┐
  │  processGeneration(ctx, sessionID, content, attachments)        │
  │                                                                  │
  │  1. List existing messages from DB                              │
  │  2. Generate title asynchronously (if first message)            │
  │  3. Create user message in DB                                   │
  │  4. LOOP:                                                        │
  │     ├── streamAndHandleEvents() → Call LLM provider             │
  │     ├── If tool_use: execute tools, append results              │
  │     └── If end_turn: return final response                      │
  └─────────────────────────────────────────────────────────────────┘

  func (a *agent) processGeneration(ctx, sessionID, content, attachmentParts) AgentEvent {
      // Create user message
      userMsg := a.createUserMessage(ctx, sessionID, content, attachmentParts)
      msgHistory := append(existingMsgs, userMsg)

      for {  // AGENTIC LOOP
          // Stream response from LLM
          agentMessage, toolResults, err := a.streamAndHandleEvents(ctx, sessionID, msgHistory)

          if agentMessage.FinishReason() == FinishReasonToolUse && toolResults != nil {
              // Continue with tool results
              msgHistory = append(msgHistory, agentMessage, *toolResults)
              continue
          }

          // Done - return response
          return AgentEvent{Type: AgentEventTypeResponse, Message: agentMessage, Done: true}
      }
  }

  ---
  PHASE 5: LLM Provider Communication

  agent.go:322-438 - streamAndHandleEvents

  func (a *agent) streamAndHandleEvents(ctx, sessionID, msgHistory) (Message, *Message, error) {
      ctx = context.WithValue(ctx, tools.SessionIDContextKey, sessionID)

      // STREAM RESPONSE FROM PROVIDER
      eventChan := a.provider.StreamResponse(ctx, msgHistory, a.tools)

      // Create assistant message in DB
      assistantMsg := a.messages.Create(ctx, sessionID, {Role: Assistant})

      // Process streaming events
      for event := range eventChan {
          a.processEvent(ctx, sessionID, &assistantMsg, event)
      }

      // EXECUTE TOOLS
      for _, toolCall := range assistantMsg.ToolCalls() {
          tool := findTool(toolCall.Name)
          toolResult := tool.Run(ctx, toolCall)  // Execute tool
          toolResults[i] = toolResult
      }

      return assistantMsg, toolResultsMessage, nil
  }

  Provider Interface: internal/llm/provider/provider.go:54-60

  type Provider interface {
      SendMessages(ctx, messages, tools) (*ProviderResponse, error)
      StreamResponse(ctx, messages, tools) <-chan ProviderEvent
      Model() models.Model
  }

  Anthropic Implementation: internal/llm/provider/anthropic.go:247-393

  func (a *anthropicClient) stream(ctx, messages, tools) <-chan ProviderEvent {
      preparedMessages := a.preparedMessages(
          a.convertMessages(messages),  // Convert to Anthropic format
          a.convertTools(tools),        // Convert tools to Anthropic format
      )

      eventChan := make(chan ProviderEvent)

      go func() {
          anthropicStream := a.client.Messages.NewStreaming(ctx, preparedMessages)

          for anthropicStream.Next() {
              event := anthropicStream.Current()

              switch event := event.AsAny().(type) {
              case ContentBlockStartEvent:
                  eventChan <- ProviderEvent{Type: EventContentStart}
              case ContentBlockDeltaEvent:
                  eventChan <- ProviderEvent{Type: EventContentDelta, Content: event.Delta.Text}
              case MessageStopEvent:
                  eventChan <- ProviderEvent{Type: EventComplete, Response: &ProviderResponse{...}}
              }
          }
          close(eventChan)
      }()

      return eventChan
  }

  ---
  PHASE 6: Tool Execution

  agent.go:350-420 - Tool Execution Loop

  for i, toolCall := range toolCalls {
      // Find tool
      var tool tools.BaseTool
      for _, availableTool := range a.tools {
          if availableTool.Info().Name == toolCall.Name {
              tool = availableTool
              break
          }
      }

      // Execute tool
      toolResult, err := tool.Run(ctx, ToolCall{
          ID:    toolCall.ID,
          Name:  toolCall.Name,
          Input: toolCall.Input,
      })

      // Handle permission denied
      if errors.Is(err, permission.ErrorPermissionDenied) {
          toolResults[i] = ToolResult{Content: "Permission denied", IsError: true}
          break
      }

      toolResults[i] = ToolResult{
          ToolCallID: toolCall.ID,
          Content:    toolResult.Content,
      }
  }

  Tool Interface: internal/llm/tools/tools.go:69-72

  type BaseTool interface {
      Info() ToolInfo
      Run(ctx context.Context, params ToolCall) (ToolResponse, error)
  }

  Example: Bash Tool: internal/llm/tools/bash.go:230-327

  func (b *bashTool) Run(ctx, call ToolCall) (ToolResponse, error) {
      var params BashParams
      json.Unmarshal([]byte(call.Input), &params)

      // Permission check (unless safe read-only command)
      if !isSafeReadOnly {
          p := b.permissions.Request(CreatePermissionRequest{...})
          if !p {
              return ToolResponse{}, permission.ErrorPermissionDenied
          }
      }

      // Execute command
      shell := shell.GetPersistentShell(config.WorkingDirectory())
      stdout, stderr, exitCode, interrupted, err := shell.Exec(ctx, params.Command, params.Timeout)

      return NewTextResponse(stdout), nil
  }

  ---
  PHASE 7: Response Propagation

  Event Processing: agent.go:445-492

  func (a *agent) processEvent(ctx, sessionID, assistantMsg, event ProviderEvent) error {
      switch event.Type {
      case provider.EventContentDelta:
          assistantMsg.AppendContent(event.Content)
          return a.messages.Update(ctx, *assistantMsg)  // Persist to DB

      case provider.EventToolUseStart:
          assistantMsg.AddToolCall(*event.ToolCall)
          return a.messages.Update(ctx, *assistantMsg)

      case provider.EventComplete:
          assistantMsg.SetToolCalls(event.Response.ToolCalls)
          assistantMsg.AddFinish(event.Response.FinishReason)
          a.messages.Update(ctx, *assistantMsg)
          return a.TrackUsage(ctx, sessionID, a.provider.Model(), event.Response.Usage)
      }
      return nil
  }

  PubSub System: internal/pubsub/broker.go:93-116

  func (b *Broker[T]) Publish(t EventType, payload T) {
      event := Event[T]{Type: t, Payload: payload}

      for _, sub := range subscribers {
          sub <- event  // Send to all subscribers
      }
  }

  TUI Update: internal/tui/tui.go:182-664

  func (a appModel) Update(msg tea.Msg) (tea.Model, tea.Cmd) {
      switch msg := msg.(type) {
      case pubsub.Event[message.Message]:
          // Update message display in UI

      case pubsub.Event[agent.AgentEvent]:
          if payload.Done && payload.Type == AgentEventTypeResponse {
              // Response complete - check for auto-compact
          }
      }

      // Forward to current page
      a.pages[a.currentPage], cmd = a.pages[a.currentPage].Update(msg)
  }

  ---
  Complete Flow Summary

  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │ 1. USER INPUT                                                                    │
  │    editor.go:send() → SendMsg{Text, Attachments}                                │
  └─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │ 2. CHAT PAGE                                                                     │
  │    chat.go:sendMessage() → app.CoderAgent.Run(sessionID, text)                  │
  └─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │ 3. AGENT PROCESSING                                                              │
  │    agent.go:Run() → goroutine → processGeneration()                             │
  │    ┌──────────────────────────────────────────────────────────────┐             │
  │    │  AGENTIC LOOP:                                                │             │
  │    │  ├── Create user message in DB                               │             │
  │    │  ├── streamAndHandleEvents()                                 │             │
  │    │  │   ├── provider.StreamResponse() → LLM API                 │             │
  │    │  │   ├── Process streaming events                            │             │
  │    │  │   ├── Execute tools if tool_use                           │             │
  │    │  │   └── Update messages in DB                               │             │
  │    │  ├── If tool_use → append results, continue loop             │             │
  │    │  └── If end_turn → return AgentEvent                         │             │
  │    └──────────────────────────────────────────────────────────────┘             │
  └─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │ 4. PROVIDER LAYER                                                                │
  │    provider.go:StreamResponse() → anthropic.go / openai.go / etc.               │
  │    ├── Convert messages to provider format                                       │
  │    ├── Call provider API (streaming)                                            │
  │    └── Return event channel                                                      │
  └─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │ 5. TOOL EXECUTION (if needed)                                                    │
  │    tools/*.go:Run()                                                              │
  │    ├── Parse input parameters                                                    │
  │    ├── Request permission (if needed)                                            │
  │    ├── Execute operation                                                         │
  │    └── Return ToolResponse                                                       │
  └─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │ 6. RESPONSE PROPAGATION                                                          │
  │    ├── agent.Publish(AgentEvent) → pubsub.Broker                                │
  │    ├── messages.Update() → pubsub.Broker                                         │
  │    └── TUI subscriptions receive events                                          │
  └─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │ 7. UI RENDERING                                                                  │
  │    tui.go:Update() → page/chat.go → components/chat/*.go                        │
  │    ├── Update message display                                                    │
  │    ├── Show tool execution status                                                │
  │    └── Render final response                                                     │
  └─────────────────────────────────────────────────────────────────────────────────┘

  ---
  Key File Summary

  | Phase      | File                                       | Key Function            |
  |------------|--------------------------------------------|-------------------------|
  | Entry      | main.go:8                                  | main()                  |
  | CLI        | cmd/root.go:49                             | rootCmd.RunE()          |
  | App        | internal/app/app.go:42                     | New()                   |
  | TUI        | internal/tui/tui.go:901                    | New()                   |
  | Editor     | internal/tui/components/chat/editor.go:122 | send()                  |
  | Chat       | internal/tui/page/chat.go:155              | sendMessage()           |
  | Agent      | internal/llm/agent/agent.go:198            | Run()                   |
  | Processing | internal/llm/agent/agent.go:233            | processGeneration()     |
  | Streaming  | internal/llm/agent/agent.go:322            | streamAndHandleEvents() |
  | Provider   | internal/llm/provider/provider.go:196      | StreamResponse()        |
  | Anthropic  | internal/llm/provider/anthropic.go:247     | stream()                |
  | Tools      | internal/llm/tools/tools.go:69             | BaseTool.Run()          |
  | Events     | internal/pubsub/broker.go:93               | Publish()               |
  | Messages   | internal/message/message.go:57             | Create(), Update()      |
