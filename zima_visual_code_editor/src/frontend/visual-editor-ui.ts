/**
 * Visual Editor UI - Complete Design Tools
 *
 * Provides all visual manipulation tools:
 * - Resize handles, drag to move, rotate
 * - Style panel (colors, typography, spacing)
 * - Layout tools (align, distribute)
 * - Component library
 * - Layers panel
 */

export class VisualEditorUI {
  private selectedElement: HTMLElement | null = null;
  private resizeHandles: HTMLElement | null = null;
  private stylePanel: HTMLElement | null = null;
  private toolbarPanel: HTMLElement | null = null;
  private isDragging = false;
  private isResizing = false;
  private dragStartX = 0;
  private dragStartY = 0;
  private originalWidth = 0;
  private originalHeight = 0;
  private changeLog: any[] = [];

  /**
   * Initialize visual editor
   */
  init() {
    this.createToolbar();
    this.createStylePanel();
  }

  /**
   * Select an element for editing
   */
  selectElement(element: HTMLElement) {
    this.selectedElement = element;
    this.createResizeHandles();
    this.updateStylePanel();
    this.highlightElement();
  }

  /**
   * Clear selection
   */
  clearSelection() {
    if (this.resizeHandles) {
      this.resizeHandles.remove();
      this.resizeHandles = null;
    }
    this.selectedElement = null;
    this.updateStylePanel();
  }

  // ==========================================
  // TOOLBAR (Top)
  // ==========================================

