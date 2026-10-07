import { defineConfig } from 'vite';

export default defineConfig({
    build: {
        target: 'es2022',
        outDir: 'wwwroot/js/gradient',
        emptyOutDir: true,
        rollupOptions: {
            input: 'ClientApp/login-gradient.js',
            output: {
                entryFileNames: 'login-gradient.js'
            }
        }
    }
});
