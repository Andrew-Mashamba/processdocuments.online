<x-main-layout>
    <x-slot name="title">Document Processing Tools - PDF, Excel, Word, PowerPoint Online</x-slot>
    <x-slot name="description">185+ free online document processing tools. Create, convert, merge PDF, Excel, Word files instantly. OCR, format conversion, data processing.</x-slot>

    <!-- Hero Section -->
    <section class="bg-gradient-to-b from-neutral-100 to-white py-16 px-4">
        <div class="max-w-6xl mx-auto text-center">
            <h1 class="text-4xl md:text-5xl font-bold text-neutral-900 mb-6">
                Document Processing Services
            </h1>
            <p class="text-xl text-neutral-600 max-w-3xl mx-auto mb-8">
                Comprehensive document processing capabilities powered by artificial intelligence.
                Process documents online instantly - just describe what you need in natural language and our AI handles the rest.
                Supports all major document formats including PDF, XLSX, DOCX, PPTX, CSV, JSON, and more.
            </p>
            <a href="{{ route('home') }}" class="inline-flex items-center px-8 py-3 bg-neutral-900 hover:bg-neutral-800 text-white text-lg font-semibold rounded-xl shadow-sm transition-all hover:shadow-md">
                <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                </svg>
                Start Processing Now
            </a>
        </div>
    </section>

    <!-- Service Cards Grid -->
    <section class="bg-white py-16 px-4">
        <div class="max-w-6xl mx-auto">
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">

                <!-- PDF Processing Card -->
                <article id="pdf" class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-red-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">PDF Processing Online</h2>
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
                <article id="excel" class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-green-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">Excel Spreadsheet Generator</h2>
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
                <article id="word" class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">Word Document Creator</h2>
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
                <article id="powerpoint" class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-orange-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">PowerPoint Presentation Maker</h2>
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
                <article id="ocr" class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-purple-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">Image Processing & OCR Online</h2>
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
                <article id="convert" class="bg-neutral-50 rounded-2xl p-6 border border-neutral-200 hover:border-neutral-300 hover:shadow-lg transition-all group">
                    <div class="w-14 h-14 bg-cyan-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-cyan-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">Document Format Converter</h2>
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
                    <div class="w-14 h-14 bg-yellow-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">JSON & Data Processing Tools</h2>
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
                    <div class="w-14 h-14 bg-pink-100 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-pink-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">Text File Processing Online</h2>
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
                    <div class="w-14 h-14 bg-neutral-200 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                        <svg class="w-7 h-7 text-neutral-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-semibold text-neutral-900 mb-2">Document Security & Protection</h2>
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

    <!-- Detailed Tools List -->
    <section class="bg-neutral-100 py-16 px-4">
        <div class="max-w-7xl mx-auto">
            <div class="text-center mb-12">
                <h2 class="text-3xl font-bold text-neutral-900 mb-4">Free Online Document Tools</h2>
                <p class="text-neutral-600">Convert, merge, split PDF, Excel, Word files instantly</p>
            </div>

            <!-- Tool Categories Tabs -->
            <div class="flex flex-wrap justify-center gap-2 mb-8">
                <button onclick="showCategory('pdf')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="pdf">PDF</button>
                <button onclick="showCategory('excel')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="excel">Excel</button>
                <button onclick="showCategory('word')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="word">Word</button>
                <button onclick="showCategory('powerpoint')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="powerpoint">PowerPoint</button>
                <button onclick="showCategory('text')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="text">Text</button>
                <button onclick="showCategory('json')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="json">JSON</button>
                <button onclick="showCategory('ocr')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="ocr">OCR</button>
                <button onclick="showCategory('image')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="image">Image</button>
                <button onclick="showCategory('convert')" class="category-btn px-4 py-2 bg-white hover:bg-neutral-900 hover:text-white text-neutral-900 rounded-xl transition-colors font-medium text-sm" data-category="convert">Convert</button>
            </div>

            <!-- PDF Tools -->
            <div id="pdf-tools" class="tool-category">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">PDF Tools</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Merge PDFs</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Combine multiple PDFs</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Split PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Split by pages/ranges</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Extract Pages</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract specific pages</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Remove Pages</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove specific pages</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Rotate PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Rotate pages</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Watermark</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Text watermark</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Page Numbers</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Add page numbers</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Compress PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Reduce file size</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PDF Info</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Get metadata</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Protect PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Password protect</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Unlock PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove password</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PDF to Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract text</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Text to PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert text to PDF</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Compare PDFs</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Compare two PDFs</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Crop PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Crop page margins</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Set Metadata</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Set title, author</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">HTML to PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert HTML to PDF</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Redact PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Blackout areas</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Repair PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Fix corrupted PDF</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Sign PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Digital signature</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Verify Signature</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Verify PDF signatures</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Create Certificate</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Create test certificate</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Sticky Note</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Add annotation</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Highlight</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Highlight annotation</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Underline</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Underline annotation</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Strikethrough</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Strikethrough annotation</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Free Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Free text annotation</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Link</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">URL or page link</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Stamp</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Approved/Draft/Final</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">List Annotations</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">List all annotations</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Remove Annotations</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove annotations</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Flatten Annotations</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Flatten into content</div>
                    </a>
                </div>
            </div>

            <!-- Excel Tools -->
            <div id="excel-tools" class="tool-category hidden mt-8">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">Excel Tools</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Create Excel</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Create spreadsheet</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Read Excel</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Read spreadsheet data</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Merge Workbooks</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Combine workbooks</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Split Workbook</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Split by sheets</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Excel to CSV</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to CSV</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Excel to JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to JSON</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">CSV to Excel</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">CSV to XLSX</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">JSON to Excel</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">JSON to XLSX</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Clean Excel</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove blank rows</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Excel Info</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Get metadata</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Extract Sheets</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract specific sheets</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Reorder Sheets</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Reorder sheets</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Rename Sheets</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Rename worksheets</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Delete Sheets</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove sheets</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Copy Sheet</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Copy within workbook</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Find & Replace</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Find and replace</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Excel to HTML</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to HTML table</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Formulas</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Add Excel formulas</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Chart</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Create charts</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Pivot Summary</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Create pivot summary</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Validate Data</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Validate against rules</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Protect Workbook</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Password protect</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Compress Excel</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Reduce file size</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Conditional Formatting</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Add formatting rules</div>
                    </a>
                </div>
            </div>

            <!-- Word Tools -->
            <div id="word-tools" class="tool-category hidden mt-8">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">Word Tools</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Create Word</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Create document</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Merge Word</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Combine documents</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Split Word</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Split by sections</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Extract Sections</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract sections</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Word to Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to plain text</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Word to HTML</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to HTML</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Word to JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract structure</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Text to Word</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert text to DOCX</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Find & Replace</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Find and replace text</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Header/Footer</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Add header or footer</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Word Info</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Get metadata</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Compare Documents</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Compare two docs</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Clean Formatting</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove formatting</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Mail Merge</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Personalize documents</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Track Changes</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Accept/reject changes</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Watermark</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Add text watermark</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Manage Tables</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract/remove tables</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Manage Comments</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract/remove comments</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Page Numbers</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Number pages</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Protect Word</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Password protect</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Sign Document</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Digital signature</div>
                    </a>
                </div>
            </div>

            <!-- PowerPoint Tools -->
            <div id="powerpoint-tools" class="tool-category hidden mt-8">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">PowerPoint Tools</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Create PowerPoint</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Create presentation</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Merge PPT</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Combine presentations</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Split PPT</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Split by slides</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Extract Slides</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract specific slides</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Remove Slides</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Delete slides</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Reorder Slides</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Change slide order</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PPT to Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract text</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PPT to JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract structure</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PPT Info</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Get metadata</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Duplicate Slides</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Duplicate slides</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Slide</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Insert new slide</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Watermark</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Watermark all slides</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Extract Notes</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract speaker notes</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Set Transitions</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Set slide transitions</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Find & Replace</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Find and replace text</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Extract Images</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract all images</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PPT to Images</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert slides to images</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Animations</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Animate elements</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Protect PPT</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Password protect</div>
                    </a>
                </div>
            </div>

            <!-- Text Tools -->
            <div id="text-tools" class="tool-category hidden mt-8">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">Text Processing Tools</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Merge Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Combine text files</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Split Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Split by lines/pattern</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Find & Replace</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Regex find-replace</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Remove Duplicates</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove duplicate lines</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Sort Lines</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Sort alphabetically</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Convert Case</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Upper/lower/title case</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Line Numbers</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Number each line</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Compare Files</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Text file diff</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Clean Whitespace</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove extra spaces</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Reverse Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Reverse lines/chars</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Convert Encoding</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">UTF-8, ASCII, etc</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Line Endings</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">LF, CRLF, CR</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Wrap Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Wrap at width</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Extract Columns</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract delimited columns</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Filter Lines</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Filter by pattern</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Text Stats</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Word/line count</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Encrypt Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">AES encryption</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Calculate Checksum</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">MD5, SHA1, SHA256</div>
                    </a>
                </div>
            </div>

            <!-- JSON Tools -->
            <div id="json-tools" class="tool-category hidden mt-8">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">JSON Tools</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Format JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Beautify with indent</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Minify JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove whitespace</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Validate JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Check syntax</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Merge JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Combine JSON files</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Split JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Split large arrays</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Query JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Path expressions</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Sort Keys</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Sort alphabetically</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Flatten JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Flatten nested structure</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">JSON to CSV</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to CSV</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Remove Keys</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove specific keys</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">CSV to JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert CSV</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">XML to JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert XML</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">JSON to XML</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to XML</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Validate Schema</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Validate against schema</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">JSON Stats</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Get statistics</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Remove Duplicates</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Remove duplicate objects</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Transform JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Apply transformations</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">YAML to JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert YAML</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">JSON to YAML</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to YAML</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Encrypt JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">AES encryption</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Sign JSON</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Digital signature</div>
                    </a>
                </div>
            </div>

            <!-- Image Tools -->
            <div id="image-tools" class="tool-category hidden mt-8">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">Image Tools</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Text Watermark</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Text on image</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Add Image Watermark</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Image watermark</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Redact Text</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Redact using OCR</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Redact Regions</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Black out areas</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Auto Redact</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Email, phone, SSN</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Resize Image</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Change dimensions</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Convert Format</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">PNG, JPG, WebP</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Crop Image</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Crop to size</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Rotate Image</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Rotate by degrees</div>
                    </a>
                </div>
            </div>

            <!-- OCR Tools -->
            <div id="ocr-tools" class="tool-category hidden mt-8">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">OCR Tools</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">OCR PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract text from scanned PDF</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">OCR Image</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Extract text from image</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Batch OCR</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">OCR multiple files</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">OCR Languages</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">List available languages</div>
                    </a>
                </div>
            </div>

            <!-- Conversion Tools -->
            <div id="convert-tools" class="tool-category hidden mt-8">
                <h3 class="text-2xl font-bold text-neutral-900 mb-6">Format Conversion</h3>
                <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PDF to Word</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to DOCX</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PDF to Excel</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert to XLSX</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Word to PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">DOCX to PDF</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Excel to PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">XLSX to PDF</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PPT to PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">PowerPoint to PDF</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Image to PDF</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Images to PDF</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">PDF to Image</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">PDF to JPG/PNG</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">JSON to CSV</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">Convert data format</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">CSV to Excel</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">CSV to XLSX</div>
                    </a>
                    <a href="{{ route('home') }}" class="tool-card bg-white hover:bg-neutral-900 hover:text-white p-4 rounded-xl text-center transition-all group">
                        <div class="text-sm font-medium mb-1">Excel to CSV</div>
                        <div class="text-xs text-neutral-500 group-hover:text-neutral-300">XLSX to CSV</div>
                    </a>
                </div>
            </div>
        </div>
    </section>

    @push('scripts')
    <script>
        function showCategory(category) {
            // Hide all categories
            document.querySelectorAll('.tool-category').forEach(el => el.classList.add('hidden'));

            // Remove active state from all buttons
            document.querySelectorAll('.category-btn').forEach(btn => {
                btn.classList.remove('bg-neutral-900', 'text-white');
                btn.classList.add('bg-white', 'text-neutral-900');
            });

            // Show selected category
            const categoryEl = document.getElementById(category + '-tools');
            if (categoryEl) {
                categoryEl.classList.remove('hidden');
            }

            // Highlight active button
            const activeBtn = document.querySelector(`[data-category="${category}"]`);
            if (activeBtn) {
                activeBtn.classList.remove('bg-white', 'text-neutral-900');
                activeBtn.classList.add('bg-neutral-900', 'text-white');
            }
        }

        // Show PDF tools by default
        document.addEventListener('DOMContentLoaded', () => {
            showCategory('pdf');
        });
    </script>
    @endpush

    <!-- CTA Section -->
    <section class="bg-neutral-100 py-12 px-4">
        <div class="max-w-4xl mx-auto text-center">
            <h2 class="text-2xl font-bold text-neutral-900 mb-4">Ready to Process Your Documents?</h2>
            <p class="text-neutral-600 mb-6">
                Start using our AI-powered tools now. No sign-up required for basic features.
            </p>
            <a href="{{ route('home') }}" class="inline-flex items-center px-8 py-3 bg-neutral-900 hover:bg-neutral-800 text-white font-semibold rounded-xl shadow-sm transition-all hover:shadow-md">
                Get Started Free
            </a>
        </div>
    </section>
</x-main-layout>