  private createToolbar() {
    this.toolbarPanel = document.createElement('div');
    this.toolbarPanel.id = 'visual-editor-toolbar';
    this.toolbarPanel.style.cssText = `
      position: fixed;
      top: 0;
      left: 50%;
      transform: translateX(-50%);
      background: white;
      padding: 12px 16px;
      border-radius: 0 0 12px 12px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
      z-index: 1000003;
      display: flex;
      gap: 8px;
      align-items: center;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    const tools = [
      { icon: '👆', label: 'Select', action: 'select' },
      { icon: '🖱️', label: 'Move', action: 'move' },
      { icon: '📏', label: 'Resize', action: 'resize' },
      { icon: '🎨', label: 'Style', action: 'style' },
      { separator: true },
      { icon: '⬅️', label: 'Align Left', action: 'align-left' },
      { icon: '➡️', label: 'Align Right', action: 'align-right' },
      { icon: '⬆️', label: 'Align Top', action: 'align-top' },
      { icon: '⬇️', label: 'Align Bottom', action: 'align-bottom' },
      { icon: '🎯', label: 'Center', action: 'center' },
      { separator: true },
      { icon: '📋', label: 'Copy Style', action: 'copy-style' },
      { icon: '📑', label: 'Duplicate', action: 'duplicate' },
      { icon: '🗑️', label: 'Delete', action: 'delete' },
      { separator: true },
      { icon: '↩️', label: 'Undo', action: 'undo' },
      { icon: '↪️', label: 'Redo', action: 'redo' },
    ];

    tools.forEach(tool => {
      if (tool.separator) {
        const sep = document.createElement('div');
        sep.style.cssText = 'width: 1px; height: 24px; background: #e2e8f0; margin: 0 4px;';
        this.toolbarPanel!.appendChild(sep);
      } else {
        const btn = this.createToolButton(tool.icon, tool.label, tool.action);
        this.toolbarPanel!.appendChild(btn);
      }
    });

    document.body.appendChild(this.toolbarPanel);
  }

  private createToolButton(icon: string, label: string, action: string): HTMLElement {
    const btn = document.createElement('button');
    btn.className = 'visual-editor-tool-btn';
    btn.title = label;
    btn.style.cssText = `
      width: 36px;
      height: 36px;
      border: none;
      background: #f8fafc;
      border-radius: 6px;
      cursor: pointer;
      font-size: 18px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s;
    `;
    btn.innerHTML = icon;

    btn.onmouseover = () => {
      btn.style.background = '#e2e8f0';
      btn.style.transform = 'scale(1.05)';
    };
    btn.onmouseout = () => {
      btn.style.background = '#f8fafc';
      btn.style.transform = 'scale(1)';
    };

    btn.onclick = () => this.handleToolAction(action);

    return btn;
  }

  private handleToolAction(action: string) {
    if (!this.selectedElement) {
      alert('Please select an element first');
      return;
    }

    switch (action) {
      case 'align-left':
        this.alignElement('left');
        break;
      case 'align-right':
        this.alignElement('right');
        break;
      case 'align-top':
        this.alignElement('top');
        break;
      case 'align-bottom':
        this.alignElement('bottom');
        break;
      case 'center':
        this.centerElement();
        break;
      case 'duplicate':
        this.duplicateElement();
        break;
      case 'delete':
        this.deleteElement();
        break;
      case 'copy-style':
        this.copyStyle();
        break;
      case 'undo':
        this.undo();
        break;
      case 'redo':
        this.redo();
        break;
    }
  }

  // ==========================================
  // RESIZE HANDLES
  // ==========================================

  private createResizeHandles() {
    if (this.resizeHandles) {
      this.resizeHandles.remove();
    }

    if (!this.selectedElement) return;

    const rect = this.selectedElement.getBoundingClientRect();

    this.resizeHandles = document.createElement('div');
    this.resizeHandles.id = 'visual-editor-resize-handles';
    this.resizeHandles.style.cssText = `
      position: absolute;
      top: ${rect.top + window.scrollY}px;
      left: ${rect.left + window.scrollX}px;
      width: ${rect.width}px;
      height: ${rect.height}px;
      border: 2px solid #667eea;
      pointer-events: none;
      z-index: 1000002;
    `;

    // Create 8 resize handles
    const handles = [
      { pos: 'nw', cursor: 'nw-resize', top: -4, left: -4 },
      { pos: 'n', cursor: 'n-resize', top: -4, left: '50%', transform: 'translateX(-50%)' },
      { pos: 'ne', cursor: 'ne-resize', top: -4, right: -4 },
      { pos: 'e', cursor: 'e-resize', top: '50%', right: -4, transform: 'translateY(-50%)' },
      { pos: 'se', cursor: 'se-resize', bottom: -4, right: -4 },
      { pos: 's', cursor: 's-resize', bottom: -4, left: '50%', transform: 'translateX(-50%)' },
      { pos: 'sw', cursor: 'sw-resize', bottom: -4, left: -4 },
      { pos: 'w', cursor: 'w-resize', top: '50%', left: -4, transform: 'translateY(-50%)' },
    ];

    handles.forEach(h => {
      const handle = document.createElement('div');
      handle.className = `resize-handle-${h.pos}`;
      handle.style.cssText = `
        position: absolute;
        width: 8px;
        height: 8px;
        background: white;
        border: 2px solid #667eea;
        border-radius: 50%;
        pointer-events: auto;
        cursor: ${h.cursor};
        ${h.top !== undefined ? `top: ${h.top}px;` : ''}
        ${h.bottom !== undefined ? `bottom: ${h.bottom}px;` : ''}
        ${h.left !== undefined ? typeof h.left === 'string' ? `left: ${h.left};` : `left: ${h.left}px;` : ''}
        ${h.right !== undefined ? `right: ${h.right}px;` : ''}
        ${h.transform ? `transform: ${h.transform};` : ''}
      `;

      handle.onmousedown = (e) => this.startResize(e, h.pos);
      this.resizeHandles!.appendChild(handle);
    });

    // Add move handle (center)
    const moveHandle = document.createElement('div');
    moveHandle.style.cssText = `
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 24px;
      height: 24px;
      background: #667eea;
      border-radius: 50%;
      pointer-events: auto;
      cursor: move;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-size: 12px;
    `;
    moveHandle.innerHTML = '✥';
    moveHandle.onmousedown = (e) => this.startDrag(e);
    this.resizeHandles!.appendChild(moveHandle);

    document.body.appendChild(this.resizeHandles);
  }

  private startResize(e: MouseEvent, position: string) {
    e.preventDefault();
    e.stopPropagation();

    this.isResizing = true;
    this.dragStartX = e.clientX;
    this.dragStartY = e.clientY;

    if (this.selectedElement) {
      const rect = this.selectedElement.getBoundingClientRect();
      this.originalWidth = rect.width;
      this.originalHeight = rect.height;
    }

    const onMouseMove = (e: MouseEvent) => this.handleResize(e, position);
    const onMouseUp = () => {
      this.isResizing = false;
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
      this.logChange('resize', { position });
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }

  private handleResize(e: MouseEvent, position: string) {
    if (!this.selectedElement || !this.isResizing) return;

    const deltaX = e.clientX - this.dragStartX;
    const deltaY = e.clientY - this.dragStartY;

    let newWidth = this.originalWidth;
    let newHeight = this.originalHeight;

    // Calculate new dimensions based on handle position
    if (position.includes('e')) newWidth = this.originalWidth + deltaX;
    if (position.includes('w')) newWidth = this.originalWidth - deltaX;
    if (position.includes('s')) newHeight = this.originalHeight + deltaY;
    if (position.includes('n')) newHeight = this.originalHeight - deltaY;

    // Apply new dimensions
    if (newWidth > 20) this.selectedElement.style.width = `${newWidth}px`;
    if (newHeight > 20) this.selectedElement.style.height = `${newHeight}px`;

    // Update handles position
    if (this.resizeHandles) {
      const rect = this.selectedElement.getBoundingClientRect();
      this.resizeHandles.style.width = `${rect.width}px`;
      this.resizeHandles.style.height = `${rect.height}px`;
    }
  }

  private startDrag(e: MouseEvent) {
    e.preventDefault();
    e.stopPropagation();

    this.isDragging = true;
    this.dragStartX = e.clientX;
    this.dragStartY = e.clientY;

    const onMouseMove = (e: MouseEvent) => this.handleDrag(e);
    const onMouseUp = () => {
      this.isDragging = false;
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
      this.logChange('move', {});
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }

  private handleDrag(e: MouseEvent) {
    if (!this.selectedElement || !this.isDragging || !this.resizeHandles) return;

    const deltaX = e.clientX - this.dragStartX;
    const deltaY = e.clientY - this.dragStartY;

    const rect = this.selectedElement.getBoundingClientRect();
    const newLeft = rect.left + deltaX;
    const newTop = rect.top + deltaY;

    this.selectedElement.style.position = 'absolute';
    this.selectedElement.style.left = `${newLeft}px`;
    this.selectedElement.style.top = `${newTop}px`;

    this.resizeHandles.style.left = `${newLeft + window.scrollX}px`;
    this.resizeHandles.style.top = `${newTop + window.scrollY}px`;

    this.dragStartX = e.clientX;
    this.dragStartY = e.clientY;
  }

  // ==========================================
  // STYLE PANEL (Right Side)
  // ==========================================

  private createStylePanel() {
    this.stylePanel = document.createElement('div');
    this.stylePanel.id = 'visual-editor-style-panel';
    this.stylePanel.style.cssText = `
      position: fixed;
      top: 60px;
      right: 20px;
      width: 300px;
      max-height: calc(100vh - 100px);
      overflow-y: auto;
      background: white;
      padding: 20px;
      border-radius: 12px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
      z-index: 1000003;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      display: none;
    `;

    this.stylePanel.innerHTML = `
      <div style="margin-bottom: 20px;">
        <h3 style="margin: 0 0 4px 0; font-size: 14px; font-weight: 600; color: #1e293b;">Element Properties</h3>
        <div id="element-type" style="font-size: 12px; color: #64748b; font-family: monospace;"></div>
      </div>

      <!-- Layout -->
      <div class="style-section">
        <div class="section-title">📐 Layout</div>
        <div class="control-group">
          <label>Width</label>
          <input type="text" id="style-width" class="style-input" placeholder="auto">
        </div>
        <div class="control-group">
          <label>Height</label>
          <input type="text" id="style-height" class="style-input" placeholder="auto">
        </div>
        <div class="control-group">
          <label>Display</label>
          <select id="style-display" class="style-select">
            <option value="">Default</option>
            <option value="block">Block</option>
            <option value="flex">Flex</option>
            <option value="grid">Grid</option>
            <option value="inline-block">Inline Block</option>
            <option value="none">None</option>
          </select>
        </div>
      </div>

      <!-- Spacing -->
      <div class="style-section">
        <div class="section-title">📏 Spacing</div>
        <div class="spacing-controls">
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px;">
            <div class="control-group">
              <label>Padding</label>
              <input type="text" id="style-padding" class="style-input" placeholder="0">
            </div>
            <div class="control-group">
              <label>Margin</label>
              <input type="text" id="style-margin" class="style-input" placeholder="0">
            </div>
          </div>
        </div>
      </div>

      <!-- Colors -->
      <div class="style-section">
        <div class="section-title">🎨 Colors</div>
        <div class="control-group">
          <label>Background</label>
          <div style="display: flex; gap: 8px;">
            <input type="color" id="style-bg-color" class="style-color">
            <input type="text" id="style-bg-text" class="style-input" placeholder="#ffffff">
          </div>
        </div>
        <div class="control-group">
          <label>Text Color</label>
          <div style="display: flex; gap: 8px;">
            <input type="color" id="style-text-color" class="style-color">
            <input type="text" id="style-text-text" class="style-input" placeholder="#000000">
          </div>
        </div>
        <div class="control-group">
          <label>Opacity</label>
          <input type="range" id="style-opacity" min="0" max="100" value="100" class="style-range">
          <span id="opacity-value" style="font-size: 12px; color: #64748b;">100%</span>
        </div>
      </div>

      <!-- Typography -->
      <div class="style-section">
        <div class="section-title">✏️ Typography</div>
        <div class="control-group">
          <label>Font Size</label>
          <input type="text" id="style-font-size" class="style-input" placeholder="16px">
        </div>
        <div class="control-group">
          <label>Font Weight</label>
          <select id="style-font-weight" class="style-select">
            <option value="">Default</option>
            <option value="300">Light (300)</option>
            <option value="400">Normal (400)</option>
            <option value="500">Medium (500)</option>
            <option value="600">Semibold (600)</option>
            <option value="700">Bold (700)</option>
          </select>
        </div>
        <div class="control-group">
          <label>Text Align</label>
          <div style="display: flex; gap: 4px;">
            <button class="align-btn" data-align="left">⬅️</button>
            <button class="align-btn" data-align="center">↔️</button>
            <button class="align-btn" data-align="right">➡️</button>
            <button class="align-btn" data-align="justify">⬌</button>
          </div>
        </div>
      </div>

      <!-- Border -->
      <div class="style-section">
        <div class="section-title">🔲 Border</div>
        <div class="control-group">
          <label>Border Width</label>
          <input type="text" id="style-border-width" class="style-input" placeholder="0px">
        </div>
        <div class="control-group">
          <label>Border Color</label>
          <div style="display: flex; gap: 8px;">
            <input type="color" id="style-border-color" class="style-color">
            <input type="text" id="style-border-text" class="style-input" placeholder="#000000">
          </div>
        </div>
        <div class="control-group">
          <label>Border Radius</label>
          <input type="text" id="style-border-radius" class="style-input" placeholder="0px">
        </div>
      </div>

      <!-- Shadow -->
      <div class="style-section">
        <div class="section-title">✨ Shadow</div>
        <div class="control-group">
          <label>Box Shadow</label>
          <select id="style-shadow" class="style-select">
            <option value="">None</option>
            <option value="0 1px 2px rgba(0,0,0,0.05)">Small</option>
            <option value="0 4px 6px rgba(0,0,0,0.1)">Medium</option>
            <option value="0 10px 15px rgba(0,0,0,0.1)">Large</option>
            <option value="0 20px 25px rgba(0,0,0,0.15)">Extra Large</option>
          </select>
        </div>
      </div>

      <style>
        .style-section {
          margin-bottom: 20px;
          padding-bottom: 20px;
          border-bottom: 1px solid #e2e8f0;
        }
        .section-title {
          font-size: 13px;
          font-weight: 600;
          color: #475569;
          margin-bottom: 12px;
        }
        .control-group {
          margin-bottom: 12px;
        }
        .control-group label {
          display: block;
          font-size: 12px;
          color: #64748b;
          margin-bottom: 4px;
        }
        .style-input, .style-select {
          width: 100%;
          padding: 6px 10px;
          border: 1px solid #e2e8f0;
          border-radius: 6px;
          font-size: 13px;
          outline: none;
          transition: border-color 0.2s;
        }
        .style-input:focus, .style-select:focus {
          border-color: #667eea;
        }
        .style-color {
          width: 40px;
          height: 32px;
          border: 1px solid #e2e8f0;
          border-radius: 6px;
          cursor: pointer;
        }
        .style-range {
          width: calc(100% - 50px);
        }
        .align-btn {
          flex: 1;
          padding: 6px;
          border: 1px solid #e2e8f0;
          background: white;
          border-radius: 6px;
          cursor: pointer;
          font-size: 14px;
          transition: all 0.2s;
        }
        .align-btn:hover {
          background: #f8fafc;
          border-color: #667eea;
        }
      </style>
    `;

    document.body.appendChild(this.stylePanel);

    // Add event listeners
    this.addStylePanelListeners();
  }

  private addStylePanelListeners() {
    // Width
    const widthInput = document.getElementById('style-width') as HTMLInputElement;
    widthInput?.addEventListener('change', () => {
      if (this.selectedElement && widthInput.value) {
        this.selectedElement.style.width = widthInput.value;
        this.logChange('style', { property: 'width', value: widthInput.value });
        this.createResizeHandles(); // Update handles
      }
    });

    // Height
    const heightInput = document.getElementById('style-height') as HTMLInputElement;
    heightInput?.addEventListener('change', () => {
      if (this.selectedElement && heightInput.value) {
        this.selectedElement.style.height = heightInput.value;
        this.logChange('style', { property: 'height', value: heightInput.value });
        this.createResizeHandles();
      }
    });

    // Display
    const displaySelect = document.getElementById('style-display') as HTMLSelectElement;
    displaySelect?.addEventListener('change', () => {
      if (this.selectedElement) {
        this.selectedElement.style.display = displaySelect.value;
        this.logChange('style', { property: 'display', value: displaySelect.value });
      }
    });

    // Background color
    const bgColorInput = document.getElementById('style-bg-color') as HTMLInputElement;
    bgColorInput?.addEventListener('change', () => {
      if (this.selectedElement) {
        this.selectedElement.style.background = bgColorInput.value;
        (document.getElementById('style-bg-text') as HTMLInputElement).value = bgColorInput.value;
        this.logChange('style', { property: 'background', value: bgColorInput.value });
      }
    });

    // Text color
    const textColorInput = document.getElementById('style-text-color') as HTMLInputElement;
    textColorInput?.addEventListener('change', () => {
      if (this.selectedElement) {
        this.selectedElement.style.color = textColorInput.value;
        (document.getElementById('style-text-text') as HTMLInputElement).value = textColorInput.value;
        this.logChange('style', { property: 'color', value: textColorInput.value });
      }
    });

    // Opacity
    const opacityInput = document.getElementById('style-opacity') as HTMLInputElement;
    opacityInput?.addEventListener('input', () => {
      if (this.selectedElement) {
        const value = parseInt(opacityInput.value) / 100;
        this.selectedElement.style.opacity = value.toString();
        document.getElementById('opacity-value')!.textContent = `${opacityInput.value}%`;
        this.logChange('style', { property: 'opacity', value });
      }
    });

    // Font size
    const fontSizeInput = document.getElementById('style-font-size') as HTMLInputElement;
    fontSizeInput?.addEventListener('change', () => {
      if (this.selectedElement && fontSizeInput.value) {
        this.selectedElement.style.fontSize = fontSizeInput.value;
        this.logChange('style', { property: 'fontSize', value: fontSizeInput.value });
      }
    });

    // Add more listeners for other controls...
  }

  private updateStylePanel() {
    if (!this.stylePanel) return;

    if (this.selectedElement) {
      this.stylePanel.style.display = 'block';

      // Update element type display
      const elementType = document.getElementById('element-type');
      if (elementType) {
        const tag = this.selectedElement.tagName.toLowerCase();
        const id = this.selectedElement.id ? `#${this.selectedElement.id}` : '';
        const classes = this.selectedElement.classList.length > 0
          ? `.${Array.from(this.selectedElement.classList).join('.')}`
          : '';
        elementType.textContent = `${tag}${id}${classes}`;
      }

