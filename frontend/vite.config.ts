import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  // Trỏ thẳng HTTPS: cổng HTTP của API sẽ redirect sang HTTPS khác origin.
  const apiTarget = env.API_PROXY_TARGET || "https://localhost:7000";

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        "/api": {
          target: apiTarget,
          changeOrigin: true,
          secure: false, // chứng chỉ dev tự ký của ASP.NET Core
        },
      },
    },
  };
});
