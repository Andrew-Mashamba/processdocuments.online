# ZIMA AI Design Guidelines - Tailwind CSS

## Minimalist Design Principles

This document outlines the core design principles and guidelines for the ZIMA AI application, adapted for Tailwind CSS, focusing on minimalist aesthetics and optimal user experience.

---

## 🎨 Core Design Principles

### 1. Monochrome Color Palette

Our design philosophy centers on a sophisticated monochrome color scheme that reduces visual noise and creates a professional, focused user experience.

#### Tailwind Color Configuration

Add these custom colors to your `tailwind.config.js`:

```javascript
// tailwind.config.js
module.exports = {
  theme: {
    extend: {
      colors: {
        'zima': {
          'bg': '#FAFAFA',        // Primary Background
          'surface': '#FFFFFF',   // Button/Card Background
          'text': '#1A1A1A',      // Primary Text
          'muted': '#666666',     // Secondary Text
          'icon-bg': '#1A1A1A',   // Icon Background
          'accent': '#999999',    // Accent Elements
        }
      }
    }
  }
}
```

#### Color Class Reference

| Purpose | Hex Code | Tailwind Class | Custom Class |
|---------|----------|----------------|--------------|
| Primary Background | `#FAFAFA` | `bg-neutral-50` | `bg-zima-bg` |
| Button/Card Background | `#FFFFFF` | `bg-white` | `bg-zima-surface` |
| Primary Text | `#1A1A1A` | `text-neutral-900` | `text-zima-text` |
| Secondary Text | `#666666` | `text-neutral-500` | `text-zima-muted` |
| Icon Background | `#1A1A1A` | `bg-neutral-900` | `bg-zima-icon-bg` |
| Accent Elements | `#999999` | `text-neutral-400` | `text-zima-accent` |

#### Usage Examples

```html
<!-- Page background -->
<div class="min-h-screen bg-neutral-50">

<!-- Card/Button surface -->
<div class="bg-white rounded-2xl shadow-sm">

<!-- Primary text -->
<h1 class="text-neutral-900 font-semibold">

<!-- Secondary text -->
<p class="text-neutral-500 text-sm">

<!-- Icon container -->
<div class="bg-neutral-900 text-white rounded-xl p-3">
```

#### Design Rationale
- **No Colorful Elements**: Intentionally eliminated orange, green, and blue buttons to maintain visual harmony
- **Professional Appearance**: Monochrome palette conveys trust and sophistication essential for financial applications
- **Reduced Cognitive Load**: Minimal color variation helps users focus on content rather than decoration

---

### 2. Overflow Prevention Strategies

Preventing layout overflow is critical for maintaining a polished user experience across all device sizes.

#### Tailwind Implementation

```html
<!-- Fixed Header Height (40% of viewport) -->
<header class="h-[40vh] flex flex-col items-center justify-center">

<!-- Flexible Content Section -->
<main class="flex-1 min-h-0 overflow-auto">

<!-- Text Overflow Handling -->
<p class="truncate">Single line with ellipsis</p>
<p class="line-clamp-2">Multi-line with ellipsis after 2 lines</p>
<p class="line-clamp-3">Multi-line with ellipsis after 3 lines</p>
```

#### Typography Scale

| Element | Size | Tailwind Class |
|---------|------|----------------|
| Title Text | 15px max | `text-[15px]` or `text-sm` |
| Subtitle | 11px | `text-[11px]` or `text-xs` |
| Body Text | 12-14px | `text-xs` to `text-sm` |

#### Strategic Spacing

```html
<!-- Consistent padding values -->
<div class="p-4">   <!-- 16px -->
<div class="p-3">   <!-- 12px -->
<div class="p-2">   <!-- 8px -->

<!-- Gap between elements -->
<div class="space-y-3">  <!-- 12px gap between children -->
<div class="gap-3">      <!-- 12px gap in flex/grid -->
```

---

### 3. Clean Layout Structure

A well-organized layout enhances usability and maintains visual clarity.

#### Logo Presentation

```html
<!-- Simplified Logo Container -->
<div class="w-20 h-20 bg-white rounded-2xl shadow-sm flex items-center justify-center">
  <img src="logo.png" alt="Logo" class="w-16 h-16 object-contain" />
</div>
```

#### Typography Hierarchy

