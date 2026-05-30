import { useEffect } from "react";
import { message as antdMessage } from "antd";
import { createSignalRClient } from "../services/signalrClient";
import { API_BASE_URL } from "../../../config";

const hubUrl = "/teamNotificationHub";
const fullHubUrl = `${API_BASE_URL.replace(/\/+$/, "")}${hubUrl}`;

type TeamJoinedNotification = {
  projectId: number;
  projectName: string;
  memberName: string;
  assignedAs: string;
  message: string;
};

const teamNotificationClient = createSignalRClient<{
  WorkerJoinedProject: [TeamJoinedNotification];
}>({
  hubUrl: fullHubUrl,
  getAccessToken: () => {
    const token = localStorage.getItem("token");
    if (!token) return null;
    return token.startsWith('"') && token.endsWith('"') ? token.slice(1, -1) : token;
  },
});

export function useTeamNotifications() {
  useEffect(() => {
    let unsubscribe: (() => void) | undefined;
    let cancelled = false;

    const start = async () => {
      const token = localStorage.getItem("token");
      if (!token) return;

      try {
        await teamNotificationClient.start();

        unsubscribe = teamNotificationClient.on("WorkerJoinedProject", (payload) => {
          console.log("WorkerJoinedProject payload:", payload);
          antdMessage.success(payload.message || `${payload.memberName} joined ${payload.projectName}.`);
        });
      } catch (err) {
        if (!cancelled) {
          console.error("Team notification hub failed:", err);
        }
      }
    };

    void start();

    return () => {
      cancelled = true;
      unsubscribe?.();
    };
  }, []);
}