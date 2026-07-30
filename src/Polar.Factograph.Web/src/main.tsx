import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./app/App";
import "./app/detailsDismiss";
import "./styles/base.css";
import "./styles/layout.css";
import "./styles/components.css";
import "./styles/ontology-class-search.css";
import "./styles/portrait.css";
import "./styles/semantic-resource.css";
import "./styles/semantic-block-controls.css";
import "./styles/semantic-sections-menu.css";
import "./styles/semantic-timeline.css";
import "./styles/collections.css";
import "./styles/document-actions.css";
import "./styles/document-intake.css";
import "./styles/resource-editor.css";
import "./styles/resource-properties.css";
import "./styles/admin-dialog.css";
import "./styles/admin-cards.css";
import "./styles/auth-access.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false
    }
  }
});

const root = document.getElementById("root");
if (root === null) {
  throw new Error("Application root element was not found.");
}

createRoot(root).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>
);
