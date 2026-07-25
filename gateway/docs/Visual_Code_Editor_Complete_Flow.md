# Visual Code Editor - Complete Implementation Flow

**A comprehensive guide covering all scenarios from user interaction to code generation across any web project framework.**

---

## System Architecture Overview

The Visual Code Editor combines multimodal AI intelligence with deep project awareness to enable natural language code editing through visual selection. The system operates across multiple layers:

- **Frontend**: Browser extension/overlay for visual selection and project detection
- **Context Layer**: Project analysis, DOM inspection, and screenshot capture
- **AI Agent**: Claude-powered code generation with vision capabilities
- **Backend**: File system operations and hot-reload coordination

---

## Phase 1: Inspector Activation & Project Detection

### 1.1 User Initiates Design Mode

**Trigger**: User presses `Cmd+Shift+D` (or configured hotkey)

- Browser extension activates inspector overlay
- Overlay UI appears with semi-transparent backdrop
- Element hover highlighting begins (blue outline, like Chrome DevTools)
- Status indicator shows "Inspector Active"

### 1.2 Automatic Project Detection

The `ProjectDetector` JavaScript class runs comprehensive analysis:

| Detection Type | Method | Output |
|---|---|---|
| **Framework** | Check `window.Livewire`, `window.React`, `window.Vue`, DOM patterns | `laravel-livewire`, `react`, `vue`, `angular` |
| **Current Page** | `window.location` + route name + meta tags | `{url: '/dashboard', routeName: 'dashboard', title: '...'}` |
| **Language** | `document.documentElement.lang` + `navigator.language` | `{lang: 'en', locale: 'en_US', dir: 'ltr'}` |
| **Active Components** | `Livewire.all()`, React devtools, Vue devtools | Array of component instances with names and IDs |
| **CSS Framework** | Analyze stylesheets + class patterns | `tailwindcss`, `bootstrap`, `custom` |
| **Build Tool** | Check script sources | `vite`, `laravel-mix`, `webpack` |
| **Asset Pipeline** | Check manifest files | `vite.manifest.json`, `mix-manifest.json` |

### 1.3 Project Context Payload (Sent Once)

Complete project information sent to AI agent for session storage:

```json
{
  "project": {
    "type": "laravel-livewire",
    "version": "10.x",
    "root": "/Users/andrew/projects/zima-frontend",
    "structure": {
      "views": "resources/views",
      "components": "app/Livewire",
      "assets": "resources",
      "routes": "routes"
    }
  },
  "page": {
    "url": "/dashboard",
    "routeName": "dashboard",
    "title": "Dashboard - ZIMA",
    "lang": "en",
    "locale": "en_US"
  },
  "framework": {
    "frontend": "livewire",
    "css": "tailwindcss",
    "js": "alpinejs",
    "buildTool": "vite"
  },
  "components": {
    "active": [
      {
        "name": "ChatInterface",
        "file": "app/Livewire/ChatInterface.php",
        "view": "resources/views/livewire/chat-interface.blade.php",
        "id": "component-abc123"
      }
    ]
  },
  "env": {
    "mode": "development",
    "debug": true,
    "appUrl": "http://localhost:8000"
  }
}
```

### 1.4 Backend File Mapping

Backend receives project info and establishes file path mappings:

- **Laravel**: Map Livewire component names to PHP class files and Blade views
- **React**: Map component names to JSX/TSX file paths
- **Vue**: Map component names to `.vue` single-file components
- Store git root detection for absolute path resolution
- Cache project structure for fast lookups during session

---

## Phase 2: Element Selection & Context Capture

### 2.1 User Interaction

User hovers and clicks to select target element:

- **Hover**: Blue outline highlights element boundaries
- **Tooltip** appears showing: element tag, classes, component name
- **Click**: Element becomes selected (persistent highlight)
- Selection bounding box calculated (x, y, width, height)

### 2.2 Screenshot Capture

Browser captures visual context of selected area:

- **Method**: `html2canvas` or native browser screenshot API
- Crop to bounding box + 50px padding for surrounding context
- Convert to base64 PNG (optimized, <500KB)
- Include device pixel ratio for high-DPI displays

