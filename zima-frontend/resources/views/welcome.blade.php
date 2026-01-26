<!DOCTYPE html>
<html lang="{{ str_replace('_', '-', app()->getLocale()) }}">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">

    <!-- Primary SEO Meta Tags -->
    <title>Process Documents Online - AI-Powered PDF, Excel, Word & PowerPoint Generator | Free Document Processing</title>
    <meta name="description" content="Process documents online with AI. Create, convert, merge, and edit PDF, Excel, Word, and PowerPoint files. Document processing, OCR, format conversion. No software installation required.">
    <meta name="author" content="Process Documents Online">
    <meta name="robots" content="index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1">
    <meta name="googlebot" content="index, follow">

    <!-- Canonical URL -->
    <link rel="canonical" href="{{ url('/') }}">

    <!-- hreflang for language/region targeting -->
    <link rel="alternate" hreflang="en" href="{{ url('/') }}">
    <link rel="alternate" hreflang="x-default" href="{{ url('/') }}">

    <!-- Open Graph / Facebook -->
    <meta property="og:type" content="website">
    <meta property="og:url" content="{{ url('/') }}">
    <meta property="og:title" content="Process Documents Online - AI-Powered Document Processing Platform">
    <meta property="og:description" content="Create, convert, and process PDF, Excel, Word, and PowerPoint documents online with AI. Instant results, no installation required.">
    <meta property="og:image" content="{{ asset('images/og-image.png') }}">
    <meta property="og:site_name" content="Process Documents Online">
    <meta property="og:locale" content="en_US">

    <!-- Twitter Card -->
    <meta name="twitter:card" content="summary_large_image">
    <meta name="twitter:url" content="{{ url('/') }}">
    <meta name="twitter:title" content="Process Documents Online - AI Document Processing">
    <meta name="twitter:description" content="Process documents online with AI. Create PDF, Excel, Word, PowerPoint instantly. Free to start.">
    <meta name="twitter:image" content="{{ asset('images/twitter-card.png') }}">

    <!-- Additional SEO Meta Tags -->
    <meta name="theme-color" content="#171717">
    <meta name="msapplication-TileColor" content="#171717">
    <meta name="application-name" content="Process Documents Online">
    <meta name="apple-mobile-web-app-title" content="Process Docs">
    <meta name="apple-mobile-web-app-capable" content="yes">
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">
    <meta name="format-detection" content="telephone=no">

    <!-- Favicon -->
    <link rel="icon" type="image/x-icon" href="{{ asset('favicon.ico') }}">
    <link rel="icon" type="image/png" sizes="32x32" href="{{ asset('favicon-32x32.png') }}">
    <link rel="icon" type="image/png" sizes="16x16" href="{{ asset('favicon-16x16.png') }}">
    <link rel="apple-touch-icon" sizes="180x180" href="{{ asset('apple-touch-icon.png') }}">

    <!-- Structured Data: Organization -->
    <script type="application/ld+json">
    {
        "@@context": "https://schema.org",
        "@@type": "Organization",
        "name": "Process Documents Online",
        "legalName": "ZIMA Solutions Limited",
        "alternateName": ["ProcessDocumentsOnline", "Process Docs Online", "ZIMA"],
        "url": "{{ url('/') }}",
        "logo": "{{ asset('images/logo.png') }}",
        "description": "Free online document processing platform for creating, converting, and managing PDF, Excel, Word, and PowerPoint files. Process documents online instantly.",
        "taxID": "181-314-605",
        "address": {
            "@@type": "PostalAddress",
            "streetAddress": "Makongo, Near Ardhi University",
            "addressLocality": "Kinondoni",
            "addressRegion": "Dar es Salaam",
            "addressCountry": "TZ"
        },
        "sameAs": [
            "https://twitter.com/processdocs",
            "https://facebook.com/processdocsonline",
            "https://linkedin.com/company/process-documents-online"
        ],
        "contactPoint": [
            {
                "@@type": "ContactPoint",
                "telephone": "+255692410353",
                "contactType": "customer service",
                "availableLanguage": ["English", "Swahili"],
                "areaServed": "TZ"
            },
            {
                "@@type": "ContactPoint",
                "email": "info@zima.co.tz",
                "contactType": "customer service",
                "availableLanguage": ["English", "Swahili"]
            }
        ]
    }
    </script>

    <!-- Structured Data: WebSite -->
    <script type="application/ld+json">
    {
        "@@context": "https://schema.org",
        "@@type": "WebSite",
        "name": "Process Documents Online",
        "alternateName": "Process Docs",
        "url": "{{ url('/') }}",
        "description": "Process documents online with AI-powered tools. Create, convert, merge, and edit PDF, Excel, Word, and PowerPoint files instantly."
    }
    </script>

    <!-- Structured Data: BreadcrumbList -->
    <script type="application/ld+json">
    {
        "@@context": "https://schema.org",
        "@@type": "BreadcrumbList",
        "itemListElement": [
            {
                "@@type": "ListItem",
                "position": 1,
                "name": "Home",
                "item": "{{ url('/') }}"
            }
        ]
    }
    </script>

    <!-- Structured Data: SoftwareApplication for Quick Tools -->
    <script type="application/ld+json">
    {
        "@@context": "https://schema.org",
        "@@type": "SoftwareApplication",
        "name": "Free Online Document Processing Tools",
        "description": "Process documents online free. Merge PDF, split PDF, compress PDF, convert Excel to CSV, Word to PDF. 185+ tools for PDF, Excel, Word, PowerPoint processing without signup.",
        "applicationCategory": "BusinessApplication",
        "operatingSystem": "Web Browser",
        "offers": {
            "@@type": "Offer",
            "price": "0",
            "priceCurrency": "USD"
        },
        "featureList": [
            "Merge PDF files online free",
            "Split PDF by pages or ranges",
            "Compress PDF reduce file size",
            "Convert PDF to Word DOCX",
            "Excel to CSV conversion",
            "Excel to JSON export",
            "Word document to PDF",
            "PowerPoint to PDF conversion",
            "OCR text extraction from images",
            "Image format conversion",
            "Add watermark to PDF",
            "Password protect PDF",
            "Digital signature for PDF"
        ],
        "aggregateRating": {
            "@@type": "AggregateRating",
            "ratingValue": "4.8",
            "reviewCount": "1250",
            "bestRating": "5"
        }
    }
    </script>

    <!-- Structured Data: FAQPage -->
    <script type="application/ld+json">
    {
        "@@context": "https://schema.org",
        "@@type": "FAQPage",
        "mainEntity": [
            {
                "@@type": "Question",
                "name": "What types of documents can I process online?",
                "acceptedAnswer": {
                    "@@type": "Answer",
                    "text": "You can process PDF files, Excel spreadsheets (XLSX, XLS, CSV), Word documents (DOCX, DOC), PowerPoint presentations (PPTX, PPT), images (PNG, JPG, TIFF, WebP), JSON, XML, text files, and HTML. The platform supports over 50 file formats."
                }
            },
            {
                "@@type": "Question",
                "name": "Is it free to process documents online?",
                "acceptedAnswer": {
                    "@@type": "Answer",
                    "text": "Yes, there's a free tier for basic document processing. Premium plans are available for higher limits and advanced features like batch processing and API access."
                }
            },
            {
                "@@type": "Question",
                "name": "How does AI document processing work?",
                "acceptedAnswer": {
                    "@@type": "Answer",
                    "text": "Describe what you need in plain English - like 'create a PDF invoice' or 'convert this Excel to JSON' - and the AI selects the right tools automatically. It handles OCR, formatting, and data transformation without requiring technical knowledge."
                }
            },
            {
                "@@type": "Question",
                "name": "Are my documents secure and private?",
                "acceptedAnswer": {
                    "@@type": "Answer",
                    "text": "Yes. Documents are processed over encrypted connections (HTTPS/TLS) and automatically deleted within 24 hours. We don't share files with third parties. You can also encrypt output documents with passwords."
                }
            },
            {
                "@@type": "Question",
                "name": "Can I convert PDF to Word or Excel online?",
                "acceptedAnswer": {
                    "@@type": "Answer",
                    "text": "Yes, you can convert PDF to Word (DOCX) or Excel (XLSX). The system preserves formatting, tables, and images. For scanned PDFs, OCR extracts text to create editable documents."
                }
            },
            {
                "@@type": "Question",
                "name": "What is OCR and how can it help me?",
                "acceptedAnswer": {
                    "@@type": "Answer",
                    "text": "OCR (Optical Character Recognition) extracts text from images and scanned documents. It converts scanned PDFs to searchable text and digitizes printed materials like receipts, invoices, and contracts."
                }
            },
            {
                "@@type": "Question",
                "name": "Do I need to install any software?",
                "acceptedAnswer": {
                    "@@type": "Answer",
                    "text": "No, it's web-based and works in your browser. Use any device with an internet connection - Windows, Mac, Linux, or mobile. No Microsoft Office required."
                }
            }
        ]
    }
    </script>

    <!-- Structured Data: Service offerings -->
    <script type="application/ld+json">
    {
        "@@context": "https://schema.org",
        "@@type": "Service",
        "name": "Online Document Processing Service",
        "description": "AI-powered document processing service. Create, convert, merge, split, and edit PDF, Excel, Word, and PowerPoint files online. No software installation required.",
        "provider": {
            "@@type": "Organization",
            "name": "Process Documents Online",
            "url": "{{ url('/') }}"
        },
        "serviceType": "Document Processing",
        "areaServed": "Worldwide",
        "hasOfferCatalog": {
            "@@type": "OfferCatalog",
            "name": "Document Processing Services",
            "itemListElement": [
                {
                    "@@type": "Offer",
                    "itemOffered": {
                        "@@type": "Service",
                        "name": "PDF Processing",
                        "description": "Create, merge, split, compress, convert, and protect PDF documents online"
                    }
                },
                {
                    "@@type": "Offer",
                    "itemOffered": {
                        "@@type": "Service",
                        "name": "Excel Processing",
                        "description": "Create spreadsheets with formulas, charts, pivot tables. Convert Excel to CSV, JSON, PDF"
                    }
                },
                {
                    "@@type": "Offer",
                    "itemOffered": {
                        "@@type": "Service",
                        "name": "Word Document Processing",
                        "description": "Create Word documents with formatting, tables, images. Convert Word to PDF"
                    }
                },
                {
                    "@@type": "Offer",
                    "itemOffered": {
                        "@@type": "Service",
                        "name": "PowerPoint Processing",
                        "description": "Create presentations with slides, animations, charts. Convert PowerPoint to PDF"
                    }
                },
                {
                    "@@type": "Offer",
                    "itemOffered": {
                        "@@type": "Service",
                        "name": "OCR Text Extraction",
                        "description": "Extract text from images and scanned documents using optical character recognition"
                    }
                },
                {
                    "@@type": "Offer",
                    "itemOffered": {
                        "@@type": "Service",
                        "name": "Format Conversion",
                        "description": "Convert between document formats: PDF to Word, Excel to CSV, JSON to CSV, and more"
                    }
                }
            ]
        }
    }
    </script>

    <!-- Structured Data: LocalBusiness -->
    <script type="application/ld+json">
    {
        "@@context": "https://schema.org",
        "@@type": "ProfessionalService",
        "name": "Process Documents Online",
        "alternateName": "ZIMA Solutions",
        "description": "AI-powered document processing service. Create, convert, and manage PDF, Excel, Word, and PowerPoint files online.",
        "url": "{{ url('/') }}",
        "logo": "{{ asset('images/logo.png') }}",
        "image": "{{ asset('images/og-image.png') }}",
        "telephone": "+255692410353",
        "email": "info@zima.co.tz",
        "taxID": "181-314-605",
        "address": {
            "@@type": "PostalAddress",
            "streetAddress": "Makongo, Near Ardhi University",
            "addressLocality": "Kinondoni",
            "addressRegion": "Dar es Salaam",
            "addressCountry": "TZ"
        },
        "geo": {
            "@@type": "GeoCoordinates",
            "latitude": "-6.7627",
            "longitude": "39.2340"
        },
        "openingHoursSpecification": [
            {
                "@@type": "OpeningHoursSpecification",
                "dayOfWeek": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
                "opens": "08:00",
                "closes": "17:00"
            },
            {
                "@@type": "OpeningHoursSpecification",
                "dayOfWeek": "Saturday",
                "opens": "09:00",
                "closes": "13:00"
            }
        ],
        "priceRange": "$$",
        "currenciesAccepted": "TZS, USD",
        "paymentAccepted": "Cash, Mobile Money, Bank Transfer",
        "areaServed": {
            "@@type": "GeoCircle",
            "geoMidpoint": {
                "@@type": "GeoCoordinates",
                "latitude": "-6.7627",
                "longitude": "39.2340"
            },
            "geoRadius": "50000"
        },
        "sameAs": [
            "https://twitter.com/processdocs",
            "https://facebook.com/processdocsonline",
            "https://linkedin.com/company/process-documents-online"
        ]
    }
    </script>

    <!-- Core Web Vitals Optimization: Resource Hints -->
    <link rel="dns-prefetch" href="https://fonts.bunny.net">
    <link rel="dns-prefetch" href="https://cdn.tailwindcss.com">
    <link rel="preconnect" href="https://fonts.bunny.net" crossorigin>

    <!-- Fonts with display swap for better LCP -->
    <link href="https://fonts.bunny.net/css?family=instrument-sans:400,500,600,700&display=swap" rel="stylesheet" />

    <!-- Critical CSS inline hint -->
    <style>
        /* Critical CSS for above-the-fold content - reduces CLS */
        body { font-family: 'Instrument Sans', system-ui, -apple-system, sans-serif; }
        .antialiased { -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }
    </style>

    @vite(['resources/css/app.css', 'resources/js/app.js'])

    <!-- Tailwind CSS Browser (consider removing in production for better performance) -->
    <script src="https://cdn.tailwindcss.com" defer></script>

    @livewireStyles

    <!-- Core Web Vitals: Preload critical resources -->
    <link rel="preload" as="image" href="{{ asset('favicon.ico') }}">
