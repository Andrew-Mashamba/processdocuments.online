---
name: format-html
description: Format all responses as styled HTML with inline CSS for web rendering (Monochrome Design)
allowed-tools: all
argument-hint: "[optional: specific format preferences]"
---

# Inline CSS HTML Response Formatting - Monochrome Design

Format ALL text responses as styled HTML with inline CSS. Never return raw markdown. Always return properly styled HTML that renders correctly without external stylesheets.

## Design Philosophy

This formatting follows the ZIMA AI Monochrome Design Guidelines:
- **No colorful elements**: Use only neutral grays for a professional, focused experience
- **Reduced cognitive load**: Minimal color variation helps users focus on content
- **Professional appearance**: Monochrome palette conveys trust and sophistication

## Color Palette Reference

| Purpose | Hex Code | Usage |
|---------|----------|-------|
| Primary Background | `#FAFAFA` | Page/section backgrounds |
| Surface/Card | `#FFFFFF` | Cards, buttons, inputs |
| Primary Text | `#1A1A1A` | Headings, important text |
| Secondary Text | `#525252` | Body text |
| Muted Text | `#737373` | Captions, hints |
| Light Text | `#A3A3A3` | Disabled, tertiary |
| Border | `#E5E5E5` | Dividers, borders |
| Accent Background | `#1A1A1A` | Icons, highlights |

## Core Formatting Rules

### 1. Main Container
```html
<div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #525252;">
  <!-- content here -->
</div>
```

### 2. Headings
```html
<h1 style="font-size: 1.25rem; font-weight: 700; color: #1A1A1A; margin: 0 0 1rem 0; padding-bottom: 0.5rem; border-bottom: 1px solid #E5E5E5;">Main Title</h1>
<h2 style="font-size: 1.1rem; font-weight: 600; color: #1A1A1A; margin: 1.5rem 0 0.75rem 0;">Section Title</h2>
<h3 style="font-size: 0.95rem; font-weight: 500; color: #525252; margin: 1rem 0 0.5rem 0;">Subsection</h3>
```

### 3. Paragraphs
```html
<p style="color: #525252; margin-bottom: 1rem; font-size: 0.875rem;">Regular paragraph text...</p>
<p style="font-size: 0.75rem; color: #737373; font-style: italic;">Secondary/note text...</p>
```

### 4. Tables
```html
<div style="overflow-x: auto; margin: 1rem 0;">
  <table style="width: 100%; border-collapse: collapse; border: 1px solid #E5E5E5; border-radius: 12px; overflow: hidden;">
    <thead>
      <tr style="background: #FAFAFA;">
        <th style="padding: 12px 16px; text-align: left; font-size: 0.6875rem; font-weight: 600; color: #1A1A1A; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #E5E5E5;">Header</th>
      </tr>
    </thead>
    <tbody>
      <tr style="border-bottom: 1px solid #E5E5E5;">
        <td style="padding: 12px 16px; font-size: 0.8125rem; color: #525252;">Cell content</td>
      </tr>
      <tr style="background: #FAFAFA; border-bottom: 1px solid #E5E5E5;">
        <td style="padding: 12px 16px; font-size: 0.8125rem; color: #525252;">Alternating row</td>
      </tr>
    </tbody>
    <tfoot>
      <tr style="background: #F5F5F5;">
        <td style="padding: 12px 16px; font-size: 0.8125rem; font-weight: 600; color: #1A1A1A;">Total/Summary</td>
      </tr>
    </tfoot>
  </table>
</div>
```

### 5. Unordered Lists
```html
<ul style="list-style-type: disc; padding-left: 1.5rem; margin: 1rem 0; color: #525252;">
  <li style="margin-bottom: 0.5rem; font-size: 0.875rem;">List item with <strong style="color: #1A1A1A;">emphasis</strong></li>
  <li style="margin-bottom: 0.5rem; font-size: 0.875rem;">Another item</li>
</ul>
```

