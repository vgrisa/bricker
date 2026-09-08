import { HubConnectionBuilder } from "@microsoft/signalr";
import { useCallback, useEffect, useState } from "react";
import { api, hubUrl, type Profile } from "./api";

export function useUnreadCount(profile: Profile | null) {
  const [count, setCount] = useState(0);
  const refresh = useCallback(() => {
    if (!profile) return;
    void api<{ count: number }>("/conversations/unread-count")
      .then((result) => setCount(result.count))
      .catch(() => setCount(0));
  }, [profile]);

  useEffect(() => {
    if (!profile) return;
    refresh();
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: true })
      .withAutomaticReconnect()
      .build();
    connection.on("ConversationUpdated", refresh);
    const handleRefresh = () => refresh();
    window.addEventListener("bricker:unread-changed", handleRefresh);
    void connection.start().catch(() => undefined);
    return () => {
      window.removeEventListener("bricker:unread-changed", handleRefresh);
      void connection.stop();
    };
  }, [profile, refresh]);

  return profile ? count : 0;
}