</head>
<body class="antialiased bg-neutral-50 min-h-screen flex flex-col">
    <!-- Compact Header - Monochrome Design -->
    <header class="bg-white shadow-sm">
        <div class="max-w-full mx-auto px-4 sm:px-6">
            <div class="flex justify-between items-center h-14">
                <!-- Logo -->
                <a href="{{ url('/') }}" class="flex items-center" title="Process Documents Online - Home">
                    <div class="w-10 h-10 bg-neutral-900 rounded-xl flex items-center justify-center mr-3 shadow-sm">
                        <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                        </svg>
                    </div>
                    <div>
                        <span class="text-[15px] font-bold text-neutral-900">Process Documents Online</span>
                        <p class="text-[11px] text-neutral-500">AI-Powered Document Processing</p>
                    </div>
                </a>

                <!-- Auth Links -->
                @if (Route::has('login'))
                    <nav class="flex items-center gap-3">
                        @auth
                            <a href="{{ url('/dashboard') }}" class="text-sm text-neutral-500 hover:text-neutral-900 transition-colors">
                                Dashboard
                            </a>
                            <a href="{{ url('/files') }}" class="inline-flex items-center px-4 py-2 bg-neutral-900 hover:bg-neutral-800 text-white text-sm font-medium rounded-xl shadow-sm transition-all hover:shadow-md">
                                My Files
                            </a>
                        @endauth
                        @guest
                            <button
                                onclick="Livewire.dispatch('openAuthModal', { mode: 'login' })"
                                class="text-sm text-neutral-500 hover:text-neutral-900 transition-colors cursor-pointer"
                            >
                                Log in
                            </button>
                            <button
                                onclick="Livewire.dispatch('openAuthModal', { mode: 'register' })"
                                class="inline-flex items-center px-4 py-2 bg-neutral-900 hover:bg-neutral-800 text-white text-sm font-medium rounded-xl shadow-sm transition-all hover:shadow-md cursor-pointer"
                            >
                                Register
                            </button>
                        @endguest
                    </nav>
                @endif
            </div>
        </div>
    </header>

    <!-- Guest Notice - Monochrome Style -->
    @guest
        <div class="bg-neutral-100 border-b border-neutral-200 px-4 py-2">
            <div class="max-w-full mx-auto flex items-center justify-center">
                <div class="w-5 h-5 bg-neutral-900 rounded-lg flex items-center justify-center mr-2 flex-shrink-0">
                    <svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                    </svg>
                </div>
                <p class="text-[11px] text-neutral-500">
                    <span class="font-semibold text-neutral-900">Guest Mode</span> - Chat history won't be saved.
                    <button onclick="Livewire.dispatch('openAuthModal', { mode: 'login' })" class="underline text-neutral-700 hover:text-neutral-900 transition-colors cursor-pointer">Log in</button> or
                    <button onclick="Livewire.dispatch('openAuthModal', { mode: 'register' })" class="underline text-neutral-700 hover:text-neutral-900 transition-colors cursor-pointer">register</button>
                    to save conversations.
                </p>
            </div>
        </div>
    @endguest

    <!-- Main Content -->
    <main class="flex-1 p-4">
        @livewire('file-generator')
    </main>

    <!-- Hero Section with H1 -->
    <section class="bg-gradient-to-b from-neutral-100 to-white py-16 px-4">
        <div class="max-w-6xl mx-auto text-center">
            <h1 class="text-4xl md:text-5xl font-bold text-neutral-900 mb-6">
                Process Documents Online with AI
            </h1>
            <p class="text-xl text-neutral-600 max-w-3xl mx-auto mb-8">
                Create, convert, merge, split, and edit PDF, Excel, Word, and PowerPoint files
                using AI. No software installation required - everything works in your browser.
            </p>
            <div class="flex flex-wrap justify-center gap-4 text-sm text-neutral-500">
                <span class="flex items-center gap-2">
                    <svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/></svg>
                    Multiple Tools
                </span>
                <span class="flex items-center gap-2">
                    <svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/></svg>
                    AI-Powered Processing
                </span>
                <span class="flex items-center gap-2">
                    <svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/></svg>
                    Free to Start
                </span>
                <span class="flex items-center gap-2">
                    <svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/></svg>
                    Instant Results
                </span>
            </div>
        </div>
    </section>

    <!-- Services Section -->
    <section class="bg-white py-16 px-4" id="services">
        <div class="max-w-6xl mx-auto">
            <div class="text-center mb-12">
                <h2 class="text-3xl font-bold text-neutral-900 mb-4">Document Processing Services</h2>
                <p class="text-neutral-600 max-w-3xl mx-auto">
                    Comprehensive document processing capabilities powered by artificial intelligence.
                    Process documents online instantly - just describe what you need in natural language and our AI handles the rest.
                    Supports all major document formats including PDF, XLSX, DOCX, PPTX, CSV, JSON, and more.
                </p>
            </div>

            <!-- Service Cards Grid -->
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">

                <!-- PDF Processing Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-red-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">PDF Processing Online</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Create PDF documents from scratch or convert from other formats.
                        Merge multiple PDFs, split pages, compress files, and add password protection.
                        Advanced features include watermarks, annotations, bookmarks, digital signatures,
                        and OCR text extraction. Convert PDF to Word, Excel, images, and more.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-red-100 text-red-700 px-2 py-1 rounded-full">Create PDF</span>
                        <span class="text-xs bg-red-100 text-red-700 px-2 py-1 rounded-full">Merge PDF</span>
                        <span class="text-xs bg-red-100 text-red-700 px-2 py-1 rounded-full">Split PDF</span>
                        <span class="text-xs bg-red-100 text-red-700 px-2 py-1 rounded-full">PDF OCR</span>
                        <span class="text-xs bg-red-100 text-red-700 px-2 py-1 rounded-full">Sign PDF</span>
                        <span class="text-xs bg-red-100 text-red-700 px-2 py-1 rounded-full">Compress</span>
                        <span class="text-xs bg-neutral-200 text-neutral-600 px-2 py-1 rounded-full">+30 more tools</span>
                    </div>
                </article>

                <!-- Excel Processing Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-green-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">Excel Spreadsheet Generator</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Create Excel spreadsheets with formulas, charts, pivot tables, and conditional formatting.
                        Merge workbooks, split worksheets, and convert to CSV, JSON, or PDF.
                        Validate data, protect with passwords, and handle large datasets efficiently.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-green-100 text-green-700 px-2 py-1 rounded-full">Create Excel</span>
                        <span class="text-xs bg-green-100 text-green-700 px-2 py-1 rounded-full">Formulas</span>
                        <span class="text-xs bg-green-100 text-green-700 px-2 py-1 rounded-full">Charts</span>
                        <span class="text-xs bg-green-100 text-green-700 px-2 py-1 rounded-full">Pivot Tables</span>
                        <span class="text-xs bg-green-100 text-green-700 px-2 py-1 rounded-full">Convert</span>
                        <span class="text-xs bg-neutral-200 text-neutral-600 px-2 py-1 rounded-full">+22 more tools</span>
                    </div>
                </article>

                <!-- Word Processing Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">Word Document Creator</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Create Word documents with professional formatting, tables, images, and table of contents.
                        Use mail merge for personalized documents. Merge files, split by sections,
                        convert to PDF or HTML. Add watermarks, track changes, and protect with signatures.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded-full">Create DOCX</span>
                        <span class="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded-full">Merge</span>
                        <span class="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded-full">Mail Merge</span>
                        <span class="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded-full">Word to PDF</span>
                        <span class="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded-full">Styles</span>
                        <span class="text-xs bg-neutral-200 text-neutral-600 px-2 py-1 rounded-full">+20 more tools</span>
                    </div>
                </article>

                <!-- PowerPoint Processing Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-orange-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">PowerPoint Presentation Maker</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Create PowerPoint presentations with custom layouts, animations, and transitions.
                        Insert charts, tables, and images. Merge presentations, extract slides, reorder content,
                        and convert to PDF or images. Add speaker notes and watermarks.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-orange-100 text-orange-700 px-2 py-1 rounded-full">Create PPTX</span>
                        <span class="text-xs bg-orange-100 text-orange-700 px-2 py-1 rounded-full">Animations</span>
                        <span class="text-xs bg-orange-100 text-orange-700 px-2 py-1 rounded-full">Merge</span>
                        <span class="text-xs bg-orange-100 text-orange-700 px-2 py-1 rounded-full">PPT to PDF</span>
                        <span class="text-xs bg-neutral-200 text-neutral-600 px-2 py-1 rounded-full">+17 more tools</span>
                    </div>
                </article>

                <!-- Image & OCR Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-purple-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">Image Processing & OCR Online</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Extract text from images using OCR technology. Convert scanned documents to searchable text,
                        process handwritten notes, and digitize printed materials. Add watermarks, resize, crop,
                        convert formats (PNG, JPG, WebP, TIFF), and redact sensitive information.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-purple-100 text-purple-700 px-2 py-1 rounded-full">OCR Online</span>
                        <span class="text-xs bg-purple-100 text-purple-700 px-2 py-1 rounded-full">Watermark</span>
                        <span class="text-xs bg-purple-100 text-purple-700 px-2 py-1 rounded-full">Resize</span>
                        <span class="text-xs bg-purple-100 text-purple-700 px-2 py-1 rounded-full">Redact</span>
                        <span class="text-xs bg-purple-100 text-purple-700 px-2 py-1 rounded-full">Convert</span>
                        <span class="text-xs bg-neutral-200 text-neutral-600 px-2 py-1 rounded-full">+8 more tools</span>
                    </div>
                </article>

                <!-- Format Conversion Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-cyan-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-cyan-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">Document Format Converter</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Convert documents between formats with high fidelity. PDF to Word, PDF to Excel,
                        Word to PDF, Excel to PDF, PowerPoint to PDF. Transform data: JSON to CSV, CSV to Excel,
                        XML to JSON, YAML to JSON. Convert images to PDF and create PDF/A archival documents.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-cyan-100 text-cyan-700 px-2 py-1 rounded-full">PDF to Word</span>
                        <span class="text-xs bg-cyan-100 text-cyan-700 px-2 py-1 rounded-full">PDF to Excel</span>
                        <span class="text-xs bg-cyan-100 text-cyan-700 px-2 py-1 rounded-full">JSON to CSV</span>
                        <span class="text-xs bg-cyan-100 text-cyan-700 px-2 py-1 rounded-full">Image to PDF</span>
                        <span class="text-xs bg-neutral-200 text-neutral-600 px-2 py-1 rounded-full">+50 conversions</span>
                    </div>
                </article>

                <!-- JSON & Data Processing Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-yellow-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">JSON & Data Processing Tools</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Validate, format, beautify, and minify JSON files. Transform structures using path expressions,
                        merge files, and remove duplicates. Convert to CSV, XML, YAML, or Excel.
                        Encrypt sensitive data and validate against schemas.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-yellow-100 text-yellow-700 px-2 py-1 rounded-full">Validate JSON</span>
                        <span class="text-xs bg-yellow-100 text-yellow-700 px-2 py-1 rounded-full">Transform</span>
                        <span class="text-xs bg-yellow-100 text-yellow-700 px-2 py-1 rounded-full">JSON to CSV</span>
                        <span class="text-xs bg-yellow-100 text-yellow-700 px-2 py-1 rounded-full">Encrypt</span>
                        <span class="text-xs bg-neutral-200 text-neutral-600 px-2 py-1 rounded-full">+24 more tools</span>
                    </div>
                </article>

                <!-- Text Processing Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-pink-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-pink-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">Text File Processing Online</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Merge text files, split by lines or patterns, and use regex find-and-replace.
                        Remove duplicates, sort content, convert encodings (UTF-8, ASCII, Unicode),
                        encrypt with AES, calculate checksums, and compare files.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-pink-100 text-pink-700 px-2 py-1 rounded-full">Merge Text</span>
                        <span class="text-xs bg-pink-100 text-pink-700 px-2 py-1 rounded-full">Regex</span>
                        <span class="text-xs bg-pink-100 text-pink-700 px-2 py-1 rounded-full">Encrypt</span>
                        <span class="text-xs bg-pink-100 text-pink-700 px-2 py-1 rounded-full">Compare</span>
                        <span class="text-xs bg-pink-100 text-pink-700 px-2 py-1 rounded-full">Checksum</span>
                        <span class="text-xs bg-neutral-200 text-neutral-600 px-2 py-1 rounded-full">+24 more tools</span>
                    </div>
                </article>

                <!-- Security & Protection Card -->
                <article class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-neutral-200 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform" aria-hidden="true">
                        <svg class="w-7 h-7 text-neutral-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">Document Security & Protection</h3>
                    <p class="text-neutral-600 text-sm mb-4">
                        Password protect PDF, Word, and Excel files. Add digital signatures for authentication.
                        Encrypt with AES-256, verify signatures, and redact sensitive information
                        like personal data before sharing.
                    </p>
                    <div class="flex flex-wrap gap-2">
                        <span class="text-xs bg-neutral-200 text-neutral-700 px-2 py-1 rounded-full">Encrypt</span>
                        <span class="text-xs bg-neutral-200 text-neutral-700 px-2 py-1 rounded-full">Digital Sign</span>
                        <span class="text-xs bg-neutral-200 text-neutral-700 px-2 py-1 rounded-full">Password Protect</span>
                        <span class="text-xs bg-neutral-200 text-neutral-700 px-2 py-1 rounded-full">Redact</span>
                        <span class="text-xs bg-neutral-200 text-neutral-700 px-2 py-1 rounded-full">Verify</span>
                    </div>
                </article>

            </div>
        </div>
    </section>

    <!-- How It Works Section -->
    <section class="bg-neutral-100 py-16 px-4" id="how-it-works">
        <div class="max-w-6xl mx-auto">
            <div class="text-center mb-12">
                <h2 class="text-3xl font-bold text-neutral-900 mb-4">How to Process Documents Online</h2>
                <p class="text-neutral-600 max-w-2xl mx-auto">
                    Our AI-powered document processing platform makes it simple to create, convert, and manage your files in just three easy steps.
                </p>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
                <div class="text-center">
                    <div class="w-16 h-16 bg-neutral-900 text-white rounded-2xl flex items-center justify-center text-2xl font-bold mx-auto mb-4">1</div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">Describe Your Task</h3>
                    <p class="text-neutral-600">
                        Simply type what you need in plain English. For example: "Create a PDF invoice for John Smith with 3 line items"
                        or "Convert this Excel file to CSV format".
                    </p>
                </div>
                <div class="text-center">
                    <div class="w-16 h-16 bg-neutral-900 text-white rounded-2xl flex items-center justify-center text-2xl font-bold mx-auto mb-4">2</div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">AI Processes Your Request</h3>
                    <p class="text-neutral-600">
                        The AI analyzes your request and automatically selects the right tools
                        to complete your task accurately.
                    </p>
                </div>
                <div class="text-center">
                    <div class="w-16 h-16 bg-neutral-900 text-white rounded-2xl flex items-center justify-center text-2xl font-bold mx-auto mb-4">3</div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">Download Your Document</h3>
                    <p class="text-neutral-600">
                        Your processed document is ready instantly. Download it directly to your device or share it via a secure link.
                        All files are processed securely and deleted after 24 hours.
                    </p>
                </div>
            </div>
        </div>
    </section>

    <!-- Stats Section -->
    <section class="bg-neutral-900 py-12 px-4" aria-label="Platform Statistics">
        <div class="max-w-6xl mx-auto">
            <div class="grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
                <div>
                    <div class="text-4xl font-bold text-white mb-2">PDF</div>
                    <div class="text-neutral-400 text-sm">Create, Merge, Convert</div>
                </div>
                <div>
                    <div class="text-4xl font-bold text-white mb-2">Excel</div>
                    <div class="text-neutral-400 text-sm">Spreadsheets & Data</div>
                </div>
                <div>
                    <div class="text-4xl font-bold text-white mb-2">Word</div>
                    <div class="text-neutral-400 text-sm">Documents & Reports</div>
                </div>
                <div>
                    <div class="text-4xl font-bold text-white mb-2">AI</div>
                    <div class="text-neutral-400 text-sm">Powered Processing</div>
                </div>
            </div>
        </div>
    </section>

    <!-- FAQ Section -->
    <section class="bg-white py-16 px-4" id="faq">
        <div class="max-w-4xl mx-auto">
            <div class="text-center mb-12">
                <h2 class="text-3xl font-bold text-neutral-900 mb-4">Frequently Asked Questions</h2>
                <p class="text-neutral-600">
                    Common questions about processing documents online with our AI-powered platform.
                </p>
            </div>

            <div class="space-y-6">
                <!-- FAQ Item 1 -->
                <details class="bg-neutral-50 rounded-xl border border-neutral-200 overflow-hidden group" open>
                    <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-neutral-100 transition-colors">
                        <h3 class="text-lg font-semibold text-neutral-900 pr-4">What types of documents can I process online?</h3>
                        <svg class="w-5 h-5 text-neutral-500 transform group-open:rotate-180 transition-transform flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                        </svg>
                    </summary>
                    <div class="px-6 pb-6 text-neutral-600">
                        <p>You can process PDF files, Excel spreadsheets (XLSX, XLS, CSV), Word documents (DOCX, DOC), PowerPoint presentations (PPTX, PPT), images (PNG, JPG, TIFF, WebP), JSON, XML, text files, and HTML. The platform supports over 50 file formats.</p>
                    </div>
                </details>

                <!-- FAQ Item 2 -->
                <details class="bg-neutral-50 rounded-xl border border-neutral-200 overflow-hidden group">
                    <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-neutral-100 transition-colors">
                        <h3 class="text-lg font-semibold text-neutral-900 pr-4">Is it free to process documents online?</h3>
                        <svg class="w-5 h-5 text-neutral-500 transform group-open:rotate-180 transition-transform flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                        </svg>
                    </summary>
                    <div class="px-6 pb-6 text-neutral-600">
                        <p>Yes, there's a free tier for basic document processing. Premium plans are available for higher limits and advanced features like batch processing and API access.</p>
                    </div>
                </details>

                <!-- FAQ Item 3 -->
                <details class="bg-neutral-50 rounded-xl border border-neutral-200 overflow-hidden group">
                    <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-neutral-100 transition-colors">
                        <h3 class="text-lg font-semibold text-neutral-900 pr-4">How does AI document processing work?</h3>
                        <svg class="w-5 h-5 text-neutral-500 transform group-open:rotate-180 transition-transform flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                        </svg>
                    </summary>
                    <div class="px-6 pb-6 text-neutral-600">
                        <p>Describe what you need in plain English - like "create a PDF invoice" or "convert this Excel to JSON" - and the AI selects the right tools automatically. It handles OCR, formatting, and data transformation without requiring technical knowledge.</p>
                    </div>
                </details>

                <!-- FAQ Item 4 -->
                <details class="bg-neutral-50 rounded-xl border border-neutral-200 overflow-hidden group">
                    <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-neutral-100 transition-colors">
                        <h3 class="text-lg font-semibold text-neutral-900 pr-4">Are my documents secure and private?</h3>
                        <svg class="w-5 h-5 text-neutral-500 transform group-open:rotate-180 transition-transform flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                        </svg>
                    </summary>
                    <div class="px-6 pb-6 text-neutral-600">
                        <p>Yes. Documents are processed over encrypted connections (HTTPS/TLS) and automatically deleted within 24 hours. We don't share files with third parties. You can also encrypt output documents with passwords.</p>
                    </div>
                </details>

                <!-- FAQ Item 5 -->
                <details class="bg-neutral-50 rounded-xl border border-neutral-200 overflow-hidden group">
                    <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-neutral-100 transition-colors">
                        <h3 class="text-lg font-semibold text-neutral-900 pr-4">Can I convert PDF to Word or Excel online?</h3>
                        <svg class="w-5 h-5 text-neutral-500 transform group-open:rotate-180 transition-transform flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                        </svg>
                    </summary>
                    <div class="px-6 pb-6 text-neutral-600">
                        <p>Yes, you can convert PDF to Word (DOCX) or Excel (XLSX). The system preserves formatting, tables, and images. For scanned PDFs, OCR extracts text to create editable documents.</p>
                    </div>
                </details>

                <!-- FAQ Item 6 -->
                <details class="bg-neutral-50 rounded-xl border border-neutral-200 overflow-hidden group">
                    <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-neutral-100 transition-colors">
                        <h3 class="text-lg font-semibold text-neutral-900 pr-4">What is OCR and how can it help me?</h3>
                        <svg class="w-5 h-5 text-neutral-500 transform group-open:rotate-180 transition-transform flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                        </svg>
                    </summary>
                    <div class="px-6 pb-6 text-neutral-600">
                        <p>OCR (Optical Character Recognition) extracts text from images and scanned documents. It converts scanned PDFs to searchable text and digitizes printed materials like receipts, invoices, and contracts.</p>
                    </div>
                </details>

                <!-- FAQ Item 7 -->
                <details class="bg-neutral-50 rounded-xl border border-neutral-200 overflow-hidden group">
                    <summary class="flex items-center justify-between p-6 cursor-pointer hover:bg-neutral-100 transition-colors">
                        <h3 class="text-lg font-semibold text-neutral-900 pr-4">Do I need to install any software?</h3>
                        <svg class="w-5 h-5 text-neutral-500 transform group-open:rotate-180 transition-transform flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                        </svg>
                    </summary>
                    <div class="px-6 pb-6 text-neutral-600">
                        <p>No, it's web-based and works in your browser. Use any device with an internet connection - Windows, Mac, Linux, or mobile. No Microsoft Office required.</p>
                    </div>
                </details>
            </div>
        </div>
    </section>

    <!-- Bottom Hero / Call to Action Section -->
    <section class="bg-gradient-to-br from-neutral-900 via-neutral-800 to-neutral-900 text-white py-16 px-4">
        <div class="max-w-6xl mx-auto text-center">
            <h2 class="text-3xl md:text-4xl font-bold mb-4">
                Start Processing Documents Online Today
            </h2>
            <p class="text-xl text-neutral-300 mb-6">
                Create, convert, and manage documents with AI - completely free to start
            </p>
            <p class="text-neutral-400 max-w-2xl mx-auto text-lg leading-relaxed mb-8">
                No complex software, no steep learning curve - just describe what you need
                in plain English and get your documents processed.
            </p>
            <div class="flex flex-wrap justify-center gap-4">
                @guest
                    <button
                        onclick="Livewire.dispatch('openAuthModal', { mode: 'register' })"
                        class="inline-flex items-center px-8 py-3 bg-white hover:bg-neutral-100 text-neutral-900 text-lg font-semibold rounded-xl shadow-lg transition-all hover:shadow-xl cursor-pointer"
                    >
                        <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                        </svg>
                        Get Started Free
                    </button>
                    <button
                        onclick="Livewire.dispatch('openAuthModal', { mode: 'login' })"
                        class="inline-flex items-center px-8 py-3 bg-transparent hover:bg-neutral-700 text-white text-lg font-semibold rounded-xl border-2 border-neutral-600 transition-all cursor-pointer"
                    >
                        Sign In
                    </button>
                @endguest
                @auth
                    <a
                        href="{{ url('/dashboard') }}"
                        class="inline-flex items-center px-8 py-3 bg-white hover:bg-neutral-100 text-neutral-900 text-lg font-semibold rounded-xl shadow-lg transition-all hover:shadow-xl"
                    >
                        <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                        </svg>
                        Go to Dashboard
                    </a>
                @endauth
            </div>
            <div class="mt-10 flex flex-wrap justify-center gap-6 text-neutral-400 text-sm">
                <div class="flex items-center gap-2">
                    <svg class="w-5 h-5 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>Free to Start</span>
                </div>
                <div class="flex items-center gap-2">
                    <svg class="w-5 h-5 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>No Credit Card Required</span>
                </div>
                <div class="flex items-center gap-2">
                    <svg class="w-5 h-5 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>Instant Access</span>
                </div>
            </div>
        </div>
    </section>

    <!-- Footer -->
    <footer class="bg-neutral-800 py-12 px-4">
        <div class="max-w-6xl mx-auto">
            <!-- Footer Main Content -->
            <div class="grid grid-cols-1 md:grid-cols-5 gap-8 mb-8">
                <!-- Brand Column -->
                <div class="md:col-span-2">
                    <div class="flex items-center mb-4">
                        <div class="w-10 h-10 bg-white rounded-xl flex items-center justify-center mr-3">
                            <svg class="w-5 h-5 text-neutral-900" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                            </svg>
                        </div>
                        <span class="text-xl font-bold text-white">Process Documents Online</span>
                    </div>
                    <p class="text-neutral-400 text-sm mb-4 max-w-md">
                        AI-powered document processing platform. Create, convert, merge, and edit PDF, Excel, Word,
                        and PowerPoint files online. Instant results, no installation required.
                    </p>
                    <p class="text-neutral-500 text-xs mb-4">
                        Free online document processing tools
                    </p>
                    <!-- Social Links -->
                    <div class="flex gap-3">
                        <a href="https://twitter.com/processdocs" target="_blank" rel="noopener" class="w-8 h-8 bg-neutral-700 rounded-lg flex items-center justify-center hover:bg-neutral-600 transition-colors" aria-label="Twitter">
                            <svg class="w-4 h-4 text-neutral-300" fill="currentColor" viewBox="0 0 24 24"><path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/></svg>
                        </a>
                        <a href="https://facebook.com/processdocsonline" target="_blank" rel="noopener" class="w-8 h-8 bg-neutral-700 rounded-lg flex items-center justify-center hover:bg-neutral-600 transition-colors" aria-label="Facebook">
                            <svg class="w-4 h-4 text-neutral-300" fill="currentColor" viewBox="0 0 24 24"><path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/></svg>
                        </a>
                        <a href="https://linkedin.com/company/process-documents-online" target="_blank" rel="noopener" class="w-8 h-8 bg-neutral-700 rounded-lg flex items-center justify-center hover:bg-neutral-600 transition-colors" aria-label="LinkedIn">
                            <svg class="w-4 h-4 text-neutral-300" fill="currentColor" viewBox="0 0 24 24"><path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/></svg>
                        </a>
                    </div>
                </div>

                <!-- Document Tools Column -->
                <div>
                    <h4 class="text-white font-semibold mb-4">Document Tools</h4>
                    <ul class="space-y-2 text-sm">
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">PDF Processing</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">Excel Generator</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">Word Creator</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">PowerPoint Maker</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">OCR Online</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">Format Converter</a></li>
                    </ul>
                </div>

                <!-- Popular Conversions Column -->
                <div>
                    <h4 class="text-white font-semibold mb-4">Popular Conversions</h4>
                    <ul class="space-y-2 text-sm">
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">PDF to Word</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">PDF to Excel</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">Word to PDF</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">Excel to CSV</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">JSON to CSV</a></li>
                        <li><a href="#services" class="text-neutral-400 hover:text-white transition-colors">Merge PDF</a></li>
                    </ul>
                </div>

                <!-- Contact & Support Column -->
                <div>
                    <h4 class="text-white font-semibold mb-4">Contact Us</h4>
                    <ul class="space-y-3 text-sm">
                        <li class="flex items-start gap-2">
                            <svg class="w-4 h-4 text-neutral-500 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
                            </svg>
                            <span class="text-neutral-400">Makongo, Near Ardhi University<br>Kinondoni, Dar es Salaam, Tanzania</span>
                        </li>
                        <li class="flex items-start gap-2">
                            <svg class="w-4 h-4 text-neutral-500 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"/>
                            </svg>
                            <a href="tel:+255692410353" class="text-neutral-400 hover:text-white transition-colors">+255 69 241 0353</a>
                        </li>
                        <li class="flex items-start gap-2">
                            <svg class="w-4 h-4 text-neutral-500 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
                            </svg>
                            <a href="mailto:info@zima.co.tz" class="text-neutral-400 hover:text-white transition-colors">info@zima.co.tz</a>
                        </li>
                        <li class="flex items-start gap-2">
                            <svg class="w-4 h-4 text-neutral-500 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
                            </svg>
                            <span class="text-neutral-400">Mon-Fri: 8AM-5PM<br>Sat: 9AM-1PM</span>
                        </li>
                    </ul>
                    <div class="mt-4 pt-4 border-t border-neutral-700">
                        <ul class="space-y-2 text-sm">
                            <li><a href="/privacy-policy" class="text-neutral-400 hover:text-white transition-colors">Privacy Policy</a></li>
                            <li><a href="/terms-of-service" class="text-neutral-400 hover:text-white transition-colors">Terms of Service</a></li>
                            <li><a href="#faq" class="text-neutral-400 hover:text-white transition-colors">FAQ</a></li>
                        </ul>
                    </div>
                </div>
            </div>

            <!-- Footer Bottom -->
            <div class="border-t border-neutral-700 pt-8">
                <div class="flex flex-col md:flex-row justify-between items-center gap-4">
                    <div class="text-center md:text-left">
                        <p class="text-neutral-500 text-sm">
                            &copy; {{ date('Y') }} Process Documents Online. All rights reserved.
                        </p>
                        <p class="text-neutral-600 text-xs mt-1">
                            A service by ZIMA Solutions Limited | TIN: 181-314-605
                        </p>
                    </div>
                    <div class="flex flex-wrap justify-center gap-4 md:gap-6 text-sm">
                        <a href="/privacy-policy" class="text-neutral-400 hover:text-white transition-colors">Privacy Policy</a>
                        <a href="/terms-of-service" class="text-neutral-400 hover:text-white transition-colors">Terms of Service</a>
                        <a href="#faq" class="text-neutral-400 hover:text-white transition-colors">FAQ</a>
                        <a href="#services" class="text-neutral-400 hover:text-white transition-colors">All Tools</a>
                    </div>
                </div>
            </div>
        </div>
    </footer>

    <!-- Auth Modal -->
    @livewire('auth-modal')

    @livewireScripts
</body>
</html>