### 6. Ordered Lists
```html
<ol style="list-style-type: decimal; padding-left: 1.5rem; margin: 1rem 0; color: #525252;">
  <li style="margin-bottom: 0.5rem; font-size: 0.875rem;">First step</li>
  <li style="margin-bottom: 0.5rem; font-size: 0.875rem;">Second step</li>
</ol>
```

### 7. Code Blocks
```html
<pre style="background: #1A1A1A; color: #E5E5E5; border-radius: 12px; padding: 1rem; margin: 1rem 0; overflow-x: auto; font-size: 0.8125rem; font-family: 'Monaco', 'Menlo', monospace;"><code>code content here</code></pre>
```

### 8. Inline Code
```html
<code style="background: #F5F5F5; color: #1A1A1A; padding: 2px 6px; border-radius: 6px; font-size: 0.8125rem; font-family: monospace;">inline code</code>
```

### 9. Blockquotes
```html
<blockquote style="border-left: 4px solid #1A1A1A; padding: 0.5rem 1rem; margin: 1rem 0; background: #FAFAFA; border-radius: 0 12px 12px 0;">
  <p style="color: #525252; font-style: italic; margin: 0; font-size: 0.875rem;">Quote text...</p>
</blockquote>
```

### 10. Callout Boxes (Monochrome)
```html
<!-- Standard Callout -->
<div style="background: #FAFAFA; border-left: 4px solid #1A1A1A; padding: 1rem; margin: 1rem 0; border-radius: 0 12px 12px 0;">
  <p style="font-size: 0.8125rem; color: #1A1A1A; margin: 0; font-weight: 500;">Important information...</p>
</div>

<!-- Light Callout -->
<div style="background: #FFFFFF; border: 1px solid #E5E5E5; padding: 1rem; margin: 1rem 0; border-radius: 12px;">
  <p style="font-size: 0.8125rem; color: #525252; margin: 0;">General note...</p>
</div>

<!-- Emphasized Callout -->
<div style="background: #1A1A1A; padding: 1rem; margin: 1rem 0; border-radius: 12px;">
  <p style="font-size: 0.8125rem; color: #FFFFFF; margin: 0;">Highlighted message...</p>
</div>
```

### 11. Cards/Sections
```html
<div style="background: #FFFFFF; border: 1px solid #E5E5E5; border-radius: 16px; padding: 1.25rem; margin: 1rem 0; box-shadow: 0 1px 3px rgba(0,0,0,0.05);">
  <h3 style="font-size: 0.9375rem; font-weight: 600; color: #1A1A1A; margin: 0 0 0.75rem 0;">Card Title</h3>
  <p style="color: #737373; margin: 0; font-size: 0.8125rem;">Card content...</p>
</div>
```

### 12. Stats/Metrics Grid (Monochrome)
```html
<div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 1rem; margin: 1rem 0;">
  <div style="background: #1A1A1A; border-radius: 12px; padding: 1rem; color: white;">
    <p style="font-size: 0.6875rem; opacity: 0.7; margin: 0; text-transform: uppercase; letter-spacing: 0.05em;">Metric Label</p>
    <p style="font-size: 1.5rem; font-weight: 700; margin: 0.25rem 0 0 0;">$1,234</p>
  </div>
  <div style="background: #FFFFFF; border: 1px solid #E5E5E5; border-radius: 12px; padding: 1rem;">
    <p style="font-size: 0.6875rem; color: #737373; margin: 0; text-transform: uppercase; letter-spacing: 0.05em;">Another Metric</p>
    <p style="font-size: 1.5rem; font-weight: 700; color: #1A1A1A; margin: 0.25rem 0 0 0;">45%</p>
  </div>
</div>
```

### 13. Progress Bars (Monochrome)
```html
<div style="margin: 1rem 0;">
  <div style="display: flex; justify-content: space-between; font-size: 0.8125rem; color: #737373; margin-bottom: 0.25rem;">
    <span>Category</span>
    <span style="font-weight: 600; color: #1A1A1A;">45%</span>
  </div>
  <div style="width: 100%; background: #E5E5E5; border-radius: 9999px; height: 8px; overflow: hidden;">
    <div style="background: #1A1A1A; height: 100%; border-radius: 9999px; width: 45%;"></div>
  </div>
</div>
```

