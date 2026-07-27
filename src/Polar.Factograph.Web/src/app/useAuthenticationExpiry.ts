import { useEffect, useRef } from "react";
import type { AuthenticationSession } from "../api/authModels";

const maximumTimerDelay = 2_147_000_000;

export function useAuthenticationExpiry(
  session: AuthenticationSession | null,
  onExpired: () => void
): void {
  const callback = useRef(onExpired);
  callback.current = onExpired;

  useEffect(() => {
    if (session?.source !== "oidc" || session.expiresAt === null) return;
    const expiresAt = session.expiresAt;
    let timeout = 0;

    function schedule(): void {
      const remaining = expiresAt - Date.now();
      if (remaining <= 0) {
        callback.current();
        return;
      }
      timeout = window.setTimeout(schedule, Math.min(remaining, maximumTimerDelay));
    }

    schedule();
    return () => window.clearTimeout(timeout);
  }, [session]);
}
