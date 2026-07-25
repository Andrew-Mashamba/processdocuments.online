<x-guest-layout>
    <x-slot name="title">Privacy Policy - Process Documents Online</x-slot>
    <x-slot name="description">Privacy Policy for Process Documents Online. Learn how we collect, use, and protect your data when using our document processing services.</x-slot>

    @push('seo')
    <!-- Open Graph -->
    <meta property="og:title" content="Privacy Policy - Process Documents Online">
    <meta property="og:description" content="Privacy Policy for Process Documents Online. Learn how we protect your data.">
    <meta property="og:type" content="article">
    <meta property="og:url" content="{{ url('/privacy-policy') }}">

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
                "name": "Privacy Policy"
            }
        ]
    }
    </script>

    <!-- Structured Data: WebPage -->
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "WebPage",
        "name": "Privacy Policy",
        "description": "Privacy Policy for Process Documents Online document processing platform.",
        "url": "{{ url('/privacy-policy') }}",
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
                        <span class="text-gray-900 font-medium">Privacy Policy</span>
                    </li>
                </ol>
            </nav>

            <article class="w-full sm:max-w-2xl p-6 bg-white shadow-md overflow-hidden sm:rounded-lg prose">
                {!! $policy !!}
            </article>
        </div>
    </div>
</x-guest-layout>