### 14. Badges/Tags (Monochrome)
```html
<span style="display: inline-block; padding: 2px 10px; border-radius: 9999px; font-size: 0.6875rem; font-weight: 500; background: #1A1A1A; color: #FFFFFF;">Primary</span>
<span style="display: inline-block; padding: 2px 10px; border-radius: 9999px; font-size: 0.6875rem; font-weight: 500; background: #F5F5F5; color: #525252;">Secondary</span>
<span style="display: inline-block; padding: 2px 10px; border-radius: 9999px; font-size: 0.6875rem; font-weight: 500; background: #FFFFFF; color: #1A1A1A; border: 1px solid #E5E5E5;">Outlined</span>
```

### 15. Links
```html
<a href="#" style="color: #1A1A1A; text-decoration: underline; font-weight: 500;">Link text</a>
```

### 16. Horizontal Dividers
```html
<hr style="border: none; border-top: 1px solid #E5E5E5; margin: 1.5rem 0;"/>
```

### 17. Key-Value Pairs
```html
<div style="background: #FAFAFA; border-radius: 12px; padding: 1rem; margin: 1rem 0;">
  <div style="display: flex; justify-content: space-between; padding: 0.5rem 0; border-bottom: 1px solid #E5E5E5;">
    <span style="color: #737373; font-size: 0.8125rem;">Label:</span>
    <span style="font-weight: 500; color: #1A1A1A; font-size: 0.8125rem;">Value</span>
  </div>
  <div style="display: flex; justify-content: space-between; padding: 0.5rem 0;">
    <span style="color: #737373; font-size: 0.8125rem;">Another:</span>
    <span style="font-weight: 500; color: #1A1A1A; font-size: 0.8125rem;">Value</span>
  </div>
</div>
```

### 18. File/Download Links
```html
<div style="display: flex; align-items: center; gap: 12px; padding: 12px; background: #FFFFFF; border-radius: 12px; border: 1px solid #E5E5E5; margin: 0.75rem 0;">
  <div style="width: 40px; height: 40px; background: #1A1A1A; border-radius: 12px; display: flex; align-items: center; justify-content: center;">
    <span style="font-size: 1rem; filter: grayscale(1) brightness(10);">📄</span>
  </div>
  <div style="flex: 1;">
    <p style="font-weight: 500; color: #1A1A1A; margin: 0; font-size: 0.8125rem;">filename.xlsx</p>
    <p style="font-size: 0.6875rem; color: #737373; margin: 0;">Excel Spreadsheet - 15.2 KB</p>
  </div>
</div>
```

### 19. Pie Chart (SVG - Monochrome)
```html
<div style="margin: 1rem 0; text-align: center;">
  <svg width="200" height="200" viewBox="0 0 200 200">
    <!-- Pie slice 1: 40% - Darkest -->
    <circle cx="100" cy="100" r="80" fill="transparent" stroke="#1A1A1A" stroke-width="40"
            stroke-dasharray="201 503" stroke-dashoffset="0" transform="rotate(-90 100 100)"/>
    <!-- Pie slice 2: 30% - Dark gray -->
    <circle cx="100" cy="100" r="80" fill="transparent" stroke="#525252" stroke-width="40"
            stroke-dasharray="151 503" stroke-dashoffset="-201" transform="rotate(-90 100 100)"/>
    <!-- Pie slice 3: 20% - Medium gray -->
    <circle cx="100" cy="100" r="80" fill="transparent" stroke="#A3A3A3" stroke-width="40"
            stroke-dasharray="100 503" stroke-dashoffset="-352" transform="rotate(-90 100 100)"/>
    <!-- Pie slice 4: 10% - Light gray -->
    <circle cx="100" cy="100" r="80" fill="transparent" stroke="#D4D4D4" stroke-width="40"
            stroke-dasharray="50 503" stroke-dashoffset="-452" transform="rotate(-90 100 100)"/>
  </svg>
  <!-- Legend -->
  <div style="display: flex; justify-content: center; gap: 1rem; margin-top: 0.5rem; flex-wrap: wrap;">
    <span style="display: flex; align-items: center; gap: 4px; font-size: 0.6875rem; color: #525252;">
      <span style="width: 12px; height: 12px; background: #1A1A1A; border-radius: 2px;"></span> Housing 40%
    </span>
    <span style="display: flex; align-items: center; gap: 4px; font-size: 0.6875rem; color: #525252;">
      <span style="width: 12px; height: 12px; background: #525252; border-radius: 2px;"></span> Food 30%
    </span>
    <span style="display: flex; align-items: center; gap: 4px; font-size: 0.6875rem; color: #525252;">
      <span style="width: 12px; height: 12px; background: #A3A3A3; border-radius: 2px;"></span> Transport 20%
    </span>
    <span style="display: flex; align-items: center; gap: 4px; font-size: 0.6875rem; color: #525252;">
      <span style="width: 12px; height: 12px; background: #D4D4D4; border-radius: 2px;"></span> Other 10%
    </span>
  </div>
</div>
```

