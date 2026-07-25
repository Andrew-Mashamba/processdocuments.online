# Multi-Page Refactoring Guide

## Overview
Breaking down the single-page application into multiple independent pages with navigation.

## Created Components

### 1. Navigation Menu
- **File**: `resources/views/components/nav-menu.blade.php` ✅
- **Purpose**: Navigation links for all pages

### 2. Site Header
- **File**: `resources/views/components/site-header.blade.php` ✅
- **Purpose**: Shared header with logo, navigation, and auth buttons
- **Features**: Responsive mobile menu with Alpine.js

## Pages to Create

### Page Structure

```
/                       → Home (Chat Interface)
/tools                  → Document Processing Services
/how-it-works          → Process Explanation
/faq                   → FAQ Page
/about                 → About & Contact
```

### Files Needed

1. **Home Page** (`resources/views/home.blade.php`)
   - Chat interface (main content from welcome.blade.php)
   - Guest notice banner
   - Quick CTA

2. **Tools Page** (`resources/views/tools.blade.php`)
   - Hero section "Document Processing Services"
   - Service cards grid (PDF, Excel, Word, PPT, etc.)
   - Tool categories grid
   - Static tool list for SEO

3. **How It Works Page** (`resources/views/how-it-works.blade.php`)
   - "How to Process Documents Online" section
   - 3-step process
   - Stats section
   - CTA

4. **FAQ Page** (`resources/views/faq.blade.php`)
   - FAQ accordion
   - Contact CTA

5. **About Page** (`resources/views/about.blade.php`)
   - Company info
   - Stats
   - Contact details
   - Team (optional)

## Routes to Add (`routes/web.php`)

```php
Route::get('/', function () {
    return view('home');
})->name('home');

Route::get('/tools', function () {
    return view('tools');
})->name('tools');

Route::get('/how-it-works', function () {
    return view('how-it-works');
})->name('how-it-works');

Route::get('/faq', function () {
    return view('faq');
})->name('faq');

Route::get('/about', function () {
    return view('about');
})->name('about');
```

## Shared Components Still Needed

- [ ] `resources/views/components/site-footer.blade.php`
- [ ] `resources/views/layouts/app-layout.blade.php` (main layout wrapper)

## Design Rules to Maintain

1. **Monochrome Color Scheme**:
   - Primary: `bg-neutral-900`, `text-neutral-900`
   - Secondary: `bg-neutral-100`, `text-neutral-500`
   - Accents: `bg-white`, `shadow-sm/md`

2. **Typography**:
   - Font: 'Instrument Sans'
   - Sizes: `text-[15px]` (headings), `text-[11px]` (captions), `text-sm` (body)

3. **Spacing**:
   - Container: `max-w-6xl` or `max-w-7xl mx-auto`
   - Padding: `px-4 py-4` or `px-6 py-6`
   - Gaps: `gap-3`, `gap-6`, `gap-8`

4. **Components**:
   - Rounded corners: `rounded-xl` (buttons/cards), `rounded-2xl` (containers)
   - Shadows: `shadow-sm`, `shadow-md`
   - Transitions: `transition-all`, `transition-colors`

5. **Buttons**:
   - Primary: `bg-neutral-900 hover:bg-neutral-800 text-white px-4 py-2 rounded-xl`
   - Secondary: `text-neutral-500 hover:text-neutral-900`

## Next Steps

1. ✅ Create navigation component
2. ✅ Create header component
3. [ ] Create footer component
4. [ ] Create main layout wrapper
5. [ ] Create individual page views
6. [ ] Update routes
7. [ ] Test navigation flow
8. [ ] Move SEO metadata to appropriate pages

## Testing Checklist

- [ ] All pages load correctly
- [ ] Navigation highlights active page
- [ ] Mobile menu works on all pages
- [ ] Footer links navigate properly
- [ ] Auth modals work from all pages
- [ ] SEO metadata correct for each page
- [ ] Design consistency maintained
