import './bootstrap';

// Load ZIMA Visual Code Editor in development v2.0
if (import.meta.env.DEV) {
    const script = document.createElement('script');
    // Use Vite dev server URL (port 5173) with cache busting
    script.src = `http://localhost:5173/node_modules/@zima/visual-code-editor/public/injector.bundle.js?v=${Date.now()}`;
    script.async = true;
    script.onload = () => {
        console.log('✓ Visual Editor injector loaded (v2.0 - Multi-select)');
    };
    script.onerror = (error) => {
        console.error('✗ Failed to load Visual Editor injector', error);
    };
    document.head.appendChild(script);
}