      // Populate values from current element
      const styles = window.getComputedStyle(this.selectedElement);

      (document.getElementById('style-width') as HTMLInputElement).value = this.selectedElement.style.width || '';
      (document.getElementById('style-height') as HTMLInputElement).value = this.selectedElement.style.height || '';
      (document.getElementById('style-bg-text') as HTMLInputElement).value = styles.backgroundColor;
      (document.getElementById('style-text-text') as HTMLInputElement).value = styles.color;
      (document.getElementById('style-font-size') as HTMLInputElement).value = styles.fontSize;

    } else {
      this.stylePanel.style.display = 'none';
    }
  }

  // ==========================================
  // LAYOUT TOOLS
  // ==========================================

  private alignElement(direction: string) {
    if (!this.selectedElement) return;

    const parent = this.selectedElement.parentElement;
    if (!parent) return;

    const parentRect = parent.getBoundingClientRect();
    const elRect = this.selectedElement.getBoundingClientRect();

    switch (direction) {
      case 'left':
        this.selectedElement.style.left = '0';
        break;
      case 'right':
        this.selectedElement.style.left = `${parentRect.width - elRect.width}px`;
        break;
      case 'top':
        this.selectedElement.style.top = '0';
        break;
      case 'bottom':
        this.selectedElement.style.top = `${parentRect.height - elRect.height}px`;
        break;
    }

    this.logChange('align', { direction });
    this.createResizeHandles();
  }

  private centerElement() {
    if (!this.selectedElement) return;

    const parent = this.selectedElement.parentElement;
    if (!parent) return;

    const parentRect = parent.getBoundingClientRect();
    const elRect = this.selectedElement.getBoundingClientRect();

    this.selectedElement.style.left = `${(parentRect.width - elRect.width) / 2}px`;
    this.selectedElement.style.top = `${(parentRect.height - elRect.height) / 2}px`;

    this.logChange('center', {});
    this.createResizeHandles();
  }

  private duplicateElement() {
    if (!this.selectedElement) return;

    const clone = this.selectedElement.cloneNode(true) as HTMLElement;
    this.selectedElement.parentElement?.appendChild(clone);

    this.logChange('duplicate', {});
  }

  private deleteElement() {
    if (!this.selectedElement) return;

    const confirm = window.confirm('Are you sure you want to delete this element?');
    if (confirm) {
      this.selectedElement.remove();
      this.clearSelection();
      this.logChange('delete', {});
    }
  }

  private copyStyle() {
    if (!this.selectedElement) return;

    const styles = window.getComputedStyle(this.selectedElement);
    const styleStr = Array.from(styles).map(prop => `${prop}: ${styles.getPropertyValue(prop)}`).join('; ');

    navigator.clipboard.writeText(styleStr);
    alert('Style copied to clipboard!');
  }

  private undo() {
    // TODO: Implement undo
    alert('Undo not yet implemented');
  }

  private redo() {
    // TODO: Implement redo
    alert('Redo not yet implemented');
  }

  // ==========================================
  // CHANGE LOGGING
  // ==========================================

  private logChange(type: string, details: any) {
    this.changeLog.push({
      timestamp: Date.now(),
      type,
      details,
      element: this.getElementSelector(this.selectedElement!),
    });
  }

  private getElementSelector(el: HTMLElement): string {
    if (el.id) return `#${el.id}`;

    const path: string[] = [];
    let current: Element | null = el;

    while (current && current !== document.body) {
      let selector = current.tagName.toLowerCase();
      if (current.id) {
        selector += `#${current.id}`;
        path.unshift(selector);
        break;
      }
      if (current.className) {
        selector += `.${Array.from(current.classList).join('.')}`;
      }
      path.unshift(selector);
      current = current.parentElement;
    }

    return path.join(' > ');
  }

  getChangeLog(): any[] {
    return this.changeLog;
  }

  clearChangeLog() {
    this.changeLog = [];
  }

  private highlightElement() {
    // Visual feedback that element is selected
    if (this.selectedElement) {
      this.selectedElement.style.outline = '2px solid #667eea';
      this.selectedElement.style.outlineOffset = '2px';
    }
  }

  // ==========================================
  // CLEANUP
  // ==========================================

  destroy() {
    this.toolbarPanel?.remove();
    this.stylePanel?.remove();
    this.resizeHandles?.remove();
  }
}
