# Visual Code Editor Testing Guide

## Test Project Setup

This Laravel project has been configured to test the `@zima/visual-code-editor` package.

### What's Included

1. **Laravel 12** with **Livewire 4**
2. **Tailwind CSS** for styling
3. **Counter Component** - A Livewire component to test editing
4. **Test Welcome Page** with various UI elements
5. **@zima/visual-code-editor** package linked

---

## How to Test

### 1. Start the ZIMA Agent Server

Open a terminal and run:

```bash
cd /Volumes/DATA/QWEN/visual_code_test
npx visual-editor start
```

This will start the agent server on port **9876**.

### 2. Start the Laravel Dev Server

In a **second terminal**, run:

```bash
cd /Volumes/DATA/QWEN/visual_code_test
php artisan serve
```

This will start Laravel on **http://localhost:8000**.

### 3. Start Vite Dev Server

In a **third terminal**, run:

```bash
cd /Volumes/DATA/QWEN/visual_code_test
npm run dev
```

This enables hot module replacement (HMR).

### 4. Open in Browser

Visit **http://localhost:8000** in your browser.

### 5. Activate Visual Editor

Click the **floating purple button** in the bottom-right corner to activate the inspector.

You should see:
- The button changes color (purple → pink/red gradient)
- A dark status bar appears in the top-right corner
- Blue highlighting when hovering over elements

---

## Test Scenarios

### Test 1: Add a Button to Empty Section

1. **Click the floating purple button** to activate inspector
2. **Click on the empty dashed section** in the "Test Card"
3. **Type command**: "Add a blue download button here"
4. **Press Enter**
5. **Expected**: AI generates button code, shows preview, and applies it

### Test 2: Edit Existing Text

1. **Click the floating button** to activate inspector
2. **Click on the text** "Try selecting this text..."
3. **Type command**: "Make this text bold and red"
4. **Expected**: Text becomes bold with red color

### Test 3: Modify Existing Button

1. **Click the floating button** to activate inspector
2. **Click on the "Sample Button"**
3. **Type command**: "Change this button to green"
4. **Expected**: Button changes from blue to green

### Test 4: Edit Counter Component

1. **Click the floating button** to activate inspector
2. **Click on the Counter component**
3. **Type command**: "Add a reset button to reset count to zero"
4. **Expected**: AI adds a reset button and method

### Test 5: Add Icon to Element

1. **Click the floating button** to activate inspector
2. **Click on any feature card**
3. **Type command**: "Add a checkmark icon next to the title"
4. **Expected**: Icon appears next to title

---

## Expected Workflow

### 1. Element Selection
- Hover over elements - they should highlight in blue
- Click to select - element stays highlighted
- Tooltip shows element info (tag, classes, component)

### 2. Command Input
- Modal appears with text input
- Type natural language command
- Submit button sends to AI

### 3. AI Processing
- "AI is thinking..." indicator appears
- AI analyzes screenshot + DOM + command
- AI uses tools: read, write, edit, visual-analyze

### 4. Code Preview
- Modal shows generated code changes
- File paths and diffs displayed
- Accept or Reject buttons

### 5. Hot Reload
- Code applied to files
- Vite HMR triggers
- Changes visible instantly
- Success notification

---

## Debugging

### Agent Server Not Starting

```bash
# Check if Claude CLI is installed
claude --version

# If not installed:
npm install -g @anthropic-ai/claude-cli
claude auth login
```

### WebSocket Connection Failed

- Check that agent server is running on port 9876
- Check browser console for errors
- Verify no firewall blocking

### Inspector Not Activating

- Check browser console for JavaScript errors
- Ensure Vite dev server is running
- Try refreshing the page

### No Visual Feedback

- Open browser DevTools → Console
- Look for "✓ ZIMA Visual Code Editor loaded" message
- If missing, check Vite plugin configuration

---

## File Locations

### Key Files to Inspect

```
/Volumes/DATA/QWEN/visual_code_test/

# Configuration
.visual-editor/config.json

# Test Components
resources/views/components/⚡counter.blade.php
resources/views/welcome.blade.php

# Build Config
vite.config.js
package.json

# Generated Sessions (after use)
.visual-editor/sessions/
.visual-editor/memory.db
```

---

## What to Look For

### ✅ Success Indicators

- Floating purple button appears in bottom-right corner
- Inspector activates when button is clicked
- Button changes color when inspector is active (purple → pink/red)
- Elements highlight on hover
- Command input appears on element click
- Code preview shows generated changes
- Changes apply to actual source files
- Vite hot reloads the page
- Success notification appears

### ❌ Issues to Report

- Inspector doesn't activate
- WebSocket connection fails
- AI doesn't respond
- Code preview is empty or incorrect
- Changes don't apply to files
- Hot reload doesn't trigger
- Framework detection is wrong

---

## Advanced Testing

### Test with ZIMA File Service

If you have the ZIMA file service running on port 5000:

1. **Activate inspector**
2. **Click empty section**
3. **Type**: "Create an Excel file with sample product data"
4. **Expected**: AI uses `create_excel` tool, generates file, provides download link

### Test Memory System

1. Make several edits in sequence
2. Ask: "What changes did I make earlier?"
3. **Expected**: AI recalls previous edits from memory

### Test Multi-File Edits

1. **Click Counter component**
2. **Type**: "Add a method to reset counter and update the view"
3. **Expected**: AI edits both PHP class and Blade view

---

## Troubleshooting Commands

```bash
# Restart agent server
npx visual-editor start

# Check logs
cat .visual-editor/sessions/*.jsonl

# Verify config
cat .visual-editor/config.json

# Check npm link
npm ls @zima/visual-code-editor

# Rebuild package
cd /Volumes/DATA/QWEN/zima_visual_code_editor
npm run build
```

---

## Next Steps After Testing

1. **Document bugs** found during testing
2. **Test with real projects** (not just test project)
3. **Try complex commands** (multi-step changes)
4. **Test error handling** (invalid commands, missing files)
5. **Performance testing** (large files, many changes)

---

## Success Criteria

The package is working correctly if:

✅ Inspector activates reliably
✅ Element selection is accurate
✅ AI understands commands correctly
✅ Generated code matches intent
✅ Code applies to correct files
✅ Hot reload works smoothly
✅ No JavaScript errors in console
✅ No server crashes or errors

---

**Happy Testing! 🚀**