### 2.3 DOM Context Extraction

Deep analysis of selected element and surroundings:

| Context Type | Data Collected | Purpose |
|---|---|---|
| **Element Info** | tag, id, classes, attributes, data-* attributes | Identify element type and framework bindings |
| **CSS Selector** | Unique selector path (e.g., `#header > .nav-right`) | Pinpoint exact element for code targeting |
| **Computed Styles** | All final CSS properties from `getComputedStyle()` | Understand current visual styling |
| **Component Binding** | `wire:id`, `data-component`, React fiber info | Map to source component file |
| **Parent Context** | Parent element info, nesting level | Understand structural hierarchy |
| **Sibling Context** | Previous and next siblings (up to 2 each) | Understand surrounding layout |
| **Children Info** | Child count, child types | Know if container or leaf element |
| **Text Content** | `innerText` or `textContent` | Capture existing text for edits |

### 2.4 OCR Processing (Optional Layer)

When text is present in screenshot (not for empty areas):

- Run OCR on screenshot using Tesseract.js or cloud OCR
- Extract visible text with position coordinates
- Detect language from OCR output
- Calculate confidence score
- **Use for**: "Change this text to bold" or "Fix this typo" commands

### 2.5 User Command Capture

User provides instruction via voice or text input:

- **Voice**: Use Web Speech API (browser native) or Whisper API
- **Text**: Input box appears on selection
- **Command examples**: "Add a download button here", "Make this text bold", "Change color to blue"
- Timestamp and command type recorded

---

## Phase 3: Complete Context Assembly & Transmission

### 3.1 Full Selection Context Payload

Everything sent to AI agent in single request:

```json
{
  "projectId": "zima-frontend-abc123",

  "visual": {
    "screenshot": "data:image/png;base64,iVBORw0KG...",
    "boundingBox": {
      "x": 300,
      "y": 450,
      "width": 500,
      "height": 80
    },
    "viewport": {
      "width": 1920,
      "height": 1080
    },
    "devicePixelRatio": 2
  },

  "element": {
    "tag": "div",
    "id": "chat-header",
    "classes": ["flex", "items-center", "justify-between", "p-4", "bg-white"],
    "attributes": {
      "wire:id": "abc123",
      "data-component": "ChatInterface"
    },
    "textContent": "",
    "innerHTML": "<!-- Currently empty -->"
  },

  "selector": "#chat-header",

  "source": {
    "component": "ChatInterface",
    "phpFile": "app/Livewire/ChatInterface.php",
    "bladeFile": "resources/views/livewire/chat-interface.blade.php",
    "approximateLineNumber": 45
  },

  "context": {
    "parent": {
      "tag": "div",
      "classes": ["main-container"],
      "id": null
    },
    "siblings": {
      "before": [
        {"tag": "div", "classes": ["logo-section"]}
      ],
      "after": [
        {"tag": "div", "classes": ["user-menu"]},
        {"tag": "button", "classes": ["settings-icon"]}
      ]
    },
    "children": {
      "count": 0,
      "types": []
    },
    "depth": 3
  },

  "styles": {
    "display": "flex",
    "flexDirection": "row",
    "justifyContent": "space-between",
    "alignItems": "center",
    "padding": "16px",
    "background": "#ffffff",
    "borderRadius": "8px"
  },

  "ocr": {
    "text": "",
    "language": "en",
    "confidence": 0
  },

  "command": {
    "type": "voice",
    "text": "Add a blue download button here that saves the current chat",
    "timestamp": "2026-02-02T10:30:00Z"
  }
}
```

---

## Phase 4: AI Agent Processing & Intelligence

### 4.1 Context Prioritization (Smart Analysis)

AI agent intelligently processes all context layers:

| Priority | Layer | Usage | Fallback |
|---|---|---|---|
| **P1 Critical** | DOM Structure | ALWAYS: File mapping, selector targeting, component identification | None - request fails without this |
| **P2 High** | Visual Context | OFTEN: Layout understanding, spacing, design patterns | Use DOM computed styles if screenshot fails |
| **P3 Medium** | Project Info | ALWAYS: Framework syntax, conventions, file paths | Cached from session start |
| **P4 Optional** | OCR Text | SOMETIMES: Text editing, content changes | Use DOM textContent if OCR unavailable |
| **P5 Enhancement** | Vision AI | COMPLEX: Unusual layouts, design matching | Use other context if API unavailable |

### 4.2 Multimodal Intelligence Flow

AI agent combines all context sources intelligently:

1. **Parse user command** to understand intent (add, modify, delete, style)
2. **Use DOM selector** to identify exact file location from project mapping
3. **Analyze screenshot** OR use vision tool for complex visual understanding
4. **Check computed styles** to match existing design patterns
5. **Use OCR text ONLY** if command references visible text
6. **Apply project framework knowledge** (Livewire vs React vs Vue syntax)
7. **Read current file contents** to understand existing code structure

### 4.3 Vision Tool Usage (When Needed)

For complex visual scenarios, use Claude vision capabilities:

```javascript
// Invoke vision analysis for complex layouts
await mcp__gateway__image({
  image: screenshot_base64,
  prompt: `Analyze this UI area and describe:
  1. Visual hierarchy and element spacing
  2. Design patterns used (cards, buttons, layout)
  3. Color scheme and styling approach
  4. Where new elements should be positioned
  5. Existing elements that need alignment consideration`
});

// Vision tool helps with:
// - Understanding complex nested layouts
// - Matching design system patterns
// - Proper spacing/alignment calculations
// - Identifying visual relationships not clear from DOM
// - Custom graphics or unusual UI patterns
```

### 4.4 File Mapping & Resolution

AI agent resolves exact file paths using project context:

**For Laravel + Livewire:**
- Component name "ChatInterface" → `app/Livewire/ChatInterface.php`
- Blade view → `resources/views/livewire/chat-interface.blade.php`
- CSS selector maps to specific div in Blade template

**For React:**
- Component name "ChatHeader" → `src/components/ChatHeader.tsx`
- Props and state tracked via React Fiber

**For Vue:**
- Component name "ChatHeader" → `src/components/ChatHeader.vue`
- Template section identified for edit

---

## Phase 5: Code Generation

### 5.1 Read Current File Contents

AI agent reads target file to understand existing code:

- Use `Read` tool to fetch file contents
- Parse structure: identify the target element in code
- Understand indentation, formatting style
- Identify surrounding code context
- Note existing patterns (component structure, naming conventions)

### 5.2 Framework-Specific Code Generation

Generate code matching project framework and conventions:

| Framework | Generated Code Pattern | Key Features |
|---|---|---|
| **Laravel Livewire + Tailwind** | `<button wire:click="download" class="px-4 py-2 bg-blue-600">Download</button>` | Wire directives, Tailwind utilities, Blade syntax |
| **React + CSS Modules** | `<Button onClick={handleDownload} className={styles.primary}>Download</Button>` | JSX, event handlers, CSS module imports |
| **Vue 3 + Vuetify** | `<v-btn color="primary" @click="download" prepend-icon="mdi-download">Download</v-btn>` | Vue template syntax, Vuetify components, event binding |
| **Angular + Bootstrap** | `<button class="btn btn-primary" (click)="download()">Download</button>` | Angular binding, Bootstrap classes, TypeScript |

### 5.3 Language Localization

Generate text in correct language from project context:

- **English (en)**: "Download"
- **Spanish (es)**: "Descargar"
- **Arabic (ar)**: "تحميل" (with RTL layout adjustments)
- **French (fr)**: "Télécharger"
- Check for existing translation files and use translation keys if found

### 5.4 Style Matching

Match existing design system and styling:

- Use computed styles from context to match colors, spacing, typography
- Detect CSS framework (Tailwind, Bootstrap, Material-UI) and use appropriate classes
- Match existing component patterns (button variants, card styles)
- Respect design tokens if detected (CSS custom properties)
- Ensure consistent spacing using existing values (16px padding, 8px gap)

### 5.5 Icon/Asset Integration