### 20. Donut Chart (SVG - Monochrome)
```html
<div style="margin: 1rem 0; text-align: center;">
  <svg width="200" height="200" viewBox="0 0 200 200">
    <!-- Background circle -->
    <circle cx="100" cy="100" r="70" fill="transparent" stroke="#E5E5E5" stroke-width="20"/>
    <!-- Progress: 75% -->
    <circle cx="100" cy="100" r="70" fill="transparent" stroke="#1A1A1A" stroke-width="20"
            stroke-dasharray="330 440" stroke-linecap="round" transform="rotate(-90 100 100)"/>
    <!-- Center text -->
    <text x="100" y="95" text-anchor="middle" style="font-size: 2rem; font-weight: 700; fill: #1A1A1A;">75%</text>
    <text x="100" y="115" text-anchor="middle" style="font-size: 0.6875rem; fill: #737373;">Complete</text>
  </svg>
</div>
```

### 21. Horizontal Bar Chart (Monochrome)
```html
<div style="margin: 1rem 0;">
  <div style="margin-bottom: 0.75rem;">
    <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
      <span style="font-size: 0.8125rem; color: #525252;">Housing</span>
      <span style="font-size: 0.8125rem; font-weight: 600; color: #1A1A1A;">$1,200 (40%)</span>
    </div>
    <div style="height: 24px; background: #E5E5E5; border-radius: 6px; overflow: hidden;">
      <div style="height: 100%; width: 40%; background: #1A1A1A; border-radius: 6px;"></div>
    </div>
  </div>
  <div style="margin-bottom: 0.75rem;">
    <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
      <span style="font-size: 0.8125rem; color: #525252;">Food</span>
      <span style="font-size: 0.8125rem; font-weight: 600; color: #1A1A1A;">$900 (30%)</span>
    </div>
    <div style="height: 24px; background: #E5E5E5; border-radius: 6px; overflow: hidden;">
      <div style="height: 100%; width: 30%; background: #525252; border-radius: 6px;"></div>
    </div>
  </div>
  <div style="margin-bottom: 0.75rem;">
    <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
      <span style="font-size: 0.8125rem; color: #525252;">Transport</span>
      <span style="font-size: 0.8125rem; font-weight: 600; color: #1A1A1A;">$600 (20%)</span>
    </div>
    <div style="height: 24px; background: #E5E5E5; border-radius: 6px; overflow: hidden;">
      <div style="height: 100%; width: 20%; background: #737373; border-radius: 6px;"></div>
    </div>
  </div>
</div>
```

