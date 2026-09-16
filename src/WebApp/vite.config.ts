import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    // Keep the approved UI-POS-001 product sprite inside the generated JS bundle.
    // The hosted runtime has repeatedly rendered the externally-emitted WebP as
    // blank even though Vite produced it correctly. Inlining removes the
    // separate asset request while preserving the exact approved sprite bytes.
    assetsInlineLimit: 20_000,
  },
});
