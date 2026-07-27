import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const apiTarget = env.FACTOGRAPH_API_URL || "http://localhost:5000";

  return {
    base: "./",
    plugins: [react()],
    build: {
      outDir: "../Polar.Factograph.Api/wwwroot",
      emptyOutDir: true
    },
    server: {
      port: 5173,
      proxy: {
        "/api": {
          target: apiTarget,
          changeOrigin: true,
          secure: false
        }
      }
    }
  };
});