Include appropriate icons based on project setup:

- Detect icon library: Font Awesome, Heroicons, Material Icons, custom SVG
- Use correct icon syntax for download button
  - **Font Awesome**: `<i class="fas fa-download"></i>`
  - **Heroicons (Blade)**: `<x-heroicon-o-arrow-down-tray />`
  - **Inline SVG**: Full `<svg>` markup with correct classes

---

## Phase 6: Code Application & File Operations

### 6.1 Precise Code Insertion

AI agent uses `Edit` tool to modify file:

```typescript
// Example: Adding button to empty div
Edit({
  file_path: "resources/views/livewire/chat-interface.blade.php",
  old_string: `<div id="chat-header" class="flex items-center gap-4">
    <!-- Currently empty -->
</div>`,
  new_string: `<div id="chat-header" class="flex items-center gap-4">
    <button
        wire:click="downloadChat"
        class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition flex items-center gap-2"
    >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v12m0 0l-4-4m4 4l4-4M3 20h18"/>
        </svg>
        Download Chat
    </button>
</div>`
});
```

### 6.2 Backend Method Addition (If Needed)

If command requires backend logic (e.g., `wire:click`), add method to component class:

```php
// Edit app/Livewire/ChatInterface.php
public function downloadChat()
{
    $messages = $this->messages;

    $content = collect($messages)->map(function($msg) {
        return $msg['role'] . ': ' . $msg['content'];
    })->join("\n\n");

    return response()->streamDownload(function() use ($content) {
        echo $content;
    }, 'chat-export-' . now()->format('Y-m-d-His') . '.txt');
}
```

### 6.3 Multiple File Coordination

For complex changes affecting multiple files:

- **Blade template**: Add UI element
- **Component class**: Add backend method
- **CSS file**: Add custom styles if needed
- **Route file**: Add route if new endpoint needed
- All edits applied atomically before hot reload

---

## Phase 7: User Preview & Confirmation

### 7.1 Code Diff Preview

Show user exactly what changed before applying:

- Display side-by-side diff in overlay
- Highlight added lines (green), removed lines (red)
- Show file path and line numbers
- Provide "Accept" and "Reject" buttons

### 7.2 User Actions

| Action | Behavior | Result |
|---|---|---|
| **Accept** | Apply changes to files immediately | Hot reload triggers, page updates |
| **Reject** | Discard changes, no files modified | Return to inspector mode |
| **Modify** | Edit AI-generated code before accepting | Show inline code editor |
| **Ask AI** | Request changes via follow-up command | Send refinement request to AI |

---

## Phase 8: Hot Reload & Verification

### 8.1 Framework-Specific Reload

| Framework | Reload Mechanism | Speed |
|---|---|---|
| **Laravel Livewire** | Livewire component auto-reloads via `wire:poll` or page refresh | ~500ms |
| **React (Vite)** | Vite HMR (Hot Module Replacement) updates component | ~100ms |
| **Vue (Vite)** | Vue HMR updates template and script | ~100ms |
| **Next.js** | Fast Refresh updates React components | ~200ms |

### 8.2 Visual Confirmation

After reload, system provides visual feedback:

- Highlight newly added/modified element with green glow (2s animation)
- Show success toast: "Changes applied successfully"
- Inspector mode stays active for further edits
- Undo option available (Cmd+Z)

---

## Scenario Coverage: All Use Cases

### Scenario A: Adding Element to Empty Area

**User Command**: "Add a download button here"

- **Context**: Empty `<div>` with no children, white background
- **DOM provides**: exact file, selector, parent structure
- **Screenshot shows**: available space, surrounding layout
- **OCR result**: empty (N/A)
- **AI generates**: Complete button HTML with icon, `wire:click`, Tailwind styles
- **Backend adds**: `downloadChat()` method to component
- **Result**: Functional download button appears exactly where clicked

### Scenario B: Modifying Existing Text

**User Command**: "Make this text bold and larger"

