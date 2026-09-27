import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// 배포는 nginx 가 /api 를 백엔드로 넘긴다.
// 개발도 같은 /api 경로를 쓰고 여기서만 5042 로 프록시한다.
// 그래야 프론트 코드가 환경에 따라 백엔드 주소를 들고 다닐 필요가 없다.
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    // 이 포트를 바꾸면 백엔드 CORS 허용 목록도 같이 바꿔야 한다.
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5042',
        changeOrigin: true,
      },
    },
  },
})
