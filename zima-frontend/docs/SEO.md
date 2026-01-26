# Google Search SEO Documentation

> Comprehensive guide compiled from Google Search Central documentation.
> Source: https://developers.google.com/search/docs

---

SYSTEM NAME : PROCESS DOCUMENTS ONLINE


## Table of Contents

1. [SEO Fundamentals](#1-seo-fundamentals)
   - [Google Search Essentials](#11-google-search-essentials)
   - [How Google Search Works](#12-how-google-search-works)
   - [SEO Starter Guide](#13-seo-starter-guide)
   - [Do You Need an SEO?](#14-do-you-need-an-seo)
2. [Crawling and Indexing](#2-crawling-and-indexing)
   - [Sitemaps](#21-sitemaps)
   - [robots.txt](#22-robotstxt)
   - [Meta Tags and HTTP Headers](#23-meta-tags-and-http-headers)
   - [Canonicalization](#24-canonicalization)
   - [Redirects](#25-redirects)
   - [JavaScript SEO](#26-javascript-seo)
   - [Asking Google to Recrawl](#27-asking-google-to-recrawl)
   - [URL Structure Best Practices](#28-url-structure-best-practices)
   - [Crawlable Links](#29-crawlable-links)
   - [Link Attributes (rel values)](#210-link-attributes-rel-values)
   - [Googlebot and Crawling](#211-googlebot-and-crawling)
   - [Crawl Budget Management](#212-crawl-budget-management)
   - [HTTP Status Codes](#213-http-status-codes)
   - [Mobile-First Indexing](#214-mobile-first-indexing)
   - [Blocking Indexing (noindex)](#215-blocking-indexing-noindex)
   - [Removing Content from Google](#216-removing-content-from-google)
   - [Faceted Navigation](#217-faceted-navigation)
   - [Site Moves with URL Changes](#218-site-moves-with-url-changes)
   - [Valid Page Metadata](#219-valid-page-metadata)
   - [Indexable File Types](#220-indexable-file-types)
   - [Sitemap Detailed Guidelines](#221-sitemap-detailed-guidelines)
   - [robots.txt Detailed Guidelines](#222-robotstxt-detailed-guidelines)
   - [Robots Meta Tag Detailed Reference](#223-robots-meta-tag-detailed-reference)
3. [Search Appearance](#3-search-appearance)
   - [Visual Elements Gallery](#31-visual-elements-gallery)
   - [Title Links](#32-title-links)
   - [Snippets and Meta Descriptions](#33-snippets-and-meta-descriptions)
   - [Featured Snippets](#34-featured-snippets)
   - [Sitelinks](#35-sitelinks)
   - [Site Names](#36-site-names)
   - [Favicons](#37-favicons)
   - [Images](#38-images)
   - [Videos](#39-videos)
   - [Structured Data Overview](#310-structured-data-overview)
   - [Structured Data Types](#311-structured-data-types)
   - [Enriched Search Results](#312-enriched-search-results)
   - [Core Web Vitals](#313-core-web-vitals)
   - [Google Discover](#314-google-discover)
   - [AI Features](#315-ai-features)
   - [Business Details](#316-business-details)
   - [Publication Dates](#317-publication-dates)
   - [Translated Results](#318-translated-results)
   - [Web Stories](#319-web-stories)
   - [Flexible Sampling (Paywalls)](#320-flexible-sampling-paywalls)
   - [Top Places List](#321-top-places-list)
   - [Carousels (Beta)](#322-carousels-beta)
   - [Package Tracking (Early Adopters)](#323-package-tracking-early-adopters)
4. [International SEO](#4-international-seo)
   - [Hreflang Implementation](#41-hreflang-implementation)
5. [Mobile SEO](#5-mobile-seo)
   - [Mobile-First Indexing](#51-mobile-first-indexing)
6. [Monitoring and Debugging](#6-monitoring-and-debugging)
   - [Search Console](#61-search-console)

---

## 1. SEO Fundamentals

### 1.1 Google Search Essentials

The Google Search Essentials form the foundation for making web content eligible to appear and perform well in Google Search. They encompass three core areas:

#### Technical Requirements
What Google needs from web pages to display them in search results. Most sites pass these requirements without deliberate effort.

#### Spam Policies
Behaviors and tactics leading to lower rankings or omission from search results. Sites prioritizing quality content and user experience perform better.

#### Key Best Practices

1. **Create helpful, reliable, people-first content**
2. **Use searchable language** in prominent page locations (titles, headings, alt text, link text)
3. **Ensure links are crawlable** so Google discovers other site pages
4. **Promote your site** through relevant communities
5. **Follow specific best practices** for images, videos, structured data, and JavaScript
6. **Enable appropriate search appearance features** for your content type
7. **Control content visibility** using appropriate methods for exclusion

> **Important:** "It doesn't cost any money to appear in Google Search results, no matter what anyone tries to tell you."

---

### 1.2 How Google Search Works

Google Search operates through three sequential stages:

#### Stage 1: Crawling
Google downloads text, images, and videos from pages using automated programs called crawlers (Googlebot). These discover pages through:
- URL discovery
- Sitemap submissions
- Following links from known pages

#### Stage 2: Indexing
Google analyzes the text, images, and video files on the page and stores the information in the Google index. During this phase:
- Canonical versions of pages are identified
- Similar content is clustered together
- Signals about language, geographic relevance, and usability are collected

#### Stage 3: Serving Search Results
When users enter queries, Google searches its index for matching pages and returns results believed to be highest quality and most relevant. Relevancy depends on hundreds of factors including:
- Location
- Language
- Device type
- Query intent

> **Key Limitation:** Google doesn't guarantee crawling, indexing, or serving of any page, even when sites follow Search Essentials. Google also doesn't accept payment for higher rankings.

---

### 1.3 SEO Starter Guide

#### Help Google Find Your Content

**Check Current Indexing Status:**
Use the site search operator: `site:yourwebsite.com`

**Ensure Proper Rendering:**
Google should see pages the same way users do. Ensure CSS and JavaScript resources aren't blocked.

#### Site Organization

**Descriptive URLs:**
```
Good: https://www.example.com/pets/cats.html
Bad:  https://www.example.com/page?id=123
```

**Topic-Based Directory Structure:**
Group similar pages in directories to help Google understand content organization.

**Duplicate Content Management:**
- Not a spam violation but wastes crawling resources
- Each piece of content should be accessible through one URL
- Implement canonical tags or redirects for duplicates

#### Content Quality

High-quality content characteristics:
- Text is easy-to-read and well organized
- Content is unique (not copied from competitors)
- Content is up-to-date
- Content is helpful, reliable, and people-first

**Anticipate User Search Behavior:**
- Consider different search terms users might use
- Some users use technical terminology
- Others use conversational language

**Minimize Advertising Distractions:**
Avoid excessive ads or intrusive interstitials that obstruct content.

#### Link Best Practices

**Anchor Text Quality:**
Write descriptive anchor text explaining linked content.

**Link Trust:**
- Only link to trustworthy resources
- Use `nofollow` for links you cannot endorse
- User-generated content links should automatically receive `nofollow`

#### What Google Doesn't Use for Ranking

| Element | Status |
|---------|--------|
| Meta keywords | Not used |
| Keyword stuffing | Against spam policies |
| Domain keywords | Minimal impact |
| Specific content length | No magic word count |
| PageRank alone | Just one signal among many |
| E-E-A-T | Not a direct ranking factor |

---

### 1.4 Do You Need an SEO?

#### What SEOs Provide
- Review of site content or structure
- Technical development advice
- Content creation guidance
- Keyword research
- SEO training

#### Getting Started Independently
Small business owners can often handle SEO themselves. Results typically take **4 months to 1 year** from the time you begin making changes.

#### Red Flags to Avoid
- Claims of guaranteed #1 rankings
- Creating "shadow" domains with deceptive redirects
- Placing "doorway" pages loaded with keywords
- Involvement in link schemes or buying links
- Operating secretively or refusing transparency

> **Warning:** "You are responsible for the actions of any companies you hire."

---

## 2. Crawling and Indexing

### 2.1 Sitemaps

A sitemap is a file providing information about pages, videos, and files on a site, plus their relationships.

#### When Sitemaps Help Most
- **Large sites** - Comprehensive internal linking becomes difficult
- **New sites** - Minimal external links pointing to the site
- **Media-rich sites** - Substantial video, images, or news content

#### When Sitemaps May Be Unnecessary
- Small sites (approximately 500 pages or fewer)
- Sites with comprehensive internal linking
- Sites lacking significant media or news content

#### Sitemap Types

| Type | Purpose |
|------|---------|
| XML Sitemap | Main page listing |
| Image Sitemap | Image locations within pages |
| Video Sitemap | Runtime, rating, age-appropriateness |
| News Sitemap | Article titles and publication dates |

---

### 2.2 robots.txt

A robots.txt file tells search engine crawlers which URLs the crawler can access on your site.

#### Key Limitations

| Limitation | Explanation |
|------------|-------------|
| Cannot prevent indexing | Use noindex meta tags instead |
| Not universally enforced | Malicious crawlers may ignore |
| URLs can still appear | If external sites link to blocked URLs |

#### Appropriate Use Cases
- Manage crawl traffic for server efficiency
- Block unimportant or duplicate pages
- Prevent media files from appearing in search results
- Block scripts/stylesheets if pages load without them

> **Critical Warning:** "Don't use a robots.txt file as a means to hide your web pages from Google Search results."

#### Basic Syntax
```
User-agent: *
Disallow: /admin/
Disallow: /private/
Allow: /public/

Sitemap: https://example.com/sitemap.xml
```

---

### 2.3 Meta Tags and HTTP Headers

#### Robots Meta Tags

```html
<!-- Applies to all search engines -->
<meta name="robots" content="index, follow">

<!-- Google-specific -->
<meta name="googlebot" content="index, follow">
```

#### Common Directive Values

| Directive | Effect |
|-----------|--------|
| `index` | Allow indexing (default) |
| `noindex` | Prevent indexing |
| `follow` | Follow links (default) |
| `nofollow` | Don't follow links |
| `nosnippet` | No text snippet |
| `max-snippet:[n]` | Max snippet length |
| `noarchive` | No cached version |
| `noimageindex` | Don't index images |

#### HTTP Header Alternative
```
X-Robots-Tag: noindex, nofollow
```

#### Other Supported Meta Tags

| Tag | Purpose |
|-----|---------|
| `description` | Page summaries for snippets |
| `viewport` | Mobile-friendly design indicator |
| `google-site-verification` | Search Console ownership |
| `notranslate` | Prevent translation offers |
| `rating` | Adult content labeling |

---

### 2.4 Canonicalization

Canonicalization helps Google understand which version of duplicate pages should appear in search results.

#### Methods (Ranked by Strength)

1. **Redirects** - Strongest signal
2. **rel="canonical"** - Strong signal
3. **Sitemap Inclusion** - Weaker signal

#### Implementation

**HTML Link Element:**
```html
<link rel="canonical" href="https://example.com/preferred-url">
```

**HTTP Header:**
```
Link: <https://example.com/preferred-url>; rel="canonical"
```

#### Best Practices
- Use absolute paths, not relative
- Don't use robots.txt for canonicalization
- Prefer HTTPS over HTTP
- Link consistently to canonical URL throughout site
- Don't specify different canonicals using different techniques

---

### 2.5 Redirects

#### Redirect Types

| Type | Status Code | Use Case |
|------|-------------|----------|
| Permanent | 301, 308 | URL permanently moved |
| Temporary | 302, 303, 307 | Short-term relocation |

#### Implementation Methods

**Server-Side (PHP) - Permanent:**
```php
header('HTTP/1.1 301 Moved Permanently');
header('Location: https://www.example.com/newurl');
exit();
```

**Apache (.htaccess):**
```apache
Redirect permanent "/old" "https://example.com/new"

# With mod_rewrite
RewriteEngine on
RewriteRule "^/service$" "/about/service" [R=301]
```

**NGINX:**
```nginx
location = /service {
  return 301 $scheme://example.com/about/service;
}
```

**Meta Refresh (Instant = Permanent):**
```html
<meta http-equiv="refresh" content="0; url=https://example.com/new">
```

**JavaScript (Last Resort):**
```html
<script>
  window.location.href = "https://www.example.com/newlocation";
</script>
```

> **Warning:** "Rendering may fail for various reasons. This means that if you set a JavaScript redirect, Google might never see it."

---

### 2.6 JavaScript SEO

#### How Google Processes JavaScript

1. **Crawling** - Fetches URLs, parses HTML for links
2. **Rendering** - Pages queue for JavaScript execution via headless Chromium
3. **Indexing** - Rendered HTML gets indexed

#### Critical Best Practices

| Practice | Reason |
|----------|--------|
| Use meaningful HTTP status codes | Helps Google understand page state |
| Implement History API | Fragment-based navigation isn't crawlable |
| Avoid soft 404 errors | Use proper 404 pages or noindex |
| Set canonical URLs in HTML | More reliable than JavaScript |
| Use content fingerprinting | Avoids stale cached resources |

#### Content Optimization
- Use unique `<title>` tags and meta descriptions
- Implement structured data with JSON-LD
- Test lazy-loaded images following Google's guidelines
- Support web components with proper slot usage

> **Recommendation:** "Server-side or pre-rendering is still a great idea" for performance and broader bot compatibility.

---

### 2.7 Asking Google to Recrawl

#### Methods

| Method | Use Case | Notes |
|--------|----------|-------|
| URL Inspection Tool | Individual URLs | Quota limited |
| Sitemap Submission | Bulk URLs | Best for new sites |

#### Timeline
Crawling can take **a few days to a few weeks**.

#### Important Notes
- Requesting recrawl doesn't guarantee inclusion
- Multiple requests don't speed up crawling
- Monitor via Index Status report or URL Inspection tool

---

### 2.8 URL Structure Best Practices

#### Requirements for Crawlable URLs

**Valid URL Formats:**
- `https://example.com/products`
- `/products` (relative)
- `/products.php?id=123`

**Invalid URLs to Avoid:**
- `javascript:window.location.href='/products'`
- URL fragments for content changes (`#/products`)

#### URL Construction Guidelines

| Practice | Details |
|----------|---------|
| Use hyphens (`-`) | Separate words with hyphens, not underscores |
| Use descriptive words | Avoid long numeric IDs |
| Keep URLs short | Remove unnecessary parameters |
| Use consistent case | URLs are case-sensitive |
| Use UTF-8 encoding | Percent-encode non-ASCII characters |

#### Parameter Best Practices
- Use `=` for key-value pairs
- Use `&` to separate parameters
- Use `,` for multiple values under one key
- Remove session IDs, referral parameters

---

### 2.9 Crawlable Links

#### Required Format
Google requires `<a>` HTML elements with `href` attributes:

```html
<!-- Crawlable -->
<a href="https://example.com/page">Link Text</a>
<a href="/products/shoes">Shoes</a>

<!-- NOT Crawlable -->
<a routerLink="products">Link</a>
<span href="...">Link</span>
<a onclick="goto('url')">Link</a>
```

#### Anchor Text Best Practices

| Do | Don't |
|----|-------|
| Use descriptive text | "Click here" or "Read more" |
| Be concise and relevant | Keyword stuffing |
| Provide context | Empty link text |
| Space links with surrounding text | Chain multiple links together |

---

### 2.10 Link Attributes (rel values)

| Attribute | Purpose | Use Case |
|-----------|---------|----------|
| `rel="sponsored"` | Mark paid links | Advertisements, paid placements |
| `rel="ugc"` | User-generated content | Comments, forum posts |
| `rel="nofollow"` | Don't associate/follow | When other values don't apply |

**Multiple values:** `rel="ugc nofollow"` or `rel="ugc, nofollow"`

---

### 2.11 Googlebot and Crawling

#### Crawler Types
- **Googlebot Smartphone** - Simulates mobile users (primary for mobile-first indexing)
- **Googlebot Desktop** - Simulates desktop users

#### Crawl Frequency
- Most sites: once every few seconds on average
- First 15MB of HTML processed; CSS/JS fetched separately
- Operates in Pacific Time from US IPs

#### Verification
User-agents can be spoofed. Verify Googlebot via:
1. Reverse DNS lookup on source IP
2. Match against official Googlebot IP ranges

---

### 2.12 Crawl Budget Management

#### What Affects Crawl Budget

| Factor | Description |
|--------|-------------|
| **Crawl Capacity** | Parallel connections, server response time |
| **Crawl Demand** | Perceived inventory, popularity, staleness |

#### Optimization Best Practices

| Practice | Details |
|----------|---------|
| Consolidate duplicates | Reduce crawling wasted on duplicate URLs |
| Use robots.txt | Block unnecessary URLs (more efficient than noindex) |
| Return 404/410 | For permanently removed pages |
| Eliminate soft 404s | Return proper error codes |
| Update sitemaps | Include accurate `<lastmod>` tags |
| Avoid redirect chains | Minimize hops in redirect sequences |
| Optimize page speed | Faster pages allow more content reading |

**Target Audience:** Large sites (1M+ pages) or medium sites (10K+ daily changes)

---

### 2.13 HTTP Status Codes

#### Success Codes (2xx)

| Code | Google's Behavior |
|------|-------------------|
| 200 | Content passed to processing (indexing not guaranteed) |
| 201, 202 | Temporary wait before processing |
| 204 | No content received or processed |

#### Redirect Codes (3xx)

| Code | Signal Strength | Use Case |
|------|-----------------|----------|
| 301 | Strong (permanent) | URL permanently moved |
| 302 | Weak (temporary) | Short-term redirect |
| 307 | Weak (temporary) | Preserves request method |
| 308 | Strong (permanent) | Preserves request method |

Google follows up to 10 redirect hops.

#### Client Errors (4xx)

| Code | Google's Behavior |
|------|-------------------|
| 400, 401, 403, 404, 410 | Content doesn't exist; previously indexed pages removed |
| 429 | Server overload signal; reduces crawl rate |

#### Server Errors (5xx)

| Code | Google's Behavior |
|------|-------------------|
| 500, 502, 503 | Temporary crawl slowdown; indexed URLs may eventually drop |

---

### 2.14 Mobile-First Indexing

Google primarily uses mobile version for indexing. Follow these best practices:

#### Content Parity
- Mobile and desktop must have equivalent primary content
- Use identical titles, meta descriptions, and headings
- Preserve structured data on mobile version

#### Technical Requirements

| Requirement | Details |
|-------------|---------|
| Responsive design | Google's recommendation |
| Identical robots meta | Same tags on both versions |
| Don't lazy-load primary content | Avoid user-interaction requirements |
| High-quality images | Use supported formats, consistent URLs |
| Consistent video URLs | Same structured data on both versions |

#### Common Errors to Avoid
- Missing structured data on mobile
- `noindex` tags blocking mobile content
- Different status codes between versions
- Multiple desktop pages → single mobile page

---

### 2.15 Blocking Indexing (noindex)

#### Implementation Methods

**Meta Tag (All Search Engines):**
```html
<meta name="robots" content="noindex">
```

**Meta Tag (Google Only):**
```html
<meta name="googlebot" content="noindex">
```

**HTTP Header (Non-HTML Files):**
```
X-Robots-Tag: noindex
```

#### Critical Requirements
- Page must be crawlable (not blocked by robots.txt)
- Use URL Inspection tool to verify
- For urgent removal, use Search Console's removal tool

---

### 2.16 Removing Content from Google

#### Removal Methods

| Method | Speed | Duration | Security |
|--------|-------|----------|----------|
| Search Console Removals tool | ~1 day | ~6 months | Temporary |
| Delete content | Varies | Permanent | Most secure |
| Password protection | Varies | Permanent | Blocks all access |
| noindex tag | Varies | Permanent | Google only |

**Do NOT rely on robots.txt** for content removal.

---

### 2.17 Faceted Navigation

#### Preventing Unnecessary Crawling

| Method | Details |
|--------|---------|
| robots.txt Disallow | Block filtered URLs from crawling |
| URL fragments | Use `#filter=value` (Google ignores fragments) |
| rel="canonical" | Point filtered pages to unfiltered versions |
| rel="nofollow" | Add to all links to filtered pages |

#### If Faceted URLs Need Indexing

| Practice | Details |
|----------|---------|
| Use standard separators | `&` between parameters |
| Consistent filter ordering | Logical, predictable sequences |
| Return 404 for empty results | Proper status codes for no-match filters |
| Manage resources | Faceted navigation consumes significant crawl budget |

---

### 2.18 Site Moves with URL Changes

#### Key Steps

1. **Prepare new site** - Set up CMS, transfer content, configure robots.txt
2. **Create URL mapping** - Document old → new URL correspondence
3. **Implement redirects** - Server-side 301/308; avoid chains > 3-5 hops
4. **Submit Change of Address** - Use Search Console (except HTTP→HTTPS)
5. **Monitor traffic** - Track performance via Search Console

#### Timeline
- Medium sites: few weeks for most pages
- Keep redirects active: generally 1+ year

#### Best Practices
- Move one element at a time
- Schedule during low-traffic periods
- Update sitemaps with new URLs
- Ensure adequate server capacity

---

### 2.19 Valid Page Metadata

#### Valid `<head>` Elements
- `title`, `meta`, `link`, `script`, `style`, `base`, `noscript`, `template`

#### Invalid Elements
- `iframe`, `img`

> **Critical:** "If you use an invalid element in the `<head>` element, Google ignores any elements that appear after the invalid element."

---

### 2.20 Indexable File Types

#### Documents
PDF, DOC/DOCX, XLS/XLSX, PPT/PPTX, ODT/ODS/ODP, EPUB, RTF, CSV, XML, TeX

#### Media
- **Images:** BMP, GIF, JPEG, PNG, WebP, SVG, AVIF
- **Video:** MP4, WebM, AVI, MOV, MKV, MPEG, and many more

#### Source Code
Text files including C/C++, Java, Python, Perl, Basic

Use `filetype:` operator to search specific formats.

---

### 2.21 Sitemap Detailed Guidelines

#### Size Limits
- **Maximum:** 50MB (uncompressed) or 50,000 URLs per sitemap
- **Sitemap Index:** Up to 50,000 sitemap references

#### Format Requirements
- UTF-8 encoding mandatory
- Use fully-qualified, absolute URLs
- Google ignores `<priority>` and `<changefreq>` values
- `<lastmod>` should reflect significant content updates

#### Submission Methods
1. Search Console Sitemaps report
2. Search Console API
3. robots.txt: `Sitemap: https://example.com/sitemap.xml`
4. WebSub for Atom/RSS feeds

#### Video Sitemap Required Tags

| Tag | Description |
|-----|-------------|
| `<video:thumbnail_loc>` | Thumbnail image URL |
| `<video:title>` | Must match displayed title |
| `<video:description>` | Max 2048 characters |
| `<video:content_loc>` OR `<video:player_loc>` | Video file or player URL |

---

### 2.22 robots.txt Detailed Guidelines

#### Primary Purpose
Manage crawler traffic, NOT prevent indexing.

#### Syntax

```
User-agent: Googlebot
Disallow: /admin/
Allow: /admin/public/

User-agent: *
Disallow: /private/

Sitemap: https://example.com/sitemap.xml
```

#### What robots.txt Controls

| Content Type | Effect |
|--------------|--------|
| Web pages | Manage traffic; URLs may still appear in results |
| Media files | Prevent appearing in search results |
| Resource files | Block if pages function without them |

#### Better Alternatives for Privacy
- Password protection
- noindex meta tags
- Complete content removal

---

### 2.23 Robots Meta Tag Detailed Reference

#### Indexing Rules

| Rule | Effect |
|------|--------|
| `all` | Default; no restrictions |
| `noindex` | Don't show in search results |
| `nofollow` | Don't follow links on page |
| `none` | Equivalent to `noindex, nofollow` |
| `nosnippet` | No text snippet or video preview |
| `noimageindex` | Don't index images on page |
| `notranslate` | Don't offer translation |

#### Snippet Controls

| Rule | Effect |
|------|--------|
| `max-snippet:[n]` | Limit text snippet to n characters |
| `max-image-preview:[size]` | none, standard, or large |
| `max-video-preview:[n]` | Limit video preview to n seconds |
| `unavailable_after:[date]` | Remove after specified date |

#### data-nosnippet Attribute

```html
<p>Visible <span data-nosnippet>hidden from snippet</span> visible</p>
```

**Note:** Don't add/remove via JavaScript; apply when creating elements.

---

## 3. Search Appearance

### 3.1 Visual Elements Gallery

Google Search results contain various visual elements:

#### Primary Result Types

| Type | Description |
|------|-------------|
| **Text Result** | Based on textual content of the page |
| **Rich Result** | Uses structured data for graphical/interactive display |
| **Image Result** | Based on embedded images |
| **Video Result** | Based on embedded videos |
| **Exploration Features** | Help searchers expand and refine queries |

#### Attribution Elements
- Favicon
- Site name
- Visible URL
- Domain
- Breadcrumb

#### Text Result Child Elements
- Title link
- Snippet
- Byline date
- Sitelinks (group and individual)
- Text result image
- Rich attributes (review stars, etc.)

---

### 3.2 Title Links

The title link is the clickable heading in Google Search results.

#### Best Practices

| Do | Don't |
|----|-------|
| Every page has a `<title>` element | Vague titles like "Home" |
| Descriptive, concise text | Unnecessarily lengthy titles |
| Unique titles for each page | Duplicate titles across pages |
| Brand name at start or end | Keyword stuffing |
| Match page's primary language | Different language than content |

#### Sources Google Uses for Titles
- `<title>` element content
- Main visual page titles
- Heading elements (`<h1>`, etc.)
- `og:title` meta tags
- Large, styled text
- Anchor text from links
- WebSite structured data

#### Common Issues Google Corrects
- Half-empty titles lacking main content
- Outdated information (old dates)
- Inaccurate descriptions
- Micro-boilerplate repetition
- Multiple prominent headings without clear hierarchy
- Language mismatches

---

### 3.3 Snippets and Meta Descriptions

A snippet is the description part of a search result.

#### How Snippets Are Generated
- Primarily from page content
- Meta description used if it better describes the page
- Different queries may show different snippets

#### Controlling Snippets

```html
<!-- No snippet -->
<meta name="robots" content="nosnippet">

<!-- Max length -->
<meta name="robots" content="max-snippet:150">

<!-- Block specific sections -->
<span data-nosnippet>This text won't appear in snippets</span>
```

#### Meta Description Best Practices

| Do | Don't |
|----|-------|
| Unique description per page | Same description on every page |
| Include relevant information | Generic keyword strings |
| Generate programmatically for large sites | Keyword-only descriptions |
| Quality over quantity | Repetitive, unhelpful text |

**Good Example:**
> "Get everything you need to sew your next garment. Open Monday-Friday 8-5pm, located in the Fashion District."

---

### 3.4 Featured Snippets

Featured snippets are special boxes that display the descriptive snippet before the title link.

#### Key Points
- **Cannot be directly marked** - Google systems automatically select suitable pages
- Can appear independently or within "People Also Ask" sections
- Clicking takes users directly to the relevant page section

#### Opting Out

| Method | Effect |
|--------|--------|
| `nosnippet` | Blocks both featured and regular snippets |
| `max-snippet:[n]` | Limits snippet length (lower values reduce featured snippet likelihood) |
| `data-nosnippet` | Excludes specific text sections |

---

### 3.5 Sitelinks

Sitelinks are links from the same domain clustered under a text result, helping users navigate to relevant sections.

#### How Google Generates Them
- Automatically analyzes site link structure
- Only shown when useful to users
- Site architecture must support identifying shortcuts

#### Optimization Best Practices

| Element | Recommendation |
|---------|----------------|
| Page titles/headings | Informative, relevant, compact |
| Site structure | Logical, easy to navigate |
| Internal linking | Links to important pages from relevant sections |
| Anchor text | Concise, relevant to target page |
| Content | Avoid repetitions |

#### Removal
- Remove the page entirely, or
- Apply `noindex` tag

---

### 3.6 Site Names

Site names identify the source of pages in search results, applying to the entire website.

#### Implementation (WebSite Structured Data)

```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "WebSite",
  "name": "Your Site Name",
  "alternateName": ["Alternate Name", "Acronym"],
  "url": "https://example.com/"
}
</script>
```

#### Requirements
- Add to home page only (domain/subdomain root)
- Use canonical URLs
- Cannot apply to subdirectories
- Home page must be crawlable

#### Sources Google Uses
- Structured data markup
- Content from home page
- Open Graph tags
- Title elements and heading text
- Web references to your site

---

### 3.7 Favicons

#### Implementation

```html
<link rel="icon" href="/path/to/favicon.ico">
```

#### Supported Attributes
- `icon`
- `shortcut icon`
- `apple-touch-icon`
- `apple-touch-icon-precomposed`

#### Technical Specifications

| Requirement | Value |
|-------------|-------|
| Minimum size | 8x8 pixels |
| Recommended size | 48x48 pixels or larger |
| Aspect ratio | 1:1 (square) |
| Crawlability | Must be accessible to Googlebot |

#### Key Points
- One favicon per hostname
- URL must be stable (don't change frequently)
- Inappropriate imagery replaced with default icons

---

### 3.8 Images

#### Best Practices

| Practice | Implementation |
|----------|---------------|
| Descriptive filenames | `my-new-black-kitten.jpg` not `IMG00023.jpg` |
| Alt text | Informative, not keyword-stuffed |
| Near relevant text | Context helps Google understand images |
| Supported formats | BMP, GIF, JPEG, PNG, WebP, SVG, AVIF |
| Responsive images | Use `srcset` and `<picture>` elements |

#### Image Sitemap

```xml
<url>
  <loc>https://example.com/page</loc>
  <image:image>
    <image:loc>https://example.com/image.jpg</image:loc>
    <image:title>Image Title</image:title>
  </image:image>
</url>
```

---

### 3.9 Videos

#### Core Requirements
- Use standard HTML elements: `<video>`, `<embed>`, `<iframe>`, `<object>`
- Watch page must be indexed and perform well
- Dedicated watch page with single video focus

#### Supported Formats
3GP, 3G2, ASF, AVI, DivX, M2V, M3U, M3U8, M4V, MKV, MOV, MP4, MPEG, OGV, QVT, RAM, RM, VOB, WebM, WMV, XAP

#### Thumbnail Requirements

| Specification | Requirement |
|---------------|-------------|
| Minimum size | 60x30 pixels (larger preferred) |
| Formats | BMP, GIF, JPEG, PNG, WebP, SVG, AVIF |
| Alpha | 80%+ pixels must have alpha > 250 |

#### Video Structured Data (VideoObject)

```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "VideoObject",
  "name": "Video Title",
  "description": "Video description",
  "thumbnailUrl": "https://example.com/thumb.jpg",
  "uploadDate": "2024-01-15",
  "duration": "PT5M30S",
  "contentUrl": "https://example.com/video.mp4"
}
</script>
```

#### Advanced Features

| Feature | Implementation |
|---------|----------------|
| Video Previews | Allow Google to fetch video files |
| Key Moments | Clip or SeekToAction structured data |
| Live Badge | BroadcastEvent structured data |

---

### 3.10 Structured Data Overview

Structured data is standardized markup that explicitly labels page content to help search engines understand meaning.

#### Benefits

| Company | Result |
|---------|--------|
| Rotten Tomatoes | 25% higher click-through rates |
| Food Network | 35% increase in visits |
| Nestlé | 82% higher CTR for rich results |

#### Supported Formats

| Format | Description | Recommendation |
|--------|-------------|----------------|
| **JSON-LD** | JavaScript in `<script>` tags | Recommended - easiest to maintain |
| **Microdata** | HTML5 tag attributes | Supported |
| **RDFa** | HTML5 extension attributes | Supported |

#### How Google Uses Structured Data

- Understand page meaning and context
- Display enhanced search results (rich results)
- Enable specialized search features
- Gather information about entities

#### Implementation Best Practices

| Practice | Details |
|----------|---------|
| Validate | Use Rich Results Test during development |
| Be Accurate | Provide complete, accurate data |
| Keep Visible | Only markup content visible to users |
| Follow Guidelines | Use Google Search Central docs over schema.org |
| Measure | Compare Search Console metrics before/after |

#### Generate Structured Data with JavaScript

For JavaScript-heavy sites, structured data can be injected dynamically:

```javascript
// Create and inject JSON-LD
const script = document.createElement('script');
script.type = 'application/ld+json';
script.text = JSON.stringify({
  "@context": "https://schema.org",
  "@type": "Product",
  "name": "Product Name",
  "description": "Product description"
});
document.head.appendChild(script);
```

**Key Points:**
- Google renders JavaScript before indexing
- JSON-LD can be dynamically generated
- Test with Rich Results Test (include rendered HTML)
- Ensure data is available after JavaScript execution

---

### 3.11 Structured Data Types

#### All Supported Types

| Type | Description |
|------|-------------|
| **Article** | News, sports, or blog articles |
| **Breadcrumb** | Navigation showing page position |
| **Carousel** | Sequential lists from single site |
| **Course List** | Educational courses |
| **Dataset** | Data sets for Dataset Search |
| **Discussion Forum** | Threaded user discussions |
| **Education Q&A** | Educational flashcards |
| **Employer Aggregate Rating** | Hiring organization ratings |
| **Event** | Organized events (concerts, festivals) |
| **FAQ** | Frequently asked questions |
| **HowTo** | Step-by-step instructions |
| **Image Metadata** | Creator and rights info |
| **Job Posting** | Job listings with details |
| **Local Business** | Business info in knowledge panel |
| **Loyalty Program** | Customer loyalty/rewards programs |
| **Math Solver** | Math problem solutions |
| **Merchant Listing** | Product display in shopping experiences |
| **Movie** | Movie carousels |
| **Organization** | Company details and logo |
| **Practice Problem** | Educational practice exercises |
| **Product** | Price, availability, reviews |
| **Product Snippet** | Basic product information |
| **Product Variants** | Product variations (size, color) |
| **Profile Page** | Individual/org information |
| **Q&A** | Questions with answers |
| **Recipe** | Recipe details and carousels |
| **Return Policy** | Merchant return policies |
| **Review Snippet** | Review excerpts and ratings |
| **Shipping Policy** | Merchant shipping information |
| **Software App** | App ratings and descriptions |
| **Speakable** | Text-to-speech content |
| **Subscription/Paywalled** | Restricted access content |
| **Vacation Rental** | Property listings |
| **Video** | Video with playback options |
| **WebSite** | Site search and navigation |

#### Implementation Format (JSON-LD Recommended)

```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "Organization",
  "name": "Company Name",
  "url": "https://example.com",
  "logo": "https://example.com/logo.png"
}
</script>
```

#### Case Study Results

| Company | Result |
|---------|--------|
| Rotten Tomatoes | 25% higher CTR |
| Food Network | 35% increase in visits |
| Nestlé | 82% higher CTR for rich results |

#### Detailed Structured Data Reference

##### Article

**Types:** `Article`, `NewsArticle`, `BlogPosting`

**Recommended Properties:**
| Property | Details |
|----------|---------|
| `headline` | Article title (keep concise) |
| `image` | Multiple images, min 50K pixels, 16x9/4x3/1x1 ratios |
| `datePublished` | ISO 8601 with timezone |
| `dateModified` | ISO 8601 with timezone |
| `author` | Person/Organization with name and URL |

**Author Best Practices:**
- Use separate author objects for multiple authors
- Include `url` or `sameAs` for author profiles
- Don't include job titles or "posted by" in name field

---

##### Breadcrumb

**Required Properties:**
| Property | Details |
|----------|---------|
| `itemListElement` | Array of ListItem (min 2) |
| `position` | Integer (starts at 1) |
| `name` | Displayed text |
| `item` | URL (optional for final item) |

---

##### Carousel (ItemList)

For displaying multiple cards from the same site (Course, Movie, Recipe, Restaurant).

**Required Properties:**
| Property | Details |
|----------|---------|
| `itemListElement` | Array of ListItem (min 2, same type) |
| `position` | Integer (1-based) |
| `url` | Canonical URL of detail page |

**Guidelines:**
- All items must be same type
- Minimum 2 items required
- Items display in position order

---

##### Course List

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Course title |
| `description` | Overview (max 60 chars displayed) |
| `provider` | Organization publishing course |

**Content Requirements:**
- Series/unit of curriculum with lectures, lessons, or modules
- Explicit educational outcome
- Min 3 courses for carousel

**Prohibited:** Promotional language, pricing in titles

---

##### Dataset

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Descriptive identifier (unique per dataset) |
| `description` | 50-5000 characters |

**Recommended Properties:**
- `creator` (Person/Organization with ORCID/ROR)
- `identifier` (DOI or Compact Identifier)
- `license` (specific version URL)
- `distribution` (DataDownload with contentUrl)
- `spatialCoverage`, `temporalCoverage`

---

##### Discussion Forum

**Required Properties:**
| Property | Details |
|----------|---------|
| `author` | Person/Organization with name |
| `datePublished` | ISO 8601 |
| `text`, `image`, or `video` | At least one content type |

**For Comments:** Same as above, can be nested for threading

**Guidelines:**
- Use for user-generated forum posts only
- For Q&A sites, use QAPage instead
- Prefer Microdata/RDFa over JSON-LD to avoid text duplication

---

##### Education Q&A

**Required Properties:**
| Property | Details |
|----------|---------|
| `hasPart` | Question objects |
| `eduQuestionType` | Must be "Flashcard" |
| `text` | Full question text |
| `acceptedAnswer` | Single Answer per Question |

**Availability:** English, Portuguese, Spanish (Mexico), Vietnamese

---

##### Employer Aggregate Rating

**Required Properties:**
| Property | Details |
|----------|---------|
| `itemReviewed` | Organization being rated |
| `ratingValue` | Numerical rating |
| `ratingCount` OR `reviewCount` | At least one |

**Guidelines:**
- Users must be able to post ratings on your site
- Custom scales supported with bestRating/worstRating

---

##### Event

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Full event title (no venue/dates) |
| `startDate` | ISO 8601 with timezone |
| `location` | Place with name and address |

**Recommended Properties:**
- `endDate`, `description`, `image`
- `eventStatus`: EventScheduled, EventCancelled, EventPostponed, EventRescheduled
- `offers`: price, availability, validFrom
- `organizer`, `performer`

**Guidelines:**
- Single event per page
- Include UTC offset in dates
- For all-day events, use date only

---

##### FAQ

**Restriction:** Only for well-known, authoritative government or health websites.

**Required Properties:**
| Property | Details |
|----------|---------|
| `mainEntity` | Array of Question objects |
| `name` | Full question text |
| `acceptedAnswer` | Single Answer object |
| `text` | Complete answer (supports HTML) |

**Invalid Uses:** Forum pages, product support with user answers, hidden content

---

##### Image Metadata

For displaying creator and licensing info in Google Images.

**Required:**
- `contentUrl` (direct link to image)
- At least one of: `creator`, `creditText`, `copyrightNotice`, `license`

**For Licensable Badge:**
- Include `license` (URL to terms)
- Add `acquireLicensePage` (licensing checkout)

**Alternative:** Embed IPTC metadata directly in image files

---

##### Job Posting

**Required Properties:**
| Property | Details |
|----------|---------|
| `datePosted` | ISO 8601 date |
| `description` | Full HTML job description |
| `hiringOrganization` | Company name |
| `jobLocation` | Place with PostalAddress |
| `title` | Job title only |

**Recommended Properties:**
- `baseSalary` with currency and unitText (HOUR, MONTH, YEAR)
- `employmentType`: FULL_TIME, PART_TIME, CONTRACTOR, etc.
- `validThrough` for expiration
- `jobLocationType`: "TELECOMMUTE" for remote

---

##### Local Business

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Business name |
| `address` | PostalAddress with full details |

**Recommended Properties:**
- `geo`: latitude/longitude (5+ decimal places)
- `openingHoursSpecification`
- `telephone` with country code
- `priceRange`: "$$$" or numerical
- `menu` (for restaurants)

**Guidelines:** Use most specific subtype (Restaurant, DaySpa, etc.)

---

##### Movie

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Movie name |
| `image` | High resolution, 6:9 aspect ratio |

**Recommended:**
- `aggregateRating` (follow Review snippet guidelines)
- `dateCreated` (release date)
- `director` (Person)
- `review` (nested Review)

**Display:** Mobile devices only, uses ItemList for carousels

---

##### Organization

**No required properties.** Add what applies to your organization.

**Key Properties:**
| Property | Details |
|----------|---------|
| `name` | Organization name |
| `url` | Website URL |
| `logo` | Min 112x112px, crawlable |
| `address` | PostalAddress |
| `contactPoint` | Multiple contact methods |
| `sameAs` | Social media profiles |

**Business Identifiers:**
- `iso6523Code`: Format "ICD:identifier"
- `vatID`, `taxID`, `naics`, `duns`

---

##### Product / Merchant Listing

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Product name |
| `image` | Min 50K pixels, multiple ratios |
| `offers.price` | Number > 0 |
| `offers.priceCurrency` | ISO 4217 (USD, EUR) |

**Recommended Properties:**
- `aggregateRating`, `review`
- `brand.name`, `description`
- `gtin`, `sku`, `mpn`
- `availability`: InStock, OutOfStock, BackOrder
- `shippingDetails`, `hasMerchantReturnPolicy`

---

##### Product Variants

Use `ProductGroup` to group variations (size, color, material).

**Required:**
- `name` on ProductGroup
- `productGroupID` or `inProductGroupWithID`
- Unique identifier (SKU/GTIN) per variant

**Recommended:**
- `variesBy`: schema.org/color, schema.org/size, etc.
- `hasVariant` or `isVariantOf` for linking

---

##### Profile Page

**Required Properties:**
| Property | Details |
|----------|---------|
| `mainEntity` | Person or Organization |
| `name` | Primary identifier (real name preferred) |

**Recommended Properties:**
- `dateCreated`, `dateModified` (ISO 8601)
- `alternateName` (social handle)
- `description` (user byline)
- `image` (profile photo, no placeholders)
- `interactionStatistic` (followers, likes)
- `sameAs` (external profile links)

**Valid Uses:** Forum profiles, author pages, About Me sections
**Invalid Uses:** Store homepages, organization review sites

---

##### Q&A Page

**Required Properties:**
| Property | Details |
|----------|---------|
| `mainEntity` | Single Question object |
| `name` | Short form question text |
| `answerCount` | Total answers (can be 0) |
| `acceptedAnswer` OR `suggestedAnswer` | At least one required |
| `text` | Full answer text |

**Recommended:**
- `author` (Person/Organization with url)
- `url` for each answer (strongly recommended)
- `upvoteCount`, `datePublished`

**Valid Uses:** Forums with user-submitted answers, support pages
**Invalid Uses:** FAQ pages (use FAQPage), multiple questions per page

---

##### Recipe

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Dish name |
| `image` | Multiple images, min 50K pixels |

**Recommended Properties:**
- `author`, `description`
- `prepTime`, `cookTime`, `totalTime` (ISO 8601 duration)
- `recipeYield` (servings)
- `recipeIngredient` (array)
- `recipeInstructions` (HowToStep recommended)
- `nutrition.calories`

---

##### Review Snippet

**Required for Review:**
| Property | Details |
|----------|---------|
| `author` | Reviewer name (< 100 chars) |
| `itemReviewed` | Product, Book, Movie, etc. |
| `reviewRating.ratingValue` | Number, fraction, or percentage |

**Required for AggregateRating:**
- `ratingValue`
- `ratingCount` OR `reviewCount`

**Self-Serving Restriction:** Sites cannot display review stars for their own business.

---

##### Software App

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | App title |
| `offers.price` | 0 for free apps |
| `aggregateRating` OR `review` | At least one required |

**Recommended:**
- `applicationCategory`: GameApplication, SocialNetworkingApplication, etc.
- `operatingSystem`: "Android 1.6", "Windows 7", etc.

---

##### Video

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Unique video title |
| `thumbnailUrl` | Thumbnail URL |
| `uploadDate` | ISO 8601 with timezone |

**Recommended Properties:**
- `contentUrl` (direct video file URL)
- `description`, `duration` (ISO 8601)
- `embedUrl` (player URL)

**For LIVE Badge (BroadcastEvent):**
- `isLiveBroadcast`: true
- `startDate`, `endDate`

**For Key Moments (Clip):**
- `name`, `startOffset`, `url`, `endOffset`

---

##### Return Policy

**Required (Option A):**
- `applicableCountry` (ISO 3166-1 alpha-2)
- `returnPolicyCategory`: MerchantReturnFiniteReturnWindow, MerchantReturnNotPermitted, MerchantReturnUnlimitedWindow

**Required (Option B):**
- `merchantReturnLink` (URL to policy page)

**Recommended:**
- `merchantReturnDays`
- `returnFees`: FreeReturn, ReturnFeesCustomerResponsibility
- `returnMethod`: ReturnByMail, ReturnInStore

---

##### Shipping Policy

**Required:**
- `shippingConditions` with costs and delivery times

**Key Properties:**
- `shippingRate` (MonetaryAmount)
- `shippingDestination` (DefinedRegion with addressCountry)
- `deliveryTime` with `handlingTime` and `transitTime`

---

##### Loyalty Program

**Required Properties:**
| Property | Details |
|----------|---------|
| `name` | Program name |
| `description` | Primary benefits |
| `hasTiers` | At least one MemberProgramTier |

**MemberProgramTier:**
- `name` (tier name)
- `hasTierBenefit`: TierBenefitLoyaltyPoints or TierBenefitLoyaltyPrice

**Availability:** Australia, Brazil, Canada, France, Germany, Mexico, UK, US

---

##### Vacation Rental

**Required Properties:**
| Property | Details |
|----------|---------|
| `containsPlace` | Accommodation type |
| `containsPlace.occupancy.value` | Max guests |
| `identifier` | Unique property ID |
| `image` | Minimum 8 photos |
| `latitude`, `longitude` | 5+ decimal places |
| `name` | Listing name |

**Amenities (Boolean):** wifi, pool, petsAllowed, kitchen, ac, parking, etc.

---

##### Speakable

For text-to-speech on Google Assistant (US English only).

**Required:**
- `cssSelector` OR `xPath` to target content

**Guidelines:**
- 20-30 seconds of content per section (2-3 sentences)
- Focus on key story points, not entire articles

---

##### Paywalled Content

**Required:**
- `isAccessibleForFree`: false

**Recommended:**
- `hasPart.cssSelector` (class name for paywalled section)
- `hasPart.isAccessibleForFree`: false

Works with Article, NewsArticle, Blog, Course, and other CreativeWork types.

---

##### Deprecated/Retired Features

| Feature | Status |
|---------|--------|
| **HowTo** | Removed September 2023 |
| **Sitelinks Searchbox** | Removed November 2024 |
| **Practice Problem** | Removed January 2026 |
| **Course Info** | Removed September 2025 |
| **Learning Video** | Removed September 2025 |

---

### 3.12 Enriched Search Results

Enriched search results are an enhanced category of rich results featuring immersive experiences and advanced interactive features.

#### Definition

Enriched search uses structured data markup and enables users to search across structured data properties—for example, filtering recipes by calorie count or preparation time.

#### Supported Types

| Type | Features |
|------|----------|
| **Job Posting** | Salary filtering, location, job type |
| **Recipe** | Calories, prep time, ingredients |
| **Event** | Date, location, ticket availability |

#### Quality Guidelines

| Requirement | Details |
|-------------|---------|
| Required Properties | Each type specifies mandatory fields |
| Completeness | More recommended properties = higher quality |
| Relevance | Data must match the enriched search category |
| Leaf Content Only | Applies to detail pages, not category pages |
| Content Policies | Type-specific restrictions apply |

#### Testing

Use the [Rich Results Test](https://search.google.com/test/rich-results) for validation.

---

### 3.13 Core Web Vitals

Core Web Vitals measure real-world user experience across loading, interactivity, and visual stability.

#### The Three Metrics

| Metric | Measures | Good Threshold |
|--------|----------|----------------|
| **LCP** (Largest Contentful Paint) | Loading performance | < 2.5 seconds |
| **INP** (Interaction to Next Paint) | Responsiveness | < 200 milliseconds |
| **CLS** (Cumulative Layout Shift) | Visual stability | < 0.1 |

#### LCP Optimization
- Preload critical resources
- Optimize images
- Reduce server response time
- Use efficient caching

#### INP Optimization
- Reduce JavaScript execution time
- Break up long tasks
- Optimize event handlers
- Use passive event listeners

#### CLS Optimization
- Set dimensions for images/videos
- Reserve space for dynamic content
- Avoid inserting content above existing content
- Use `transform` for animations

### 3.14 Google Discover

Google Discover is an interest-based feed showing content related to users' interests based on their Web and App Activity.

#### Eligibility Requirements

Content qualifies for Discover when:
1. The page is indexed by Google
2. It complies with Discover's content policies

No special tags or structured data are mandatory.

#### Optimization Best Practices

| Do | Don't |
|----|-------|
| Craft titles reflecting content essence | Use sensationalism or clickbait |
| Feature high-quality images (min 1200px wide) | Mislead with preview details |
| Provide timely, story-driven content | Exploit morbid curiosity or outrage |
| Create helpful, people-first content | Withhold crucial context |

#### Content Types Featured

Discover showcases articles and videos across:
- Sports
- Health
- Entertainment
- Lifestyle

**Filtered Content:**
- Job applications
- Petitions and forms
- Code repositories
- Unmarked satirical content

#### Traffic Characteristics

Discover traffic is less predictable than keyword-driven search visits. Treat it as supplemental to regular search traffic.

#### Monitoring

Use Search Console's Discover Performance report to track impressions, clicks, and CTR across the past 16 months.

---

### 3.15 AI Features

#### AI Overviews

AI Overviews help users quickly grasp complicated topics and provide links for further exploration. They appear when Google determines they add value beyond traditional results.

#### AI Mode

Assists with queries requiring exploration, reasoning, or complex comparisons, allowing nuanced questions in a single search.

#### How Content Appears

Both features use "query fan-out" technique—issuing multiple related searches across subtopics. This displays a wider and more diverse set of helpful links, creating opportunities for new websites.

#### Optimization

According to Google: "There are no additional requirements to appear in AI Overviews or AI Mode, nor other special optimizations necessary."

**Standard SEO fundamentals apply:**
- Ensure crawling is allowed in robots.txt
- Provide quality internal linking
- Deliver strong page experience
- Present important content as readable text
- Support content with high-quality images/videos
- Match structured data to visible text
- Maintain current Business Profile and Merchant Center information

#### Controls

Manage content inclusion using existing controls:

| Control | Effect |
|---------|--------|
| `nosnippet` | Exclude from AI features |
| `data-nosnippet` | Exclude specific sections |
| `max-snippet` | Limit snippet length |
| `noindex` | Prevent indexing entirely |
| `Google-Extended` | Block broader AI training |

---

### 3.16 Business Details

#### Methods to Establish Business Presence

| Method | Purpose |
|--------|---------|
| Google Business Profile | Manage listings on Maps and Search |
| Search Console | Verify website ownership |
| Knowledge Panel | Update automatically gathered information |

#### Google Business Profile

Claim your local business to provide:
- Address and contact information
- Business type and categories
- Photos and business hours

Once verified, information appears in Google Maps and knowledge panels.

#### Structured Data for Business Details

**Organization Schema:**
```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "Organization",
  "name": "Company Name",
  "url": "https://example.com",
  "logo": "https://example.com/logo.png",
  "contactPoint": {
    "@type": "ContactPoint",
    "telephone": "+1-800-555-1234",
    "contactType": "customer service"
  },
  "sameAs": [
    "https://facebook.com/company",
    "https://twitter.com/company"
  ]
}
</script>
```

**Breadcrumbs:**
Add Breadcrumb structured data to help Google understand site hierarchy.

#### Best Practices

- Validate markup with Rich Results Test
- Allow approximately one week for updates
- Highlight customer support methods
- Report incorrect information via knowledge panel feedback

---

### 3.17 Publication Dates

#### What Byline Dates Are

A byline date represents when Google estimates a page was published or updated. When determinable, it displays alongside the title and snippet.

#### Implementation Requirements

**1. User-Visible Date**

Display a prominent, clearly labeled date:
- "Posted Feb 4, 2019"
- "Published February 4, 2019"
- "Last updated: Feb 14, 2018"

**2. Structured Data**

Use `CreativeWork` subtypes with date properties:

```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "Article",
  "headline": "Article Title",
  "datePublished": "2024-01-15T08:00:00+00:00",
  "dateModified": "2024-01-16T10:30:00+00:00"
}
</script>
```

#### Date Format Requirements

| Requirement | Details |
|-------------|---------|
| Date | Mandatory |
| Time | Optional |
| Timezone | Recommended for precision |
| Format | ISO 8601 in structured data |

#### Best Practices

| Do | Don't |
|----|-------|
| Ensure visible and structured dates match | Use future dates |
| Include timezone information | Use event dates unrelated to publication |
| Account for daylight saving time | Display multiple confusing dates |
| Use ISO 8601 format in markup | Mismatch visible vs. structured dates |

> **Note:** Google doesn't guarantee byline dates will display, but following guidelines improves detection likelihood.

---

### 3.18 Translated Results

Google may translate search result title links and snippets into the user's local language when results aren't available in that language.

#### How They Work

- Users click translated title link → see machine-translated page
- Original page remains accessible as an option
- JavaScript and embedded images typically function normally
- No different from using Google Translate

#### Supported Languages

Arabic, Bengali, English, French, German, Gujarati, Hindi, Indonesian, Kannada, Korean, Malayalam, Marathi, Persian, Portuguese, Spanish, Tamil, Telugu, Thai, Turkish, Urdu, Vietnamese

#### Monitoring

Track clicks and impressions using the "Search Appearance filter" in Search Console's Performance report.

#### Opt-Out

```html
<!-- Meta tag -->
<meta name="googlebot" content="notranslate">
```

```
# HTTP header
X-Robots-Tag: notranslate
```

---

### 3.19 Web Stories

Web Stories blend video, audio, images, animation and text for a dynamic, tappable consumption experience.

#### Where They Appear

- Single results in Google Search (all regions/languages)
- Individual cards in Discover feed (US, India, Brazil primarily)

#### How to Enable

1. **Create the Story** - Use story editor tools or build with AMP
2. **Validate AMP** - Use Web Stories Google Test Tool or AMP Linter
3. **Verify Metadata** - Supply required fields
4. **Check Indexing** - Use URL Inspection Tool
5. **Follow Policies** - Adhere to content guidelines

#### Required Metadata

| Field | Description |
|-------|-------------|
| `publisher-logo-src` | Publisher logo image |
| `poster-portrait-src` | Story poster image |
| `title` | Story title |
| `publisher` | Publisher name |

#### Technical Requirements

- Must be canonical with proper `rel="canonical"` self-reference
- Cannot be blocked via robots.txt or noindex
- Use hreflang for multilingual versions

---

### 3.20 Flexible Sampling (Paywalls)

Flexible sampling refers to strategies for limiting access to paywalled content while maintaining search visibility.

#### Two Primary Types

| Type | Description |
|------|-------------|
| **Metering** | Users receive quota of free articles monthly before paywall |
| **Lead-in** | Display article opening sentences above paywall |

#### Optimal Amounts

For daily news publishers: **6-10 articles per user per month**

Start with 10 articles and iterate from there.

#### Key Caution

User satisfaction degrades when paywalls appear beyond 10% of visits.

#### Implementation

Enclose paywalled content with structured data to distinguish legitimate paywalls from cloaking:

```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "NewsArticle",
  "isAccessibleForFree": false,
  "hasPart": {
    "@type": "WebPageElement",
    "isAccessibleForFree": false,
    "cssSelector": ".paywall"
  }
}
</script>
```

---

### 3.21 Top Places List

Top Places List displays curated "top places" lists that mention businesses with physical locations in search results.

#### Eligibility Requirements

| Requirement | Details |
|-------------|---------|
| Curation | List must be curated, genuine, independent, not sponsored |
| Authenticity | Cannot be templated sentences from data/automated metrics |
| Content | No vulgar or offensive language |

#### How It Works

- Businesses don't need to take action
- Google automatically indexes qualifying lists
- Applies only to businesses with physical locations

#### Opting Out

Use Google's official opt-out mechanism for local search features.

---

### 3.22 Carousels (Beta)

Structured data carousels are horizontal scrollable lists in Google Search results.

#### Supported Content Types

- **LocalBusiness** subtypes (Restaurant, Hotel, VacationRental)
- **Product**
- **Event**

#### Geographic Availability

Currently limited to European Economic Area, Turkey, and South Africa.

#### Implementation

Combines `ItemList` structured data with supported content types:

```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "ItemList",
  "itemListElement": [
    {
      "@type": "ListItem",
      "position": 1,
      "item": {
        "@type": "Restaurant",
        "name": "Restaurant Name",
        "image": "https://example.com/image.jpg",
        "url": "https://example.com/restaurant",
        "aggregateRating": {
          "@type": "AggregateRating",
          "ratingValue": "4.5",
          "reviewCount": "100"
        }
      }
    }
  ]
}
</script>
```

#### Requirements

| Property | Required |
|----------|----------|
| `itemListElement` | Yes |
| `image` | Yes (50K+ pixels recommended) |
| `name` | Yes |
| `url` | Yes (same domain) |
| `position` | Yes (1-based) |
| `aggregateRating` | Recommended |

---

### 3.23 Package Tracking (Early Adopters)

Package tracking allows delivery companies to display real-time shipment information directly in search results.

#### Geographic Availability

Available in all languages/countries where Google Search is available. Early adopters program currently limited to India, Japan, and Brazil.

#### API Requirements

| Requirement | Specification |
|-------------|---------------|
| Format | RESTful JSON API |
| Method | POST requests |
| Response Time | < 700ms average, < 1000ms 95th percentile |
| Uptime | Near-zero downtime expected |

#### Required Data Field

- `CurrentStatus`: Current package status with date/time and error states

#### Strongly Recommended Fields

- `DeliveredDate`, `PromisedDate`, `TrackingNumber`
- `TrackingURL`, `SupportPhoneNumbers`
- `TransitEvents` (location and timestamp)
- `CreateDate`, `PickupDate`, `CanReschedule`

#### Data Restrictions

API cannot return personal data or geographical information about senders/recipients.

---

## 4. International SEO

### 4.1 Hreflang Implementation

#### When to Use
- Content in single language with template translations
- Regional variations with similar content
- Fully translated content across multiple languages

#### Implementation Methods

**1. HTML Tags:**
```html
<link rel="alternate" hreflang="en" href="https://example.com/en/page">
<link rel="alternate" hreflang="es" href="https://example.com/es/page">
<link rel="alternate" hreflang="x-default" href="https://example.com/page">
```

**2. HTTP Headers (for PDFs):**
```
Link: <https://example.com/file.pdf>; rel="alternate"; hreflang="en"
```

**3. Sitemap:**
```xml
<url>
  <loc>https://example.com/en/page</loc>
  <xhtml:link rel="alternate" hreflang="en" href="https://example.com/en/page"/>
  <xhtml:link rel="alternate" hreflang="es" href="https://example.com/es/page"/>
</url>
```

#### Language Codes

| Code | Target |
|------|--------|
| `en` | English (all regions) |
| `en-GB` | English (UK) |
| `en-US` | English (US) |
| `de` | German |
| `zh-Hans` | Chinese (Simplified) |
| `x-default` | Fallback for unmatched |

#### Critical Requirements
- Bidirectional linking (each version links to all others)
- Use fully-qualified URLs
- Include self-referencing hreflang

---

## 5. Mobile SEO

### 5.1 Mobile-First Indexing

Google uses the mobile version of sites for indexing and ranking.

#### Configuration Options

| Method | Description |
|--------|-------------|
| **Responsive Design** (Recommended) | Same HTML, different display |
| **Dynamic Serving** | Same URL, different HTML per user-agent |
| **Separate URLs** | Different URLs for mobile (m-dot) |

#### Critical Requirements

| Requirement | Details |
|-------------|---------|
| Content parity | Same content on mobile and desktop |
| Same metadata | Titles, descriptions, structured data |
| Accessible resources | Don't block with robots.txt |
| Same robots tags | Never noindex mobile only |

#### Common Critical Errors

| Error | Impact |
|-------|--------|
| Missing structured data on mobile | Reduced indexing |
| `noindex` on mobile only | Pages excluded |
| Low-quality/missing images | Ranking loss |
| Error pages on mobile | Index removal |
| URL fragments (#) | Non-indexable |

---

## 6. Monitoring and Debugging

### 6.1 Search Console

#### Key Setup Steps

1. **Verify Ownership** - Confirm site ownership
2. **Check Indexing** - Review Index Coverage report
3. **Submit Sitemap** - Accelerate discovery (optional)
4. **Monitor Performance** - Track traffic from Google Search

#### Monitoring Frequency
- Monthly check-ins recommended
- Email alerts sent for new issues

#### Essential Reports

| For SEO/Marketing | For Developers |
|-------------------|----------------|
| Manual Actions | Index Coverage |
| Removals tool | URL Inspection |
| Change of Address | Security Issues |
| Rich Result status | Core Web Vitals |

---

## Quick Reference Checklists

### Pre-Launch SEO Checklist

- [ ] Unique `<title>` tags on every page
- [ ] Meta descriptions for all pages
- [ ] Proper heading hierarchy (H1 → H6)
- [ ] Alt text on all images
- [ ] Mobile-responsive design
- [ ] HTTPS enabled
- [ ] XML sitemap created
- [ ] robots.txt configured
- [ ] Canonical URLs set
- [ ] Structured data implemented
- [ ] Core Web Vitals optimized
- [ ] Search Console verified

### Technical SEO Checklist

- [ ] No blocked resources in robots.txt
- [ ] Proper 301 redirects for moved pages
- [ ] No duplicate content issues
- [ ] Hreflang for multilingual sites
- [ ] Clean URL structure
- [ ] Internal linking strategy
- [ ] Page speed optimized
- [ ] Mobile-first compatible

### Content SEO Checklist

- [ ] Helpful, people-first content
- [ ] Unique content (not copied)
- [ ] Up-to-date information
- [ ] Clear content hierarchy
- [ ] Relevant internal links
- [ ] Descriptive anchor text
- [ ] No keyword stuffing
- [ ] Appropriate content length

---

## Resources

### Official Documentation
- [Google Search Central](https://developers.google.com/search)
- [Search Console Help](https://support.google.com/webmasters)
- [Google Search Blog](https://developers.google.com/search/blog)

### Tools
- [Rich Results Test](https://search.google.com/test/rich-results)
- [PageSpeed Insights](https://pagespeed.web.dev/)
- [Mobile-Friendly Test](https://search.google.com/test/mobile-friendly)
- [URL Inspection Tool](https://support.google.com/webmasters/answer/9012289)

---

*Last updated: January 2025*
*Source: Google Search Central Documentation*