```html
<!-- Primary Heading -->
<h1 class="text-neutral-900 font-bold text-xl">Primary Heading</h1>

<!-- Secondary Heading -->
<h2 class="text-neutral-900 font-semibold text-lg">Secondary Heading</h2>

<!-- Body Text -->
<p class="text-neutral-700 text-sm">Body text content</p>

<!-- Muted/Supporting Text -->
<span class="text-neutral-500 text-xs">Supporting information</span>
```

#### Spacing System

| Use Case | Value | Tailwind Class |
|----------|-------|----------------|
| Button gaps | 12px | `gap-3` or `space-y-3` |
| Container padding | 16px | `p-4` |
| Section margins | 24px | `my-6` or `mx-6` |
| Major sections | 32px | `my-8` |

#### Material Design Elements

```html
<!-- Card with elevation -->
<div class="bg-white rounded-2xl shadow-md">  <!-- elevation 2-4dp -->

<!-- Smaller elements -->
<div class="bg-white rounded-xl shadow-sm">   <!-- elevation 1-2dp -->

<!-- Touch target minimum -->
<button class="min-w-[48px] min-h-[48px]">
```

---

### 4. Professional Button Design

Buttons are primary interaction points and must be both functional and aesthetically consistent.

#### Primary Action Button

```html
<button class="
  w-full
  min-h-[72px] max-h-20
  bg-white
  rounded-2xl
  shadow-md hover:shadow-lg
  transition-shadow duration-200
  px-4 py-3
  flex items-center gap-4
">
  <!-- Icon Container -->
  <div class="w-12 h-12 bg-neutral-900 rounded-xl flex items-center justify-center flex-shrink-0">
    <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <!-- icon path -->
    </svg>
  </div>

  <!-- Text Content -->
  <div class="flex-1 min-w-0 text-left">
    <p class="text-[15px] font-semibold text-neutral-900 truncate">Button Title</p>
    <p class="text-[11px] text-neutral-500 truncate">Subtitle description</p>
  </div>
</button>
```

#### Button Specifications Summary

| Property | Value | Tailwind Class |
|----------|-------|----------------|
| Background | White | `bg-white` |
| Border Radius | 16px | `rounded-2xl` |
| Min Height | 72px | `min-h-[72px]` |
| Max Height | 80px | `max-h-20` |
| Padding | 16px horizontal | `px-4` |
| Shadow | Subtle | `shadow-md` |
| Icon Size | 24px | `w-6 h-6` |
| Icon Container | 48x48px | `w-12 h-12` |
| Title Size | 15-16px, weight 600 | `text-[15px] font-semibold` |
| Subtitle Size | 11-12px, weight 400 | `text-[11px] font-normal` |

#### Button Grid Layout

```html
<!-- 2-column button grid -->
<div class="grid grid-cols-2 gap-3">
  <button class="...">Button 1</button>
  <button class="...">Button 2</button>
</div>

<!-- Full-width buttons stacked -->
<div class="flex flex-col gap-3">
  <button class="w-full ...">Primary Action</button>
  <button class="w-full ...">Secondary Action</button>
</div>
```

---

### 5. Performance & Accessibility

Design decisions must prioritize both performance and accessibility for all users.

#### Simplified Animations

```html
<!-- Fade animation -->
<div class="animate-fade-in">
  <!-- content -->
</div>

<!-- Add to tailwind.config.js -->
```

```javascript
// tailwind.config.js
module.exports = {
  theme: {
    extend: {
      animation: {
        'fade-in': 'fadeIn 600ms ease-out',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
      },
    },
  },
}
```

#### Transition Classes

```html
<!-- Standard hover transitions -->
<button class="transition-shadow duration-200 hover:shadow-lg">
<button class="transition-colors duration-150 hover:bg-neutral-100">
<button class="transition-transform duration-200 hover:scale-[1.02]">
```

#### Accessibility Classes

```html
<!-- Focus states -->
<button class="focus:outline-none focus:ring-2 focus:ring-neutral-400 focus:ring-offset-2">

<!-- Screen reader text -->
<span class="sr-only">Description for screen readers</span>

<!-- High contrast text -->
<p class="text-neutral-900">  <!-- Contrast ratio > 4.5:1 on white -->

<!-- Large touch targets -->
<button class="min-w-[48px] min-h-[48px] p-3">
```

---

## 📱 Layout Templates

### Standard Page Structure

