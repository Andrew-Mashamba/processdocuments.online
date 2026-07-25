import { defineConfig } from 'vite';
import laravel from 'laravel-vite-plugin';
import tailwindcss from '@tailwindcss/vite';
import { createRequire } from 'module';

const require = createRequire(import.meta.url);
const visualEditor = require('@zima/visual-code-editor/dist/plugins/vite-plugin').default;

export default defineConfig({
    plugins: [
        laravel({
            input: ['resources/css/app.css', 'resources/js/app.js'],
            refresh: true,
        }),
        tailwindcss(),
        visualEditor({
            enabled: process.env.NODE_ENV === 'development',
            port: 9876,
        }),
    ],
    server: {
        watch: {
            ignored: ['**/storage/framework/views/**'],
        },
    },
});