### 22. Vertical Bar Chart (Monochrome)
```html
<div style="margin: 1rem 0;">
  <div style="display: flex; align-items: flex-end; justify-content: space-around; height: 200px; padding: 0 1rem; border-bottom: 2px solid #E5E5E5;">
    <div style="display: flex; flex-direction: column; align-items: center; width: 60px;">
      <span style="font-size: 0.6875rem; font-weight: 600; color: #1A1A1A; margin-bottom: 4px;">$1.2k</span>
      <div style="width: 40px; height: 160px; background: #1A1A1A; border-radius: 6px 6px 0 0;"></div>
    </div>
    <div style="display: flex; flex-direction: column; align-items: center; width: 60px;">
      <span style="font-size: 0.6875rem; font-weight: 600; color: #1A1A1A; margin-bottom: 4px;">$900</span>
      <div style="width: 40px; height: 120px; background: #525252; border-radius: 6px 6px 0 0;"></div>
    </div>
    <div style="display: flex; flex-direction: column; align-items: center; width: 60px;">
      <span style="font-size: 0.6875rem; font-weight: 600; color: #1A1A1A; margin-bottom: 4px;">$600</span>
      <div style="width: 40px; height: 80px; background: #737373; border-radius: 6px 6px 0 0;"></div>
    </div>
    <div style="display: flex; flex-direction: column; align-items: center; width: 60px;">
      <span style="font-size: 0.6875rem; font-weight: 600; color: #1A1A1A; margin-bottom: 4px;">$300</span>
      <div style="width: 40px; height: 40px; background: #A3A3A3; border-radius: 6px 6px 0 0;"></div>
    </div>
  </div>
  <div style="display: flex; justify-content: space-around; padding: 0.5rem 1rem;">
    <span style="font-size: 0.6875rem; color: #737373; width: 60px; text-align: center;">Jan</span>
    <span style="font-size: 0.6875rem; color: #737373; width: 60px; text-align: center;">Feb</span>
    <span style="font-size: 0.6875rem; color: #737373; width: 60px; text-align: center;">Mar</span>
    <span style="font-size: 0.6875rem; color: #737373; width: 60px; text-align: center;">Apr</span>
  </div>
</div>
```

### 23. Line Chart (SVG - Monochrome)
```html
<div style="margin: 1rem 0;">
  <svg width="100%" height="200" viewBox="0 0 400 200" preserveAspectRatio="xMidYMid meet">
    <!-- Grid lines -->
    <line x1="40" y1="20" x2="40" y2="160" stroke="#E5E5E5" stroke-width="1"/>
    <line x1="40" y1="160" x2="380" y2="160" stroke="#E5E5E5" stroke-width="1"/>
    <line x1="40" y1="90" x2="380" y2="90" stroke="#E5E5E5" stroke-width="1" stroke-dasharray="4"/>
    <line x1="40" y1="20" x2="380" y2="20" stroke="#E5E5E5" stroke-width="1" stroke-dasharray="4"/>
    <!-- Y-axis labels -->
    <text x="35" y="165" text-anchor="end" style="font-size: 0.5625rem; fill: #737373;">$0</text>
    <text x="35" y="95" text-anchor="end" style="font-size: 0.5625rem; fill: #737373;">$500</text>
    <text x="35" y="25" text-anchor="end" style="font-size: 0.5625rem; fill: #737373;">$1k</text>
    <!-- Line path -->
    <polyline fill="none" stroke="#1A1A1A" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"
              points="60,120 120,80 180,100 240,40 300,60 360,30"/>
    <!-- Area fill -->
    <polygon fill="#1A1A1A" opacity="0.1"
             points="60,120 120,80 180,100 240,40 300,60 360,30 360,160 60,160"/>
    <!-- Data points -->
    <circle cx="60" cy="120" r="5" fill="#1A1A1A"/>
    <circle cx="120" cy="80" r="5" fill="#1A1A1A"/>
    <circle cx="180" cy="100" r="5" fill="#1A1A1A"/>
    <circle cx="240" cy="40" r="5" fill="#1A1A1A"/>
    <circle cx="300" cy="60" r="5" fill="#1A1A1A"/>
    <circle cx="360" cy="30" r="5" fill="#1A1A1A"/>
    <!-- X-axis labels -->
    <text x="60" y="180" text-anchor="middle" style="font-size: 0.5625rem; fill: #737373;">Jan</text>
    <text x="120" y="180" text-anchor="middle" style="font-size: 0.5625rem; fill: #737373;">Feb</text>
    <text x="180" y="180" text-anchor="middle" style="font-size: 0.5625rem; fill: #737373;">Mar</text>
    <text x="240" y="180" text-anchor="middle" style="font-size: 0.5625rem; fill: #737373;">Apr</text>
    <text x="300" y="180" text-anchor="middle" style="font-size: 0.5625rem; fill: #737373;">May</text>
    <text x="360" y="180" text-anchor="middle" style="font-size: 0.5625rem; fill: #737373;">Jun</text>
  </svg>
</div>
```

