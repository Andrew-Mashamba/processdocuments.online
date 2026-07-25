// Browser Injector Script for Visual Code Editor
// This script is injected into the user's application via Vite plugin

(function () {
  'use strict';

  let inspectorActive = false;
  let selectedElements: HTMLElement[] = [];
  let overlay: HTMLElement | null = null;
  let commandPanel: HTMLElement | null = null;
  let ws: WebSocket | null = null;
  let projectInfo: any = null;
  let domChangeLog: any[] = [];
  let sessionId: string = '';
  let visualEditor: any = null;
  let allChanges: any[] = []; // Combined: AI commands + visual edits
  let elementActionTooltip: HTMLElement | null = null;
  let contextElements: HTMLElement[] = []; // Elements added to context

  // Import DOM executor
  const domExecutor = {
    changeLog: [] as any[],

    async execute(toolName: string, params: any): Promise<any> {
      const timestamp = Date.now();
      let result: any;

      try {
        result = await this.executeTool(toolName, params);

        this.changeLog.push({
          timestamp,
          tool: toolName,
          params,
          result,
          description: this.describeChange(toolName, params),
        });

        return { success: true, result };
      } catch (error: any) {
        return { success: false, error: error.message };
      }
    },

    executeTool(toolName: string, params: any): any {
      // Implementation will be expanded inline
      switch (toolName) {
        case 'dom_set_style':
          return this.setStyle(params.selector, params.styles);
        case 'dom_add_class':
          return this.addClass(params.selector, params.classes);
        case 'dom_remove_class':
          return this.removeClass(params.selector, params.classes);
        case 'dom_set_text':
          return this.setText(params.selector, params.text, params.type);
        case 'dom_set_html':
          return this.setHtml(params.selector, params.html);
        case 'dom_set_attribute':
          return this.setAttribute(params.selector, params.attribute, params.value);
        case 'dom_append_child':
          return this.appendChild(params.parentSelector, params.childHtml);
        case 'dom_remove':
          return this.remove(params.selector);
        case 'dom_show':
          return this.show(params.selector, params.method);
        case 'dom_hide':
          return this.hide(params.selector, params.method);

        // Text tools
        case 'dom_set_font_family':
          return this.setFontFamily(params.selector, params.fontFamily);
        case 'dom_set_font_size':
          return this.setFontSize(params.selector, params.fontSize);
        case 'dom_set_font_weight':
          return this.setFontWeight(params.selector, params.fontWeight);
        case 'dom_set_font_style':
          return this.setFontStyle(params.selector, params.fontStyle);
        case 'dom_set_text_color':
          return this.setTextColor(params.selector, params.color);
        case 'dom_set_text_align':
          return this.setTextAlign(params.selector, params.align);
        case 'dom_set_text_decoration':
          return this.setTextDecoration(params.selector, params.decoration, params.style, params.color);
        case 'dom_set_text_transform':
          return this.setTextTransform(params.selector, params.transform);
        case 'dom_set_line_height':
          return this.setLineHeight(params.selector, params.lineHeight);
        case 'dom_set_letter_spacing':
          return this.setLetterSpacing(params.selector, params.spacing);
        case 'dom_set_word_spacing':
          return this.setWordSpacing(params.selector, params.spacing);
        case 'dom_set_text_indent':
          return this.setTextIndent(params.selector, params.indent);
        case 'dom_set_text_shadow':
          return this.setTextShadow(params.selector, params.shadow);
        case 'dom_set_white_space':
          return this.setWhiteSpace(params.selector, params.whiteSpace);
        case 'dom_set_text_overflow':
          return this.setTextOverflow(params.selector, params.overflow);
        case 'dom_set_word_break':
          return this.setWordBreak(params.selector, params.wordBreak);
        case 'dom_format_text_bold':
          return this.formatTextBold(params.selector, params.bold);
        case 'dom_format_text_italic':
          return this.formatTextItalic(params.selector, params.italic);
        case 'dom_format_text_underline':
          return this.formatTextUnderline(params.selector, params.underline);
        case 'dom_format_text_strikethrough':
          return this.formatTextStrikethrough(params.selector, params.strikethrough);
        case 'dom_select_text':
          return this.selectText(params.selector, params.start, params.end);
        case 'dom_wrap_text':
          return this.wrapText(params.selector, params.text, params.tag, params.classes, params.styles);

        // Font tools
        case 'dom_load_google_font':
          return this.loadGoogleFont(params.fontFamily, params.weights, params.styles);
        case 'dom_load_custom_font':
          return this.loadCustomFont(params.fontFamily, params.fontUrl, params.fontWeight, params.fontStyle, params.format);
        case 'dom_get_available_fonts':
          return this.getAvailableFonts();
        case 'dom_apply_font_to_selection':
          return this.applyFontToSelection(params.fontFamily);
        case 'dom_get_font_info':
          return this.getFontInfo(params.selector);
        case 'dom_set_font_fallback':
          return this.setFontFallback(params.selector, params.fontStack);

        // Drag and drop tools
        case 'dom_enable_dragging':
          return this.enableDragging(params.selector, params.axis, params.containment);
        case 'dom_set_position':
          return this.setPosition(params.selector, params.position, params.top, params.left, params.right, params.bottom, params.zIndex);
        case 'dom_move_to_coordinates':
          return this.moveToCoordinates(params.selector, params.x, params.y, params.transition);
        case 'dom_swap_elements':
          return this.swapElements(params.selector1, params.selector2, params.animate);
        case 'dom_reorder_children':
          return this.reorderChildren(params.parentSelector, params.order);
        case 'dom_bring_to_front':
          return this.bringToFront(params.selector);
        case 'dom_send_to_back':
          return this.sendToBack(params.selector);

        default:
          throw new Error(`Unknown DOM tool: ${toolName}`);
      }
    },

    setStyle(selector: string, styles: Record<string, string>): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => Object.assign(el.style, styles));
      return elements.length;
    },

    addClass(selector: string, classes: string[]): number {
      const elements = document.querySelectorAll(selector);
      elements.forEach((el) => el.classList.add(...classes));
      return elements.length;
    },

    removeClass(selector: string, classes: string[]): number {
      const elements = document.querySelectorAll(selector);
      elements.forEach((el) => el.classList.remove(...classes));
      return elements.length;
    },

    setText(selector: string, text: string, type: string = 'innerText'): number {
      const elements = document.querySelectorAll(selector);
      elements.forEach((el: any) => {
        if (type === 'innerHTML') el.innerHTML = text;
        else if (type === 'textContent') el.textContent = text;
        else el.innerText = text;
      });
      return elements.length;
    },

    setHtml(selector: string, html: string): number {
      const elements = document.querySelectorAll(selector);
      elements.forEach((el: any) => el.innerHTML = html);
      return elements.length;
    },

    setAttribute(selector: string, attribute: string, value: string): number {
      const elements = document.querySelectorAll(selector);
      elements.forEach((el) => el.setAttribute(attribute, value));
      return elements.length;
    },

    appendChild(parentSelector: string, childHtml: string): boolean {
      const parent = document.querySelector(parentSelector);
      if (!parent) throw new Error(`Parent not found: ${parentSelector}`);
      const temp = document.createElement('div');
      temp.innerHTML = childHtml;
      while (temp.firstChild) parent.appendChild(temp.firstChild);
      return true;
    },

    remove(selector: string): number {
      const elements = document.querySelectorAll(selector);
      elements.forEach((el) => el.remove());
      return elements.length;
    },

    show(selector: string, method: string = 'display'): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => {
        if (method === 'display') el.style.display = '';
        else if (method === 'visibility') el.style.visibility = 'visible';
        else if (method === 'opacity') el.style.opacity = '1';
      });
      return elements.length;
    },

    hide(selector: string, method: string = 'display'): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => {
        if (method === 'display') el.style.display = 'none';
        else if (method === 'visibility') el.style.visibility = 'hidden';
        else if (method === 'opacity') el.style.opacity = '0';
      });
      return elements.length;
    },

    // ==========================================
    // TEXT MANIPULATION METHODS
    // ==========================================

    setFontFamily(selector: string, fontFamily: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.fontFamily = fontFamily);
      return elements.length;
    },

    setFontSize(selector: string, fontSize: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.fontSize = fontSize);
      return elements.length;
    },

    setFontWeight(selector: string, fontWeight: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.fontWeight = fontWeight);
      return elements.length;
    },

    setFontStyle(selector: string, fontStyle: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.fontStyle = fontStyle);
      return elements.length;
    },

    setTextColor(selector: string, color: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.color = color);
      return elements.length;
    },

    setTextAlign(selector: string, align: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.textAlign = align);
      return elements.length;
    },

    setTextDecoration(selector: string, decoration: string, style?: string, color?: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => {
        el.style.textDecoration = decoration;
        if (style) el.style.textDecorationStyle = style;
        if (color) el.style.textDecorationColor = color;
      });
      return elements.length;
    },

    setTextTransform(selector: string, transform: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.textTransform = transform);
      return elements.length;
    },

    setLineHeight(selector: string, lineHeight: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.lineHeight = lineHeight);
      return elements.length;
    },

    setLetterSpacing(selector: string, spacing: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.letterSpacing = spacing);
      return elements.length;
    },

    setWordSpacing(selector: string, spacing: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.wordSpacing = spacing);
      return elements.length;
    },

    setTextIndent(selector: string, indent: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.textIndent = indent);
      return elements.length;
    },

    setTextShadow(selector: string, shadow: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.textShadow = shadow);
      return elements.length;
    },

    setWhiteSpace(selector: string, whiteSpace: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.whiteSpace = whiteSpace);
      return elements.length;
    },

    setTextOverflow(selector: string, overflow: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => {
        el.style.textOverflow = overflow;
        el.style.overflow = 'hidden';
        el.style.whiteSpace = 'nowrap';
      });
      return elements.length;
    },

    setWordBreak(selector: string, wordBreak: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.wordBreak = wordBreak);
      return elements.length;
    },

    formatTextBold(selector: string, bold: boolean): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.fontWeight = bold ? 'bold' : 'normal');
      return elements.length;
    },

    formatTextItalic(selector: string, italic: boolean): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.fontStyle = italic ? 'italic' : 'normal');
      return elements.length;
    },

    formatTextUnderline(selector: string, underline: boolean): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.textDecoration = underline ? 'underline' : 'none');
      return elements.length;
    },

    formatTextStrikethrough(selector: string, strikethrough: boolean): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      elements.forEach((el) => el.style.textDecoration = strikethrough ? 'line-through' : 'none');
      return elements.length;
    },

    selectText(selector: string, start?: number, end?: number): boolean {
      const element = document.querySelector(selector) as HTMLElement;
      if (!element) throw new Error(`Element not found: ${selector}`);

      const range = document.createRange();
      const selection = window.getSelection();

      if (element.firstChild) {
        range.setStart(element.firstChild, start || 0);
        range.setEnd(element.firstChild, end !== undefined ? end : element.textContent?.length || 0);
        selection?.removeAllRanges();
        selection?.addRange(range);
      }

      return true;
    },

    wrapText(selector: string, text: string, tag: string, classes?: string[], styles?: Record<string, string>): boolean {
      const element = document.querySelector(selector) as HTMLElement;
      if (!element) throw new Error(`Element not found: ${selector}`);

      const innerHTML = element.innerHTML;
      const wrapper = `<${tag}${classes ? ` class="${classes.join(' ')}"` : ''}${styles ? ` style="${Object.entries(styles).map(([k, v]) => `${k}:${v}`).join(';')}"` : ''}>${text}</${tag}>`;
      element.innerHTML = innerHTML.replace(text, wrapper);

      return true;
    },

    // ==========================================
    // FONT MANAGEMENT METHODS
    // ==========================================

    loadGoogleFont(fontFamily: string, weights?: string[], styles?: string[]): boolean {
      // Check if already loaded
      const existingLink = document.querySelector(`link[href*="fonts.googleapis.com"][href*="${fontFamily.replace(/\s/g, '+')}"]`);
      if (existingLink) return true;

      // Build Google Fonts URL
      let fontUrl = `https://fonts.googleapis.com/css2?family=${fontFamily.replace(/\s/g, '+')}`;

      if (weights && weights.length > 0) {
        const ital = styles?.includes('italic') ? '0,1' : '0';
        fontUrl += `:ital,wght@${weights.map(w => `${ital},${w}`).join(';')}`;
      }

      // Create link element
      const link = document.createElement('link');
      link.rel = 'stylesheet';
      link.href = fontUrl;
      document.head.appendChild(link);

      return true;
    },

    loadCustomFont(fontFamily: string, fontUrl: string, fontWeight?: string, fontStyle?: string, format?: string): boolean {
      const styleId = `custom-font-${fontFamily.replace(/\s/g, '-')}`;

      // Check if already loaded
      if (document.getElementById(styleId)) return true;

      // Create @font-face rule
      const formatMap: Record<string, string> = {
        'woff': 'woff',
        'woff2': 'woff2',
        'truetype': 'truetype',
        'opentype': 'opentype',
      };

      const fontFormat = format ? formatMap[format] : 'woff2';

      const fontFace = `
        @font-face {
          font-family: '${fontFamily}';
          src: url('${fontUrl}') format('${fontFormat}');
          font-weight: ${fontWeight || 'normal'};
          font-style: ${fontStyle || 'normal'};
        }
      `;

      const style = document.createElement('style');
      style.id = styleId;
      style.textContent = fontFace;
      document.head.appendChild(style);

      return true;
    },

    getAvailableFonts(): string[] {
      // Get loaded fonts from document
      const loadedFonts: string[] = [];

      // Get Google Fonts
      document.querySelectorAll('link[href*="fonts.googleapis.com"]').forEach((link: any) => {
        const href = link.href;
        const match = href.match(/family=([^:&]+)/);
        if (match) loadedFonts.push(match[1].replace(/\+/g, ' '));
      });

      // Get custom fonts from @font-face
      Array.from(document.styleSheets).forEach(sheet => {
        try {
          Array.from(sheet.cssRules || []).forEach((rule: any) => {
            if (rule.type === CSSRule.FONT_FACE_RULE) {
              const fontFamily = rule.style.fontFamily?.replace(/['"]/g, '');
              if (fontFamily) loadedFonts.push(fontFamily);
            }
          });
        } catch (e) {
          // Cross-origin stylesheets may throw
        }
      });

      // Add common system fonts
      const systemFonts = [
        'Arial', 'Helvetica', 'Times New Roman', 'Georgia', 'Verdana',
        'Courier New', 'Trebuchet MS', 'Comic Sans MS', 'Impact',
        'Palatino', 'Garamond', 'Bookman', 'Lucida', 'Tahoma'
      ];

      return [...new Set([...systemFonts, ...loadedFonts])];
    },

    applyFontToSelection(fontFamily: string): boolean {
      const selection = window.getSelection();
      if (!selection || selection.rangeCount === 0) {
        throw new Error('No text selected');
      }

      const range = selection.getRangeAt(0);
      const span = document.createElement('span');
      span.style.fontFamily = fontFamily;

      try {
        range.surroundContents(span);
        return true;
      } catch (e) {
        throw new Error('Cannot apply font to selection');
      }
    },

    getFontInfo(selector: string): any {
      const element = document.querySelector(selector) as HTMLElement;
      if (!element) throw new Error(`Element not found: ${selector}`);

      const computed = window.getComputedStyle(element);

      return {
        fontFamily: computed.fontFamily,
        fontSize: computed.fontSize,
        fontWeight: computed.fontWeight,
        fontStyle: computed.fontStyle,
        lineHeight: computed.lineHeight,
        letterSpacing: computed.letterSpacing,
        textAlign: computed.textAlign,
        textTransform: computed.textTransform,
      };
    },

    setFontFallback(selector: string, fontStack: string[]): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
      const fontFamily = fontStack.map(f => f.includes(' ') ? `"${f}"` : f).join(', ');
      elements.forEach((el) => el.style.fontFamily = fontFamily);
      return elements.length;
    },

    // ==========================================
    // DRAG AND DROP METHODS
    // ==========================================

    enableDragging(selector: string, axis: string = 'both', containment?: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;

      elements.forEach((el) => {
        // Make element draggable
        el.setAttribute('draggable', 'true');
        el.style.cursor = 'move';

        // If not already positioned, make it relative
        if (getComputedStyle(el).position === 'static') {
          el.style.position = 'relative';
        }

        // Store initial position
        let startX = 0, startY = 0, initialLeft = 0, initialTop = 0;

        const onMouseDown = (e: MouseEvent) => {
          e.preventDefault();
          startX = e.clientX;
          startY = e.clientY;

          const computed = window.getComputedStyle(el);
          initialLeft = parseInt(computed.left) || 0;
          initialTop = parseInt(computed.top) || 0;

          document.addEventListener('mousemove', onMouseMove);
          document.addEventListener('mouseup', onMouseUp);
        };

        const onMouseMove = (e: MouseEvent) => {
          const deltaX = e.clientX - startX;
          const deltaY = e.clientY - startY;

          if (axis === 'both' || axis === 'x') {
            el.style.left = `${initialLeft + deltaX}px`;
          }
          if (axis === 'both' || axis === 'y') {
            el.style.top = `${initialTop + deltaY}px`;
          }

          // Containment check
          if (containment) {
            const container = document.querySelector(containment);
            if (container) {
              const containerRect = container.getBoundingClientRect();
              const elRect = el.getBoundingClientRect();

              if (elRect.left < containerRect.left) el.style.left = '0px';
              if (elRect.top < containerRect.top) el.style.top = '0px';
              if (elRect.right > containerRect.right) el.style.left = `${containerRect.width - elRect.width}px`;
              if (elRect.bottom > containerRect.bottom) el.style.top = `${containerRect.height - elRect.height}px`;
            }
          }
        };

        const onMouseUp = () => {
          document.removeEventListener('mousemove', onMouseMove);
          document.removeEventListener('mouseup', onMouseUp);
        };

        el.addEventListener('mousedown', onMouseDown);
      });

      return elements.length;
    },

    setPosition(selector: string, position: string, top?: string, left?: string, right?: string, bottom?: string, zIndex?: number): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;

      elements.forEach((el) => {
        el.style.position = position;
        if (top !== undefined) el.style.top = top;
        if (left !== undefined) el.style.left = left;
        if (right !== undefined) el.style.right = right;
        if (bottom !== undefined) el.style.bottom = bottom;
        if (zIndex !== undefined) el.style.zIndex = String(zIndex);
      });

      return elements.length;
    },

    moveToCoordinates(selector: string, x: number, y: number, transition: boolean = false): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;

      elements.forEach((el) => {
        if (getComputedStyle(el).position === 'static') {
          el.style.position = 'absolute';
        }

        if (transition) {
          el.style.transition = 'left 0.3s, top 0.3s';
        }

        el.style.left = `${x}px`;
        el.style.top = `${y}px`;

        if (transition) {
          setTimeout(() => el.style.transition = '', 300);
        }
      });

      return elements.length;
    },

    swapElements(selector1: string, selector2: string, animate: boolean = false): boolean {
      const el1 = document.querySelector(selector1) as HTMLElement;
      const el2 = document.querySelector(selector2) as HTMLElement;

      if (!el1 || !el2) throw new Error('One or both elements not found');

      const parent1 = el1.parentElement;
      const parent2 = el2.parentElement;
      const next1 = el1.nextSibling;
      const next2 = el2.nextSibling;

      if (animate) {
        el1.style.transition = 'transform 0.3s';
        el2.style.transition = 'transform 0.3s';
      }

      // Swap positions
      if (next2) {
        parent2!.insertBefore(el1, next2);
      } else {
        parent2!.appendChild(el1);
      }

      if (next1) {
        parent1!.insertBefore(el2, next1);
      } else {
        parent1!.appendChild(el2);
      }

      if (animate) {
        setTimeout(() => {
          el1.style.transition = '';
          el2.style.transition = '';
        }, 300);
      }

      return true;
    },

    reorderChildren(parentSelector: string, order: number[]): boolean {
      const parent = document.querySelector(parentSelector);
      if (!parent) throw new Error(`Parent not found: ${parentSelector}`);

      const children = Array.from(parent.children);

      if (order.length !== children.length) {
        throw new Error('Order array must match number of children');
      }

      // Create new order
      const newOrder = order.map(idx => children[idx]);

      // Clear parent
      parent.innerHTML = '';

      // Append in new order
      newOrder.forEach(child => parent.appendChild(child));

      return true;
    },

    bringToFront(selector: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;

      // Find highest z-index
      let maxZ = 1000;
      document.querySelectorAll('*').forEach((el: any) => {
        const z = parseInt(getComputedStyle(el).zIndex);
        if (!isNaN(z) && z > maxZ) maxZ = z;
      });

      elements.forEach((el) => {
        el.style.zIndex = String(maxZ + 1);
      });

      return elements.length;
    },

    sendToBack(selector: string): number {
      const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;

      elements.forEach((el) => {
        el.style.zIndex = '0';
      });

      return elements.length;
    },

    describeChange(tool: string, params: any): string {
      const desc: Record<string, string> = {
        dom_set_style: `Applied styles to "${params.selector}"`,
        dom_add_class: `Added classes to "${params.selector}": ${params.classes.join(', ')}`,
        dom_set_text: `Changed text of "${params.selector}"`,
        dom_set_html: `Changed HTML of "${params.selector}"`,
        dom_append_child: `Added content to "${params.parentSelector}"`,
        dom_remove: `Removed "${params.selector}"`,
        dom_set_font_family: `Changed font family of "${params.selector}" to ${params.fontFamily}`,
        dom_set_font_size: `Changed font size of "${params.selector}" to ${params.fontSize}`,
        dom_set_font_weight: `Changed font weight of "${params.selector}" to ${params.fontWeight}`,
        dom_set_font_style: `Changed font style of "${params.selector}" to ${params.fontStyle}`,
        dom_set_text_color: `Changed text color of "${params.selector}" to ${params.color}`,
        dom_set_text_align: `Aligned text of "${params.selector}" to ${params.align}`,
        dom_set_text_decoration: `Applied ${params.decoration} to "${params.selector}"`,
        dom_set_text_transform: `Transformed text of "${params.selector}" to ${params.transform}`,
        dom_set_line_height: `Changed line height of "${params.selector}" to ${params.lineHeight}`,
        dom_set_letter_spacing: `Changed letter spacing of "${params.selector}" to ${params.spacing}`,
        dom_set_word_spacing: `Changed word spacing of "${params.selector}" to ${params.spacing}`,
        dom_set_text_indent: `Changed text indent of "${params.selector}" to ${params.indent}`,
        dom_set_text_shadow: `Applied text shadow to "${params.selector}"`,
        dom_set_white_space: `Changed white space handling of "${params.selector}" to ${params.whiteSpace}`,
        dom_set_text_overflow: `Set text overflow of "${params.selector}" to ${params.overflow}`,
        dom_set_word_break: `Changed word break of "${params.selector}" to ${params.wordBreak}`,
        dom_format_text_bold: `Made "${params.selector}" ${params.bold ? 'bold' : 'normal weight'}`,
        dom_format_text_italic: `Made "${params.selector}" ${params.italic ? 'italic' : 'normal style'}`,
        dom_format_text_underline: `${params.underline ? 'Added underline to' : 'Removed underline from'} "${params.selector}"`,
        dom_format_text_strikethrough: `${params.strikethrough ? 'Added strikethrough to' : 'Removed strikethrough from'} "${params.selector}"`,
        dom_select_text: `Selected text in "${params.selector}"`,
        dom_wrap_text: `Wrapped text in "${params.selector}" with <${params.tag}>`,
        dom_load_google_font: `Loaded Google Font: ${params.fontFamily}`,
        dom_load_custom_font: `Loaded custom font: ${params.fontFamily}`,
        dom_get_available_fonts: `Retrieved available fonts list`,
        dom_apply_font_to_selection: `Applied ${params.fontFamily} to selected text`,
        dom_get_font_info: `Retrieved font info from "${params.selector}"`,
        dom_set_font_fallback: `Set font fallback stack for "${params.selector}"`,
        dom_enable_dragging: `Enabled dragging for "${params.selector}"`,
        dom_set_position: `Set position of "${params.selector}" to ${params.position}`,
        dom_move_to_coordinates: `Moved "${params.selector}" to (${params.x}, ${params.y})`,
        dom_swap_elements: `Swapped positions of "${params.selector1}" and "${params.selector2}"`,
        dom_reorder_children: `Reordered children of "${params.parentSelector}"`,
        dom_bring_to_front: `Brought "${params.selector}" to front`,
        dom_send_to_back: `Sent "${params.selector}" to back`,
      };
      return desc[tool] || `Executed ${tool}`;
    },

    getChangeSummary(): string {
      return this.changeLog.map(c => c.description).join('\n') || 'No changes made';
    },

    clearChangeLog(): void {
      this.changeLog = [];
    }
  };

  // Connect to WebSocket server
  function connectWebSocket() {
    const wsPort = 9877; // Default WebSocket port
    ws = new WebSocket(`ws://localhost:${wsPort}`);

    ws.onopen = () => {
      console.log('✓ Visual Editor connected');

      // Send project info
      if (projectInfo) {
        ws.send(JSON.stringify({
          type: 'init',
          projectInfo,
        }));
      }
    };

    ws.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data);
        handleWSMessage(message);
      } catch (error) {
        console.error('WS message error:', error);
      }
    };

    ws.onerror = (error) => {
      console.error('WebSocket error:', error);
    };

    ws.onclose = () => {
      console.log('Visual Editor disconnected');
      // Attempt reconnect after 3 seconds
      setTimeout(connectWebSocket, 3000);
    };
  }

  // Handle WebSocket messages
  function handleWSMessage(message: any) {
    switch (message.type) {
      case 'ready':
        console.log('✓ Visual Editor ready');
        sessionId = message.sessionId || '';
        break;

      case 'dom_tool_call':
        // AI wants to execute a DOM tool
        executeDOMTool(message.tool, message.params);
        break;

      case 'dom_preview_complete':
        // AI finished DOM manipulation, show save option
        showSaveChangesPrompt(message.summary);
        break;

      case 'code_preview':
        showCodePreview(message.changes, message.explanation);
        break;

      case 'changes_applied':
        showSuccessNotification('Changes applied! Page will reload...');
        setTimeout(() => window.location.reload(), 1500);
        break;

      case 'component_generated':
        // AI generated component HTML
        showComponentPreview(message.html, message.explanation);
        showQuickNotification('✓ Component generated!', '#10b981');
        break;

      case 'component_refined':
        // AI refined the component
        showComponentPreview(message.html, message.explanation);
        showQuickNotification('✓ Component refined!', '#10b981');
        break;

      case 'error':
        showErrorNotification(message.error);
        break;
    }
  }

  // Execute DOM tool from AI
  async function executeDOMTool(toolName: string, params: any) {
    console.log(`🔧 Executing: ${toolName}`, params);

    const result = await domExecutor.execute(toolName, params);

    // Log AI change to combined log
    allChanges.push({
      timestamp: Date.now(),
      source: 'ai',
      tool: toolName,
      params,
      result,
      description: domExecutor.describeChange(toolName, params),
    });

    // Send result back to AI
    if (ws) {
      ws.send(JSON.stringify({
        type: 'dom_tool_result',
        tool: toolName,
        result,
      }));
    }

    // Visual feedback
    if (result.success) {
      showQuickNotification(`✓ ${domExecutor.describeChange(toolName, params)}`, '#10b981');
    } else {
      showQuickNotification(`✗ Failed: ${result.error}`, '#ef4444');
    }
  }

  // Detect project information
  function detectProject() {
    const project: any = {
      framework: detectFramework(),
      buildTool: detectBuildTool(),
      cssFramework: detectCssFramework(),
      root: '',
      componentPaths: [],
      assetPaths: [],
      page: {
        url: window.location.href,
        title: document.title,
        lang: document.documentElement.lang || 'en',
        locale: navigator.language,
      },
      components: detectActiveComponents(),
    };

    return project;
  }

  function detectFramework(): string {
    if ((window as any).Livewire) return 'laravel-livewire';
    if ((window as any).React) return 'react';
    if ((window as any).Vue) return 'vue';
    if ((window as any).ng) return 'angular';
    return 'unknown';
  }

  function detectBuildTool(): string {
    // Check for Vite
    if ((window as any).__vite_plugin_react_preamble_installed__) return 'vite';
    // Check for Webpack
    if ((window as any).webpackChunk) return 'webpack';
    return 'unknown';
  }

  function detectCssFramework(): string {
    // Simple detection based on common class patterns
    const hasElement = (selector: string) => document.querySelector(selector);

    if (hasElement('[class*="tw-"]') || hasElement('[class*="bg-"]')) {
      return 'tailwindcss';
    }
    if (hasElement('[class*="btn-"]') || hasElement('[class*="col-"]')) {
      return 'bootstrap';
    }
    return 'custom';
  }

  function detectActiveComponents(): any[] {
    const components: any[] = [];

    // Livewire
    if ((window as any).Livewire) {
      const livewireComponents = (window as any).Livewire.all();
      for (const component of livewireComponents) {
        components.push({
          name: component.name,
          type: 'livewire',
          id: component.id,
        });
      }
    }

    return components;
  }

  // Global highlight functions
  let globalHighlightBox: HTMLElement | null = null;

  function highlightElement(element: HTMLElement) {
    if (!globalHighlightBox) {
      globalHighlightBox = document.createElement('div');
      globalHighlightBox.style.cssText = `
        position: absolute;
        background: rgba(59, 130, 246, 0.2);
        border: 2px solid #3b82f6;
        pointer-events: none;
        z-index: 999998;
      `;
      document.body.appendChild(globalHighlightBox);
    }

    const rect = element.getBoundingClientRect();
    globalHighlightBox.style.top = `${rect.top + window.scrollY}px`;
    globalHighlightBox.style.left = `${rect.left + window.scrollX}px`;
    globalHighlightBox.style.width = `${rect.width}px`;
    globalHighlightBox.style.height = `${rect.height}px`;
    globalHighlightBox.style.display = 'block';
  }

  function removeHighlight() {
    if (globalHighlightBox) {
      globalHighlightBox.style.display = 'none';
    }
  }

  // Activate inspector
  function activateInspector() {
    if (inspectorActive) return;

    inspectorActive = true;
    selectedElements = [];
    allChanges = [];
    createOverlay();
    createCommandPanel();
    createVisualToolbar();
    enableElementSelection();
    connectWebSocket();
  }

  // Deactivate inspector
  function deactivateInspector() {
    inspectorActive = false;
    if (overlay) {
      overlay.remove();
      overlay = null;
    }
    if (commandPanel) {
      commandPanel.remove();
      commandPanel = null;
    }
    if (toolbar) {
      toolbar.remove();
      toolbar = null;
    }
    hideVisualTools();
    selectedElements = [];
    allChanges = [];

    // Reset button color
    const button = document.getElementById('zima-visual-editor-toggle');
    if (button) {
      (button as HTMLElement).style.background = 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)';
    }
  }

  // Create overlay UI
  function createOverlay() {
    overlay = document.createElement('div');
    overlay.id = 'visual-editor-overlay';
    overlay.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.05);
      z-index: 999999;
      pointer-events: none;
    `;

    const statusBar = document.createElement('div');
    statusBar.style.cssText = `
      position: fixed;
      top: 10px;
      right: 10px;
      background: #1e293b;
      color: white;
      padding: 8px 16px;
      border-radius: 6px;
      font-family: monospace;
      font-size: 12px;
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
      z-index: 1000000;
      pointer-events: auto;
      cursor: pointer;
    `;
    statusBar.textContent = '✓ Inspector Active (ESC to exit)';
    statusBar.onclick = deactivateInspector;

    document.body.appendChild(overlay);
    document.body.appendChild(statusBar);
  }

  // Enable element selection
  function enableElementSelection() {
    let highlightBox: HTMLElement | null = null;

    const highlight = (element: HTMLElement) => {
      if (!highlightBox) {
        highlightBox = document.createElement('div');
        highlightBox.style.cssText = `
          position: absolute;
          background: rgba(59, 130, 246, 0.2);
          border: 2px solid #3b82f6;
          pointer-events: none;
          z-index: 999998;
        `;
        document.body.appendChild(highlightBox);
      }

      const rect = element.getBoundingClientRect();
      highlightBox.style.top = `${rect.top + window.scrollY}px`;
      highlightBox.style.left = `${rect.left + window.scrollX}px`;
      highlightBox.style.width = `${rect.width}px`;
      highlightBox.style.height = `${rect.height}px`;
      highlightBox.style.display = 'block';
    };

    const removeHighlight = () => {
      if (highlightBox) {
        highlightBox.style.display = 'none';
      }
    };

    document.addEventListener('mouseover', (e) => {
      if (!inspectorActive) return;
      const target = e.target as HTMLElement;

      // Don't highlight editor UI elements
      if (
        target === overlay ||
        target.id === 'zima-visual-editor-toggle' ||
        target.closest('#zima-visual-editor-toggle') ||
        target.closest('#zima-command-panel') ||
        target.id === 'visual-editor-overlay'
      ) {
        removeHighlight();
        return;
      }

      highlight(target);
    });

    document.addEventListener('mouseout', () => {
      if (!inspectorActive) return;
      removeHighlight();
    });

    document.addEventListener('click', async (e) => {
      if (!inspectorActive) return;

      const target = e.target as HTMLElement;

      // Exclude editor UI elements from selection
      if (
        commandPanel && commandPanel.contains(target) ||
        target.id === 'zima-visual-editor-toggle' ||
        target.closest('#zima-visual-editor-toggle') ||
        target.closest('#zima-command-panel') ||
        target.id === 'visual-editor-overlay' ||
        target.classList.contains('visual-editor-modal')
      ) {
        return; // Don't select editor UI elements
      }

      e.preventDefault();
      e.stopPropagation();

      // Show element action tooltip
      showElementActionTooltip(target, e.clientX, e.clientY);

      removeHighlight();
    });

    // ESC to deactivate
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && inspectorActive) {
        deactivateInspector();
      }
    });
  }

  // Capture element context
  async function captureElementContext(element: HTMLElement) {
    const rect = element.getBoundingClientRect();

    return {
      visual: {
        screenshot: '', // Would capture with html2canvas
        boundingBox: {
          x: rect.left,
          y: rect.top,
          width: rect.width,
          height: rect.height,
        },
        viewport: {
          width: window.innerWidth,
          height: window.innerHeight,
        },
        devicePixelRatio: window.devicePixelRatio,
      },
      element: {
        tag: element.tagName.toLowerCase(),
        id: element.id,
        classes: Array.from(element.classList),
        attributes: getAttributes(element),
        textContent: element.textContent?.trim() || '',
        innerHTML: element.innerHTML,
      },
      selector: generateSelector(element),
      component: detectComponentBinding(element),
      styles: getComputedStylesObject(element),
    };
  }

  function getAttributes(element: HTMLElement): Record<string, string> {
    const attrs: Record<string, string> = {};
    for (let i = 0; i < element.attributes.length; i++) {
      const attr = element.attributes[i];
      attrs[attr.name] = attr.value;
    }
    return attrs;
  }

  function generateSelector(element: HTMLElement): string {
    if (element.id) return `#${element.id}`;
    const path: string[] = [];
    let current: Element | null = element;

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

  function detectComponentBinding(element: HTMLElement): any {
    // Livewire
    if (element.hasAttribute('wire:id')) {
      return {
        type: 'livewire',
        name: element.getAttribute('wire:id'),
        id: element.getAttribute('wire:id'),
      };
    }
    return null;
  }

  function getComputedStylesObject(element: HTMLElement): Record<string, string> {
    const computed = window.getComputedStyle(element);
    const styles: Record<string, string> = {};
    const importantProps = [
      'display', 'flexDirection', 'justifyContent', 'alignItems',
      'padding', 'margin', 'background', 'color', 'fontSize',
      'fontWeight', 'borderRadius', 'border',
    ];

    for (const prop of importantProps) {
      styles[prop] = computed.getPropertyValue(prop);
    }

    return styles;
  }

  // Create command panel
  function createCommandPanel() {
    commandPanel = document.createElement('div');
    commandPanel.id = 'zima-command-panel';
    commandPanel.style.cssText = `
      position: fixed;
      bottom: 20px;
      left: 50%;
      transform: translateX(-50%);
      background: white;
      padding: 16px;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
      z-index: 1000001;
      width: 600px;
      max-width: 90%;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    commandPanel.innerHTML = `
      <div style="margin-bottom: 12px;">
        <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">
          Selected Elements (click elements to select):
        </div>
        <div id="selected-tags" style="display: flex; flex-wrap: wrap; gap: 6px; min-height: 32px; align-items: center;">
          <span style="color: #94a3b8; font-size: 13px;">No elements selected</span>
        </div>
      </div>
      <div style="display: flex; gap: 8px;">
        <textarea
          id="command-input"
          style="flex: 1; height: 60px; padding: 10px 12px; border: 2px solid #e2e8f0; border-radius: 8px; font-family: inherit; font-size: 14px; resize: none; outline: none; transition: border-color 0.2s;"
          placeholder="Optional: Describe changes (e.g., 'Add icons') OR just use visual tools and click Save"></textarea>
        <div style="display: flex; flex-direction: column; gap: 8px;">
          <button
            id="submit-command-btn"
            style="padding: 8px 24px; border: none; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 8px; cursor: pointer; font-weight: 600; font-size: 14px; transition: transform 0.2s, box-shadow 0.2s; box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);"
            onmouseover="this.style.transform='translateY(-2px)'; this.style.boxShadow='0 6px 16px rgba(102, 126, 234, 0.4)';"
            onmouseout="this.style.transform='translateY(0)'; this.style.boxShadow='0 4px 12px rgba(102, 126, 234, 0.3)';">
            AI Edit
          </button>
          <button
            id="save-visual-btn"
            style="padding: 8px 24px; border: none; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border-radius: 8px; cursor: pointer; font-weight: 600; font-size: 14px; transition: transform 0.2s, box-shadow 0.2s; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);"
            onmouseover="this.style.transform='translateY(-2px)'; this.style.boxShadow='0 6px 16px rgba(16, 185, 129, 0.4)';"
            onmouseout="this.style.transform='translateY(0)'; this.style.boxShadow='0 4px 12px rgba(16, 185, 129, 0.3)';">
            💾 Save
          </button>
        </div>
      </div>
      <div style="margin-top: 10px; font-size: 12px; color: #94a3b8;">
        💡 Type AI commands OR use visual tools (toolbar, resize, style panel) → then click Save
      </div>
    `;

    document.body.appendChild(commandPanel);

    // Focus textarea on input
    const textarea = document.getElementById('command-input') as HTMLTextAreaElement;
    textarea.onfocus = () => {
      (textarea as HTMLElement).style.borderColor = '#667eea';
    };
    textarea.onblur = () => {
      (textarea as HTMLElement).style.borderColor = '#e2e8f0';
    };

    // AI Edit handler
    document.getElementById('submit-command-btn')!.onclick = async () => {
      const command = textarea.value.trim();
      if (!command) {
        textarea.style.borderColor = '#ef4444';
        setTimeout(() => {
          textarea.style.borderColor = '#e2e8f0';
        }, 500);
        return;
      }

      if (selectedElements.length === 0) {
        showNotification('⚠️ Please select at least one element first', '#f59e0b');
        return;
      }

      // Capture context for all selected elements
      const contexts = await Promise.all(
        selectedElements.map(el => captureElementContext(el))
      );

      if (ws) {
        ws.send(JSON.stringify({
          type: 'command_submitted',
          contexts,
          command,
        }));
        textarea.value = '';
        selectedElements = [];
        updateCommandPanel();
        hideVisualTools();
        showLoadingIndicator();
      }
    };

    // Save Visual Changes handler (no AI command needed)
    document.getElementById('save-visual-btn')!.onclick = () => {
      if (allChanges.length === 0) {
        showQuickNotification('⚠️ No changes to save. Make some edits first!', '#f59e0b');
        return;
      }

      // Show save prompt directly
      showSaveChangesPrompt();
    };

    // Enter to submit
    textarea.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        document.getElementById('submit-command-btn')!.click();
      }
    });
  }

  // Update command panel with selected elements
  function updateCommandPanel() {
    if (!commandPanel) return;

    const tagsContainer = document.getElementById('selected-tags');
    if (!tagsContainer) return;

    if (selectedElements.length === 0) {
      tagsContainer.innerHTML = '<span style="color: #94a3b8; font-size: 13px;">No elements selected</span>';
      return;
    }

    tagsContainer.innerHTML = selectedElements.map((el, index) => {
      const tag = el.tagName.toLowerCase();
      const id = el.id ? `#${el.id}` : '';
      const classes = el.classList.length > 0 ? `.${Array.from(el.classList).slice(0, 2).join('.')}` : '';
      const label = `${tag}${id}${classes}`;

      return `
        <div style="display: inline-flex; align-items: center; gap: 6px; background: linear-gradient(135deg, #667eea15, #764ba215); border: 1px solid #667eea30; padding: 4px 10px; border-radius: 6px; font-size: 13px; color: #475569;">
          <span style="font-family: 'Courier New', monospace;">${label}</span>
          <button
            onclick="window.removeSelectedElement(${index})"
            style="background: none; border: none; color: #64748b; cursor: pointer; padding: 0; font-size: 16px; line-height: 1; transition: color 0.2s;"
            onmouseover="this.style.color='#ef4444'"
            onmouseout="this.style.color='#64748b'">
            ×
          </button>
        </div>
      `;
    }).join('');
  }

  // Remove selected element
  (window as any).removeSelectedElement = (index: number) => {
    selectedElements.splice(index, 1);
    updateCommandPanel();
    if (selectedElements.length === 0) {
      hideVisualTools();
    }
  };

  // ==========================================
  // ELEMENT ACTION TOOLTIP
  // ==========================================

  function showElementActionTooltip(element: HTMLElement, clickX: number, clickY: number) {
    // Remove existing tooltip
    if (elementActionTooltip) {
      elementActionTooltip.remove();
    }

    // Highlight the element
    highlightElement(element);

    // Create tooltip
    elementActionTooltip = document.createElement('div');
    elementActionTooltip.className = 'visual-editor-modal';

    const rect = element.getBoundingClientRect();
    const tag = element.tagName.toLowerCase();
    const id = element.id ? `#${element.id}` : '';
    const classes = element.classList.length > 0 ? `.${Array.from(element.classList).slice(0, 2).join('.')}` : '';
    const elementLabel = `${tag}${id}${classes}`;

    const isInContext = contextElements.includes(element);

    elementActionTooltip.style.cssText = `
      position: fixed;
      left: ${Math.min(clickX + 10, window.innerWidth - 250)}px;
      top: ${Math.min(clickY + 10, window.innerHeight - 300)}px;
      background: white;
      padding: 12px;
      border-radius: 10px;
      box-shadow: 0 6px 30px rgba(0, 0, 0, 0.2);
      z-index: 1000010;
      min-width: 220px;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      border: 2px solid #667eea;
    `;

    elementActionTooltip.innerHTML = `
      <div style="margin-bottom: 10px; padding-bottom: 8px; border-bottom: 1px solid #e2e8f0;">
        <div style="font-size: 11px; color: #94a3b8; text-transform: uppercase; font-weight: 600; letter-spacing: 0.5px; margin-bottom: 4px;">Selected Element</div>
        <code style="font-size: 13px; color: #475569; font-family: 'Courier New', monospace; font-weight: 500;">${elementLabel}</code>
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px;">
        ${isInContext ? `
          <div style="padding: 6px 10px; background: #10b98115; border: 1px solid #10b98130; border-radius: 6px; font-size: 12px; color: #059669; font-weight: 500; text-align: center;">
            ✓ In Context (${contextElements.length} total)
          </div>
        ` : `
          <button class="tooltip-action-btn" data-action="add-to-context" style="
            padding: 8px 12px;
            background: linear-gradient(135deg, #667eea, #764ba2);
            color: white;
            border: none;
            border-radius: 6px;
            font-size: 13px;
            font-weight: 500;
            cursor: pointer;
            transition: all 0.2s;
            box-shadow: 0 2px 4px rgba(102, 126, 234, 0.3);
          "
          onmouseover="this.style.transform='translateY(-1px)'; this.style.boxShadow='0 4px 8px rgba(102, 126, 234, 0.4)'"
          onmouseout="this.style.transform=''; this.style.boxShadow='0 2px 4px rgba(102, 126, 234, 0.3)'">
            ➕ Add to Context
          </button>
        `}

        <button class="tooltip-action-btn" data-action="edit-now" style="
          padding: 8px 12px;
          background: white;
          color: #667eea;
          border: 1px solid #667eea;
          border-radius: 6px;
          font-size: 13px;
          font-weight: 500;
          cursor: pointer;
          transition: all 0.2s;
        "
        onmouseover="this.style.background='#f8fafc'"
        onmouseout="this.style.background='white'">
          ✏️ Edit Now
        </button>

        <button class="tooltip-action-btn" data-action="insert-component" style="
          padding: 8px 12px;
          background: white;
          color: #8b5cf6;
          border: 1px solid #8b5cf6;
          border-radius: 6px;
          font-size: 13px;
          font-weight: 500;
          cursor: pointer;
          transition: all 0.2s;
        "
        onmouseover="this.style.background='#f8fafc'"
        onmouseout="this.style.background='white'">
          ➕ Insert Component
        </button>

        <button class="tooltip-action-btn" data-action="inspect" style="
          padding: 8px 12px;
          background: white;
          color: #0891b2;
          border: 1px solid #0891b2;
          border-radius: 6px;
          font-size: 13px;
          font-weight: 500;
          cursor: pointer;
          transition: all 0.2s;
        "
        onmouseover="this.style.background='#f8fafc'"
        onmouseout="this.style.background='white'">
          🔍 Inspect
        </button>

        <button class="tooltip-action-btn" data-action="delete" style="
          padding: 8px 12px;
          background: white;
          color: #ef4444;
          border: 1px solid #ef4444;
          border-radius: 6px;
          font-size: 13px;
          font-weight: 500;
          cursor: pointer;
          transition: all 0.2s;
        "
        onmouseover="this.style.background='#fef2f2'"
        onmouseout="this.style.background='white'">
          🗑️ Delete
        </button>
      </div>

      ${contextElements.length > 0 ? `
        <div style="margin-top: 10px; padding-top: 8px; border-top: 1px solid #e2e8f0;">
          <div style="font-size: 11px; color: #94a3b8; margin-bottom: 4px;">Context: ${contextElements.length} element${contextElements.length > 1 ? 's' : ''}</div>
          <button class="tooltip-action-btn" data-action="view-context" style="
            width: 100%;
            padding: 6px 10px;
            background: #f1f5f9;
            color: #475569;
            border: 1px solid #cbd5e1;
            border-radius: 6px;
            font-size: 12px;
            font-weight: 500;
            cursor: pointer;
            transition: all 0.2s;
          "
          onmouseover="this.style.background='#e2e8f0'"
          onmouseout="this.style.background='#f1f5f9'">
            👁️ View Context
          </button>
        </div>
      ` : ''}

      <button class="tooltip-action-btn" data-action="close" style="
        margin-top: 8px;
        width: 100%;
        padding: 6px 10px;
        background: #f8fafc;
        color: #64748b;
        border: 1px solid #e2e8f0;
        border-radius: 6px;
        font-size: 12px;
        cursor: pointer;
        transition: all 0.2s;
      "
      onmouseover="this.style.background='#f1f5f9'"
      onmouseout="this.style.background='#f8fafc'">
        Close
      </button>
    `;

    document.body.appendChild(elementActionTooltip);

    // Add event listeners to buttons
    const buttons = elementActionTooltip.querySelectorAll('.tooltip-action-btn');
    buttons.forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const action = (btn as HTMLElement).dataset.action;
        handleTooltipAction(action!, element);
      });
    });

    // Close on outside click
    setTimeout(() => {
      document.addEventListener('click', closeTooltipOnOutsideClick);
    }, 100);
  }

  function closeTooltipOnOutsideClick(e: MouseEvent) {
    if (elementActionTooltip && !elementActionTooltip.contains(e.target as Node)) {
      closeTooltip();
    }
  }

  function closeTooltip() {
    if (elementActionTooltip) {
      elementActionTooltip.remove();
      elementActionTooltip = null;
      removeHighlight();
      document.removeEventListener('click', closeTooltipOnOutsideClick);
    }
  }

  function handleTooltipAction(action: string, element: HTMLElement) {
    closeTooltip();

    switch (action) {
      case 'add-to-context':
        addElementToContext(element);
        break;

      case 'edit-now':
        // Single element edit - add to selection and show tools
        selectedElements = [element];
        updateCommandPanel();
        showResizeHandles(element);
        if (!commandPanel) {
          createCommandPanel();
        }
        commandPanel!.style.display = 'block';
        break;

      case 'insert-component':
        showComponentInserter(element);
        break;

      case 'inspect':
        showElementInspector(element);
        break;

      case 'delete':
        if (confirm(`Delete ${element.tagName.toLowerCase()}?`)) {
          element.remove();
          logVisualChange('delete-element', { selector: getElementSelector(element) });
          showQuickNotification('✓ Element deleted', '#10b981');
        }
        break;

      case 'view-context':
        viewContextElements();
        break;

      case 'close':
        // Already closed
        break;
    }
  }

  function addElementToContext(element: HTMLElement) {
    if (!contextElements.includes(element)) {
      contextElements.push(element);

      // Visual indicator on element
      element.style.outline = '2px dashed #10b981';
      element.style.outlineOffset = '2px';

      showQuickNotification(`✓ Added to context (${contextElements.length} total)`, '#10b981');
      updateContextBadge();
    }
  }

  function viewContextElements() {
    if (contextElements.length === 0) return;

    // Add all context elements to selection
    selectedElements = [...contextElements];
    updateCommandPanel();

    if (!commandPanel) {
      createCommandPanel();
    }
    commandPanel!.style.display = 'block';

    showQuickNotification(`Viewing ${contextElements.length} context elements`, '#667eea');
  }

  function updateContextBadge() {
    let badge = document.getElementById('context-badge');

    if (contextElements.length > 0) {
      if (!badge) {
        badge = document.createElement('div');
        badge.id = 'context-badge';
        badge.style.cssText = `
          position: fixed;
          top: 20px;
          right: 20px;
          background: linear-gradient(135deg, #10b981, #059669);
          color: white;
          padding: 10px 16px;
          border-radius: 50px;
          box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
          z-index: 1000002;
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
          font-size: 13px;
          font-weight: 600;
          cursor: pointer;
          transition: all 0.3s;
        `;
        badge.onmouseover = () => {
          badge!.style.transform = 'scale(1.05)';
          badge!.style.boxShadow = '0 6px 16px rgba(16, 185, 129, 0.5)';
        };
        badge.onmouseout = () => {
          badge!.style.transform = '';
          badge!.style.boxShadow = '0 4px 12px rgba(16, 185, 129, 0.4)';
        };
        badge.onclick = viewContextElements;
        document.body.appendChild(badge);
      }

      badge.innerHTML = `
        <div style="display: flex; align-items: center; gap: 8px;">
          <span style="font-size: 16px;">📦</span>
          <span>Context: ${contextElements.length}</span>
          <button onclick="event.stopPropagation(); window.clearContext()" style="
            background: rgba(255,255,255,0.2);
            border: none;
            color: white;
            width: 20px;
            height: 20px;
            border-radius: 50%;
            cursor: pointer;
            font-size: 14px;
            line-height: 1;
            padding: 0;
            transition: background 0.2s;
          "
          onmouseover="this.style.background='rgba(255,255,255,0.3)'"
          onmouseout="this.style.background='rgba(255,255,255,0.2)'">×</button>
        </div>
      `;
    } else if (badge) {
      badge.remove();
    }
  }

  // Clear context
  (window as any).clearContext = () => {
    contextElements.forEach(el => {
      el.style.outline = '';
      el.style.outlineOffset = '';
    });
    contextElements = [];
    updateContextBadge();
    showQuickNotification('Context cleared', '#94a3b8');
  };

  function showElementInspector(element: HTMLElement) {
    const computed = window.getComputedStyle(element);
    const rect = element.getBoundingClientRect();

    const inspector = document.createElement('div');
    inspector.className = 'visual-editor-modal';
    inspector.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 20px;
      border-radius: 12px;
      box-shadow: 0 6px 30px rgba(0, 0, 0, 0.2);
      z-index: 1000011;
      width: 500px;
      max-height: 70vh;
      overflow-y: auto;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    inspector.innerHTML = `
      <h3 style="margin: 0 0 16px 0; font-size: 18px; color: #1e293b;">Element Inspector</h3>

      <div style="background: #f8fafc; padding: 12px; border-radius: 6px; margin-bottom: 16px;">
        <code style="font-size: 13px; color: #475569; font-family: 'Courier New', monospace;">${getElementSelector(element)}</code>
      </div>

      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 16px;">
        <div>
          <div style="font-size: 11px; color: #94a3b8; margin-bottom: 4px;">Tag</div>
          <div style="font-size: 14px; color: #1e293b; font-weight: 500;">${element.tagName.toLowerCase()}</div>
        </div>
        <div>
          <div style="font-size: 11px; color: #94a3b8; margin-bottom: 4px;">ID</div>
          <div style="font-size: 14px; color: #1e293b; font-weight: 500;">${element.id || '(none)'}</div>
        </div>
        <div>
          <div style="font-size: 11px; color: #94a3b8; margin-bottom: 4px;">Width × Height</div>
          <div style="font-size: 14px; color: #1e293b; font-weight: 500;">${Math.round(rect.width)}px × ${Math.round(rect.height)}px</div>
        </div>
        <div>
          <div style="font-size: 11px; color: #94a3b8; margin-bottom: 4px;">Position</div>
          <div style="font-size: 14px; color: #1e293b; font-weight: 500;">${computed.position}</div>
        </div>
      </div>

      <div style="margin-bottom: 12px;">
        <div style="font-size: 11px; color: #94a3b8; margin-bottom: 6px; font-weight: 600;">CLASSES</div>
        <div style="background: #f8fafc; padding: 8px; border-radius: 6px; font-size: 12px; font-family: monospace; color: #475569;">
          ${element.className || '(no classes)'}
        </div>
      </div>

      <div style="margin-bottom: 12px;">
        <div style="font-size: 11px; color: #94a3b8; margin-bottom: 6px; font-weight: 600;">TEXT CONTENT</div>
        <div style="background: #f8fafc; padding: 8px; border-radius: 6px; font-size: 12px; color: #475569; max-height: 100px; overflow-y: auto;">
          ${element.textContent?.trim().substring(0, 200) || '(empty)'}
        </div>
      </div>

      <button onclick="this.parentElement.remove()" style="
        width: 100%;
        padding: 8px 12px;
        background: #667eea;
        color: white;
        border: none;
        border-radius: 6px;
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
      ">Close</button>
    `;

    document.body.appendChild(inspector);
  }

  // ==========================================
  // VISUAL EDITING TOOLS
  // ==========================================

  let resizeHandles: HTMLElement | null = null;
  let stylePanel: HTMLElement | null = null;
  let toolbar: HTMLElement | null = null;
  let isDragging = false;
  let isResizing = false;
  let dragStartX = 0;
  let dragStartY = 0;
  let originalWidth = 0;
  let originalHeight = 0;
  let resizeDirection = '';

  function createVisualToolbar() {
    toolbar = document.createElement('div');
    toolbar.id = 'visual-editor-toolbar';
    toolbar.className = 'visual-editor-modal';
    toolbar.style.cssText = `
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
      { icon: '📏', label: 'Resize', id: 'resize-mode' },
      { icon: '🖱️', label: 'Move (Drag)', id: 'move-mode' },
      { icon: '🎨', label: 'Style Panel', id: 'toggle-style' },
      { icon: '🔤', label: 'Load Font', id: 'load-font' },
      { icon: '➕', label: 'Insert Component Here', id: 'insert-component' },
      { separator: true },
      { icon: '⬅️', label: 'Align Left', id: 'align-left' },
      { icon: '🎯', label: 'Center', id: 'center' },
      { icon: '➡️', label: 'Align Right', id: 'align-right' },
      { separator: true },
      { icon: 'B', label: 'Bold', id: 'text-bold', isText: true },
      { icon: 'I', label: 'Italic', id: 'text-italic', isText: true },
      { icon: 'U', label: 'Underline', id: 'text-underline', isText: true },
      { icon: 'S', label: 'Strikethrough', id: 'text-strike', isText: true },
      { separator: true },
      { icon: '⬆️', label: 'Bring to Front', id: 'bring-front' },
      { icon: '⬇️', label: 'Send to Back', id: 'send-back' },
      { separator: true },
      { icon: '📑', label: 'Duplicate', id: 'duplicate' },
      { icon: '🗑️', label: 'Delete', id: 'delete' },
    ];

    tools.forEach(tool => {
      if (tool.separator) {
        const sep = document.createElement('div');
        sep.style.cssText = 'width: 1px; height: 24px; background: #e2e8f0; margin: 0 4px;';
        toolbar!.appendChild(sep);
      } else {
        const btn = document.createElement('button');
        btn.className = 'visual-tool-btn';
        btn.title = tool.label;
        const isText = (tool as any).isText;
        btn.style.cssText = `
          width: 36px;
          height: 36px;
          border: none;
          background: #f8fafc;
          border-radius: 6px;
          cursor: pointer;
          font-size: ${isText ? '16px' : '18px'};
          font-weight: ${isText ? 'bold' : 'normal'};
          font-family: ${isText ? 'Georgia, serif' : 'inherit'};
          font-style: ${tool.id === 'text-italic' ? 'italic' : 'normal'};
          text-decoration: ${tool.id === 'text-underline' ? 'underline' : (tool.id === 'text-strike' ? 'line-through' : 'none')};
          display: flex;
          align-items: center;
          justify-content: center;
          transition: all 0.2s;
        `;
        btn.innerHTML = tool.icon;
        btn.onclick = () => handleToolClick(tool.id);
        btn.onmouseover = () => {
          btn.style.background = '#e2e8f0';
          btn.style.transform = 'scale(1.05)';
        };
        btn.onmouseout = () => {
          btn.style.background = '#f8fafc';
          btn.style.transform = 'scale(1)';
        };
        toolbar!.appendChild(btn);
      }
    });

    document.body.appendChild(toolbar);
  }

  function handleToolClick(toolId: string) {
    if (selectedElements.length === 0) {
      showQuickNotification('⚠️ Please select an element first', '#f59e0b');
      return;
    }

    const element = selectedElements[selectedElements.length - 1];

    switch (toolId) {
      case 'toggle-style':
        toggleStylePanel();
        break;
      case 'align-left':
        element.style.marginLeft = '0';
        element.style.marginRight = 'auto';
        logVisualChange('align', { direction: 'left', selector: getElementSelector(element) });
        break;
      case 'center':
        element.style.marginLeft = 'auto';
        element.style.marginRight = 'auto';
        logVisualChange('center', { selector: getElementSelector(element) });
        break;
      case 'align-right':
        element.style.marginLeft = 'auto';
        element.style.marginRight = '0';
        logVisualChange('align', { direction: 'right', selector: getElementSelector(element) });
        break;
      case 'text-bold':
        const isBold = element.style.fontWeight === 'bold' || element.style.fontWeight === '700';
        element.style.fontWeight = isBold ? 'normal' : 'bold';
        logVisualChange('text-format', {
          format: 'bold',
          value: !isBold,
          selector: getElementSelector(element)
        });
        showQuickNotification(isBold ? '✓ Bold removed' : '✓ Bold applied', '#10b981');
        break;
      case 'text-italic':
        const isItalic = element.style.fontStyle === 'italic';
        element.style.fontStyle = isItalic ? 'normal' : 'italic';
        logVisualChange('text-format', {
          format: 'italic',
          value: !isItalic,
          selector: getElementSelector(element)
        });
        showQuickNotification(isItalic ? '✓ Italic removed' : '✓ Italic applied', '#10b981');
        break;
      case 'text-underline':
        const hasUnderline = element.style.textDecoration.includes('underline');
        element.style.textDecoration = hasUnderline ? 'none' : 'underline';
        logVisualChange('text-format', {
          format: 'underline',
          value: !hasUnderline,
          selector: getElementSelector(element)
        });
        showQuickNotification(hasUnderline ? '✓ Underline removed' : '✓ Underline applied', '#10b981');
        break;
      case 'text-strike':
        const hasStrike = element.style.textDecoration.includes('line-through');
        element.style.textDecoration = hasStrike ? 'none' : 'line-through';
        logVisualChange('text-format', {
          format: 'strikethrough',
          value: !hasStrike,
          selector: getElementSelector(element)
        });
        showQuickNotification(hasStrike ? '✓ Strikethrough removed' : '✓ Strikethrough applied', '#10b981');
        break;
      case 'move-mode':
        enableElementDragging(element);
        showQuickNotification('✓ Drag mode enabled - Click and drag to move', '#10b981');
        break;
      case 'load-font':
        showFontLoader();
        break;
      case 'insert-component':
        showComponentInserter(element);
        break;
      case 'bring-front':
        const maxZ = Math.max(...Array.from(document.querySelectorAll('*')).map((el: any) => {
          const z = parseInt(getComputedStyle(el).zIndex);
          return isNaN(z) ? 0 : z;
        }), 1000);
        element.style.zIndex = String(maxZ + 1);
        logVisualChange('z-index', { action: 'bring-front', selector: getElementSelector(element), zIndex: maxZ + 1 });
        showQuickNotification('✓ Brought to front', '#10b981');
        break;
      case 'send-back':
        element.style.zIndex = '0';
        logVisualChange('z-index', { action: 'send-back', selector: getElementSelector(element), zIndex: 0 });
        showQuickNotification('✓ Sent to back', '#10b981');
        break;
      case 'duplicate':
        const clone = element.cloneNode(true) as HTMLElement;
        element.parentElement?.appendChild(clone);
        logVisualChange('duplicate', { selector: getElementSelector(element) });
        showQuickNotification('✓ Element duplicated', '#10b981');
        break;
      case 'delete':
        if (confirm('Delete this element?')) {
          element.remove();
          selectedElements = selectedElements.filter(el => el !== element);
          updateCommandPanel();
          hideVisualTools();
          logVisualChange('delete', { selector: getElementSelector(element) });
          showQuickNotification('✓ Element deleted', '#10b981');
        }
        break;
    }
  }

  function enableElementDragging(element: HTMLElement) {
    // Make element draggable
    element.style.cursor = 'move';
    if (getComputedStyle(element).position === 'static') {
      element.style.position = 'relative';
    }

    let startX = 0, startY = 0, initialLeft = 0, initialTop = 0;
    let isDragging = false;

    const onMouseDown = (e: MouseEvent) => {
      if (e.button !== 0) return; // Only left click
      e.preventDefault();
      e.stopPropagation();

      isDragging = true;
      startX = e.clientX;
      startY = e.clientY;

      const computed = window.getComputedStyle(element);
      initialLeft = parseInt(computed.left) || 0;
      initialTop = parseInt(computed.top) || 0;

      document.addEventListener('mousemove', onMouseMove);
      document.addEventListener('mouseup', onMouseUp);
    };

    const onMouseMove = (e: MouseEvent) => {
      if (!isDragging) return;

      const deltaX = e.clientX - startX;
      const deltaY = e.clientY - startY;

      element.style.left = `${initialLeft + deltaX}px`;
      element.style.top = `${initialTop + deltaY}px`;
    };

    const onMouseUp = () => {
      if (isDragging) {
        logVisualChange('drag-move', {
          selector: getElementSelector(element),
          left: element.style.left,
          top: element.style.top
        });
      }
      isDragging = false;
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
    };

    element.addEventListener('mousedown', onMouseDown);
  }

  function showFontLoader() {
    const modal = document.createElement('div');
    modal.className = 'visual-editor-modal';
    modal.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 24px;
      border-radius: 12px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
      z-index: 1000004;
      width: 400px;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    modal.innerHTML = `
      <h3 style="margin: 0 0 16px 0; font-size: 18px; color: #1e293b;">Load Font</h3>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 14px; color: #64748b; margin-bottom: 8px;">
          Google Fonts (Popular)
        </label>
        <select id="font-preset" style="width: 100%; padding: 8px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 14px;">
          <option value="">-- Select a font --</option>
          <option value="Roboto">Roboto</option>
          <option value="Open Sans">Open Sans</option>
          <option value="Lato">Lato</option>
          <option value="Montserrat">Montserrat</option>
          <option value="Poppins">Poppins</option>
          <option value="Playfair Display">Playfair Display</option>
          <option value="Merriweather">Merriweather</option>
          <option value="Inter">Inter</option>
          <option value="Raleway">Raleway</option>
          <option value="Oswald">Oswald</option>
        </select>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 14px; color: #64748b; margin-bottom: 8px;">
          Or enter custom Google Font name
        </label>
        <input type="text" id="font-custom" placeholder="e.g., Dancing Script" style="width: 100%; padding: 8px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 14px;">
      </div>

      <div style="display: flex; gap: 8px; justify-content: flex-end;">
        <button id="font-cancel" style="padding: 8px 16px; border: 1px solid #e2e8f0; background: white; border-radius: 6px; cursor: pointer; font-size: 14px;">
          Cancel
        </button>
        <button id="font-load" style="padding: 8px 16px; border: none; background: #667eea; color: white; border-radius: 6px; cursor: pointer; font-size: 14px; font-weight: 500;">
          Load Font
        </button>
      </div>
    `;

    document.body.appendChild(modal);

    document.getElementById('font-cancel')!.onclick = () => modal.remove();
    document.getElementById('font-load')!.onclick = () => {
      const preset = (document.getElementById('font-preset') as HTMLSelectElement).value;
      const custom = (document.getElementById('font-custom') as HTMLInputElement).value;
      const fontName = custom || preset;

      if (!fontName) {
        showQuickNotification('⚠️ Please select or enter a font name', '#f59e0b');
        return;
      }

      // Load Google Font
      const link = document.createElement('link');
      link.rel = 'stylesheet';
      link.href = `https://fonts.googleapis.com/css2?family=${fontName.replace(/\s/g, '+')}:wght@400;700&display=swap`;
      document.head.appendChild(link);

      logVisualChange('load-font', { fontFamily: fontName });
      showQuickNotification(`✓ Loaded font: ${fontName}`, '#10b981');
      modal.remove();

      // Update style panel font dropdown
      updateStylePanelFontList(fontName);
    };
  }

  function updateStylePanelFontList(newFont: string) {
    const fontSelect = document.getElementById('vis-font-family') as HTMLSelectElement;
    if (fontSelect) {
      const option = document.createElement('option');
      option.value = `'${newFont}', sans-serif`;
      option.textContent = newFont;
      fontSelect.appendChild(option);
      fontSelect.value = option.value;
    }
  }

  // ==========================================
  // COMPONENT INSERTION
  // ==========================================

  let insertionTarget: HTMLElement | null = null;
  let componentPreviewModal: HTMLElement | null = null;
  let currentComponentHtml: string = '';

  function showComponentInserter(targetElement: HTMLElement) {
    insertionTarget = targetElement;

    // Tailwind component categories
    const tailwindComponents = {
      'Layout': [
        'Container', 'Grid', 'Flex Container', 'Stack', 'Divider', 'Spacer'
      ],
      'Forms': [
        'Text Input', 'Text Area', 'Select Dropdown', 'Checkbox', 'Radio Button',
        'Toggle Switch', 'Search Input', 'File Upload', 'Form Group'
      ],
      'Buttons': [
        'Primary Button', 'Secondary Button', 'Outline Button', 'Icon Button',
        'Button Group', 'Loading Button', 'Dropdown Button'
      ],
      'Cards': [
        'Basic Card', 'Card with Image', 'Product Card', 'Profile Card',
        'Stat Card', 'Pricing Card', 'Blog Card'
      ],
      'Navigation': [
        'Navbar', 'Sidebar', 'Breadcrumb', 'Tabs', 'Pagination',
        'Menu Dropdown', 'Mobile Menu'
      ],
      'Feedback': [
        'Alert', 'Toast Notification', 'Badge', 'Progress Bar',
        'Spinner', 'Skeleton Loader', 'Empty State'
      ],
      'Data Display': [
        'Table', 'List', 'Avatar', 'Tooltip', 'Popover',
        'Accordion', 'Timeline', 'Stats Grid'
      ],
      'Overlays': [
        'Modal Dialog', 'Drawer', 'Popover', 'Dropdown Menu',
        'Context Menu', 'Command Palette'
      ]
    };

    const modal = document.createElement('div');
    modal.className = 'visual-editor-modal';
    modal.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 24px;
      border-radius: 12px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
      z-index: 1000005;
      width: 500px;
      max-height: 70vh;
      overflow-y: auto;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    // Build component options grouped by category
    let optionsHtml = '<option value="">-- Select a component --</option>';
    for (const [category, components] of Object.entries(tailwindComponents)) {
      optionsHtml += `<optgroup label="${category}">`;
      components.forEach(comp => {
        optionsHtml += `<option value="${comp}">${comp}</option>`;
      });
      optionsHtml += '</optgroup>';
    }

    modal.innerHTML = `
      <h3 style="margin: 0 0 8px 0; font-size: 18px; color: #1e293b;">Insert Component Here</h3>
      <p style="margin: 0 0 16px 0; font-size: 13px; color: #64748b;">
        Inserting into: <code style="background: #f1f5f9; padding: 2px 6px; border-radius: 4px;">${getElementSelector(targetElement)}</code>
      </p>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 14px; color: #64748b; margin-bottom: 8px;">
          Search or Select Component
        </label>
        <input type="text" id="component-search" placeholder="Search components..."
          style="width: 100%; padding: 8px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 14px; margin-bottom: 8px;">
        <select id="component-select" size="10"
          style="width: 100%; padding: 8px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 14px;">
          ${optionsHtml}
        </select>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 14px; color: #64748b; margin-bottom: 8px;">
          Additional Instructions (Optional)
        </label>
        <textarea id="component-instructions" placeholder="e.g., Make it purple, add a shadow, include an icon..."
          style="width: 100%; padding: 8px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 14px; min-height: 60px; resize: vertical;"></textarea>
      </div>

      <div style="display: flex; gap: 8px; justify-content: flex-end;">
        <button id="component-cancel" style="padding: 8px 16px; border: 1px solid #e2e8f0; background: white; border-radius: 6px; cursor: pointer; font-size: 14px;">
          Cancel
        </button>
        <button id="component-generate" style="padding: 8px 16px; border: none; background: #667eea; color: white; border-radius: 6px; cursor: pointer; font-size: 14px; font-weight: 500;">
          Generate Component
        </button>
      </div>
    `;

    document.body.appendChild(modal);

    // Search functionality
    const searchInput = document.getElementById('component-search') as HTMLInputElement;
    const selectElement = document.getElementById('component-select') as HTMLSelectElement;

    searchInput.addEventListener('input', () => {
      const searchTerm = searchInput.value.toLowerCase();
      Array.from(selectElement.options).forEach((option: HTMLOptionElement) => {
        if (option.value && option.text) {
          const matches = option.text.toLowerCase().includes(searchTerm);
          option.style.display = matches ? '' : 'none';
        }
      });
    });

    // Select on click
    selectElement.addEventListener('change', () => {
      if (selectElement.value) {
        searchInput.value = selectElement.value;
      }
    });

    // Buttons
    document.getElementById('component-cancel')!.onclick = () => modal.remove();
    document.getElementById('component-generate')!.onclick = () => {
      const componentType = selectElement.value;
      const instructions = (document.getElementById('component-instructions') as HTMLTextAreaElement).value;

      if (!componentType) {
        showQuickNotification('⚠️ Please select a component', '#f59e0b');
        return;
      }

      modal.remove();
      generateComponent(componentType, instructions);
    };
  }

  function generateComponent(componentType: string, instructions: string) {
    showQuickNotification('🤖 Generating component...', '#667eea');

    // Get page context
    const pageContext = {
      url: window.location.href,
      title: document.title,
      framework: detectFramework(),
      cssFramework: detectCssFramework(),
      insertionPoint: insertionTarget ? getElementSelector(insertionTarget) : 'body',
      existingClasses: Array.from(document.querySelectorAll('[class]'))
        .slice(0, 10)
        .map((el: any) => el.className)
        .filter(c => c),
    };

    // Send to server
    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({
        type: 'generate_component',
        componentType,
        instructions,
        pageContext,
        insertionPoint: insertionTarget ? getElementSelector(insertionTarget) : 'body',
      }));
    } else {
      showQuickNotification('❌ WebSocket not connected', '#ef4444');
    }
  }

  function showComponentPreview(html: string, explanation: string) {
    currentComponentHtml = html;

    if (componentPreviewModal) {
      componentPreviewModal.remove();
    }

    componentPreviewModal = document.createElement('div');
    componentPreviewModal.className = 'visual-editor-modal';
    componentPreviewModal.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 24px;
      border-radius: 12px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
      z-index: 1000006;
      width: 700px;
      max-height: 80vh;
      overflow-y: auto;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    componentPreviewModal.innerHTML = `
      <h3 style="margin: 0 0 16px 0; font-size: 18px; color: #1e293b;">Component Preview</h3>

      <div style="margin-bottom: 16px; padding: 12px; background: #f8fafc; border-radius: 6px; border: 1px solid #e2e8f0;">
        <p style="margin: 0; font-size: 13px; color: #64748b; line-height: 1.5;">${explanation}</p>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 14px; color: #64748b; margin-bottom: 8px; font-weight: 500;">
          Live Preview:
        </label>
        <div id="component-preview-render" style="padding: 20px; background: #f8fafc; border: 2px dashed #cbd5e1; border-radius: 8px; min-height: 100px;">
          ${html}
        </div>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 14px; color: #64748b; margin-bottom: 8px; font-weight: 500;">
          HTML Code:
        </label>
        <pre style="background: #1e293b; color: #e2e8f0; padding: 16px; border-radius: 8px; overflow-x: auto; font-size: 12px; line-height: 1.5;"><code>${escapeHtml(html)}</code></pre>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 14px; color: #64748b; margin-bottom: 8px;">
          Refine Component (Optional)
        </label>
        <textarea id="refine-instructions" placeholder="e.g., Make it larger, change color to blue, add an icon..."
          style="width: 100%; padding: 8px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 14px; min-height: 60px; resize: vertical;"></textarea>
      </div>

      <div style="display: flex; gap: 8px; justify-content: flex-end;">
        <button id="preview-cancel" style="padding: 8px 16px; border: 1px solid #e2e8f0; background: white; border-radius: 6px; cursor: pointer; font-size: 14px;">
          Cancel
        </button>
        <button id="preview-refine" style="padding: 8px 16px; border: 1px solid #667eea; background: white; color: #667eea; border-radius: 6px; cursor: pointer; font-size: 14px; font-weight: 500;">
          🔄 Refine
        </button>
        <button id="preview-insert" style="padding: 8px 16px; border: none; background: #10b981; color: white; border-radius: 6px; cursor: pointer; font-size: 14px; font-weight: 500;">
          ✓ Insert Component
        </button>
      </div>
    `;

    document.body.appendChild(componentPreviewModal);

    // Buttons
    document.getElementById('preview-cancel')!.onclick = () => {
      componentPreviewModal?.remove();
      componentPreviewModal = null;
    };

    document.getElementById('preview-refine')!.onclick = () => {
      const refineText = (document.getElementById('refine-instructions') as HTMLTextAreaElement).value;
      if (!refineText) {
        showQuickNotification('⚠️ Please enter refinement instructions', '#f59e0b');
        return;
      }
      refineComponent(refineText);
    };

    document.getElementById('preview-insert')!.onclick = () => {
      insertComponent();
    };
  }

  function refineComponent(instructions: string) {
    showQuickNotification('🔄 Refining component...', '#667eea');

    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({
        type: 'refine_component',
        instructions,
        currentHtml: currentComponentHtml,
        insertionPoint: insertionTarget ? getElementSelector(insertionTarget) : 'body',
      }));
    }
  }

  function insertComponent() {
    if (!insertionTarget || !currentComponentHtml) return;

    // Create temporary container
    const temp = document.createElement('div');
    temp.innerHTML = currentComponentHtml;

    // Insert into target
    while (temp.firstChild) {
      insertionTarget.appendChild(temp.firstChild);
    }

    // Log change
    logVisualChange('insert-component', {
      selector: getElementSelector(insertionTarget),
      html: currentComponentHtml,
    });

    // Close modal
    componentPreviewModal?.remove();
    componentPreviewModal = null;

    showQuickNotification('✓ Component inserted! Click Save to persist.', '#10b981');
  }

  function escapeHtml(html: string): string {
    return html
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  function createStylePanel() {
    if (stylePanel) return;

    stylePanel = document.createElement('div');
    stylePanel.id = 'visual-style-panel';
    stylePanel.className = 'visual-editor-modal';
    stylePanel.style.cssText = `
      position: fixed;
      top: 60px;
      right: 20px;
      width: 280px;
      max-height: calc(100vh - 100px);
      overflow-y: auto;
      background: white;
      padding: 16px;
      border-radius: 12px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
      z-index: 1000003;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      display: none;
    `;

    stylePanel.innerHTML = `
      <h3 style="margin: 0 0 16px 0; font-size: 14px; font-weight: 600; color: #1e293b;">Style Editor</h3>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Background Color</label>
        <div style="display: flex; gap: 8px;">
          <input type="color" id="vis-bg-color" style="width: 40px; height: 32px; border: 1px solid #e2e8f0; border-radius: 6px; cursor: pointer;">
          <input type="text" id="vis-bg-text" placeholder="#ffffff" style="flex: 1; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
        </div>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Text Color</label>
        <div style="display: flex; gap: 8px;">
          <input type="color" id="vis-text-color" style="width: 40px; height: 32px; border: 1px solid #e2e8f0; border-radius: 6px; cursor: pointer;">
          <input type="text" id="vis-text-text" placeholder="#000000" style="flex: 1; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
        </div>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Font Size</label>
        <input type="text" id="vis-font-size" placeholder="16px" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Font Family</label>
        <select id="vis-font-family" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
          <option value="">Default</option>
          <option value="Arial, sans-serif">Arial</option>
          <option value="Helvetica, sans-serif">Helvetica</option>
          <option value="Georgia, serif">Georgia</option>
          <option value="'Times New Roman', serif">Times New Roman</option>
          <option value="'Courier New', monospace">Courier New</option>
          <option value="Verdana, sans-serif">Verdana</option>
          <option value="-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif">System</option>
        </select>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Font Weight</label>
        <select id="vis-font-weight" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
          <option value="normal">Normal</option>
          <option value="bold">Bold</option>
          <option value="lighter">Lighter</option>
          <option value="bolder">Bolder</option>
          <option value="100">100 (Thin)</option>
          <option value="300">300 (Light)</option>
          <option value="400">400 (Normal)</option>
          <option value="500">500 (Medium)</option>
          <option value="600">600 (Semibold)</option>
          <option value="700">700 (Bold)</option>
          <option value="900">900 (Black)</option>
        </select>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Line Height</label>
        <input type="text" id="vis-line-height" placeholder="1.5 or 24px" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Letter Spacing</label>
        <input type="text" id="vis-letter-spacing" placeholder="0px or normal" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Text Align</label>
        <select id="vis-text-align" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
          <option value="left">Left</option>
          <option value="center">Center</option>
          <option value="right">Right</option>
          <option value="justify">Justify</option>
        </select>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Text Transform</label>
        <select id="vis-text-transform" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
          <option value="none">None</option>
          <option value="uppercase">UPPERCASE</option>
          <option value="lowercase">lowercase</option>
          <option value="capitalize">Capitalize</option>
        </select>
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Padding</label>
        <input type="text" id="vis-padding" placeholder="10px" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Border Radius</label>
        <input type="text" id="vis-border-radius" placeholder="0px" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
      </div>

      <div style="margin-bottom: 16px;">
        <label style="display: block; font-size: 12px; color: #64748b; margin-bottom: 4px;">Shadow</label>
        <select id="vis-shadow" style="width: 100%; padding: 6px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px;">
          <option value="">None</option>
          <option value="0 1px 2px rgba(0,0,0,0.05)">Small</option>
          <option value="0 4px 6px rgba(0,0,0,0.1)">Medium</option>
          <option value="0 10px 15px rgba(0,0,0,0.1)">Large</option>
        </select>
      </div>
    `;

    document.body.appendChild(stylePanel);
    addStylePanelListeners();
  }

  function addStylePanelListeners() {
    const applyStyle = (property: string, value: string) => {
      if (selectedElements.length === 0) return;
      const element = selectedElements[selectedElements.length - 1];
      (element.style as any)[property] = value;
      logVisualChange('style', { property, value, selector: getElementSelector(element) });
      showQuickNotification(`✓ ${property} updated`, '#10b981');
    };

    document.getElementById('vis-bg-color')?.addEventListener('change', (e: any) => {
      applyStyle('background', e.target.value);
      (document.getElementById('vis-bg-text') as HTMLInputElement).value = e.target.value;
    });

    document.getElementById('vis-bg-text')?.addEventListener('change', (e: any) => {
      applyStyle('background', e.target.value);
    });

    document.getElementById('vis-text-color')?.addEventListener('change', (e: any) => {
      applyStyle('color', e.target.value);
      (document.getElementById('vis-text-text') as HTMLInputElement).value = e.target.value;
    });

    document.getElementById('vis-text-text')?.addEventListener('change', (e: any) => {
      applyStyle('color', e.target.value);
    });

    document.getElementById('vis-font-size')?.addEventListener('change', (e: any) => {
      applyStyle('fontSize', e.target.value);
    });

    document.getElementById('vis-padding')?.addEventListener('change', (e: any) => {
      applyStyle('padding', e.target.value);
    });

    document.getElementById('vis-border-radius')?.addEventListener('change', (e: any) => {
      applyStyle('borderRadius', e.target.value);
    });

    document.getElementById('vis-shadow')?.addEventListener('change', (e: any) => {
      applyStyle('boxShadow', e.target.value);
    });

    // Text formatting controls
    document.getElementById('vis-font-family')?.addEventListener('change', (e: any) => {
      applyStyle('fontFamily', e.target.value);
    });

    document.getElementById('vis-font-weight')?.addEventListener('change', (e: any) => {
      applyStyle('fontWeight', e.target.value);
    });

    document.getElementById('vis-line-height')?.addEventListener('change', (e: any) => {
      applyStyle('lineHeight', e.target.value);
    });

    document.getElementById('vis-letter-spacing')?.addEventListener('change', (e: any) => {
      applyStyle('letterSpacing', e.target.value);
    });

    document.getElementById('vis-text-align')?.addEventListener('change', (e: any) => {
      applyStyle('textAlign', e.target.value);
    });

    document.getElementById('vis-text-transform')?.addEventListener('change', (e: any) => {
      applyStyle('textTransform', e.target.value);
    });
  }

  function toggleStylePanel() {
    if (!stylePanel) {
      createStylePanel();
    }
    stylePanel!.style.display = stylePanel!.style.display === 'none' ? 'block' : 'none';
  }

  function showResizeHandles(element: HTMLElement) {
    if (resizeHandles) {
      resizeHandles.remove();
    }

    const rect = element.getBoundingClientRect();

    resizeHandles = document.createElement('div');
    resizeHandles.id = 'visual-resize-handles';
    resizeHandles.className = 'visual-editor-modal';
    resizeHandles.style.cssText = `
      position: absolute;
      top: ${rect.top + window.scrollY}px;
      left: ${rect.left + window.scrollX}px;
      width: ${rect.width}px;
      height: ${rect.height}px;
      border: 2px solid #667eea;
      pointer-events: none;
      z-index: 1000002;
    `;

    // 8 resize handles
    const positions = ['nw', 'n', 'ne', 'e', 'se', 's', 'sw', 'w'];
    const cursors = ['nw-resize', 'n-resize', 'ne-resize', 'e-resize', 'se-resize', 's-resize', 'sw-resize', 'w-resize'];

    positions.forEach((pos, idx) => {
      const handle = document.createElement('div');
      handle.style.cssText = `
        position: absolute;
        width: 8px;
        height: 8px;
        background: white;
        border: 2px solid #667eea;
        border-radius: 50%;
        pointer-events: auto;
        cursor: ${cursors[idx]};
        ${pos.includes('n') ? 'top: -4px;' : ''}
        ${pos.includes('s') ? 'bottom: -4px;' : ''}
        ${pos.includes('w') ? 'left: -4px;' : ''}
        ${pos.includes('e') ? 'right: -4px;' : ''}
        ${pos === 'n' || pos === 's' ? 'left: 50%; transform: translateX(-50%);' : ''}
        ${pos === 'w' || pos === 'e' ? 'top: 50%; transform: translateY(-50%);' : ''}
      `;
      handle.onmousedown = (e) => startResize(e, pos, element);
      resizeHandles!.appendChild(handle);
    });

    document.body.appendChild(resizeHandles);
  }

  function startResize(e: MouseEvent, direction: string, element: HTMLElement) {
    e.preventDefault();
    e.stopPropagation();

    isResizing = true;
    resizeDirection = direction;
    dragStartX = e.clientX;
    dragStartY = e.clientY;

    const rect = element.getBoundingClientRect();
    originalWidth = rect.width;
    originalHeight = rect.height;

    const onMouseMove = (e: MouseEvent) => handleResize(e, element);
    const onMouseUp = () => {
      isResizing = false;
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
      logVisualChange('resize', {
        width: element.style.width,
        height: element.style.height,
        selector: getElementSelector(element)
      });
      showQuickNotification('✓ Element resized', '#10b981');
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }

  function handleResize(e: MouseEvent, element: HTMLElement) {
    if (!isResizing) return;

    const deltaX = e.clientX - dragStartX;
    const deltaY = e.clientY - dragStartY;

    let newWidth = originalWidth;
    let newHeight = originalHeight;

    if (resizeDirection.includes('e')) newWidth = originalWidth + deltaX;
    if (resizeDirection.includes('w')) newWidth = originalWidth - deltaX;
    if (resizeDirection.includes('s')) newHeight = originalHeight + deltaY;
    if (resizeDirection.includes('n')) newHeight = originalHeight - deltaY;

    if (newWidth > 20) element.style.width = `${newWidth}px`;
    if (newHeight > 20) element.style.height = `${newHeight}px`;

    if (resizeHandles) {
      const rect = element.getBoundingClientRect();
      resizeHandles.style.width = `${rect.width}px`;
      resizeHandles.style.height = `${rect.height}px`;
    }
  }

  function hideVisualTools() {
    if (resizeHandles) {
      resizeHandles.remove();
      resizeHandles = null;
    }
    if (stylePanel) {
      stylePanel.style.display = 'none';
    }
  }

  function logVisualChange(type: string, details: any) {
    const change = {
      timestamp: Date.now(),
      source: 'visual',
      type,
      details,
    };
    allChanges.push(change);
    console.log('📝 Visual change:', change);
  }

  function getElementSelector(el: HTMLElement): string {
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
        selector += `.${Array.from(current.classList).slice(0, 2).join('.')}`;
      }
      path.unshift(selector);
      current = current.parentElement;
    }
    return path.join(' > ');
  }

  function showLoadingIndicator() {
    const loader = document.createElement('div');
    loader.id = 'visual-editor-loader';
    loader.className = 'visual-editor-modal';
    loader.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 30px;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
      z-index: 1000001;
      text-align: center;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;
    loader.innerHTML = `
      <div style="font-size: 14px; color: #64748b; display: flex; align-items: center; gap: 12px;">
        <div style="width: 20px; height: 20px; border: 3px solid #e2e8f0; border-top-color: #667eea; border-radius: 50%; animation: spin 1s linear infinite;"></div>
        AI is analyzing...
      </div>
      <style>
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
      </style>
    `;
    document.body.appendChild(loader);
  }

  function showAIMessage(message: string) {
    const modal = document.createElement('div');
    modal.className = 'visual-editor-modal';
    modal.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 24px;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
      z-index: 1000001;
      width: 500px;
      max-width: 90%;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    modal.innerHTML = `
      <div style="display: flex; align-items: start; gap: 12px; margin-bottom: 20px;">
        <div style="font-size: 24px;">🤖</div>
        <div>
          <h3 style="margin: 0 0 8px 0; font-size: 16px; font-weight: 600; color: #1e293b;">AI Response</h3>
          <p style="margin: 0; color: #475569; font-size: 14px; line-height: 1.6;">${message}</p>
        </div>
      </div>
      <div style="display: flex; justify-content: flex-end;">
        <button id="ok-btn" style="padding: 10px 24px; border: none; background: #667eea; color: white; border-radius: 8px; cursor: pointer; font-weight: 600; transition: all 0.2s;" onmouseover="this.style.background='#5568d3'" onmouseout="this.style.background='#667eea'">
          Got it
        </button>
      </div>
    `;

    document.body.appendChild(modal);

    document.getElementById('ok-btn')!.onclick = () => {
      modal.remove();
    };
  }

  function showCodePreview(changes: any[], explanation: string) {
    const loader = document.getElementById('visual-editor-loader');
    if (loader) loader.remove();

    // If no code changes, show message modal instead
    if (!changes || changes.length === 0) {
      showAIMessage(explanation);
      return;
    }

    const modal = document.createElement('div');
    modal.className = 'visual-editor-modal';
    modal.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 20px;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
      z-index: 1000001;
      width: 700px;
      max-width: 90%;
      max-height: 80vh;
      overflow-y: auto;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    modal.innerHTML = `
      <h3 style="margin: 0 0 15px 0; font-size: 18px; font-weight: 600; color: #1e293b;">Preview Changes</h3>
      <p style="margin: 0 0 20px 0; color: #64748b; font-size: 14px; line-height: 1.6;">${explanation}</p>
      ${changes.map(change => `
        <div style="margin-bottom: 20px; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;">
          <div style="background: #f8fafc; padding: 8px 12px; font-family: 'Courier New', monospace; font-size: 12px; color: #475569; border-bottom: 1px solid #e2e8f0;">
            📄 ${change.file}
          </div>
          <pre style="background: #f8fafc; padding: 16px; margin: 0; overflow-x: auto; font-size: 13px; line-height: 1.5;">${escapeHtml(change.newContent)}</pre>
        </div>
      `).join('')}
      <div style="margin-top: 20px; display: flex; gap: 12px; justify-content: flex-end;">
        <button id="reject-btn" style="padding: 10px 20px; border: 1px solid #e2e8f0; background: white; border-radius: 8px; cursor: pointer; font-weight: 500; color: #64748b; transition: all 0.2s;" onmouseover="this.style.background='#f8fafc'" onmouseout="this.style.background='white'">
          Reject
        </button>
        <button id="accept-btn" style="padding: 10px 20px; border: none; background: #10b981; color: white; border-radius: 8px; cursor: pointer; font-weight: 600; transition: all 0.2s; box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3);" onmouseover="this.style.background='#059669'" onmouseout="this.style.background='#10b981'">
          Accept & Apply
        </button>
      </div>
    `;

    document.body.appendChild(modal);

    document.getElementById('reject-btn')!.onclick = () => {
      modal.remove();
      if (ws) {
        ws.send(JSON.stringify({ type: 'code_preview_rejected' }));
      }
    };

    document.getElementById('accept-btn')!.onclick = () => {
      modal.remove();
      if (ws) {
        ws.send(JSON.stringify({
          type: 'code_preview_accepted',
          changes,
        }));
      }
    };
  }

  function showSuccessNotification(message: string) {
    showNotification(message, '#10b981');
  }

  function showErrorNotification(message: string) {
    showNotification(message, '#ef4444');
  }

  function showNotification(message: string, color: string) {
    const notification = document.createElement('div');
    notification.style.cssText = `
      position: fixed;
      bottom: 20px;
      right: 20px;
      background: ${color};
      color: white;
      padding: 12px 20px;
      border-radius: 6px;
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
      z-index: 1000002;
      font-family: sans-serif;
      font-size: 14px;
    `;
    notification.textContent = message;
    document.body.appendChild(notification);

    setTimeout(() => {
      notification.remove();
    }, 3000);
  }

  function showQuickNotification(message: string, color: string) {
    const notification = document.createElement('div');
    notification.className = 'visual-editor-quick-notification';
    notification.style.cssText = `
      position: fixed;
      top: 60px;
      right: 20px;
      background: ${color};
      color: white;
      padding: 8px 16px;
      border-radius: 6px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      z-index: 1000002;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      font-size: 13px;
      opacity: 0;
      transform: translateX(20px);
      transition: all 0.3s ease;
    `;
    notification.textContent = message;
    document.body.appendChild(notification);

    // Animate in
    setTimeout(() => {
      notification.style.opacity = '1';
      notification.style.transform = 'translateX(0)';
    }, 10);

    // Animate out and remove
    setTimeout(() => {
      notification.style.opacity = '0';
      notification.style.transform = 'translateX(20px)';
      setTimeout(() => notification.remove(), 300);
    }, 2000);
  }

  function showSaveChangesPrompt(summary: string = '') {
    // Remove loading indicator
    const loader = document.getElementById('visual-editor-loader');
    if (loader) loader.remove();

    // Combine all changes (AI + visual)
    const totalChanges = allChanges.length;

    if (totalChanges === 0) {
      showQuickNotification('⚠️ No changes to save', '#f59e0b');
      return;
    }

    const modal = document.createElement('div');
    modal.className = 'visual-editor-modal';
    modal.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 24px;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
      z-index: 1000001;
      width: 500px;
      max-width: 90%;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;

    // Build change list
    const aiChanges = allChanges.filter(c => c.source === 'ai');
    const visualChanges = allChanges.filter(c => c.source === 'visual');

    let changeList = '';
    if (aiChanges.length > 0) {
      changeList += '<div style="margin-bottom: 8px; font-weight: 600; color: #667eea;">🤖 AI Changes:</div>';
      changeList += aiChanges.slice(0, 3).map(c => `• ${c.description || c.tool}`).join('\n');
      if (aiChanges.length > 3) changeList += `\n... and ${aiChanges.length - 3} more`;
    }
    if (visualChanges.length > 0) {
      if (changeList) changeList += '\n\n';
      changeList += '<div style="margin-bottom: 8px; font-weight: 600; color: #10b981;">🎨 Visual Edits:</div>';
      changeList += visualChanges.slice(0, 3).map(c => {
        if (c.type === 'resize') return `• Resized to ${c.details.width} × ${c.details.height}`;
        if (c.type === 'style') return `• Changed ${c.details.property}`;
        if (c.type === 'move') return `• Repositioned element`;
        if (c.type === 'align') return `• Aligned ${c.details.direction}`;
        return `• ${c.type}`;
      }).join('\n');
      if (visualChanges.length > 3) changeList += `\n... and ${visualChanges.length - 3} more`;
    }

    modal.innerHTML = `
      <div style="display: flex; align-items: start; gap: 12px; margin-bottom: 20px;">
        <div style="font-size: 32px;">✨</div>
        <div style="flex: 1;">
          <h3 style="margin: 0 0 8px 0; font-size: 18px; font-weight: 600; color: #1e293b;">Changes Ready!</h3>
          <p style="margin: 0; color: #64748b; font-size: 14px; line-height: 1.6;">
            ${totalChanges} change${totalChanges > 1 ? 's' : ''} made (${aiChanges.length} AI + ${visualChanges.length} visual). Save to persist to code!
          </p>
        </div>
      </div>

      <div style="background: #f8fafc; padding: 16px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #e2e8f0;">
        <div style="font-size: 12px; font-weight: 600; color: #64748b; margin-bottom: 8px; text-transform: uppercase;">Changes Made:</div>
        <div style="font-size: 13px; line-height: 1.8; color: #475569; white-space: pre-wrap;">${changeList}</div>
      </div>

      <div style="display: flex; gap: 12px; justify-content: flex-end;">
        <button id="discard-btn" style="padding: 10px 20px; border: 1px solid #e2e8f0; background: white; border-radius: 8px; cursor: pointer; font-weight: 500; color: #64748b; transition: all 0.2s;" onmouseover="this.style.background='#f8fafc'" onmouseout="this.style.background='white'">
          Discard & Reload
        </button>
        <button id="save-changes-btn" style="padding: 10px 24px; border: none; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border-radius: 8px; cursor: pointer; font-weight: 600; transition: all 0.2s; box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3);" onmouseover="this.style.transform='translateY(-1px)'; this.style.boxShadow='0 4px 12px rgba(16, 185, 129, 0.4)'" onmouseout="this.style.transform='translateY(0)'; this.style.boxShadow='0 2px 8px rgba(16, 185, 129, 0.3)'">
          💾 Save to Code
        </button>
      </div>
    `;

    document.body.appendChild(modal);

    document.getElementById('discard-btn')!.onclick = () => {
      modal.remove();
      window.location.reload();
    };

    document.getElementById('save-changes-btn')!.onclick = () => {
      modal.remove();
      persistChangesToCode();
    };
  }

  function persistChangesToCode() {
    showLoadingIndicator();

    // Build comprehensive summary
    const aiChanges = allChanges.filter(c => c.source === 'ai');
    const visualChanges = allChanges.filter(c => c.source === 'visual');

    let summary = '';
    if (aiChanges.length > 0) {
      summary += 'AI Commands:\n' + aiChanges.map(c => `- ${c.description || c.tool}`).join('\n');
    }
    if (visualChanges.length > 0) {
      if (summary) summary += '\n\n';
      summary += 'Visual Edits:\n' + visualChanges.map(c => {
        if (c.type === 'resize') return `- Resized ${c.details.selector} to ${c.details.width} × ${c.details.height}`;
        if (c.type === 'style') return `- Changed ${c.details.property} of ${c.details.selector} to ${c.details.value}`;
        if (c.type === 'align') return `- Aligned ${c.details.selector} ${c.details.direction}`;
        if (c.type === 'center') return `- Centered ${c.details.selector}`;
        if (c.type === 'duplicate') return `- Duplicated ${c.details.selector}`;
        if (c.type === 'delete') return `- Deleted ${c.details.selector}`;
        return `- ${c.type}`;
      }).join('\n');
    }

    // Send combined changes to backend for code generation
    if (ws) {
      ws.send(JSON.stringify({
        type: 'persist_changes',
        changeLog: allChanges,
        aiChanges,
        visualChanges,
        summary,
        sessionId,
      }));
    }
  }

  // Create floating activation button
  function createFloatingButton() {
    const button = document.createElement('button');
    button.id = 'zima-visual-editor-toggle';
    button.innerHTML = `
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M12 20h9"></path>
        <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path>
      </svg>
    `;

    button.style.cssText = `
      position: fixed;
      bottom: 20px;
      right: 20px;
      width: 56px;
      height: 56px;
      border-radius: 50%;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border: none;
      color: white;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
      z-index: 999998;
      transition: all 0.3s ease;
      font-size: 0;
    `;

    // Hover effect
    button.addEventListener('mouseenter', () => {
      button.style.transform = 'scale(1.1)';
      button.style.boxShadow = '0 6px 20px rgba(102, 126, 234, 0.6)';
    });

    button.addEventListener('mouseleave', () => {
      button.style.transform = 'scale(1)';
      button.style.boxShadow = '0 4px 12px rgba(102, 126, 234, 0.4)';
    });

    // Click handler
    button.addEventListener('click', () => {
      if (inspectorActive) {
        deactivateInspector();
        button.style.background = 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)';
      } else {
        activateInspector();
        button.style.background = 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)';
      }
    });

    document.body.appendChild(button);
    return button;
  }

  // Initialize
  function init() {
    // Detect project on load
    projectInfo = detectProject();

    // Create floating button
    createFloatingButton();

    console.log('%c✓ ZIMA Visual Code Editor loaded', 'color: #3b82f6; font-weight: bold;');
    console.log('%cClick the floating button in the bottom-right to activate inspector', 'color: #666;');
  }

  // Auto-initialize on DOM ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
