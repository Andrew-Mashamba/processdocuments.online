<!-- Navigation Menu Component -->
<nav class="flex items-center gap-6">
    <a href="{{ route('home') }}"
       class="text-sm {{ request()->routeIs('home') ? 'text-neutral-900 font-semibold' : 'text-neutral-500 hover:text-neutral-900' }} transition-colors">
        Home
    </a>
    <a href="{{ route('tools') }}"
       class="text-sm {{ request()->routeIs('tools') ? 'text-neutral-900 font-semibold' : 'text-neutral-500 hover:text-neutral-900' }} transition-colors">
        Tools
    </a>
    <a href="{{ route('how-it-works') }}"
       class="text-sm {{ request()->routeIs('how-it-works') ? 'text-neutral-900 font-semibold' : 'text-neutral-500 hover:text-neutral-900' }} transition-colors">
        How It Works
    </a>
    <a href="{{ route('faq') }}"
       class="text-sm {{ request()->routeIs('faq') ? 'text-neutral-900 font-semibold' : 'text-neutral-500 hover:text-neutral-900' }} transition-colors">
        FAQ
    </a>
    <a href="{{ route('about') }}"
       class="text-sm {{ request()->routeIs('about') ? 'text-neutral-900 font-semibold' : 'text-neutral-500 hover:text-neutral-900' }} transition-colors">
        About
    </a>
</nav>