### 24. Multi-Line Chart (SVG - Monochrome)
```html
<div style="margin: 1rem 0;">
  <svg width="100%" height="220" viewBox="0 0 400 220" preserveAspectRatio="xMidYMid meet">
    <!-- Grid -->
    <line x1="50" y1="20" x2="50" y2="160" stroke="#E5E5E5"/>
    <line x1="50" y1="160" x2="380" y2="160" stroke="#E5E5E5"/>
    <!-- Line 1 - Solid (Primary) -->
    <polyline fill="none" stroke="#1A1A1A" stroke-width="2.5" stroke-linecap="round"
              points="70,100 130,80 190,90 250,60 310,70 370,40"/>
    <!-- Line 2 - Dashed (Secondary) -->
    <polyline fill="none" stroke="#737373" stroke-width="2.5" stroke-linecap="round" stroke-dasharray="8 4"
              points="70,130 130,120 190,140 250,110 310,120 370,100"/>
    <!-- Legend -->
    <line x1="100" y1="191" x2="130" y2="191" stroke="#1A1A1A" stroke-width="2.5"/>
    <text x="138" y="195" style="font-size: 0.6875rem; fill: #525252;">Income</text>
    <line x1="200" y1="191" x2="230" y2="191" stroke="#737373" stroke-width="2.5" stroke-dasharray="8 4"/>
    <text x="238" y="195" style="font-size: 0.6875rem; fill: #525252;">Expenses</text>
  </svg>
</div>
```

### 25. Gauge/Meter Chart (SVG - Monochrome)
```html
<div style="margin: 1rem 0; text-align: center;">
  <svg width="200" height="120" viewBox="0 0 200 120">
    <!-- Background arc -->
    <path d="M 20 100 A 80 80 0 0 1 180 100" fill="none" stroke="#E5E5E5" stroke-width="16" stroke-linecap="round"/>
    <!-- Progress arc (75%) -->
    <path d="M 20 100 A 80 80 0 0 1 155 35" fill="none" stroke="#1A1A1A" stroke-width="16" stroke-linecap="round"/>
    <!-- Needle -->
    <line x1="100" y1="100" x2="155" y2="55" stroke="#1A1A1A" stroke-width="3" stroke-linecap="round"/>
    <circle cx="100" cy="100" r="8" fill="#1A1A1A"/>
    <!-- Value -->
    <text x="100" y="85" text-anchor="middle" style="font-size: 1.25rem; font-weight: 700; fill: #1A1A1A;">75</text>
    <text x="100" y="115" text-anchor="middle" style="font-size: 0.6875rem; fill: #737373;">Health Score</text>
  </svg>
</div>
```

### 26. Sparkline (SVG - Monochrome)
```html
<span style="display: inline-block; vertical-align: middle;">
  <svg width="80" height="24" viewBox="0 0 80 24">
    <polyline fill="none" stroke="#1A1A1A" stroke-width="2" stroke-linecap="round"
              points="2,18 12,14 22,16 32,8 42,12 52,6 62,10 72,4 78,8"/>
  </svg>
</span>
```

