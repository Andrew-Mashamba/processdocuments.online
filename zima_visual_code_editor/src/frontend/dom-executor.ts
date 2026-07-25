/**
 * DOM Tool Executor - Browser Side
 *
 * Executes DOM manipulation commands sent from the AI
 * Tracks all changes for later code generation
 */

export interface DOMChange {
  timestamp: number;
  tool: string;
  params: any;
  result: any;
  description: string;
}

export class DOMExecutor {
  private changeLog: DOMChange[] = [];

  /**
   * Execute a DOM tool command
   */
  async execute(toolName: string, params: any): Promise<any> {
    const timestamp = Date.now();
    let result: any;

    try {
      result = await this.executeTool(toolName, params);

      // Log the change
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
  }

  /**
   * Execute specific tool
   */
  private async executeTool(toolName: string, params: any): Promise<any> {
    switch (toolName) {
      // ==========================================
      // SELECTING
      // ==========================================
      case 'dom_select':
        return this.select(params.selector, params.multiple);

      // ==========================================
      // READING
      // ==========================================
      case 'dom_get_text':
        return this.getText(params.selector, params.type);

      case 'dom_get_attribute':
        return this.getAttribute(params.selector, params.attribute);

      case 'dom_get_style':
        return this.getStyle(params.selector, params.property);

      // ==========================================
      // MODIFYING CONTENT
      // ==========================================
      case 'dom_set_text':
        return this.setText(params.selector, params.text, params.type);

      case 'dom_set_html':
        return this.setHtml(params.selector, params.html);

      // ==========================================
      // MODIFYING ATTRIBUTES
      // ==========================================
      case 'dom_set_attribute':
        return this.setAttribute(params.selector, params.attribute, params.value);

      case 'dom_remove_attribute':
        return this.removeAttribute(params.selector, params.attribute);

      // ==========================================
      // MODIFYING STYLES
      // ==========================================
      case 'dom_set_style':
        return this.setStyle(params.selector, params.styles);

      case 'dom_add_class':
        return this.addClass(params.selector, params.classes);

      case 'dom_remove_class':
        return this.removeClass(params.selector, params.classes);

      case 'dom_toggle_class':
        return this.toggleClass(params.selector, params.classes);

      // ==========================================
      // CREATING & INSERTING
      // ==========================================
      case 'dom_create_element':
        return this.createElement(params);

      case 'dom_append_child':
        return this.appendChild(params.parentSelector, params.childHtml);

      case 'dom_prepend_child':
        return this.prependChild(params.parentSelector, params.childHtml);

      case 'dom_insert_before':
        return this.insertBefore(params.targetSelector, params.html);

      case 'dom_insert_after':
        return this.insertAfter(params.targetSelector, params.html);

      // ==========================================
      // REMOVING
      // ==========================================
      case 'dom_remove':
        return this.remove(params.selector);

      case 'dom_remove_children':
        return this.removeChildren(params.selector);

      // ==========================================
      // REPLACING
      // ==========================================
      case 'dom_replace':
        return this.replace(params.selector, params.html);

      // ==========================================
      // TRAVERSING
      // ==========================================
      case 'dom_get_parent':
        return this.getParent(params.selector);

      case 'dom_get_children':
        return this.getChildren(params.selector);

      case 'dom_get_siblings':
        return this.getSiblings(params.selector);

      // ==========================================
      // FORM
      // ==========================================
      case 'dom_get_value':
        return this.getValue(params.selector);

      case 'dom_set_value':
        return this.setValue(params.selector, params.value);

      case 'dom_enable_input':
        return this.enableInput(params.selector, params.enabled);

      // ==========================================
      // DATA ATTRIBUTES
      // ==========================================
      case 'dom_set_data':
        return this.setData(params.selector, params.key, params.value);

      case 'dom_get_data':
        return this.getData(params.selector, params.key);

      // ==========================================
      // VISIBILITY
      // ==========================================
      case 'dom_show':
        return this.show(params.selector, params.method);

      case 'dom_hide':
        return this.hide(params.selector, params.method);

      case 'dom_toggle_visibility':
        return this.toggleVisibility(params.selector);

      // ==========================================
      // MEASURING
      // ==========================================
      case 'dom_get_dimensions':
        return this.getDimensions(params.selector);

      case 'dom_get_bounding_box':
        return this.getBoundingBox(params.selector);

      // ==========================================
      // SPECIAL
      // ==========================================
      case 'dom_snapshot':
        return this.snapshot(params.description);

      case 'dom_get_change_log':
        return this.getChangeLog();

      default:
        throw new Error(`Unknown DOM tool: ${toolName}`);
    }
  }

  // ==========================================
  // IMPLEMENTATION METHODS
  // ==========================================

  private select(selector: string, multiple: boolean = false): any {
    if (multiple) {
      return Array.from(document.querySelectorAll(selector)).map((el) => this.getElementInfo(el as HTMLElement));
    } else {
      const el = document.querySelector(selector) as HTMLElement;
      return el ? this.getElementInfo(el) : null;
    }
  }

  private getText(selector: string, type: string = 'innerText'): string {
    const el = document.querySelector(selector) as HTMLElement;
    if (!el) throw new Error(`Element not found: ${selector}`);

    switch (type) {
      case 'innerText':
        return el.innerText;
      case 'textContent':
        return el.textContent || '';
      case 'innerHTML':
        return el.innerHTML;
      default:
        return el.innerText;
    }
  }

  private getAttribute(selector: string, attribute: string): string | null {
    const el = document.querySelector(selector);
    if (!el) throw new Error(`Element not found: ${selector}`);
    return el.getAttribute(attribute);
  }

  private getStyle(selector: string, property: string): string {
    const el = document.querySelector(selector) as HTMLElement;
    if (!el) throw new Error(`Element not found: ${selector}`);
    return window.getComputedStyle(el).getPropertyValue(property);
  }

  private setText(selector: string, text: string, type: string = 'innerText'): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el: any) => {
      switch (type) {
        case 'innerText':
          el.innerText = text;
          break;
        case 'textContent':
          el.textContent = text;
          break;
        case 'innerHTML':
          el.innerHTML = text;
          break;
      }
    });
    return elements.length;
  }

  private setHtml(selector: string, html: string): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el: any) => {
      el.innerHTML = html;
    });
    return elements.length;
  }

  private setAttribute(selector: string, attribute: string, value: string): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el) => {
      el.setAttribute(attribute, value);
    });
    return elements.length;
  }

  private removeAttribute(selector: string, attribute: string): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el) => {
      el.removeAttribute(attribute);
    });
    return elements.length;
  }

  private setStyle(selector: string, styles: Record<string, string>): number {
    const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
    elements.forEach((el) => {
      Object.assign(el.style, styles);
    });
    return elements.length;
  }

  private addClass(selector: string, classes: string[]): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el) => {
      el.classList.add(...classes);
    });
    return elements.length;
  }

  private removeClass(selector: string, classes: string[]): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el) => {
      el.classList.remove(...classes);
    });
    return elements.length;
  }

  private toggleClass(selector: string, classes: string[]): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el) => {
      classes.forEach((cls) => el.classList.toggle(cls));
    });
    return elements.length;
  }

  private createElement(params: any): string {
    const el = document.createElement(params.tag);

    if (params.id) el.id = params.id;
    if (params.classes) el.classList.add(...params.classes);
    if (params.attributes) {
      Object.entries(params.attributes).forEach(([key, value]: any) => {
        el.setAttribute(key, value);
      });
    }
    if (params.styles) Object.assign(el.style, params.styles);
    if (params.content) el.innerHTML = params.content;

    return el.outerHTML;
  }

  private appendChild(parentSelector: string, childHtml: string): boolean {
    const parent = document.querySelector(parentSelector);
    if (!parent) throw new Error(`Parent not found: ${parentSelector}`);

    const temp = document.createElement('div');
    temp.innerHTML = childHtml;
    while (temp.firstChild) {
      parent.appendChild(temp.firstChild);
    }
    return true;
  }

  private prependChild(parentSelector: string, childHtml: string): boolean {
    const parent = document.querySelector(parentSelector);
    if (!parent) throw new Error(`Parent not found: ${parentSelector}`);

    const temp = document.createElement('div');
    temp.innerHTML = childHtml;
    while (temp.lastChild) {
      parent.insertBefore(temp.lastChild, parent.firstChild);
    }
    return true;
  }

  private insertBefore(targetSelector: string, html: string): boolean {
    const target = document.querySelector(targetSelector);
    if (!target || !target.parentNode) throw new Error(`Target not found: ${targetSelector}`);

    const temp = document.createElement('div');
    temp.innerHTML = html;
    while (temp.firstChild) {
      target.parentNode.insertBefore(temp.firstChild, target);
    }
    return true;
  }

  private insertAfter(targetSelector: string, html: string): boolean {
    const target = document.querySelector(targetSelector);
    if (!target || !target.parentNode) throw new Error(`Target not found: ${targetSelector}`);

    const temp = document.createElement('div');
    temp.innerHTML = html;
    while (temp.lastChild) {
      target.parentNode.insertBefore(temp.lastChild, target.nextSibling);
    }
    return true;
  }

  private remove(selector: string): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el) => el.remove());
    return elements.length;
  }

  private removeChildren(selector: string): number {
    const elements = document.querySelectorAll(selector);
    let count = 0;
    elements.forEach((el) => {
      count += el.childNodes.length;
      el.innerHTML = '';
    });
    return count;
  }

  private replace(selector: string, html: string): number {
    const elements = document.querySelectorAll(selector);
    elements.forEach((el) => {
      const temp = document.createElement('div');
      temp.innerHTML = html;
      el.replaceWith(...Array.from(temp.childNodes));
    });
    return elements.length;
  }

  private getParent(selector: string): any {
    const el = document.querySelector(selector);
    if (!el || !el.parentElement) throw new Error(`Element or parent not found: ${selector}`);
    return this.getElementInfo(el.parentElement);
  }

  private getChildren(selector: string): any[] {
    const el = document.querySelector(selector);
    if (!el) throw new Error(`Element not found: ${selector}`);
    return Array.from(el.children).map((child) => this.getElementInfo(child as HTMLElement));
  }

  private getSiblings(selector: string): any {
    const el = document.querySelector(selector);
    if (!el) throw new Error(`Element not found: ${selector}`);

    return {
      previous: el.previousElementSibling ? this.getElementInfo(el.previousElementSibling as HTMLElement) : null,
      next: el.nextElementSibling ? this.getElementInfo(el.nextElementSibling as HTMLElement) : null,
    };
  }

  private getValue(selector: string): string {
    const el = document.querySelector(selector) as HTMLInputElement;
    if (!el) throw new Error(`Element not found: ${selector}`);
    return el.value;
  }

  private setValue(selector: string, value: string): number {
    const elements = document.querySelectorAll(selector) as NodeListOf<HTMLInputElement>;
    elements.forEach((el) => {
      el.value = value;
    });
    return elements.length;
  }

  private enableInput(selector: string, enabled: boolean): number {
    const elements = document.querySelectorAll(selector) as NodeListOf<HTMLInputElement>;
    elements.forEach((el) => {
      el.disabled = !enabled;
    });
    return elements.length;
  }

  private setData(selector: string, key: string, value: string): number {
    const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
    elements.forEach((el) => {
      el.dataset[key] = value;
    });
    return elements.length;
  }

  private getData(selector: string, key: string): string | undefined {
    const el = document.querySelector(selector) as HTMLElement;
    if (!el) throw new Error(`Element not found: ${selector}`);
    return el.dataset[key];
  }

  private show(selector: string, method: string = 'display'): number {
    const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
    elements.forEach((el) => {
      switch (method) {
        case 'display':
          el.style.display = '';
          break;
        case 'visibility':
          el.style.visibility = 'visible';
          break;
        case 'opacity':
          el.style.opacity = '1';
          break;
      }
    });
    return elements.length;
  }

  private hide(selector: string, method: string = 'display'): number {
    const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
    elements.forEach((el) => {
      switch (method) {
        case 'display':
          el.style.display = 'none';
          break;
        case 'visibility':
          el.style.visibility = 'hidden';
          break;
        case 'opacity':
          el.style.opacity = '0';
          break;
      }
    });
    return elements.length;
  }

  private toggleVisibility(selector: string): number {
    const elements = document.querySelectorAll(selector) as NodeListOf<HTMLElement>;
    elements.forEach((el) => {
      el.style.display = el.style.display === 'none' ? '' : 'none';
    });
    return elements.length;
  }

  private getDimensions(selector: string): any {
    const el = document.querySelector(selector) as HTMLElement;
    if (!el) throw new Error(`Element not found: ${selector}`);

    return {
      offsetWidth: el.offsetWidth,
      offsetHeight: el.offsetHeight,
      clientWidth: el.clientWidth,
      clientHeight: el.clientHeight,
      scrollWidth: el.scrollWidth,
      scrollHeight: el.scrollHeight,
    };
  }

  private getBoundingBox(selector: string): any {
    const el = document.querySelector(selector) as HTMLElement;
    if (!el) throw new Error(`Element not found: ${selector}`);

    const rect = el.getBoundingClientRect();
    return {
      x: rect.x,
      y: rect.y,
      width: rect.width,
      height: rect.height,
      top: rect.top,
      right: rect.right,
      bottom: rect.bottom,
      left: rect.left,
    };
  }

  private snapshot(description: string): any {
    return {
      timestamp: Date.now(),
      description,
      changeCount: this.changeLog.length,
    };
  }

  private getChangeLog(): DOMChange[] {
    return this.changeLog;
  }

  // ==========================================
  // HELPERS
  // ==========================================

  private getElementInfo(el: HTMLElement): any {
    return {
      tag: el.tagName.toLowerCase(),
      id: el.id,
      classes: Array.from(el.classList),
      selector: this.generateSelector(el),
    };
  }

  private generateSelector(el: HTMLElement): string {
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

  private describeChange(tool: string, params: any): string {
    const descriptions: Record<string, string> = {
      dom_set_text: `Changed text of "${params.selector}" to "${params.text}"`,
      dom_set_style: `Applied styles to "${params.selector}": ${JSON.stringify(params.styles)}`,
      dom_add_class: `Added classes to "${params.selector}": ${params.classes.join(', ')}`,
      dom_remove_class: `Removed classes from "${params.selector}": ${params.classes.join(', ')}`,
      dom_append_child: `Appended content to "${params.parentSelector}"`,
      dom_remove: `Removed "${params.selector}"`,
      dom_hide: `Hidden "${params.selector}"`,
      dom_show: `Shown "${params.selector}"`,
    };

    return descriptions[tool] || `Executed ${tool}`;
  }

  /**
   * Clear change log
   */
  clearChangeLog(): void {
    this.changeLog = [];
  }

  /**
   * Get summary of all changes for code generation
   */
  getChangeSummary(): string {
    const summary = this.changeLog.map((change) => change.description).join('\n');
    return summary || 'No changes made';
  }
}