- **Context**: `<p>` tag with existing text "Welcome to Dashboard"
- **DOM provides**: textContent, current classes
- **OCR extracts**: "Welcome to Dashboard" (confidence 0.98)
- **AI modifies**: Adds `font-bold` and `text-xl` classes
- **Result**: Text becomes bold and larger, preserving content

### Scenario C: Complex Layout Restructuring

**User Command**: "Move this section below the header and add padding"

- **Context**: Complex nested `<section>` with multiple children
- **Vision tool analyzes**: Current layout structure, spacing patterns
- **AI understands**: Need to move entire section and adjust parent structure
- **AI edits**: Removes section from current location, inserts after header div, adds `p-6` class
- **Result**: Section moves smoothly with proper spacing

### Scenario D: Multi-Language Site

**User Command**: "Add save button" (on Spanish site)

- **Project context**: `lang="es"`, Laravel translations enabled
- **AI detects**: Translation file exists at `lang/es/app.php`
- **AI generates**: `<button>{{ __('app.save') }}</button>`
- **AI adds**: Translation key to `lang/es/app.php`: `'save' => 'Guardar'`
- **Result**: Button shows "Guardar" and respects site language switching

### Scenario E: React Component with State

**User Command**: "Add a toggle button that shows/hides this panel"

- **Context**: React functional component with existing hooks
- **AI analyzes**: Component structure, state management pattern
- **AI generates**: `useState` hook for visibility, button with `onClick`, conditional rendering
- **AI inserts**: All changes in correct positions within React component
- **Result**: Fully functional toggle with state management

---

## Error Handling & Edge Cases

### Edge Case 1: File Not Found

- **Issue**: Component mapping fails, file doesn't exist
- **Fallback**: AI searches for similar files using `Glob` tool
- **User prompt**: "Cannot find exact file. Did you mean: [suggestions]?"
- **Resolution**: User selects correct file or AI creates new component

### Edge Case 2: Ambiguous Selection

- **Issue**: User selected element that spans multiple components
- **Detection**: DOM shows nested components with different source files
- **AI response**: "This area includes 3 components. Which one should I edit?" with visual highlights
- **Resolution**: User clicks specific sub-element

### Edge Case 3: Complex Styling Conflicts

- **Issue**: Generated styles conflict with existing CSS
- **AI detection**: Analyzes computed styles, checks for `!important`, specificity issues
- **AI solution**: Uses more specific selectors or inline styles with proper precedence
- **Verification**: After reload, AI checks if element matches intended design

---

## Performance Optimizations

### Caching Strategy

- **Project context**: Cached for entire session (no re-detection)
- **File contents**: Cached after first read, invalidated on edit
- **Component mappings**: Stored in memory for fast lookups
- **Screenshot optimization**: Compress to <500KB, use WebP when supported

### Parallel Processing

- Screenshot capture + DOM extraction run in parallel
- OCR processing happens asynchronously (doesn't block main flow)
- Vision API called only when needed (complex layouts)
- File reads batched when multiple files need editing

---

## Summary: Complete Data Flow Diagram

```
User Activates Inspector
    ↓
Project Detection (once)
    ↓
Element Selection
    ↓
Context Capture (screenshot + DOM + OCR)
    ↓
User Command
    ↓
AI Analysis (multimodal)
    ↓
File Mapping
    ↓
Code Generation
    ↓
User Preview
    ↓
File Edit
    ↓
Hot Reload
    ↓
Visual Confirmation
```

### Critical Success Factors

- Comprehensive project detection on first activation
- Rich context payload with all necessary information
- Intelligent prioritization of context sources (DOM > Visual > OCR)
- Framework-aware code generation
- Precise file mapping and editing
- Fast hot reload with visual feedback

### Technologies Required

**Frontend**: Browser Extension (Chrome/Firefox), html2canvas, Web Speech API or Whisper API

**Backend**: File system operations, Git integration, Component mapping logic

**AI**: Claude Opus 4.5 with vision capabilities, `mcp__gateway__image` tool

**Optional**: Tesseract.js for OCR, Framework devtools for advanced component inspection

---

**Document Version**: 1.0
**Last Updated**: 2026-02-02
**Author**: ZIMA AI Agent
