<x-guest-layout>
    <x-slot name="title">Terms of Service - Process Documents Online</x-slot>
    <x-slot name="description">Terms of Service for Process Documents Online. Read our terms and conditions for using our document processing platform.</x-slot>

    @push('seo')
    <!-- Open Graph -->
    <meta property="og:title" content="Terms of Service - Process Documents Online">
    <meta property="og:description" content="Terms of Service for Process Documents Online document processing platform.">
    <meta property="og:type" content="article">
    <meta property="og:url" content="{{ url('/terms-of-service') }}">

    <!-- Structured Data: BreadcrumbList -->
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "BreadcrumbList",
        "itemListElement": [
            {
                "@type": "ListItem",
                "position": 1,
                "name": "Home",
                "item": "{{ url('/') }}"
            },
            {
                "@type": "ListItem",
                "position": 2,
                "name": "Terms of Service"
            }
        ]
    }
    </script>

    <!-- Structured Data: WebPage -->
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "WebPage",
        "name": "Terms of Service",
        "description": "Terms of Service for Process Documents Online document processing platform.",
        "url": "{{ url('/terms-of-service') }}",
        "datePublished": "2026-01-01",
        "dateModified": "2026-01-26",
        "publisher": {
            "@type": "Organization",
            "name": "Process Documents Online",
            "url": "{{ url('/') }}"
        },
        "inLanguage": "en-US"
    }
    </script>
    @endpush

    <div class="pt-4 bg-gray-100">
        <div class="min-h-screen flex flex-col items-center pt-6 sm:pt-0">
            <!-- Breadcrumb Navigation -->
            <nav class="w-full sm:max-w-2xl mb-4" aria-label="Breadcrumb">
                <ol class="flex items-center space-x-2 text-sm text-gray-600">
                    <li>
                        <a href="/" class="hover:text-gray-900 transition-colors">Home</a>
                    </li>
                    <li>
                        <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                        </svg>
                    </li>
                    <li>
                        <span class="text-gray-900 font-medium">Terms of Service</span>
                    </li>
                </ol>
            </nav>

            <article class="w-full sm:max-w-2xl p-6 bg-white shadow-md overflow-hidden sm:rounded-lg prose">
                {!! $terms !!}
            </article>
        </div>
    </div>
</x-guest-layout>