### 27. Stacked Bar Chart (Monochrome)
```html
<div style="margin: 1rem 0;">
  <div style="margin-bottom: 1rem;">
    <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
      <span style="font-size: 0.8125rem; color: #525252;">Q1 2024</span>
      <span style="font-size: 0.8125rem; font-weight: 600; color: #1A1A1A;">$3,000</span>
    </div>
    <div style="display: flex; height: 28px; border-radius: 6px; overflow: hidden;">
      <div style="width: 40%; background: #1A1A1A;" title="Housing $1,200"></div>
      <div style="width: 30%; background: #525252;" title="Food $900"></div>
      <div style="width: 20%; background: #A3A3A3;" title="Transport $600"></div>
      <div style="width: 10%; background: #D4D4D4;" title="Other $300"></div>
    </div>
  </div>
  <div style="margin-bottom: 1rem;">
    <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
      <span style="font-size: 0.8125rem; color: #525252;">Q2 2024</span>
      <span style="font-size: 0.8125rem; font-weight: 600; color: #1A1A1A;">$3,200</span>
    </div>
    <div style="display: flex; height: 28px; border-radius: 6px; overflow: hidden;">
      <div style="width: 38%; background: #1A1A1A;"></div>
      <div style="width: 32%; background: #525252;"></div>
      <div style="width: 18%; background: #A3A3A3;"></div>
      <div style="width: 12%; background: #D4D4D4;"></div>
    </div>
  </div>
  <!-- Legend -->
  <div style="display: flex; gap: 1rem; flex-wrap: wrap; margin-top: 0.5rem;">
    <span style="display: flex; align-items: center; gap: 4px; font-size: 0.6875rem; color: #525252;">
      <span style="width: 12px; height: 12px; background: #1A1A1A; border-radius: 2px;"></span> Housing
    </span>
    <span style="display: flex; align-items: center; gap: 4px; font-size: 0.6875rem; color: #525252;">
      <span style="width: 12px; height: 12px; background: #525252; border-radius: 2px;"></span> Food
    </span>
    <span style="display: flex; align-items: center; gap: 4px; font-size: 0.6875rem; color: #525252;">
      <span style="width: 12px; height: 12px; background: #A3A3A3; border-radius: 2px;"></span> Transport
    </span>
    <span style="display: flex; align-items: center; gap: 4px; font-size: 0.6875rem; color: #525252;">
      <span style="width: 12px; height: 12px; background: #D4D4D4; border-radius: 2px;"></span> Other
    </span>
  </div>
</div>
```

### 28. Comparison Chart (Monochrome)
```html
<div style="margin: 1rem 0;">
  <div style="margin-bottom: 1rem;">
    <span style="font-size: 0.8125rem; color: #525252; display: block; margin-bottom: 4px;">Revenue vs Target</span>
    <div style="display: flex; gap: 4px; align-items: center;">
      <div style="flex: 1; height: 20px; background: #E5E5E5; border-radius: 6px; overflow: hidden; position: relative;">
        <div style="position: absolute; height: 100%; width: 85%; background: #1A1A1A; border-radius: 6px;"></div>
        <div style="position: absolute; height: 100%; width: 100%; border-right: 3px dashed #737373;"></div>
      </div>
      <span style="font-size: 0.6875rem; color: #1A1A1A; font-weight: 600; width: 50px;">85%</span>
    </div>
    <div style="display: flex; justify-content: space-between; font-size: 0.5625rem; color: #737373; margin-top: 2px;">
      <span>$0</span>
      <span>Target: $100k</span>
    </div>
  </div>
</div>
```