```html
<div class="min-h-screen bg-neutral-50">
  <div class="safe-area-inset-top"></div>

  <div class="px-6 flex flex-col min-h-screen">
    <!-- Header Section (40% of screen) -->
    <header class="h-[40vh] flex flex-col items-center justify-center">
      <!-- Logo -->
      <div class="w-20 h-20 bg-white rounded-2xl shadow-sm flex items-center justify-center mb-4">
        <img src="logo.png" alt="ZIMA AI" class="w-16 h-16" />
      </div>

      <!-- Title -->
      <h1 class="text-xl font-bold text-neutral-900 text-center">Welcome</h1>
      <p class="text-sm text-neutral-500 text-center mt-1">Subtitle text</p>
    </header>

    <!-- Content Section (Flexible) -->
    <main class="flex-1 min-h-0">
      <!-- Main content here -->
    </main>

    <!-- Bottom Padding -->
    <div class="h-6"></div>
  </div>

  <div class="safe-area-inset-bottom"></div>
</div>
```

### Card Component

```html
<div class="bg-white rounded-2xl shadow-md p-4">
  <h3 class="text-[15px] font-semibold text-neutral-900 mb-2">Card Title</h3>
  <p class="text-sm text-neutral-500">Card content goes here.</p>
</div>
```

### Icon Button Component

```html
<button class="
  w-full min-h-[72px] max-h-20
  bg-white rounded-2xl shadow-md
  hover:shadow-lg active:shadow-sm
  transition-shadow duration-200
  px-4 py-3
  flex items-center gap-4
  focus:outline-none focus:ring-2 focus:ring-neutral-300 focus:ring-offset-2
">
  <div class="w-12 h-12 bg-neutral-900 rounded-xl flex items-center justify-center flex-shrink-0">
    <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
    </svg>
  </div>
  <div class="flex-1 min-w-0 text-left">
    <p class="text-[15px] font-semibold text-neutral-900 truncate">Action Title</p>
    <p class="text-[11px] text-neutral-500 truncate">Action description</p>
  </div>
  <svg class="w-5 h-5 text-neutral-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
  </svg>
</button>
```

---

## 🎯 Quick Reference

### Essential Class Combinations

```html
<!-- Page Container -->
class="min-h-screen bg-neutral-50 px-6"

<!-- Card/Surface -->
class="bg-white rounded-2xl shadow-md p-4"

<!-- Primary Button -->
class="w-full min-h-[72px] bg-white rounded-2xl shadow-md px-4 py-3 flex items-center gap-4 transition-shadow hover:shadow-lg"

<!-- Icon Container -->
class="w-12 h-12 bg-neutral-900 rounded-xl flex items-center justify-center"

<!-- Icon -->
class="w-6 h-6 text-white"

<!-- Title Text -->
class="text-[15px] font-semibold text-neutral-900 truncate"

<!-- Subtitle Text -->
class="text-[11px] text-neutral-500 truncate"

<!-- Muted Text -->
class="text-sm text-neutral-500"
```

### Spacing Quick Reference

| Tailwind | Pixels | Use Case |
|----------|--------|----------|
| `gap-2` / `space-y-2` | 8px | Tight spacing |
| `gap-3` / `space-y-3` | 12px | Button gaps |
| `p-4` / `gap-4` | 16px | Container padding |
| `my-6` / `gap-6` | 24px | Section margins |
| `my-8` / `gap-8` | 32px | Major sections |

### Shadow Scale

| Tailwind | Equivalent | Use Case |
|----------|------------|----------|
| `shadow-sm` | 1dp elevation | Subtle depth |
| `shadow-md` | 2-4dp elevation | Cards, buttons |
| `shadow-lg` | 4-8dp elevation | Hover states, modals |

---

## 🔄 Version History

- **v1.0.0** (2024-12-11): Initial Flutter design guidelines
- **v2.0.0** (2025-01-24): Converted to Tailwind CSS
  - Mapped all Flutter styles to Tailwind utilities
  - Created component templates
  - Added custom color configuration

---

## 📝 Notes for Developers

1. Install Tailwind CSS plugins for additional utilities:
   ```bash
   npm install @tailwindcss/line-clamp
   ```

2. Add the custom colors to your `tailwind.config.js` for consistent theming

3. Use the component templates as starting points for new features

4. Test responsive behavior on various screen sizes

5. Maintain accessibility by including focus states and semantic HTML

---

*These guidelines are living documentation and should be updated as the application evolves while maintaining the core minimalist philosophy.*
