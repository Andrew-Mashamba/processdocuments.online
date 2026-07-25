/**
 * DOM Manipulation Tools for Visual Editor
 *
 * These tools allow the AI to manipulate the DOM directly in the browser
 * for instant preview before persisting changes to source files.
 */

export const domTools = [
  // ==========================================
  // 1️⃣ SELECTING ELEMENTS
  // ==========================================
  {
    name: 'dom_select',
    description: 'Select DOM element(s) using CSS selector. Returns element references.',
    input_schema: {
      type: 'object',
      properties: {
        selector: {
          type: 'string',
          description: 'CSS selector (e.g., "#myId", ".myClass", "button.primary")',
        },
        multiple: {
          type: 'boolean',
          description: 'If true, returns all matching elements. If false, returns first match.',
          default: false,
        },
      },
      required: ['selector'],
    },
  },

  // ==========================================
  // 2️⃣ READING ELEMENT DATA
  // ==========================================
  {
    name: 'dom_get_text',
    description: 'Get text content from an element.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        type: {
          type: 'string',
          enum: ['innerText', 'textContent', 'innerHTML'],
          description: 'Type of text to get',
          default: 'innerText',
        },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_get_attribute',
    description: 'Get attribute value from an element.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        attribute: { type: 'string', description: 'Attribute name (e.g., "href", "src", "data-id")' },
      },
      required: ['selector', 'attribute'],
    },
  },

  {
    name: 'dom_get_style',
    description: 'Get computed style property value.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        property: { type: 'string', description: 'CSS property (e.g., "color", "fontSize")' },
      },
      required: ['selector', 'property'],
    },
  },

  // ==========================================
  // 3️⃣ MODIFYING CONTENT
  // ==========================================
  {
    name: 'dom_set_text',
    description: 'Change text content of element(s). Changes are instant and visible.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        text: { type: 'string', description: 'New text content' },
        type: {
          type: 'string',
          enum: ['innerText', 'textContent', 'innerHTML'],
          description: 'Type of content to set',
          default: 'innerText',
        },
      },
      required: ['selector', 'text'],
    },
  },

  {
    name: 'dom_set_html',
    description: 'Change HTML content of element(s). Use for complex content.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        html: { type: 'string', description: 'New HTML content' },
      },
      required: ['selector', 'html'],
    },
  },

  // ==========================================
  // 4️⃣ MODIFYING ATTRIBUTES
  // ==========================================
  {
    name: 'dom_set_attribute',
    description: 'Set or update an attribute on element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        attribute: { type: 'string', description: 'Attribute name' },
        value: { type: 'string', description: 'Attribute value' },
      },
      required: ['selector', 'attribute', 'value'],
    },
  },

  {
    name: 'dom_remove_attribute',
    description: 'Remove an attribute from element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        attribute: { type: 'string', description: 'Attribute name to remove' },
      },
      required: ['selector', 'attribute'],
    },
  },

  // ==========================================
  // 5️⃣ MODIFYING STYLES (CSS)
  // ==========================================
  {
    name: 'dom_set_style',
    description: 'Set inline CSS style(s) on element(s). Changes are instant.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        styles: {
          type: 'object',
          description: 'CSS properties as key-value pairs (e.g., {color: "blue", fontSize: "16px"})',
          additionalProperties: { type: 'string' },
        },
      },
      required: ['selector', 'styles'],
    },
  },

  {
    name: 'dom_add_class',
    description: 'Add CSS class(es) to element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        classes: {
          type: 'array',
          items: { type: 'string' },
          description: 'Class names to add',
        },
      },
      required: ['selector', 'classes'],
    },
  },

  {
    name: 'dom_remove_class',
    description: 'Remove CSS class(es) from element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        classes: {
          type: 'array',
          items: { type: 'string' },
          description: 'Class names to remove',
        },
      },
      required: ['selector', 'classes'],
    },
  },

  {
    name: 'dom_toggle_class',
    description: 'Toggle CSS class(es) on element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector' },
        classes: {
          type: 'array',
          items: { type: 'string' },
          description: 'Class names to toggle',
        },
      },
      required: ['selector', 'classes'],
    },
  },

  // ==========================================
  // 6️⃣ CREATING ELEMENTS
  // ==========================================
  {
    name: 'dom_create_element',
    description: 'Create a new DOM element (not yet inserted).',
    input_schema: {
      type: 'object',
      properties: {
        tag: { type: 'string', description: 'HTML tag name (e.g., "div", "button", "span")' },
        id: { type: 'string', description: 'Optional ID' },
        classes: {
          type: 'array',
          items: { type: 'string' },
          description: 'Optional class names',
        },
        attributes: {
          type: 'object',
          description: 'Optional attributes',
          additionalProperties: { type: 'string' },
        },
        styles: {
          type: 'object',
          description: 'Optional inline styles',
          additionalProperties: { type: 'string' },
        },
        content: { type: 'string', description: 'Text or HTML content' },
      },
      required: ['tag'],
    },
  },

  // ==========================================
  // 7️⃣ INSERTING ELEMENTS
  // ==========================================
  {
    name: 'dom_append_child',
    description: 'Append element(s) as last child of target.',
    input_schema: {
      type: 'object',
      properties: {
        parentSelector: { type: 'string', description: 'Parent element selector' },
        childHtml: { type: 'string', description: 'HTML to append' },
      },
      required: ['parentSelector', 'childHtml'],
    },
  },

  {
    name: 'dom_prepend_child',
    description: 'Insert element(s) as first child of target.',
    input_schema: {
      type: 'object',
      properties: {
        parentSelector: { type: 'string', description: 'Parent element selector' },
        childHtml: { type: 'string', description: 'HTML to prepend' },
      },
      required: ['parentSelector', 'childHtml'],
    },
  },

  {
    name: 'dom_insert_before',
    description: 'Insert element before target element.',
    input_schema: {
      type: 'object',
      properties: {
        targetSelector: { type: 'string', description: 'Target element selector' },
        html: { type: 'string', description: 'HTML to insert' },
      },
      required: ['targetSelector', 'html'],
    },
  },

  {
    name: 'dom_insert_after',
    description: 'Insert element after target element.',
    input_schema: {
      type: 'object',
      properties: {
        targetSelector: { type: 'string', description: 'Target element selector' },
        html: { type: 'string', description: 'HTML to insert' },
      },
      required: ['targetSelector', 'html'],
    },
  },

  // ==========================================
  // 8️⃣ REMOVING ELEMENTS
  // ==========================================
  {
    name: 'dom_remove',
    description: 'Remove element(s) from the DOM.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'CSS selector of element(s) to remove' },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_remove_children',
    description: 'Remove all child elements from target.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Parent element selector' },
      },
      required: ['selector'],
    },
  },

  // ==========================================
  // 9️⃣ REPLACING ELEMENTS
  // ==========================================
  {
    name: 'dom_replace',
    description: 'Replace element(s) with new HTML.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element(s) to replace' },
        html: { type: 'string', description: 'New HTML content' },
      },
      required: ['selector', 'html'],
    },
  },

  // ==========================================
  // 🔟 TRAVERSING THE DOM
  // ==========================================
  {
    name: 'dom_get_parent',
    description: 'Get parent element selector.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Child element selector' },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_get_children',
    description: 'Get all child elements.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Parent element selector' },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_get_siblings',
    description: 'Get sibling elements (previous and next).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
      },
      required: ['selector'],
    },
  },

  // ==========================================
  // 1️⃣1️⃣ EVENT HANDLING
  // ==========================================
  {
    name: 'dom_add_event',
    description: 'Add event listener to element(s). Use sparingly for preview.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        event: { type: 'string', description: 'Event type (e.g., "click", "hover", "input")' },
        action: { type: 'string', description: 'Action to perform (e.g., "show", "hide", "toggle")' },
        targetSelector: { type: 'string', description: 'Optional target element for action' },
      },
      required: ['selector', 'event', 'action'],
    },
  },

  // ==========================================
  // 1️⃣2️⃣ FORM MANIPULATION
  // ==========================================
  {
    name: 'dom_get_value',
    description: 'Get value from input/select/textarea.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Form element selector' },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_set_value',
    description: 'Set value of input/select/textarea.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Form element selector' },
        value: { type: 'string', description: 'New value' },
      },
      required: ['selector', 'value'],
    },
  },

  {
    name: 'dom_enable_input',
    description: 'Enable or disable input element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Input element selector' },
        enabled: { type: 'boolean', description: 'true to enable, false to disable' },
      },
      required: ['selector', 'enabled'],
    },
  },

  // ==========================================
  // 1️⃣3️⃣ DATA ATTRIBUTES
  // ==========================================
  {
    name: 'dom_set_data',
    description: 'Set data-* attribute on element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        key: { type: 'string', description: 'Data attribute name (without "data-" prefix)' },
        value: { type: 'string', description: 'Data value' },
      },
      required: ['selector', 'key', 'value'],
    },
  },

  {
    name: 'dom_get_data',
    description: 'Get data-* attribute value.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        key: { type: 'string', description: 'Data attribute name (without "data-" prefix)' },
      },
      required: ['selector', 'key'],
    },
  },

  // ==========================================
  // 1️⃣4️⃣ ANIMATIONS & VISIBILITY
  // ==========================================
  {
    name: 'dom_show',
    description: 'Show element(s) by removing display:none or adding visibility.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        method: {
          type: 'string',
          enum: ['display', 'visibility', 'opacity'],
          description: 'Method to show element',
          default: 'display',
        },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_hide',
    description: 'Hide element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        method: {
          type: 'string',
          enum: ['display', 'visibility', 'opacity'],
          description: 'Method to hide element',
          default: 'display',
        },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_toggle_visibility',
    description: 'Toggle element visibility.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
      },
      required: ['selector'],
    },
  },

  // ==========================================
  // 1️⃣5️⃣ MEASURING ELEMENTS
  // ==========================================
  {
    name: 'dom_get_dimensions',
    description: 'Get element dimensions and position.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_get_bounding_box',
    description: 'Get element bounding box (position, width, height).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
      },
      required: ['selector'],
    },
  },

  // ==========================================
  // 1️⃣6️⃣ TEXT MANIPULATION & FORMATTING
  // ==========================================
  {
    name: 'dom_set_font_family',
    description: 'Change font family of text element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        fontFamily: { type: 'string', description: 'Font family (e.g., "Arial", "Helvetica, sans-serif")' },
      },
      required: ['selector', 'fontFamily'],
    },
  },

  {
    name: 'dom_set_font_size',
    description: 'Change font size of text element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        fontSize: { type: 'string', description: 'Font size (e.g., "16px", "1.2em", "large")' },
      },
      required: ['selector', 'fontSize'],
    },
  },

  {
    name: 'dom_set_font_weight',
    description: 'Change font weight (boldness) of text.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        fontWeight: {
          type: 'string',
          description: 'Font weight (e.g., "normal", "bold", "lighter", "bolder", "100"-"900")',
        },
      },
      required: ['selector', 'fontWeight'],
    },
  },

  {
    name: 'dom_set_font_style',
    description: 'Change font style (italic/normal) of text.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        fontStyle: {
          type: 'string',
          enum: ['normal', 'italic', 'oblique'],
          description: 'Font style',
        },
      },
      required: ['selector', 'fontStyle'],
    },
  },

  {
    name: 'dom_set_text_color',
    description: 'Change text color of element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        color: { type: 'string', description: 'Color value (e.g., "red", "#ff0000", "rgb(255,0,0)")' },
      },
      required: ['selector', 'color'],
    },
  },

  {
    name: 'dom_set_text_align',
    description: 'Change text alignment.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        align: {
          type: 'string',
          enum: ['left', 'center', 'right', 'justify', 'start', 'end'],
          description: 'Text alignment',
        },
      },
      required: ['selector', 'align'],
    },
  },

  {
    name: 'dom_set_text_decoration',
    description: 'Add or remove text decoration (underline, strikethrough, etc).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        decoration: {
          type: 'string',
          enum: ['none', 'underline', 'overline', 'line-through', 'underline overline'],
          description: 'Text decoration style',
        },
        style: {
          type: 'string',
          enum: ['solid', 'double', 'dotted', 'dashed', 'wavy'],
          description: 'Optional decoration style',
        },
        color: { type: 'string', description: 'Optional decoration color' },
      },
      required: ['selector', 'decoration'],
    },
  },

  {
    name: 'dom_set_text_transform',
    description: 'Transform text case (uppercase, lowercase, capitalize).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        transform: {
          type: 'string',
          enum: ['none', 'uppercase', 'lowercase', 'capitalize'],
          description: 'Text transformation',
        },
      },
      required: ['selector', 'transform'],
    },
  },

  {
    name: 'dom_set_line_height',
    description: 'Change line height (spacing between lines).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        lineHeight: { type: 'string', description: 'Line height (e.g., "1.5", "24px", "normal")' },
      },
      required: ['selector', 'lineHeight'],
    },
  },

  {
    name: 'dom_set_letter_spacing',
    description: 'Change spacing between letters.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        spacing: { type: 'string', description: 'Letter spacing (e.g., "2px", "0.1em", "normal")' },
      },
      required: ['selector', 'spacing'],
    },
  },

  {
    name: 'dom_set_word_spacing',
    description: 'Change spacing between words.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        spacing: { type: 'string', description: 'Word spacing (e.g., "5px", "0.2em", "normal")' },
      },
      required: ['selector', 'spacing'],
    },
  },

  {
    name: 'dom_set_text_indent',
    description: 'Set indentation of first line of text.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        indent: { type: 'string', description: 'Text indent (e.g., "20px", "2em")' },
      },
      required: ['selector', 'indent'],
    },
  },

  {
    name: 'dom_set_text_shadow',
    description: 'Add shadow effect to text.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        shadow: {
          type: 'string',
          description: 'Text shadow (e.g., "2px 2px 4px rgba(0,0,0,0.5)", "none")',
        },
      },
      required: ['selector', 'shadow'],
    },
  },

  {
    name: 'dom_set_white_space',
    description: 'Control how white space inside element is handled.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        whiteSpace: {
          type: 'string',
          enum: ['normal', 'nowrap', 'pre', 'pre-line', 'pre-wrap', 'break-spaces'],
          description: 'White space handling',
        },
      },
      required: ['selector', 'whiteSpace'],
    },
  },

  {
    name: 'dom_set_text_overflow',
    description: 'Control how overflowed text is displayed (ellipsis, clip).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        overflow: {
          type: 'string',
          enum: ['clip', 'ellipsis'],
          description: 'Text overflow behavior',
        },
      },
      required: ['selector', 'overflow'],
    },
  },

  {
    name: 'dom_set_word_break',
    description: 'Control how words break when reaching line end.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        wordBreak: {
          type: 'string',
          enum: ['normal', 'break-all', 'keep-all', 'break-word'],
          description: 'Word break behavior',
        },
      },
      required: ['selector', 'wordBreak'],
    },
  },

  {
    name: 'dom_format_text_bold',
    description: 'Make text bold (toggles between normal and bold).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        bold: { type: 'boolean', description: 'true to make bold, false to remove bold' },
      },
      required: ['selector', 'bold'],
    },
  },

  {
    name: 'dom_format_text_italic',
    description: 'Make text italic (toggles between normal and italic).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        italic: { type: 'boolean', description: 'true to make italic, false to remove italic' },
      },
      required: ['selector', 'italic'],
    },
  },

  {
    name: 'dom_format_text_underline',
    description: 'Underline text (toggles underline on/off).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        underline: { type: 'boolean', description: 'true to underline, false to remove underline' },
      },
      required: ['selector', 'underline'],
    },
  },

  {
    name: 'dom_format_text_strikethrough',
    description: 'Add strikethrough to text (toggles on/off).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        strikethrough: { type: 'boolean', description: 'true to add strikethrough, false to remove' },
      },
      required: ['selector', 'strikethrough'],
    },
  },

  {
    name: 'dom_select_text',
    description: 'Select/highlight text in an element.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        start: { type: 'number', description: 'Optional start position (default 0)' },
        end: { type: 'number', description: 'Optional end position (default: all text)' },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_wrap_text',
    description: 'Wrap selected text with HTML tag (e.g., <span>, <strong>, <em>).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector containing text' },
        text: { type: 'string', description: 'Text to wrap' },
        tag: { type: 'string', description: 'HTML tag to wrap with (e.g., "span", "strong", "em")' },
        classes: {
          type: 'array',
          items: { type: 'string' },
          description: 'Optional classes to add to wrapper',
        },
        styles: {
          type: 'object',
          description: 'Optional styles for wrapper',
          additionalProperties: { type: 'string' },
        },
      },
      required: ['selector', 'text', 'tag'],
    },
  },

  // ==========================================
  // 1️⃣7️⃣ FONT MANAGEMENT & WEB FONTS
  // ==========================================
  {
    name: 'dom_load_google_font',
    description: 'Load a Google Font dynamically into the page.',
    input_schema: {
      type: 'object',
      properties: {
        fontFamily: {
          type: 'string',
          description: 'Google Font name (e.g., "Roboto", "Open Sans", "Playfair Display")',
        },
        weights: {
          type: 'array',
          items: { type: 'string' },
          description: 'Optional font weights to load (e.g., ["400", "700", "900"])',
        },
        styles: {
          type: 'array',
          items: { type: 'string' },
          description: 'Optional font styles (e.g., ["normal", "italic"])',
        },
      },
      required: ['fontFamily'],
    },
  },

  {
    name: 'dom_load_custom_font',
    description: 'Load a custom font from a URL using @font-face.',
    input_schema: {
      type: 'object',
      properties: {
        fontFamily: { type: 'string', description: 'Font family name to use' },
        fontUrl: { type: 'string', description: 'URL to the font file (.woff, .woff2, .ttf, etc.)' },
        fontWeight: { type: 'string', description: 'Font weight (e.g., "normal", "bold", "400")' },
        fontStyle: { type: 'string', description: 'Font style (e.g., "normal", "italic")' },
        format: {
          type: 'string',
          enum: ['woff', 'woff2', 'truetype', 'opentype'],
          description: 'Font file format',
        },
      },
      required: ['fontFamily', 'fontUrl'],
    },
  },

  {
    name: 'dom_get_available_fonts',
    description: 'Get list of fonts available in the document (system + loaded).',
    input_schema: {
      type: 'object',
      properties: {},
    },
  },

  {
    name: 'dom_apply_font_to_selection',
    description: 'Apply font family to selected text range.',
    input_schema: {
      type: 'object',
      properties: {
        fontFamily: { type: 'string', description: 'Font family to apply' },
      },
      required: ['fontFamily'],
    },
  },

  {
    name: 'dom_get_font_info',
    description: 'Get detailed font information from element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_set_font_fallback',
    description: 'Set font family with fallback chain.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        fontStack: {
          type: 'array',
          items: { type: 'string' },
          description: 'Font stack array (e.g., ["Helvetica", "Arial", "sans-serif"])',
        },
      },
      required: ['selector', 'fontStack'],
    },
  },

  // ==========================================
  // 1️⃣8️⃣ DRAG AND DROP POSITIONING
  // ==========================================
  {
    name: 'dom_enable_dragging',
    description: 'Enable drag and drop repositioning for element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        axis: {
          type: 'string',
          enum: ['both', 'x', 'y'],
          description: 'Axis constraint (both, x-only, y-only)',
          default: 'both',
        },
        containment: {
          type: 'string',
          description: 'Optional containment selector (element must stay within)',
        },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_set_position',
    description: 'Set absolute/relative position of element(s).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        position: {
          type: 'string',
          enum: ['static', 'relative', 'absolute', 'fixed', 'sticky'],
          description: 'CSS position type',
        },
        top: { type: 'string', description: 'Top position (e.g., "10px", "20%")' },
        left: { type: 'string', description: 'Left position' },
        right: { type: 'string', description: 'Right position' },
        bottom: { type: 'string', description: 'Bottom position' },
        zIndex: { type: 'number', description: 'Z-index value' },
      },
      required: ['selector', 'position'],
    },
  },

  {
    name: 'dom_move_to_coordinates',
    description: 'Move element to specific x, y coordinates.',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
        x: { type: 'number', description: 'X coordinate in pixels' },
        y: { type: 'number', description: 'Y coordinate in pixels' },
        transition: { type: 'boolean', description: 'Animate the movement', default: false },
      },
      required: ['selector', 'x', 'y'],
    },
  },

  {
    name: 'dom_swap_elements',
    description: 'Swap positions of two elements.',
    input_schema: {
      type: 'object',
      properties: {
        selector1: { type: 'string', description: 'First element selector' },
        selector2: { type: 'string', description: 'Second element selector' },
        animate: { type: 'boolean', description: 'Animate the swap', default: false },
      },
      required: ['selector1', 'selector2'],
    },
  },

  {
    name: 'dom_reorder_children',
    description: 'Reorder child elements within parent.',
    input_schema: {
      type: 'object',
      properties: {
        parentSelector: { type: 'string', description: 'Parent element selector' },
        order: {
          type: 'array',
          items: { type: 'number' },
          description: 'New order by index (e.g., [2, 0, 1] moves 3rd item first)',
        },
      },
      required: ['parentSelector', 'order'],
    },
  },

  {
    name: 'dom_bring_to_front',
    description: 'Bring element to front (highest z-index).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
      },
      required: ['selector'],
    },
  },

  {
    name: 'dom_send_to_back',
    description: 'Send element to back (lowest z-index).',
    input_schema: {
      type: 'object',
      properties: {
        selector: { type: 'string', description: 'Element selector' },
      },
      required: ['selector'],
    },
  },

  // ==========================================
  // 🎯 SPECIAL: SNAPSHOT & APPLY
  // ==========================================
  {
    name: 'dom_snapshot',
    description: 'Take a snapshot of current DOM state for later persistence.',
    input_schema: {
      type: 'object',
      properties: {
        description: { type: 'string', description: 'Description of changes made' },
      },
      required: ['description'],
    },
  },

  {
    name: 'dom_get_change_log',
    description: 'Get log of all DOM changes made in this session.',
    input_schema: {
      type: 'object',
      properties: {},
    },
  },
];
