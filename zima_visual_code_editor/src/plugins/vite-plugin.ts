import * as fs from 'fs';
import * as path from 'path';
import type { Plugin } from 'vite';

export interface VisualEditorPluginOptions {
  enabled?: boolean;
  port?: number;
}

export default function visualEditorPlugin(options: VisualEditorPluginOptions = {}): Plugin {
  const enabled = options.enabled !== false;
  const port = options.port || 9876;

  return {
    name: 'vite-plugin-visual-editor',
    enforce: 'pre',

    transformIndexHtml(html) {
      if (!enabled) return html;

      // Inject the visual editor script
      const scriptTag = `
        <script type="module">
          // Visual Editor Injector
          (function() {
            const script = document.createElement('script');
            script.src = '/node_modules/@zima/visual-code-editor/public/injector.bundle.js';
            script.async = true;
            document.head.appendChild(script);
          })();
        </script>
      `;

      // Insert before </head> or </body>
      if (html.includes('</head>')) {
        return html.replace('</head>', `${scriptTag}</head>`);
      } else if (html.includes('</body>')) {
        return html.replace('</body>', `${scriptTag}</body>`);
      }

      return html + scriptTag;
    },

    configureServer(server) {
      if (!enabled) return;

      server.httpServer?.once('listening', () => {
        console.log('');
        console.log('\x1b[36m%s\x1b[0m', '  ➜  Visual Editor: Press Cmd+Shift+D to activate inspector');
        console.log('');
      });
    },
  };
}