### 29. Radial/Circle Progress (Monochrome)
```html
<div style="display: flex; justify-content: space-around; margin: 1rem 0; flex-wrap: wrap; gap: 1rem;">
  <!-- Circle 1 - Primary -->
  <div style="text-align: center;">
    <svg width="80" height="80" viewBox="0 0 80 80">
      <circle cx="40" cy="40" r="35" fill="none" stroke="#E5E5E5" stroke-width="6"/>
      <circle cx="40" cy="40" r="35" fill="none" stroke="#1A1A1A" stroke-width="6"
              stroke-dasharray="176 220" stroke-linecap="round" transform="rotate(-90 40 40)"/>
      <text x="40" y="44" text-anchor="middle" style="font-size: 1rem; font-weight: 700; fill: #1A1A1A;">80%</text>
    </svg>
    <p style="font-size: 0.6875rem; color: #737373; margin: 0.25rem 0 0 0;">Sales</p>
  </div>
  <!-- Circle 2 - Medium -->
  <div style="text-align: center;">
    <svg width="80" height="80" viewBox="0 0 80 80">
      <circle cx="40" cy="40" r="35" fill="none" stroke="#E5E5E5" stroke-width="6"/>
      <circle cx="40" cy="40" r="35" fill="none" stroke="#525252" stroke-width="6"
              stroke-dasharray="154 220" stroke-linecap="round" transform="rotate(-90 40 40)"/>
      <text x="40" y="44" text-anchor="middle" style="font-size: 1rem; font-weight: 700; fill: #1A1A1A;">70%</text>
    </svg>
    <p style="font-size: 0.6875rem; color: #737373; margin: 0.25rem 0 0 0;">Growth</p>
  </div>
  <!-- Circle 3 - Light -->
  <div style="text-align: center;">
    <svg width="80" height="80" viewBox="0 0 80 80">
      <circle cx="40" cy="40" r="35" fill="none" stroke="#E5E5E5" stroke-width="6"/>
      <circle cx="40" cy="40" r="35" fill="none" stroke="#737373" stroke-width="6"
              stroke-dasharray="198 220" stroke-linecap="round" transform="rotate(-90 40 40)"/>
      <text x="40" y="44" text-anchor="middle" style="font-size: 1rem; font-weight: 700; fill: #1A1A1A;">90%</text>
    </svg>
    <p style="font-size: 0.6875rem; color: #737373; margin: 0.25rem 0 0 0;">Target</p>
  </div>
</div>
```

### 30. Area Chart (SVG - Monochrome)
```html
<div style="margin: 1rem 0;">
  <svg width="100%" height="150" viewBox="0 0 400 150" preserveAspectRatio="xMidYMid meet">
    <!-- Area fill -->
    <polygon fill="#1A1A1A" opacity="0.15"
             points="40,130 80,100 120,110 160,70 200,80 240,50 280,60 320,40 360,55 360,130 40,130"/>
    <!-- Line -->
    <polyline fill="none" stroke="#1A1A1A" stroke-width="2.5"
              points="40,130 80,100 120,110 160,70 200,80 240,50 280,60 320,40 360,55"/>
    <!-- Axis -->
    <line x1="40" y1="130" x2="360" y2="130" stroke="#E5E5E5" stroke-width="1"/>
  </svg>
</div>
```

## Monochrome Chart Color Scale

Use these grayscale values for data visualization:
- **Primary/Darkest**: #1A1A1A (neutral-900)
- **Secondary**: #525252 (neutral-600)
- **Tertiary**: #737373 (neutral-500)
- **Quaternary**: #A3A3A3 (neutral-400)
- **Light**: #D4D4D4 (neutral-300)
- **Background**: #E5E5E5 (neutral-200)
- **Surface**: #F5F5F5 (neutral-100)

## Chart Calculation Notes

For **Pie Charts** (stroke-dasharray calculation):
- Circumference = 2 × π × radius ≈ 503 (for r=80)
- For X%: stroke-dasharray = (X/100 × 503) 503
- stroke-dashoffset = negative sum of previous segments

For **Donut Charts**:
- Circumference = 2 × π × 70 ≈ 440
- For X%: stroke-dasharray = (X/100 × 440) 440

## Important Rules

1. **ALWAYS** return valid HTML with inline styles
2. **NEVER** return raw markdown (no **, ##, |---|, etc.)
3. **NEVER** use Tailwind classes - only inline CSS
4. Use semantic HTML elements (table, ul, ol, blockquote, etc.)
5. Apply consistent spacing with margin/padding
6. Escape special characters (&lt; &gt; &amp;)
7. Use tabular-nums or monospace for number alignment
8. **Use ONLY monochrome colors** - no blues, greens, reds, or other hues
9. For charts, calculate percentages and positions accurately
10. Always include legends for charts with multiple data series
11. Border radius should be 12px (0.75rem) for cards, 6px for smaller elements
12. Typography: 15px titles, 13px body, 11px captions
