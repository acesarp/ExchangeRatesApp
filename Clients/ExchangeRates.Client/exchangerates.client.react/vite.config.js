import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    base: '/exchangerates/',
    server: {
        port: 62866,
        proxy: {
            '/exchangerates/api': {
                target: 'https://localhost:7255',
                changeOrigin: true,
                secure: false,
                rewrite: path => path.replace(/^\/exchangerates/, '')
            },
            '/exchangerates/hubs': {
                target: 'https://localhost:7255',
                changeOrigin: true,
                secure: false,
                ws: true,
                rewrite: path => path.replace(/^\/exchangerates/, '')
            }
        }
    }
})