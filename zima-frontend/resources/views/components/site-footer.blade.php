<!-- Shared Site Footer -->
<footer class="bg-neutral-800 py-12 px-4 mt-auto">
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
                    <li><a href="{{ route('tools') }}#pdf" class="text-neutral-400 hover:text-white transition-colors">PDF Processing</a></li>
                    <li><a href="{{ route('tools') }}#excel" class="text-neutral-400 hover:text-white transition-colors">Excel Generator</a></li>
                    <li><a href="{{ route('tools') }}#word" class="text-neutral-400 hover:text-white transition-colors">Word Creator</a></li>
                    <li><a href="{{ route('tools') }}#powerpoint" class="text-neutral-400 hover:text-white transition-colors">PowerPoint Maker</a></li>
                    <li><a href="{{ route('tools') }}#ocr" class="text-neutral-400 hover:text-white transition-colors">OCR Online</a></li>
                    <li><a href="{{ route('tools') }}#convert" class="text-neutral-400 hover:text-white transition-colors">Format Converter</a></li>
                </ul>
            </div>

            <!-- Popular Conversions Column -->
            <div>
                <h4 class="text-white font-semibold mb-4">Popular Conversions</h4>
                <ul class="space-y-2 text-sm">
                    <li><a href="{{ route('tools') }}#pdf-to-word" class="text-neutral-400 hover:text-white transition-colors">PDF to Word</a></li>
                    <li><a href="{{ route('tools') }}#pdf-to-excel" class="text-neutral-400 hover:text-white transition-colors">PDF to Excel</a></li>
                    <li><a href="{{ route('tools') }}#word-to-pdf" class="text-neutral-400 hover:text-white transition-colors">Word to PDF</a></li>
                    <li><a href="{{ route('tools') }}#excel-to-csv" class="text-neutral-400 hover:text-white transition-colors">Excel to CSV</a></li>
                    <li><a href="{{ route('tools') }}#json-to-csv" class="text-neutral-400 hover:text-white transition-colors">JSON to CSV</a></li>
                    <li><a href="{{ route('tools') }}#merge-pdf" class="text-neutral-400 hover:text-white transition-colors">Merge PDF</a></li>
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
                        <li><a href="{{ route('faq') }}" class="text-neutral-400 hover:text-white transition-colors">FAQ</a></li>
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
                    <a href="{{ route('faq') }}" class="text-neutral-400 hover:text-white transition-colors">FAQ</a>
                    <a href="{{ route('tools') }}" class="text-neutral-400 hover:text-white transition-colors">All Tools</a>
                </div>
            </div>
        </div>
    </div>
</footer>
